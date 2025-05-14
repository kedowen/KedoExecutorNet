using Furion;
using Furion.DatabaseAccessor;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Kedo.EntityFramework.Core;

namespace Kedo.rqbbitMQ.BIData;

public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDatabaseAccessor(options =>
        {
            options.AddDb<ConsoleDbContext>(DbProvider.MySql);
            options.AddDb<MultiTenantDbContext, MultiTenantDbContextLocator>(DbProvider.MySql);
        }, "Kedo.Database.Migrations");

        services.AddRabbitMQ();

        services.AddHostedService<Service>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseInject(string.Empty);

      
    }
}
