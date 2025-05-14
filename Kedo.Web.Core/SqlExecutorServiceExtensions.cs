using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.SqlExecutor;
using Kedo.Comm.WechatWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class SqlExecutorServiceExtensions
    {
        public static void AddSqlExecutorService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(SqlExecutorService));
        }
    }
}
