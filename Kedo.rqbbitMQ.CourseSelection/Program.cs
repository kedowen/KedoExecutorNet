using Topshelf;

using Kedo.rqbbitMQ.BIData;

var rc = HostFactory.Run(x =>
{
    x.Service<MainService>(s =>
    {
        s.ConstructUsing(name => new MainService(args));
        s.WhenStarted(tc => tc.Start());
        s.WhenStopped(tc => tc.Stop());
    });
    x.RunAsLocalSystem();

    x.SetDescription("rabbitMQ（Onionbit BIData）");
    x.SetDisplayName("rabbitMQ（Onionbit BIData）");
    x.SetServiceName("rabbitMQ_Onionbit_BIData");
});

var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
Environment.ExitCode = exitCode;
