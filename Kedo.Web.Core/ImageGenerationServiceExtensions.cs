using Microsoft.Extensions.DependencyInjection;
using Kedo.Comm.Email;
using Kedo.Comm.ImageGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kedo.Web.Core
{
    public static class ImageGenerationServiceExtensions
    {
        public static void AddImageGenerationService(this IServiceCollection services)
        {
            services.AddSingleton(typeof(ImageGenerationService));
        }
    }
}
