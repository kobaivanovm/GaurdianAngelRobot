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
using OneDriveDataRobot.Models;

namespace OneDriveDataRobot.Utils
{
    public class OneDriveHelper
    {
        private readonly string accessToken;

        public const string RootPath = @"https://graph.microsoft.com/v1.0/me/drive/root/";
        private const string DriveItemsPath = @"https://graph.microsoft.com/v1.0/me/drive/items/";
 
        public OneDriveHelper(string _accessToken)
        {
            accessToken = _accessToken;
        }

        public async Task<string> GetIDByPath(string path)
        {
            var data = await HttpHelper.Default.GetAsync<JObject>(path, accessToken);
            var id = data.SelectToken("id").Value<string>();
            Debug.WriteLine($"The ID of the driveitem: {path} is {id}");
            return id;
        }

        public async Task<DriveItem> GetDriveItemByID(string id)
        {
            var driveItem = await HttpHelper.Default.GetAsync<DriveItem>(DriveItemsPath + id, accessToken);
            Debug.WriteLine($"Your file info:{driveItem.Name} size: {driveItem.Size} WebUrl: {driveItem.WebUrl}");
            return driveItem;
        }
        public async Task<List<DriveItem>> GetChildrenByFolderID(string folderID)
        {
            var uri = DriveItemsPath + folderID + "/children";
            var responseMassage = await HttpHelper.Default.GetResponseAsStirngAsync(uri, accessToken);
            var result = JsonConvert.DeserializeObject<ODataResponse<DriveItem>>(responseMassage);
            return result.Value;
        }
        public async Task<List<string>> GetChildrenIDsByFolderID(string folderID)
        {
            var children = await GetChildrenByFolderID(folderID);
            
            return children.Select(p => p.Id).ToList();
        }
        public async Task<List<string>> GetContainedFoldersIDs(string folderID)
        {
            var allChildren = await GetChildrenByFolderID(folderID);
            var folderIdChildern = from item in allChildren
                                     where item.Folder != null select item.Id;
            return folderIdChildern.ToList<string>(); ;
        }
        public string UploadFileToFolder(string folderID , string filename,byte[] filebytes)
        {

            var fileUrl = new Uri(DriveItemsPath + folderID + "/children/" + filename + "/content");
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fileUrl);
            request.Method = "PUT";
            request.ContentLength = filebytes.Length;
            request.AllowWriteStreamBuffering = true;
            request.ContentType = "text/plain";
            request.Headers.Add("Authorization", "Bearer " + accessToken);

            var stream = request.GetRequestStream();
            stream.Write(filebytes, 0, filebytes.Length);
            stream.Close();
            var receiveStream = request.GetResponse().GetResponseStream();
            var readStream = new StreamReader(receiveStream, Encoding.UTF8);
            var result = readStream.ReadToEnd();

            return JObject.Parse(result).SelectToken("id").Value<string>();
        }


    }
}
