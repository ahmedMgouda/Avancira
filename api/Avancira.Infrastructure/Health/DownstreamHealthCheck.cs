using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Health;

/// <summary>
/// Health check that probes downstream HTTP endpoints and aggregates the status.
/// </summary>
public sealed class DownstreamHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DownstreamHealthCheckOptions _options;
    private readonly ILogger<DownstreamHealthCheck> _logger;

    public DownstreamHealthCheck(
        IHttpClientFactory httpClientFactory,
        DownstreamHealthCheckOptions options,
        ILogger<DownstreamHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_options.Endpoints.Count == 0)
        {
            return HealthCheckResult.Healthy("No downstream endpoints configured.");
        }

        var probes = new List<EndpointResult>(_options.Endpoints.Count);

        foreach (var endpoint in _options.Endpoints)
        {
            var result = await ProbeEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false);
            probes.Add(result);
        }

        var overallStatus = DetermineOverallStatus(probes);

        var data = probes.ToDictionary(
            probe => probe.Name,
            probe => (object)new
            {
                status = probe.Status.ToString(),
                url = probe.Url?.ToString(),
                duration = probe.Duration.TotalMilliseconds,
                error = probe.Error
            });

        var unhealthyCount = probes.Count(result => result.Status != HealthStatus.Healthy);
        var description = unhealthyCount == 0
            ? "All downstream services are healthy."
            : $"{unhealthyCount} of {probes.Count} downstream service checks failed.";

        return new HealthCheckResult(overallStatus, description, data: data);
    }

    private async Task<EndpointResult> ProbeEndpointAsync(
        DownstreamEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        if (endpoint.Url is null)
        {
            return EndpointResult.Unhealthy(endpoint.Name, null, TimeSpan.Zero, "Endpoint URL is not configured.");
        }

        var stopwatch = Stopwatch.StartNew();

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint.Url);

        try
        {
            var client = _httpClientFactory.CreateClient(_options.HttpClientName);

            using var timeoutCts = CreateTimeoutToken(cancellationToken, endpoint.Timeout);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts?.Token ?? cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return EndpointResult.Healthy(endpoint.Name, endpoint.Url, stopwatch.Elapsed);
            }

            var status = MapStatus(response.StatusCode);
            var message = $"Received {(int)response.StatusCode} ({response.StatusCode}).";

            return EndpointResult.FromStatus(endpoint.Name, endpoint.Url, status, stopwatch.Elapsed, message);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for {Endpoint} timed out", endpoint.Name);
            return EndpointResult.Unhealthy(endpoint.Name, endpoint.Url, stopwatch.Elapsed, "Request timed out.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for {Endpoint} failed", endpoint.Name);
            return EndpointResult.Unhealthy(endpoint.Name, endpoint.Url, stopwatch.Elapsed, ex.Message);
        }
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<EndpointResult> probes)
    {
        var overall = HealthStatus.Healthy;

        foreach (var probe in probes)
        {
            if (probe.Status == HealthStatus.Unhealthy)
            {
                return HealthStatus.Unhealthy;
            }

            if (probe.Status == HealthStatus.Degraded)
            {
                overall = HealthStatus.Degraded;
            }
        }

        return overall;
    }

    private static HealthStatus MapStatus(HttpStatusCode statusCode)
        => statusCode switch
        {
            HttpStatusCode.ServiceUnavailable => HealthStatus.Degraded,
            HttpStatusCode.TooManyRequests => HealthStatus.Degraded,
            _ => HealthStatus.Unhealthy
        };

    private static CancellationTokenSource? CreateTimeoutToken(CancellationToken cancellationToken, TimeSpan? timeout)
    {
        if (!timeout.HasValue || timeout.Value <= TimeSpan.Zero)
        {
            return null;
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout.Value);
        return linkedCts;
    }

    private sealed record EndpointResult(
        string Name,
        Uri? Url,
        HealthStatus Status,
        TimeSpan Duration,
        string? Error)
    {
        public static EndpointResult Healthy(string name, Uri url, TimeSpan duration)
            => new(name, url, HealthStatus.Healthy, duration, null);

        public static EndpointResult FromStatus(
            string name,
            Uri url,
            HealthStatus status,
            TimeSpan duration,
            string? error)
            => new(name, url, status, duration, error);

        public static EndpointResult Unhealthy(
            string name,
            Uri? url,
            TimeSpan duration,
            string? error)
            => new(name, url, HealthStatus.Unhealthy, duration, error);
    }
}

public sealed class DownstreamHealthCheckOptions
{
    public string HttpClientName { get; set; } = "downstream-health-check";

    public List<DownstreamEndpoint> Endpoints { get; set; } = new();
}

public sealed class DownstreamEndpoint
{
    public string Name { get; set; } = string.Empty;

    public Uri? Url { get; set; }

    public TimeSpan? Timeout { get; set; }

    public static DownstreamEndpoint Create(string name, string? baseUrl, string healthPath = "health")
    {
        Uri? healthUri = null;

        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            healthUri = new Uri(uri, healthPath);
        }

        return new DownstreamEndpoint
        {
            Name = name,
            Url = healthUri
        };
    }
}
