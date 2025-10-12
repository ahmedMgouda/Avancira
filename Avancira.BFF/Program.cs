using Avancira.BFF.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddBffTokenManagement(builder.Configuration);
builder.Services.AddBffCors(builder.Configuration);
builder.Services.AddBffAuthorization();
builder.Services.AddBffReverseProxy(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapReverseProxy();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("BFF starting on {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Auth Server Authority: {Authority}", builder.Configuration["Auth:Authority"]);
logger.LogInformation("Proxy target: {Destination}", builder.Configuration["ReverseProxy:Clusters:api-cluster:Destinations:primary:Address"]);

app.Run();
