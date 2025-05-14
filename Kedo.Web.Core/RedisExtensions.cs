using Furion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Kedo.Web.Core;

public static class RedisExtensions
{
    public static void AddRedis(this IServiceCollection services)
    {
        var config = App.Configuration.GetValue<string>("Redis:Configuration");
        var instanceName = App.Configuration.GetValue<string>("Redis:InstanceName");


        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = config;
            options.InstanceName = instanceName;
        });
    }
}
