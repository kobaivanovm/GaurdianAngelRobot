using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OneDriveDataRobot.Utils
{
    public class HoneypotHelper
    {
        private readonly string accessToken;
       
        public const string RootPath = @"https://graph.microsoft.com/v1.0/me/drive/root/";
        private const string DriveItemsPath = @"https://graph.microsoft.com/v1.0/me/drive/items/";
        private  static List<string> fileExtensions;
        private int counter { get; set; } //only for debugging
        static HoneypotHelper()
        {
            fileExtensions = new List<string> { ".DOC", ".DOCX", ".PPT"
            ,".TXT" ,".MP3" ,".WAV" ,".7Z" ,".ZIP" ,".C" ,".CS" ,".CPP" ,".JAVA" ,".PY" 
            ,".SWIFT" ,".TORRENT" ,".ICS" ,".SQL" ,".XLS"
            ,".XLSX" ,".PDF" ,".JPG" ,".PNG" };
        }
        public  HoneypotHelper(string _accessToken)
        {
            accessToken = _accessToken;
        }

        public async Task<string> GetDriveItemIdByPath(string path)
        {
            var data = await HttpHelper.Default.GetAsync<JObject>(path, accessToken);
            var id = data.SelectToken("id").Value<string>();
            Debug.WriteLine($"The ID of the driveitem: {path} is {id}");
            return id;
        }
        public  async Task<int> SpreadHoneypotsFromRootAsync()
        {
            var honeypotsCounter = 0;
            var rootID = await GetDriveItemIdByPath(RootPath);
            var q = new Queue<string>();
            q.Enqueue(rootID);
            while (q.Count > 0)
            {
                string currentFolderID = q.Dequeue();
                if (currentFolderID == null)
                    continue;
                var listChildrenfolderID = await GetListChildrenFolderID(currentFolderID);
                foreach(var id in listChildrenfolderID)
                {
                    q.Enqueue(id);
                }
                 UploadFileToOneDrive(currentFolderID, "honeypot");
                ++honeypotsCounter;
            }
            return honeypotsCounter;
        }
        public async Task<List<string>> GetListChildrenFolderID(string folderID)
        {
            var uri = DriveItemsPath + folderID + "/children";
            dynamic data = await HttpHelper.Default.GetAsync<dynamic>(uri, accessToken);
            var folderIDs = new List<string>();
            if (data.value != null)
            {
                foreach (var driveItem in data.value)
                {
                    if (driveItem.folder != null) {
                        string tmpid = Convert.ToString(driveItem.id);
                        folderIDs.Add(tmpid);
                        Debug.WriteLine(tmpid);
                    }
                }
            }

            return folderIDs;
        }

        internal async Task<string> getFileContentfromUrl(string url, int start, int end)
        {
            
            //var additionalHeaders = new Dictionary<string, string>();
            //additionalHeaders["Range"] = $"bytes={start}-{end}";
            var data = await HttpHelper.Default.GetAsync<string>(url, null, null);
            return data;
        }
        internal  string UploadFileToOneDrive(string folderID, string filename)
        {
            //remove those 2 lines
            counter++;
            filename += counter.ToString();
            var filebytes = new Byte[1000];
            var random = new Random();
            random.NextBytes(filebytes);
            var extension = fileExtensions[random.Next(0, fileExtensions.Count)];
            filename += extension.ToLower();
            var fileUrl = new Uri(DriveItemsPath + folderID + "/children/" + filename + "/content");
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileUrl);
            request.Method = "PUT";
            request.ContentLength = filebytes.Length;
            request.AllowWriteStreamBuffering = true;
            //request.Accept = "application/json;odata=verbose";
            request.ContentType = "text/plain";
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            var stream = request.GetRequestStream();
            //filestream.CopyTo(stream);
            stream.Write(filebytes, 0, filebytes.Length);
            stream.Close();

            var response =  request.GetResponse();
            // Get the stream associated with the response.
            var receiveStream = response.GetResponseStream();

            // Pipes the stream to a higher level stream reader with the required encoding format.
            var readStream = new StreamReader(receiveStream, Encoding.UTF8);

            //Debug.WriteLine("UploadFileToOneDrive Response stream received:");
            var result = readStream.ReadToEnd();
            // Debug.WriteLine(result);
            var jo = JObject.Parse(result);
            string fileId = jo.SelectToken("id").Value<string>();

            Debug.WriteLine("fileId uploaded: " + fileId);
            return fileId;
        }


    }
}
