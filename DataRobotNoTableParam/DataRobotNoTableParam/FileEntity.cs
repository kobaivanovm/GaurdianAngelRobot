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
                this.Size = tmp;
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
            if (item.CreatedBy != null)
            {
                if (item.CreatedBy.User != null)
                {
                    this.CreatedByUserName = item.CreatedBy.User.DisplayName;
                    this.CreatedByUserID = item.CreatedBy.User.Id;
                }
            }
            if (item.File != null)
            {
                this.IsFile = true;
            }
            else if (item.Folder != null)
            {
                this.IsFolder = true;
            }
            this.IsHoneypot = item.Name.Contains("honeypot");
        }

        public FileEntity() { }

        public bool IsNew { get; set; }
        public bool IsHoneypot { get; set; }
        public Stream Content { get; set; }
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
        public string Description { get; set; }
        public string WebUrl { get; set; }
        public User CreatedByUser { get; set; }
        public User LastModifiedByUser { get; set; }
    }
}