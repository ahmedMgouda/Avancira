using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using Avancira.BFF.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Avancira.BFF.Services;

/// <summary>
/// Health check that verifies downstream API and Auth services are reachable.
/// </summary>
public sealed class DownstreamHealthCheck : IHealthCheck
{
    public const string HttpClientName = "downstream-health-check";
    public const string RegistrationName = "downstream_services";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BffSettings _settings;
    private readonly ILogger<DownstreamHealthCheck> _logger;

    public DownstreamHealthCheck(
        IHttpClientFactory httpClientFactory,
        BffSettings settings,
        ILogger<DownstreamHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoints = new[]
        {
            EndpointDefinition.For("api", _settings.ApiBaseUrl),
            EndpointDefinition.For("auth", _settings.Auth.Authority)
        };

        var probes = await Task.WhenAll(
            endpoints.Select(endpoint => ProbeEndpointAsync(endpoint, cancellationToken)));

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

        return new HealthCheckResult(
            overallStatus,
            description: "Aggregated downstream service health",
            data: data);
    }

    private async Task<EndpointProbeResult> ProbeEndpointAsync(
        EndpointDefinition endpoint,
        CancellationToken cancellationToken)
    {
        if (endpoint.Url is null)
        {
            _logger.LogWarning(
                "Skipping downstream health probe for {Endpoint} because no valid endpoint was configured.",
                endpoint.Name);

            return EndpointProbeResult.Unhealthy(
                endpoint.Name,
                null,
                TimeSpan.Zero,
                "Health endpoint is not configured.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint.Url);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return EndpointProbeResult.Healthy(endpoint.Name, endpoint.Url, stopwatch.Elapsed);
            }

            var message = $"Received {(int)response.StatusCode} ({response.StatusCode}).";
            var status = response.StatusCode switch
            {
                HttpStatusCode.ServiceUnavailable => HealthStatus.Degraded,
                HttpStatusCode.TooManyRequests => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };

            return EndpointProbeResult.FromStatus(
                endpoint.Name,
                endpoint.Url,
                status,
                stopwatch.Elapsed,
                message);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for {Endpoint} timed out", endpoint.Name);
            return EndpointProbeResult.Unhealthy(
                endpoint.Name,
                endpoint.Url,
                stopwatch.Elapsed,
                "Health request timed out.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check for {Endpoint} failed", endpoint.Name);
            return EndpointProbeResult.Unhealthy(
                endpoint.Name,
                endpoint.Url,
                stopwatch.Elapsed,
                ex.Message);
        }
    }

    private static Uri? BuildHealthUri(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return new Uri(uri, "health");
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<EndpointProbeResult> probes)
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

    private sealed record EndpointDefinition(string Name, Uri? Url)
    {
        public static EndpointDefinition For(string name, string? baseUrl)
            => new(name, BuildHealthUri(baseUrl));
    }

    private sealed record EndpointProbeResult(
        string Name,
        Uri? Url,
        HealthStatus Status,
        TimeSpan Duration,
        string? Error)
    {
        public static EndpointProbeResult Healthy(string name, Uri url, TimeSpan duration) =>
            new(name, url, HealthStatus.Healthy, duration, null);

        public static EndpointProbeResult FromStatus(
            string name,
            Uri url,
            HealthStatus status,
            TimeSpan duration,
            string? error) =>
            new(name, url, status, duration, error);

        public static EndpointProbeResult Unhealthy(
            string name,
            Uri? url,
            TimeSpan duration,
            string? error) =>
            new(name, url, HealthStatus.Unhealthy, duration, error);
    }
}
