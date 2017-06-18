using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataRobotNoTableParam
{
    public class BlobControl
    {
        private string _ContainerName;
        private string _BlobConnectionString;

        private string _DefaultContainerName = "users-files-blob";
        private string _DefaultBlobConnectionString = "DefaultEndpointsProtocol=https;AccountName=guardian1angel1storage;AccountKey=A4y6aQhgZcCD6eU/yjssfFDYKBmMcj7wFnmqe2euOdBrzHxs2WAzcRXtTWvvOKQn06yMAhHSHAV5KynWN32liw==;EndpointSuffix=core.windows.net";

        CloudStorageAccount _storageAccount;
        CloudBlobClient _blobClient;
        CloudBlobContainer _container;

        public void Insert(string BlobName)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(this._DefaultBlobConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(this._DefaultContainerName);

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(@"path\myfile"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
        }

        public BlobControl(string ContainerName, string ConnectionString)
        {
            // Retrieve storage account from connection string.
            this._storageAccount = CloudStorageAccount.Parse(ConnectionString);

            // Create the blob client.
            this._blobClient = this._storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            this._container = this._blobClient.GetContainerReference(ContainerName);

            // Create the container if it doesn't already exist.
            this._container.CreateIfNotExists();

            this._ContainerName = ContainerName;
            this._BlobConnectionString = ConnectionString;
        }
        public BlobControl()
        {
            // Retrieve storage account from connection string.
            this._storageAccount = CloudStorageAccount.Parse(this._DefaultBlobConnectionString);

            // Create the blob client.
            this._blobClient = this._storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            this._container = this._blobClient.GetContainerReference(this._DefaultContainerName);

            // Create the container if it doesn't already exist.
            this._container.CreateIfNotExists();

            this._ContainerName = this._DefaultContainerName;
            this._BlobConnectionString = this._DefaultBlobConnectionString;
        }
    }
}
