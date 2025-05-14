using Furion;
using Furion.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System;
using Aspose.Words;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using Kedo.Comm.CodeExecutor;
using Kedo.Comm.Email;
using Kedo.Comm.ImageGeneration;
using Kedo.Comm.LLm;
using Kedo.Comm.SqlExecutor;
using Kedo.Comm.WechatWork;



namespace Kedo.Web.Core;

public class Startup : AppStartup
{


    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
       // services.AddJwt<JwtHandler>(enableGlobalAuthorize: true);
        services.AddCorsAccessor();
        services.AddControllers().AddInjectWithUnifyResult();
        services.AddRemoteRequest();
        services.AddRabbitMQ();
        services.AddRedis();
        services.AddMongodbHelper();
        services.AddWordHelper();
        services.AddShortMessageHelper();
        services.AddEmailMessageHelper();
        services.AddEmailService();
        services.AddWechatWorkService();
        services.AddLlmService();
        services.AddSqlExecutorService();
        services.AddCodeExecutorService();
        services.AddImageGenerationService();
      

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 1048576*1024; // 设置表单数据的最大长度为100MB
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCorsAccessor();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseInject(string.Empty);
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

  
}
