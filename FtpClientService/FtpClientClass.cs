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
using log4net.Repository.Hierarchy;
using log4net;

namespace FtpClientService
{
    public class FtpClientClass
    {
        private readonly string ftpServerHost = "localhost";
        private readonly string ftpClientUser = "user1";
        private readonly string ftpClientPassword = "123456";
        private IEnumerable<FtpListItem> recentFilesInfoFromServer;
        private readonly ILog logger;

        private string localDataPath = @"D:\Data\";
        private string fileSave = @"recentFilesInfoFromServer.xml";

        FtpClient client;
        public FtpClientClass()
        {
            logger = LogManager.GetLogger(typeof(FtpClientClass));

            client = new FtpClient(this.ftpServerHost, this.ftpClientUser, this.ftpClientPassword);
            client.EncryptionMode = FtpEncryptionMode.Explicit;
            client.SslProtocols = SslProtocols.Tls12;
            client.ValidateCertificate += new FtpSslValidation((control, e) => e.Accept = true);
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
                logger.Info("No File To Download");
                return null;
            }

            var fileToParseXlsx = new List<string>();
            fileToDownloads.Where(f => f.isModified == false).ToList()
                            .ForEach(f => fileToParseXlsx.Add(f.name));

            var previousHash = new List<(string name, uint hash)>();
            fileToDownloads.Where(f => f.isModified).ToList()
                            .ForEach(f => previousHash.Add((f.name, this.ComputeHashFile(f.name))));


            logger.Info("Begin Download Files:");
            logger.Info(string.Join('\t', fileToDownloads.Select(f => f.name)));

            var client = this.GetClient();
            var result = client.DownloadFiles(this.localDataPath,
                                fileToDownloads.Select(f => f.path),
                                FtpLocalExists.Overwrite,
                                FtpVerify.OnlyChecksum);

            logger.Info($"Finish Download Files - newfileDownloaded={result}");

            previousHash.Where(f => f.hash != this.ComputeHashFile(f.name)).ToList()
                        .ForEach(f => fileToParseXlsx.Add(f.name));


            return fileToParseXlsx;

        }

        public IEnumerable<FtpListItem> GetFilesInfoFromServer()
        {
            logger.Info("Request to check file");

            var client = this.GetClient();
            var list = client.GetListing();

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
                    StreamReader file = new StreamReader(this.fileSave);
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

            FileStream file = File.Create(this.fileSave);

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
                logger.Info("No File To Parse");
                return;
            }
            logger.Info("Start Parse Xlsx files");
            logger.Info("List File To Parse:");
            logger.Info(String.Join('\t', fileNames));

            logger.Info("Finish Parse Xlsx files");
        }

        public uint ComputeHashFile(string fileName)
        {
            var fileContent = File.ReadAllBytes(this.localDataPath + fileName);
            var hash = Crc32CAlgorithm.Compute(fileContent);
            return hash;
        }
    }
}
