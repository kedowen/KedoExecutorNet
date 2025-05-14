using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.Email;
using Kedo.Comm.WechatWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class WechatWorkServiceExtensions
    {
        public static void AddWechatWorkService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(WechatWorkService));
        }

    }
}
