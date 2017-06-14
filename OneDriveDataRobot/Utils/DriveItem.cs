using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OneDriveDataRobot.Utils
{
    [JsonObject]
    public class Application
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
    [JsonObject]
    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
    [JsonObject]
    public class CreatedBy
    {
        [JsonProperty("application")]
        public Application Application { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
    }

    [JsonObject]
    public class LastModifiedBy
    {
        [JsonProperty("application")]
        public Application Application { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
    }
    [JsonObject]
    public class ParentReference
    {
        [JsonProperty("driveId")]
        public string DriveId { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
    }
    [JsonObject]
    public class FileSystemInfo
    {
        [JsonProperty("createdDateTime")]
        public string CreatedDateTime { get; set; }
        [JsonProperty("lastModifiedDateTime")]
        public string LastModifiedDateTime { get; set; }
    }
    [JsonObject]
    public class Folder
    {
        [JsonProperty("childCount")]
        public int ChildCount { get; set; }
    }

    [JsonObject]
    public class SpecialFolder
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    [JsonObject]
    public class DriveItem
    {
        [JsonProperty("createdBy")]
        public CreatedBy CreatedBy { get; set; }

        [JsonProperty("createdDateTime")]
        public string CreatedDateTime { get; set; }

        [JsonProperty("eTag")]
        public string Etag { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("lastModifiedBy")]
        public LastModifiedBy LastModifiedBy { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public string LastModifiedDateTime { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parentReference")]
        public ParentReference ParentReference { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        [JsonProperty("cTag")]
        public string Ctag { get; set; }

        [JsonProperty("fileSystemInfo")]
        public FileSystemInfo FileSystemInfo { get; set; }

        [JsonProperty("folder")]
        public Folder Folder { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("specialFolder")]
        public SpecialFolder SpecialFolder { get; set; }
    }
 
}