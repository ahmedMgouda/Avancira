var builder = DistributedApplication.CreateBuilder(args);


var username = builder.AddParameter("pg-username", "admin");
var password = builder.AddParameter("pg-password", "admin");

var database = builder.AddPostgres("db", username, password, port: 5432)
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("avancira");

builder.AddProject<Projects.Avancira_API>("webapi")
    .WaitFor(database)
    .WithReference(database);

builder.Build().Run();
