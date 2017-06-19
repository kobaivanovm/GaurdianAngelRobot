using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRobotNoTableParam
{
    public class FileEntity : TableEntity
    {
        public FileEntity(string UserSubscriptionID, string FileID)
        {
            this.PartitionKey = UserSubscriptionID;
            this.RowKey = FileID;
            this.SubscriptionID = UserSubscriptionID;
            this.FileId = FileID;
        }

        public void AddParametersFromDriveItem(DriveItem item)
        {
            this.Name = item.Name;
            if (item.Size != null)
            {
                long tmp = item.Size.Value;
                this.Size = (int)tmp;//On purpose. No reason to leave it long
            }
            this.WebUrl = item.WebUrl;
            this.Description = item.Description;
            if (item.LastModifiedBy != null)
            {
                if (item.LastModifiedBy.User != null)
                {
                    this.LastModifiedByUserName = item.LastModifiedBy.User.DisplayName;
                    this.LastModifiedByUserID = item.LastModifiedBy.User.Id;
                }
            }
            if (item.CreatedByUser != null)
            {
                this.CreatedByUserName = item.CreatedByUser.DisplayName;
                this.CreatedByUserID = item.CreatedByUser.Id;
            }
            this.CreatedByUser = item.CreatedByUser;
            this.LastModifiedByUser = item.LastModifiedByUser;
            if (item.File != null)
            {
                this.IsFile = true;
            }
            else if (item.Folder != null)
            {
                this.IsFolder = true;
            }
            //this.LastModifiedDateTime = item.LastModifiedDateTime;
            //this.CreatedDateTime = item.CreatedDateTime;
            //this.Content = item.Content;
            /*
            this.CreatedBy = item.CreatedBy;
            this.LastModifiedBy = item.LastModifiedBy;
            this.ODataType = item.ODataType;
            this.FileSystemInformation = item.FileSystemInfo;
            this.File = item.File;
            this.Folder = item.Folder;
            */
            /*using (var reader = new StreamReader(item.Content))
            {
                this.FileStringContent = reader.ReadToEnd();
            }*/
            //Don't think we need a specific encoding
            /*using (var reader = new StreamReader(item.Content, Encoding.UTF8))
            {
                this.FileStringContent = reader.ReadToEnd();
            }*/
        }

        public FileEntity() { }

        public string LastModifiedByUserID { get; set; }
        public string LastModifiedByUserName { get; set; }
        public string CreatedByUserName { get; set; }
        public string CreatedByUserID { get; set; }
        public string Name { get; set; }
        public string FileId { get; set; }
        public string SubscriptionID { get; set; }
        public long Size { get; set; }
        public bool IsFolder { get; set; }
        public bool IsFile { get; set; }
        public double Entropy { get; set; }
        public string FileMagic { get; set; }
        //public System.Collections.ObjectModel.Collection<bool> FileMagicBytes { get; set; }
        public string Description { get; set; }
        public string WebUrl { get; set; }
        public User CreatedByUser { get; set; }
        public User LastModifiedByUser { get; set; }
        //public DateTimeOffset? LastModifiedDateTime { get; set; }
        //public DateTimeOffset? CreatedDateTime { get; set; }
        //public Stream Content { get; set; }
        /*
        public IdentitySet LastModifiedBy { get; set; }
        public string FileType { get; set; }
        public IdentitySet CreatedBy { get; set; }
        public string ODataType { get; set; }
        public Microsoft.Graph.FileSystemInfo FileSystemInformation { get; set; }
        public Microsoft.Graph.File File { get; set; }
        public Folder Folder { get; set; }
        */
    }
}