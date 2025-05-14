using Serilog;
using Serilog.Events;

using System.Diagnostics;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Kedo.rqbbitMQ.BIData;

public class MainService
{
    private string[] args;
    public MainService(string[] vs)
    {
        args = vs;
    }

    [Obsolete]
    public void Start()
    {
        var isService = !(Debugger.IsAttached || args.Contains("--console"));
        var builder = CreateHostBuilder(args.Where(arg => arg != "--console").ToArray());

        if (isService)
        {
            var pathToExe = Environment.ProcessPath;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);
            builder.UseContentRoot(pathToContentRoot);
        }

        builder.Build().Run();
    }

    public void Stop()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [Obsolete]
    public IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .Inject()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
            .UseSerilogDefault(config =>//默认集成了 控制台 和 文件 方式。如需自定义写入，则传入需要写入的介质即可：
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");//按时间创建文件夹
                string outputTemplate = "{NewLine}【{Level:u3}】{Timestamp:yyyy-MM-dd HH:mm:ss.fff}" +
                "{NewLine}#Msg#{Message:lj}" +
                "{NewLine}#Pro #{Properties:j}" +
                "{NewLine}#Exc#{Exception}" +
                new string('-', 50);//输出模板

                ///1.输出所有restrictedToMinimumLevel：LogEventLevel类型
                config
                    //.MinimumLevel.Debug() // 所有Sink的最小记录级别
                    //.MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
                    //.Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: outputTemplate)
                    .WriteTo.File($"_log/{date}/application.log",
                           outputTemplate: outputTemplate,
                            restrictedToMinimumLevel: LogEventLevel.Information,
                            //日志按日保存，这样会在文件名称后自动加上日期后缀
                            //rollOnFileSizeLimit: true,          // 限制单个文件的最大长度
                            //retainedFileCountLimit: 10,         // 最大保存文件数,等于null时永远保留文件。
                            //fileSizeLimitBytes: 10 * 1024,      // 最大单个文件大小
                            //文件字符编码
                            rollingInterval: RollingInterval.Day,
                            encoding: Encoding.UTF8
                        )

                #region 2.按LogEventLevel.输出独立发布/单文件

                ///2.1仅输出 LogEventLevel.Debug 类型
                .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Debug)//筛选过滤
                    .WriteTo.File($"_log/{date}/{LogEventLevel.Debug}.log",
                        outputTemplate: outputTemplate,
                        rollingInterval: RollingInterval.Day,//日志按日保存，这样会在文件名称后自动加上日期后缀
                        encoding: Encoding.UTF8            // 文件字符编码
                     )
                )

                ///2.2仅输出 LogEventLevel.Error 类型
                .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error)//筛选过滤
                    .WriteTo.File($"_log/{date}/{LogEventLevel.Error}.log",
                        outputTemplate: outputTemplate,
                        rollingInterval: RollingInterval.Day,//日志按日保存，这样会在文件名称后自动加上日期后缀
                        encoding: Encoding.UTF8            // 文件字符编码
                     )
                );
                #endregion 按LogEventLevel 独立发布/单文件
            })
            .UseStartup<Startup>();
        });

}

