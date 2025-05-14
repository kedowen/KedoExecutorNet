using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm;
using Kedo.Comm.WordHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class WordHelperExtensions
    {
        public static void AddWordHelper(this IServiceCollection services)
        {
            services.AddSingleton(typeof(WordHelper));
        }
    }
}
