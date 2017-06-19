using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using static DataRobotNoTableParam.DataRobotNoTableParamFunction;
//

namespace DataRobotNoTableParam
{
    static public class FileCloudTable
    {
        /// <summary>
        /// Insert a file entity to files table.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        public static async Task<bool> Insert(CloudTable table, TableEntity entity)
        {
            TableOperation insert = TableOperation.Insert(entity);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(insert);
            if (retrievedResult.Result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static async Task<bool> InsertOrReplace(CloudTable table, TableEntity entity)
        {
            TableOperation insert = TableOperation.InsertOrReplace(entity);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(insert);
            if (retrievedResult.Result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Same as other insert, but avoid using this one.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="PartitionKey"></param>
        /// <param name="RowKey"></param>
        /// <returns></returns>
        public static async Task<bool> Insert(CloudTable table, string PartitionKey, string RowKey)
        {
            FileEntity tmpFile = new FileEntity(PartitionKey, RowKey);
            TableOperation insert;
            if (PartitionKey == NumberOfSuspectsPartitionKey)
            {
                insert = TableOperation.InsertOrReplace(tmpFile);
            }
            else
            {
                insert = TableOperation.Insert(tmpFile);
            }

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(insert);
            if (retrievedResult.Result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> InsertSuspectsNumber(CloudTable table, string SubsriptionID, long? NumberToUpdate)
        {
            FileEntity tmpFile = new FileEntity(SubsriptionID, SubsriptionID);
            tmpFile.Size = NumberToUpdate.Value;
            TableOperation insert = TableOperation.InsertOrReplace(tmpFile);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(insert);
            if (retrievedResult.Result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static async Task<long?> FindSuspectsNumber(CloudTable table, string SubsriptionID)
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<FileEntity>(SubsriptionID, SubsriptionID);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            //TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                long NumberSuspects = ((FileEntity)retrievedResult.Result).Size;
                return NumberSuspects;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }

        /// <summary>
        /// Delete a file entity from files table.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static async Task<bool> Delete(CloudTable table, TableEntity entity)
        {
            bool result = true;
            string partitionKey = entity.PartitionKey;
            string rowKey = entity.RowKey;
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(partitionKey, rowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            TableEntity deleteEntity = (TableEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
                //table.Execute(deleteOperation);

                //return true;
            }
            else
            {
                result = false;
                //return false;
            }
            return result;
            //return ((bool)Delete(table, partitionKey, rowKey));
        }
        /// <summary>
        /// Delete a file entity from files table.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="PartitionKey"></param>
        /// <param name="RowKey"></param>
        /// <returns></returns>
        public static async Task<bool> Delete(CloudTable table, string PartitionKey, string RowKey)
        {
            bool result = true;
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            TableEntity deleteEntity = (TableEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
                //table.Execute(deleteOperation);

                //return true;
            }
            else
            {
                result = false;
                //return false;
            }
            return result;
        }

        public static async Task<FileEntity> FindFile(CloudTable table, string PartitionKey, string RowKey)
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<FileEntity>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            //TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                return (FileEntity)retrievedResult.Result;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }

        /// <summary>
        /// Find a file entity in files table.
        /// Return the file entity if found, null otherwise.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="PartitionKey"></param>
        /// <param name="RowKey"></param>
        /// <returns></returns>
        public static async Task<TableEntity> Find(CloudTable table, string PartitionKey, string RowKey)
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            //TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                return (TableEntity)retrievedResult.Result;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }

        /// <summary>
        /// Find a file entity in files table.
        /// Return the file entity if found, null otherwise.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static async Task<TableEntity> Find(CloudTable table, TableEntity entity)
        {
            string partitionKey = entity.PartitionKey;
            string rowKey = entity.RowKey;
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(partitionKey, rowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            //TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                return (TableEntity)retrievedResult.Result;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }

    }
    public class CloudTableControl
    {
        private CloudTable _table;
        private CloudStorageAccount _storageAccount;
        private CloudTableClient _tableClient;
        private string _ConnectionString;
        private string _TableName;
        public CloudTableControl(CloudTable table)
        {
            this._table = table;
        }
        public CloudTableControl(string TableName, string ConnectionString)
        {
            this._TableName = TableName;
            this._ConnectionString = ConnectionString;
            // Retrieve the storage account from the connection string.
            this._storageAccount = CloudStorageAccount.Parse(ConnectionString);

            // Create the table client.
            this._tableClient = this._storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            this._table = this._tableClient.GetTableReference(TableName);
        }
        /// <summary>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public void Insert(TableEntity entity)
        {
            TableOperation insert = TableOperation.Insert(entity);
            (this._table).Execute(insert);
        }

        /// <summary>
        /// Return the entity if exists, null otherwise.
        /// Need to provide PartitionKey and RowKey.
        /// </summary>
        /// <param name="PartitionKey"></param>
        /// <param name="RowKey"></param>
        /// <returns></returns>
        public TableEntity FindEntityInTable(string PartitionKey, string RowKey)
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = this._table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                return (TableEntity)retrievedResult.Result;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }
        /// <summary>
        /// Return the entity if exists, null otherwise.
        /// Need to provide a Table Entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TableEntity FindEntityInTable(TableEntity entity)
        {
            return (this.FindEntityInTable(entity.PartitionKey, entity.RowKey));
        }

        /// <summary>
        /// Return true if exists, false otherwise.
        /// Need to provide PartitionKey and RowKey.
        /// </summary>
        /// <param name="PartitionKey"></param>
        /// <param name="RowKey"></param>
        /// <returns></returns>
        public bool DeleteEntityFromTable(string PartitionKey, string RowKey)
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(PartitionKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = this._table.Execute(retrieveOperation);

            // Assign the result to a CustomerEntity.
            TableEntity deleteEntity = (TableEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                this._table.Execute(deleteOperation);

                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Return true if exists, false otherwise.
        /// Need to provide a Table Entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DeleteEntityFromTable(TableEntity entity)
        {
            return (this.DeleteEntityFromTable(entity.PartitionKey, entity.RowKey));
        }
    }
    public class LocalUserEntity : TableEntity
    {
        string partitionKey = "PartKey";
        public LocalUserEntity()
        {
            this.PartitionKey = "PartKey";
        }
        public LocalUserEntity(string RowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = RowKey;
        }
        /*public static async Task<LocalUserEntity> FindLocalUserQuery(CloudTable table, string RowKey)
        {
            try
            {
                var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PartKey");
                var userIdFilter = TableQuery.GenerateFilterCondition("SignInUserId", QueryComparisons.Equal, RowKey);
                string filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, userIdFilter);

                var query = new TableQuery<LocalUserEntity>().Where(filter).Take(1);
                var matchingEntry = table.ExecuteQuery(query).FirstOrDefault();
                return matchingEntry;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }*/
        public static async Task<LocalUserEntity> FindLocalUser(CloudTable table, string RowKey, string PartKey = "PartKey")
        {
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<LocalUserEntity>(PartKey, RowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);
            //TableResult retrievedResult = table.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                //Console.WriteLine(((CustomerEntity)retrievedResult.Result).PhoneNumber);
                return (LocalUserEntity)retrievedResult.Result;
            }
            else
            {
                //Console.WriteLine("The phone number could not be retrieved.");
                return null;
            }
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string SubscriptionID { get; set; }
    }
}
