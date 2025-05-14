using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.Email;
using Kedo.Comm.EmailMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class EmailServiceExtensions
    {
        public static void AddEmailService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(EmailService));
        }
    }
}
