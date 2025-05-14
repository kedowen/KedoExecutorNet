using Serilog;
using Serilog.Events;

using System.Text;

var builder = WebApplication.CreateBuilder(args).Inject();

builder.Host.UseSerilogDefault(config =>//Ĭ�ϼ����� ����̨ �� �ļ� ��ʽ�������Զ���д�룬������Ҫд��Ľ��ʼ��ɣ�
{
    string date = DateTime.Now.ToString("yyyy-MM-dd");//��ʱ�䴴���ļ���
    string outputTemplate = "{NewLine}��{Level:u3}��{Timestamp:yyyy-MM-dd HH:mm:ss.fff}" +
    "{NewLine}#Msg#{Message:lj}" +
    "{NewLine}#Pro #{Properties:j}" +
    "{NewLine}#Exc#{Exception}" +
    new string('-', 50);//���ģ��

    ///1.�������restrictedToMinimumLevel��LogEventLevel����
    config
        //.MinimumLevel.Debug() // ����Sink����С��¼����
        //.MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
        //.Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: outputTemplate)
        .WriteTo.File($"_log/{date}/application.log",
               outputTemplate: outputTemplate,
                restrictedToMinimumLevel: LogEventLevel.Information,
                //��־���ձ��棬���������ļ����ƺ��Զ��������ں�׺
                //rollOnFileSizeLimit: true,          // ���Ƶ����ļ�����󳤶�
                //retainedFileCountLimit: 10,         // ��󱣴��ļ���,����nullʱ��Զ�����ļ���
                //fileSizeLimitBytes: 10 * 1024,      // ��󵥸��ļ���С
                //�ļ��ַ�����
                rollingInterval: RollingInterval.Day,
                encoding: Encoding.UTF8
            )

    #region 2.��LogEventLevel.�����������/���ļ�

        ///2.1����� LogEventLevel.Debug ����
        .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Debug)//ɸѡ����
            .WriteTo.File($"_log/{date}/{LogEventLevel.Debug}.log",
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,//��־���ձ��棬���������ļ����ƺ��Զ��������ں�׺
                encoding: Encoding.UTF8            // �ļ��ַ�����
             )
        )

        ///2.2����� LogEventLevel.Error ����
        .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error)//ɸѡ����
            .WriteTo.File($"_log/{date}/{LogEventLevel.Error}.log",
                outputTemplate: outputTemplate,
                rollingInterval: RollingInterval.Day,//��־���ձ��棬���������ļ����ƺ��Զ��������ں�׺
                encoding: Encoding.UTF8            // �ļ��ַ�����
             )
        );
    #endregion ��LogEventLevel ��������/���ļ�
});
var app = builder.Build();
app.Run();