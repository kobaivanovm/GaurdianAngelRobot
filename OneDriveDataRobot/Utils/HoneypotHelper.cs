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
        private readonly OneDriveHelper oneDriveHelper;
       
        private  static List<string> fileExtensions;
        static HoneypotHelper()
        {
            fileExtensions = new List<string> { ".DOC", ".DOCX", ".PPT"
            ,".TXT" ,".MP3" ,".WAV" ,".7Z" ,".ZIP" ,".C" ,".CS" ,".CPP" ,".JAVA" ,".PY" 
            ,".SWIFT" ,".TORRENT" ,".ICS" ,".SQL" ,".XLS"
            ,".XLSX" ,".PDF" ,".JPG" ,".PNG" };
        }
        public  HoneypotHelper(string _accessToken)
        {
            oneDriveHelper = new OneDriveHelper(_accessToken);
        }

        internal static byte[] getRandomByteArray(int size)
        {
            var random = new Random();
            var filebytes = new Byte[size];
            random.NextBytes(filebytes);
            return filebytes;
        }
        internal static string getRandomFilename()
        {
            var random = new Random();
            var filename = "honeypot";
            var extension = fileExtensions[random.Next(0, fileExtensions.Count)];
            filename += extension.ToLower();
            return filename;
        }
       public  async Task<int> SpreadHoneypotsFromRootAsync()
        {
            var honeypotsCounter = 0;
            var rootID = await oneDriveHelper.GetIDByPath(OneDriveHelper.RootPath);
            var q = new Queue<string>();
            q.Enqueue(rootID);
            while (q.Count > 0)
            {
                string currentFolderID = q.Dequeue();
                if (currentFolderID == null) continue;

                var listChildrenfolderID = await oneDriveHelper.GetChildrenIDsByFolderID(currentFolderID);
                foreach(var id in listChildrenfolderID) q.Enqueue(id);
             
                oneDriveHelper.UploadFileToFolder(currentFolderID, getRandomFilename(), 
                    getRandomByteArray(2000));
        
                ++honeypotsCounter;
            }
            return honeypotsCounter;
        }
        //public async Task<int> DeleteAllHoneypotsAsync()
        //{

        //}

      


    }
}
