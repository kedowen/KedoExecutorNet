using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class ShortMessageHelperExtensions
    {
        public static void AddShortMessageHelper(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ShortMessageHelper));
        }
    }
}
