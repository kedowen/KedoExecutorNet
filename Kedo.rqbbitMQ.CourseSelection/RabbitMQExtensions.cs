using Microsoft.Extensions.DependencyInjection;

using Kedo.Comm;

namespace Kedo.rqbbitMQ.BIData;

public static class RabbitMQExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services)
    {
        services.AddSingleton(typeof(RabbitMQHelper));
        return services;
    }
}
