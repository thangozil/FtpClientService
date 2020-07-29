using System.Text;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using FluentFTP;
using System.Security.Authentication;
using Force.Crc32;
using System.Timers;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace Demo
{
    public class Test
    {
        private readonly string ftpServerHost = "123456test.hopto.org";
        private readonly int port = 990;
        private readonly string ftpClientUser = "user2";
        private readonly string ftpClientPassword = "123456";
        private IEnumerable<FtpListItem> recentFilesInfoFromServer;

        private string localDataPath = "./Data/";

        FtpClient client;
        public Test()
        {
            client = new FtpClient(this.ftpServerHost, 990, this.ftpClientUser, this.ftpClientPassword);
            client.EncryptionMode = FtpEncryptionMode.Explicit;
            client.SslProtocols = SslProtocols.Tls12;
            client.ValidateCertificate += new FtpSslValidation((control, e) => e.Accept = true);
            client.ConnectTimeout = 60 * 1000;
            client.DataConnectionConnectTimeout = 60 * 1000;
            client.DataConnectionReadTimeout = 60 * 1000;
            client.ReadTimeout = 60 * 1000;
            client.Connect();
        }

        public FtpClient GetClient()
        {
            return client;
        }
        public IEnumerable<string> DownloadFiles(IEnumerable<(string path, string name, bool isModified)> fileToDownloads)
        {
            if (fileToDownloads == null || fileToDownloads.Count() == 0)
            {
                System.Console.WriteLine("No File To Download");
                return null;
            }

            var fileToParseXlsx = new List<string>();
            fileToDownloads.Where(f => f.isModified == false).ToList()
                            .ForEach(f => fileToParseXlsx.Add(f.name));

            var previousHash = new List<(string name, uint hash)>();
            fileToDownloads.Where(f => f.isModified).ToList()
                            .ForEach(f => previousHash.Add((f.name, this.ComputeHashFile(f.name))));

            System.Console.WriteLine("Begin Download Files:");
            System.Console.WriteLine(string.Join('\t', fileToDownloads.Select(f => f.name)));

            var client = this.GetClient();
            var result = client.DownloadFiles(this.localDataPath,
                                fileToDownloads.Select(f => f.path),
                                FtpLocalExists.Overwrite,
                                FtpVerify.OnlyChecksum);

            System.Console.WriteLine($"Finish Download Files - newfileDownloaded={result}");

            previousHash.Where(f => f.hash != this.ComputeHashFile(f.name)).ToList()
                        .ForEach(f => fileToParseXlsx.Add(f.name));


            return fileToParseXlsx;

        }

        public IEnumerable<FtpListItem> GetFilesInfoFromServer()
        {
            System.Console.WriteLine("Get List File From Server:");

            var client = this.GetClient();
            var list = client.GetListing();

            //System.Console.WriteLine(String.Join<FtpListItem>('\n', list));

            this.SaveFilesInfoFromServer(list);
            this.recentFilesInfoFromServer = list;
            return list;
        }

        public IEnumerable<FileSystemInfo> GetFilesInfoFromLocal()
        {
            return new DirectoryInfo(this.localDataPath).EnumerateFileSystemInfos().ToList();
        }

        public IEnumerable<FtpListItem> GetFilesInfoFromServerPrev()
        {
            if (this.recentFilesInfoFromServer != null)
            {
                return this.recentFilesInfoFromServer;
            }
            else
            {
                try
                {
                    XmlSerializer reader = new XmlSerializer(typeof(List<FtpListItem>));
                    StreamReader file = new StreamReader("./recentFilesInfoFromServer.xml");
                    var data = (IEnumerable<FtpListItem>)reader.Deserialize(file);
                    file.Close();
                    return data;
                }
                catch (System.Exception)
                {
                    return new List<FtpListItem>();
                }
            }
        }

        public void SaveFilesInfoFromServer(FtpListItem[] list)
        {
            XmlSerializer writer = new XmlSerializer(list.GetType());

            FileStream file = File.Create("./recentFilesInfoFromServer.xml");

            writer.Serialize(file, this.recentFilesInfoFromServer);
            file.Close();
        }

        public IEnumerable<(string path, string name, bool isModified)> GetFileToDownload()
        {
            var recentFilesInfoFromServer = this.GetFilesInfoFromServerPrev();
            var newestFileInfoFromServer = this.GetFilesInfoFromServer();
            var localFilesInfo = this.GetFilesInfoFromLocal();

            var recentFilesDictionary = new Dictionary<string, FtpListItem>();
            foreach (var fileInfo in recentFilesInfoFromServer)
            {
                recentFilesDictionary.Add(fileInfo.Name, fileInfo);
            }
            var newestFilesDictionary = new Dictionary<string, FtpListItem>();
            foreach (var fileInfo in newestFileInfoFromServer)
            {
                newestFilesDictionary.Add(fileInfo.Name, fileInfo);
            }
            var localFilesDictionary = new Dictionary<string, FileSystemInfo>();
            foreach (var fileInfo in localFilesInfo)
            {
                localFilesDictionary.Add(fileInfo.Name, fileInfo);
            }


            var fileToDownloads = new List<(string path, string name, bool isModified)>();
            foreach (var fileInfo in newestFileInfoFromServer)
            {
                if (!localFilesDictionary.ContainsKey(fileInfo.Name))
                {
                    fileToDownloads.Add((fileInfo.FullName, fileInfo.Name, false));
                }
                else if ((!recentFilesDictionary.ContainsKey(fileInfo.Name)) ||
                         (fileInfo.Modified != recentFilesDictionary[fileInfo.Name].Modified))
                {
                    fileToDownloads.Add((fileInfo.FullName, fileInfo.Name, true));
                }
            }

            return fileToDownloads;
        }

        public void ParseXlsx(IEnumerable<string> fileNames)
        {
            if (fileNames == null || fileNames.Count() == 0)
            {
                System.Console.WriteLine("No File To Parse");
                return;
            }
            System.Console.WriteLine("Start Parse Xlsx files");
            System.Console.WriteLine("List File To Parse:");
            System.Console.WriteLine(String.Join('\t', fileNames));

            System.Console.WriteLine("Finish Parse Xlsx files");
        }

        public uint ComputeHashFile(string fileName)
        {
            var fileContent = File.ReadAllBytes(this.localDataPath + fileName);
            var hash = Crc32CAlgorithm.Compute(fileContent);
            return hash;
        }
    }
}