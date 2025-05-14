using Furion;
using Furion.DatabaseAccessor;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Kedo.rqbbitMQ.BIData;

public class ConsoleDbContext : AppDbContext<ConsoleDbContext>, IMultiTenantOnDatabase
{

    public ConsoleDbContext(DbContextOptions<ConsoleDbContext> options) : base(options)
    {
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
        var memoryCache = App.GetService<IMemoryCache>();
        var host = memoryCache.Get<string>("host");//var host = "localhost:44342";

        if (string.IsNullOrEmpty(host)) return "";

        var tenant = memoryCache.GetOrCreate($"{host}:MultiTenants", cache =>
        {
            // 读取数据库
            var tenantDbContext = Db.GetDbContext<MultiTenantDbContextLocator>();
            if (tenantDbContext == null) return default;

            return tenantDbContext.Set<Tenant>().FirstOrDefault(u => u.Host == host);
        });

        return tenant?.ConnectionString ?? "";
    }
}
