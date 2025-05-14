using Furion;
using Furion.DatabaseAccessor;

using Microsoft.Extensions.DependencyInjection;

namespace Kedo.EntityFramework.Core;

public sealed class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDatabaseAccessor(options =>{ options.AddDb<FurionDbContext>(DbProvider.MySql);});
    }
}
