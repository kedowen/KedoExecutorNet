using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm;
using Kedo.Comm.EmailMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class EmailMessageHelperExtensions
    {
        public static void AddEmailMessageHelper(this IServiceCollection services)
        {
            services.AddSingleton(typeof(EmailMessageHelper));
        }
    }
}
