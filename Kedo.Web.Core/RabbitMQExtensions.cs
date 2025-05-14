using Microsoft.Extensions.DependencyInjection;

using Kedo.Comm;

namespace Kedo.Web.Core;

public static class RabbitMQExtensions
{
    public static void AddRabbitMQ(this IServiceCollection services)
    {
        services.AddSingleton(typeof(RabbitMQHelper));
    }
}
