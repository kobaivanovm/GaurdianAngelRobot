using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Microsoft.OneDrive.Sdk;
using System.Threading.Tasks;

namespace OneDriveDataRobot.Utils
{
    public class HoneypotHelper
    {
        public int Counter{ get; set; }
      /*  public async Task<bool> SpreadHoneypots(string accessToken)
        {
            var rootItem = await oneDriveClient
                            .Drive
                            .Root
                            .Request()
                            .GetAsync();
        }*/
        private string UploadFileToOneDrive(string filename, string accessToken)
        {
            //remove those 2 lines
            Counter++;
            filename += Counter.ToString();
            var filebytes = new Byte[1000];
            var random = new Random();
            random.NextBytes(filebytes);
            var fileUrl = new Uri("https://graph.microsoft.com/v1.0/me/drive/root/children/" + filename + "/content");
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

            var response = request.GetResponse();
            // Get the stream associated with the response.
            var receiveStream = response.GetResponseStream();

            // Pipes the stream to a higher level stream reader with the required encoding format. 
            var readStream = new StreamReader(receiveStream, Encoding.UTF8);

            Debug.WriteLine("Response stream received:");
            var result = readStream.ReadToEnd();
            Debug.WriteLine(result);
            var jo = JObject.Parse(result);
            string fileId = jo.SelectToken("id").Value<string>();

            Debug.WriteLine("fileId: " + fileId);
            return fileId;
        }


    }
}