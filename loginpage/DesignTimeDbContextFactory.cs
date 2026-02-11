using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using loginpage.Data;

namespace loginpage
{

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            var configuration = builder.Build();


            var conn = configuration.GetConnectionString("DefaultConnection")
                       ?? configuration.GetConnectionString("Connection")
                       ?? configuration["ConnectionStrings:Connection"]
                       ?? configuration["Connection"];

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Connection string not found. Add 'DefaultConnection' to appsettings.json or set the environment variable ConnectionStrings__DefaultConnection.");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(conn);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
