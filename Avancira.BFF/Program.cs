
using Avancira.BFF.Extensions;
using Avancira.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===== STEP 1: Configure Infrastructure =====
// Sets up database, logging, and core services
builder.ConfigureAvanciraFramework();

// ===== STEP 2: Register BFF Services =====
// Authentication, token management, caching, CORS, reverse proxy
builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddBffTokenManagement(builder.Configuration);
builder.Services.AddBffAuthorization();
builder.Services.AddBffReverseProxy(builder.Configuration);

// ===== STEP 3: Add Standard Services =====
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===== STEP 4: Configure Middleware Pipeline =====
// Order matters! This is the correct order for security

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS must come early
app.UseCors();

// Session middleware (required for token management)
app.UseSession();

// Authentication and Authorization must be in this order
app.UseAuthentication();
app.UseAuthorization();

// ===== STEP 5: Map Endpoints =====
app.MapHealthChecks("/health");
app.MapControllers();

// Reverse proxy to backend API
// Must come last so other routes take precedence
app.MapReverseProxy();

// ===== STEP 6: Log Configuration =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("BFF starting in {Environment} environment", app.Environment.EnvironmentName);
logger.LogInformation("Auth Server Authority: {Authority}",
    builder.Configuration["Auth:Authority"]);
logger.LogInformation("Reverse Proxy Target: {Target}",
    builder.Configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]);

app.Run();

public partial class Program { }

// ============================================================================
// EXPLANATION - BFF Program.cs Configuration
// ============================================================================
// 
// The BFF acts as an intermediary between web clients and the backend API:
//
// 1. AUTHENTICATION LAYER
//    - Uses OpenID Connect to authenticate with auth server
//    - Receives ID token + access token from auth server
//    - Stores session in secure cookie (browser → BFF)
//    - BFF validates every request using cookie
//
// 2. TOKEN MANAGEMENT LAYER
//    - Caches access tokens in memory for performance
//    - Tracks sessions with device info and refresh token references
//    - Provides /refresh endpoint for clients to refresh tokens
//    - Handles token lifecycle and revocation
//
// 3. REVERSE PROXY LAYER
//    - Forwards API requests to backend with injected access token
//    - Backend trusts BFF to authenticate requests (no redirect needed)
//    - Prevents Set-Cookie from backend to avoid session confusion
//
// 4. SECURITY CONSIDERATIONS
//    - Cookies: Secure + HttpOnly + SameSite=Strict
//    - CORS: Configured for specific origins, allows credentials
//    - AutoRefresh: DISABLED - prevents race conditions and stale token caching
//    - Session timeout: Matches token lifetime + buffer
//
// 5. MIGRATION TO REDIS
//    - When you add Redis, only change:
//      * AddBffTokenManagement() to use IConnectionMultiplexer
//      * AddSession() to use Redis session provider
//    - Cache keys and logic remain unchanged
