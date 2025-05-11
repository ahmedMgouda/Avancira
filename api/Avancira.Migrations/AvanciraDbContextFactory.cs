using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avancira.Application.Persistence;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;

namespace Avancira.Migrations
{
    public class AvanciraDbContextFactory : IDesignTimeDbContextFactory<AvanciraDbContext>
    {
        public AvanciraDbContext CreateDbContext(string[] args)
        {
            var host = Environment.GetEnvironmentVariable("Avancira__Database__Host") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("Avancira__Database__Port") ?? "5432";
            var database = Environment.GetEnvironmentVariable("Avancira__Database__Name") ?? "AvanciraDb";
            var username = Environment.GetEnvironmentVariable("Avancira__Database__User") ?? "postgres";
            var password = Environment.GetEnvironmentVariable("Avancira__Database__Password") ?? "password";

            var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

            var optionsBuilder = new DbContextOptionsBuilder<AvanciraDbContext>();
            optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly("Avancira.Migrations"));

            var options = Options.Create(new DatabaseOptions
            {
                ConnectionString = connectionString,
                Provider = DbProviders.PostgreSQL
            });

            var publisher = new Mock<IPublisher>().Object;

            return new AvanciraDbContext(optionsBuilder.Options, publisher, options);
        }
    }
}
