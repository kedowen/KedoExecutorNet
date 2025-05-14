using Furion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kedo.Web.Core;

public static class MongodbExtensions
{
    public static void AddMongodbHelper(this IServiceCollection services)
    {
        var config = App.Configuration.GetValue<string>("Mongodb:Configuration");
        services.AddMongoDB(config);
    }
}
