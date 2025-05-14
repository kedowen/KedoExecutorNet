using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.CodeExecutor;
using Kedo.Comm.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class CodeExecutorServiceExtensions
    {
        public static void AddCodeExecutorService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(CodeExecutorService));
        }
    }
}
