using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRobotNoTableParam
{
    public static class FileTypes
    {
        public static string[] Extensions = { ".xlsx", ".docx", ".pptx", ".txt", ".wmv", ".jpg", ".jpeg",
            ".xls", ".doc", ".ppt", ".pptx", ".mp3", ".wav", ".zip", ".7z", ".c", ".h", ".cpp", ".cs", ".java",
            ".py", ".swift", ".torrent", ".ics", ".sql", ".pdf", ".png", ".mp4"};
        public static string Excel = ".xlsx";
        public static string Word = ".docx";
        public static string PowerPoint = ".pptx";
        public static string Text = ".txt";
        public static string WindowsVideoMedia = ".wmv";
    }
    public static class RansomwareExtentions
    {
        public static string[] Extensions = {".ecc", ".ezz", ".exx", ".zzz", ".xyz",
            ".aaa", ".abc", ".ccc", ".vvv", ".xxx", ".ttt", ".micro", ".encrypted",
            ".locked", ".crypto", "_crypt", ".crinf", ".r5a", ".XRNT", ".XTBL", ".crypt", ".R16M01D05",
            ".pzdc", ".good", ".LOL!", ".OMG!", ".RDM", ".RRK", ".encryptedRSA", ".crjoker",
            ".EnCiPhErEd", ".LeChiffre", ".keybtc@inbox_com", ".0x0", ".bleep", ".1999", ".vault",
            ".HA3", ".toxcrypt", ".magic", ".SUPERCRYPT", ".CTBL", ".CTB2", ".locky", ".spartan" };
    }

    public class FileTypesList
    {
        List<string> FileTypesListStrings { get; set; }
        List<string> RansomwareExtenstionsList { get; set; }
        public FileTypesList()
        {
            this.FileTypesListStrings = new List<string>();
            this.FileTypesListStrings.AddRange(FileTypes.Extensions);
            //this.FileTypesListStrings.Add(FileTypes.Word);
            /*FileTypesListStrings.Add(FileTypes.Excel);
            FileTypesListStrings.Add(FileTypes.Word);
            FileTypesListStrings.Add(FileTypes.PowerPoint);
            FileTypesListStrings.Add(FileTypes.Text);
            FileTypesListStrings.Add(FileTypes.WindowsVideoMedia);*/
            this.RansomwareExtenstionsList = new List<string>();
            this.RansomwareExtenstionsList.AddRange(RansomwareExtentions.Extensions);
        }
        public List<string> GetListOfFiles()
        {
            return this.FileTypesListStrings;
        }
        public List<string> GetListOfRansomwares()
        {
            return this.RansomwareExtenstionsList;
        }
    }
}
