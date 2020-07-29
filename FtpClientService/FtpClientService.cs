using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClientService
{
    public class FtpClientService
    {
        private bool isStop = false;
        private readonly ILog logger;
        public FtpClientService()
        {
            logger = LogManager.GetLogger(typeof(FtpClientService));
        }

        public void Todo()
        {

            var test = new FtpClientClass();

            Task.Run(() =>
            {
                while (!isStop)
                {
                    var time1 = new DateTime();

                    var fileToDownloads = test.GetFileToDownload();
                    var fileToParse = test.DownloadFiles(fileToDownloads);
                    test.ParseXlsx(fileToParse);

                    var time2 = new DateTime();

                    var interval = time2 - time1;
                    if (interval.Milliseconds < 5 * 1000)
                    {
                        Thread.Sleep(5 * 1000 - interval.Milliseconds);
                    }
                    System.Console.WriteLine("----------------------------");
                }
            });
            
        }

        public void Start()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            logger.Info("Started Service");
            this.Todo();
        }

        public void Stop()
        {
            this.isStop = true;
            logger.Info("Stopped Service");
        }

        public void Pause()
        {
            this.isStop = true;
            logger.Info("Pause Service");
        }

        public void Continue()
        {
            this.isStop = false;
            this.Todo();
            logger.Info("Continue Service");
        }

        public void Shutdown()
        {
            this.isStop = true;
            logger.Info("Shutdown Service");
        }
    }
}
