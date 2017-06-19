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
        public  HoneypotHelper(string accessToken)
        {
            oneDriveHelper = new OneDriveHelper(accessToken);
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
        public  async Task<List<string>> SpreadHoneypotsFromRootAsync()
        {
            var honeypotsId = new List<string>();
            var honeypotsCounter = 0;
            var rootId = await oneDriveHelper.GetIDByPath(OneDriveHelper.RootPath);
            var q = new Queue<string>();
            q.Enqueue(rootId);
            while (q.Count > 0)
            {
                string currentFolderId = q.Dequeue();
                if (currentFolderId == null) continue;

                var listChildrenfolderId = await oneDriveHelper.GetContainedFoldersIDs(currentFolderId);
                foreach(var id in listChildrenfolderId) q.Enqueue(id);
             
                var honeypotId= oneDriveHelper.UploadFileToFolder(currentFolderId, getRandomFilename(), 
                    getRandomByteArray(2000));
                honeypotsId.Add(honeypotId);
                ++honeypotsCounter;
            }
            return honeypotsId;
        }


        public int DeleteAllHoneypotsAsync()
        {
            return 3;

        }




    }
}
