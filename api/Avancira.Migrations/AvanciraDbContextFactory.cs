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
            var connectionString = Environment.GetEnvironmentVariable("Avancira__Database__ConnectionString")
                ?? "Host=localhost;Port=5432;Database=AvanciraDb;Username=postgres;Password=password";

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
