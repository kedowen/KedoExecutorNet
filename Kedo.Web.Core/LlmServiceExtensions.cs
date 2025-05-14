using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.Email;
using Kedo.Comm.LLm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class LlmServiceExtensions
    {
        public static void AddLlmService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(LlmService));
        }
    }
}
