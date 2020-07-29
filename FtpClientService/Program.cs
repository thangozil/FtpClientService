using FluentFTP;
using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Threading;
using Topshelf;

namespace FtpClientService
{
    class Program
    {
        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            HostFactory.Run(hostConfig =>
            {
                hostConfig.StartManually();
                hostConfig.RunAsLocalService();
                hostConfig.UseAssemblyInfoForServiceInfo();

                hostConfig.UseLog4Net();

                hostConfig.Service<FtpClientService>(serviceConfig =>
                {
                    serviceConfig.ConstructUsing(() => new FtpClientService());
                    serviceConfig.WhenStarted(s => s.Start());
                    serviceConfig.WhenStopped(s => s.Stop());

                    serviceConfig.WhenPaused(s => s.Pause());
                    serviceConfig.WhenContinued(s => s.Continue());
                    serviceConfig.WhenShutdown(s => s.Shutdown());
                });
            });
        }
        


    }
    
    
}
