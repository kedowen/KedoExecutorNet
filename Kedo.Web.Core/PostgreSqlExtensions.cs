
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Kedo.Web.Core
{
    /// <summary>
    /// 知识库服务注册扩展
    /// </summary>
    public static class PostgreSqlExtensions
    {
        /// <summary>
        /// 注册知识库服务
        /// </summary>
        public static void AddKnowledgeBaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            //// 注册 PostgreSQL 向量数据库配置
            //services.AddPostgreSqlVector(configuration);

            //// 注册知识库相关服务
            //services.AddScoped<PostgreSqlVectorDbService>();
            //services.AddScoped<IKnowledgeBaseHandleService, KnowledgeBaseHandleService>();

            //return services;
           // services.AddDatabaseAccessor.(typeof(PostgreSqlHelper));
        }
    }
} 