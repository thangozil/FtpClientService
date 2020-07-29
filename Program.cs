using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Timers;
using FluentFTP;

namespace Demo
{
    class Program
    {
        // static void Main(string[] args)
        // {
        //     FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://demo.wftpserver.com:21/");
        //     request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

        //     request.Credentials = new NetworkCredential("demo", "demo");

        //     FtpWebResponse response = (FtpWebResponse)request.GetResponse();

        //     Stream responseStream = response.GetResponseStream();
        //     StreamReader reader = new StreamReader(responseStream);
        //     Console.WriteLine(reader.ReadToEnd());

        //     Console.WriteLine($"Directory List Complete, status {response.StatusDescription}");

        //     reader.Close();
        //     response.Close();
        // }


        // static void Main(string[] args)
        // {
        //     FtpClient client = new FtpClient("localhost", 21, "ftpuser", "123456");
        //     client.EncryptionMode = FtpEncryptionMode.Explicit;
        //     client.SslProtocols = SslProtocols.Tls12;
        //     client.ValidateCertificate += new FtpSslValidation((control, e) => e.Accept = true);
        //     client.Connect();

        //     var list = client.GetListing();
        //     foreach (var item in list)
        //     {
        //         System.Console.WriteLine(item);
        //     }

        // }
        // static void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        // {
        //     // add logic to test if certificate is valid here
        //     e.Accept = true;
        // }

        static void Main(string[] args)
        {
            var test = new Test();

            while (true)
            {
                System.Console.WriteLine("Start Request");
                var time1 = new DateTime();

                var fileToDownloads = test.GetFileToDownload();
                var fileToParse = test.DownloadFiles(fileToDownloads);
                test.ParseXlsx(fileToParse);

                var time2 = new DateTime();

                var interval = time2 - time1;
                if (interval.Milliseconds < 5 * 1000)
                {
                    Thread.Sleep(60 * 1000 - interval.Milliseconds);
                }
                System.Console.WriteLine("----------------------------");
            }

        }


    }
}
