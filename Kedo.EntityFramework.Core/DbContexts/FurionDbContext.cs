using Furion.DatabaseAccessor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kedo.EntityFramework.Core;

public class FurionDbContext : AppDbContext<FurionDbContext>, IMultiTenantOnDatabase
{
    private readonly ILogger<FurionDbContext> _logger;
    private readonly string _connectionString;
    public FurionDbContext(ILogger<FurionDbContext> logger, DbContextOptions<FurionDbContext> options, IConfiguration configuration) : base(options)
    {
        _connectionString = configuration["ConnectionStrings:onionbithostConnectionString"];
        _logger = logger;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var databaseConnectionString = GetDatabaseConnectionString();
        optionsBuilder.UseMySql(databaseConnectionString, ServerVersion.Parse("5.6.15-mysql"), options =>
        {
            options.MigrationsAssembly("Kedo.Database.Migrations");
        });
        base.OnConfiguring(optionsBuilder);
    }

    public string GetDatabaseConnectionString()
    {
        return _connectionString;
    }
}
