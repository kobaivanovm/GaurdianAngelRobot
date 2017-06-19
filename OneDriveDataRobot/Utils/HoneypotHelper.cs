using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace OneDriveDataRobot.Utils
{
    public class HoneypotHelper
    {
        private readonly OneDriveHelper _oneDriveHelper;
        public static readonly byte[] TextFromUrl;
        private static readonly List<string> FileExtensions;
        static HoneypotHelper()
        {
            var url = @"http://www.textfiles.com/stories/gulliver.txt";
            TextFromUrl = new WebClient().DownloadData(url);
            FileExtensions = new List<string> { ".DOC", ".DOCX", ".PPT"
            ,".TXT" ,".MP3" ,".WAV" ,".7Z" ,".ZIP" ,".C" ,".CS" ,".CPP" ,".JAVA" ,".PY"
            ,".SWIFT" ,".TORRENT" ,".ICS" ,".SQL" ,".XLS"
            ,".XLSX" ,".PDF" ,".JPG" ,".PNG" };
        }
        public HoneypotHelper(string accessToken)
        {
            _oneDriveHelper = new OneDriveHelper(accessToken);
        }

        internal static byte[] GetRandomByteArray(int size)
        {
            var random = new Random();
            var filebytes = new Byte[size];
            random.NextBytes(filebytes);
            return filebytes;
        }

        internal static string GetRandomFilename()
        {
            var random = new Random();
            var filename = "honeypot";
            var extension = FileExtensions[random.Next(0, FileExtensions.Count)];
            filename += extension.ToLower();
            return filename;
        }
        public async Task<List<string>> SpreadHoneypotsFromRootAsync(byte[] content)
        {
            var honeypotsId = new List<string>();
            var honeypotsCounter = 0;
            var rootId = await _oneDriveHelper.GetIDByPath(OneDriveHelper.RootPath);
            var q = new Queue<string>();
            q.Enqueue(rootId);
            while (q.Count > 0)
            {
                string currentFolderId = q.Dequeue();
                if (currentFolderId == null) continue;

                var listChildrenfolderId = await _oneDriveHelper.GetContainedFoldersIDs(currentFolderId);
                foreach (var id in listChildrenfolderId) q.Enqueue(id);

                var honeypotId = _oneDriveHelper.UploadFileToFolder(currentFolderId, GetRandomFilename(),
                    content);
                honeypotsId.Add(honeypotId);
                ++honeypotsCounter;
            }
            return honeypotsId;
        }


        public static async Task DeleteAllHoneypotsAsync(GraphServiceClient client)
        {
            var a = await client.Me.Drive.Search("honeypot").Request().GetAsync();
            foreach (var file in a.CurrentPage)
            {
                await client.Me.Drive.Items[file.Id].Request().DeleteAsync();
            }

        }




    }
}