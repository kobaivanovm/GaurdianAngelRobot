using System;
using System.Net;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Graph;
using Microsoft.Azure.WebJobs;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;

namespace DataRobotNoTableParam
{
    public static class DataRobotNoTableParamFunction
    {
        public const string TriggerWord = "!odbot";
        public const string idaClientId = "6b03b509-64e8-4773-99a3-0df397284fd7";
        public const string idaClientSecret = "I7FvOsEeO9+Om58MkHa5/jcLznGLRbUw1QBfw/aF39s=";
        public const string idaAuthorityUrl = "https://login.microsoftonline.com/common";
        public const string idaMicrosoftGraphUrl = "https://graph.microsoft.com";
        public const string StorageConnentionString = "DefaultEndpointsProtocol=https;AccountName=guardian1angel1storage;AccountKey=A4y6aQhgZcCD6eU/yjssfFDYKBmMcj7wFnmqe2euOdBrzHxs2WAzcRXtTWvvOKQn06yMAhHSHAV5KynWN32liw==;EndpointSuffix=core.windows.net";
        public const string SyncName = "syncState";
        public const string TokenCacheName = "tokenCache";
        public const string UsersTable = "UsersManageHistory";
        public const string FilesTable = "FilesMonitorHistory";
        public const int MaxNumberOfSuspects = 30;
        public const string NumberOfSuspectsPartitionKey = "GuardianAngelSuspectsPartition";
        public const string NumberOfSuspectsRowKey = "GuardianAngelSuspectsRow";
        public const int MaxFileSize = 2 ^ 40;//1GB
        public const int MinFileSize = 512;//512 Bytes

        // Main entry point for our Azure Function. Listens for webhooks from OneDrive and responds to the webhook with a 204 No Content.
        [FunctionName("OneDriveRobotFunctionVersion2")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        //public static async Task<object> Run(HttpRequestMessage req, CloudTable syncStateTable, CloudTable tokenCacheTable, TraceWriter log)
        {
            log.Info($"Running Version 45 (for debugging: 00:52)");

            log.Info($"Webhook was triggered!");

            CloudTable syncStateTable = CloudTableInterface.GetCloudTable(SyncName, StorageConnentionString);
            CloudTable tokenCacheTable = CloudTableInterface.GetCloudTable(TokenCacheName, StorageConnentionString);
            CloudTable FilesMonitorHistoryTable = CloudTableInterface.GetCloudTable(FilesTable, StorageConnentionString);
            //CloudTableControl UsersManageHistoryTable = new CloudTableControl(UsersTable, StorageConnentionString);
            //CloudTableControl FilesMonitorHistoryTable = new CloudTableControl(FilesTable, StorageConnentionString);
            // Handle validation scenario for creating a new webhook subscription
            Dictionary<string, string> qs = req.GetQueryNameValuePairs()
                                    .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
            if (qs.ContainsKey("validationToken"))
            {
                var token = qs["validationToken"];
                log.Info($"Responding to validationToken: {token}");
                return PlainTextResponse(token);
            }

            // If not the validation scenario, read the body of the request and parse the notification
            //log.Info($"No ValidatonToken. Start ReadAsStringAsync");
            string jsonContent = await req.Content.ReadAsStringAsync();
            log.Verbose($"Raw request content: {jsonContent}");
            //log.Info($"Finished ReadAsStringAsync");
            // Since webhooks can be batched together, loop over all the notifications we receive and process them individually.
            // In the real world, this shouldn't be done in the request handler, but rather queued to be done later.
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            //log.Info($"Finished DeserializeObject");
            if (data.value != null)
            {
                //log.Info($"data.value != null");
                //int i = 0;//
                foreach (var subscription in data.value)
                {
                    //log.Info($"var subscription in data.value. i= {i}");//
                    var clientState = subscription.clientState;
                    var resource = subscription.resource;
                    string subscriptionId = (string)subscription.subscriptionId;
                    log.Info($"Notification for subscription: '{subscriptionId}' Resource: '{resource}', clientState: '{clientState}'");

                    // Process the individual subscription information
                    bool exists = await ProcessSubscriptionNotificationAsync(FilesMonitorHistoryTable, subscriptionId, syncStateTable, tokenCacheTable, log);
                    if (!exists)
                    {
                        return req.CreateResponse(HttpStatusCode.Gone);
                    }
                }
                return req.CreateResponse(HttpStatusCode.NoContent);
            }

            log.Info($"Request was incorrect. Returning bad request.");
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        private static HttpResponseMessage PlainTextResponse(string text)
        {
            HttpResponseMessage response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                        text,
                        System.Text.Encoding.UTF8,
                        "text/plain"
                    )
            };
            return response;
        }

        // Retrieve a new access token from AAD
        private static async Task<string> RetrieveAccessTokenAsync(string signInUserId, CloudTable tokenCacheTable, TraceWriter log)
        {
            log.Verbose($"Retriving new accessToken for signInUser: {signInUserId}");

            var tokenCache = new AzureTableTokenCache(signInUserId, tokenCacheTable);
            var authContext = new AuthenticationContext(idaAuthorityUrl, tokenCache);

            try
            {
                var userCredential = new UserIdentifier(signInUserId, UserIdentifierType.UniqueId);
                // Don't really store your clientId and clientSecret in your code. Read these from configuration.
                var clientCredential = new ClientCredential(idaClientId, idaClientSecret);
                var authResult = await authContext.AcquireTokenSilentAsync(idaMicrosoftGraphUrl, clientCredential, userCredential);
                return authResult.AccessToken;
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                log.Info($"ADAL Error: Unable to retrieve access token: {ex.Message}");
                return null;
            }
        }

        // Do the work to retrieve deltas from this subscription and then find any changed Excel files
        private static async Task<bool> ProcessSubscriptionNotificationAsync(CloudTable FilesMonitorTable, string subscriptionId, CloudTable syncStateTable, CloudTable tokenCacheTable, TraceWriter log)
        {
            // Retrieve our stored state from an Azure Table
            StoredSubscriptionState state = StoredSubscriptionState.Open(subscriptionId, syncStateTable);
            if (state == null)
            {
                log.Info($"Missing data for subscription '{subscriptionId}'.");
                return false;
            }

            log.Info($"Found subscription '{subscriptionId}' with lastDeltaUrl: '{state.LastDeltaToken}'.");

            GraphServiceClient client = new GraphServiceClient(new DelegateAuthenticationProvider(async (request) =>
            {
                string accessToken = await RetrieveAccessTokenAsync(state.SignInUserId, tokenCacheTable, log);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
            }));

            // Query for items that have changed since the last notification was received

            bool Result = await FileMonitorOneDrive.ProccessChangesInFiles(FilesMonitorTable, client, state, subscriptionId, syncStateTable, tokenCacheTable, log);
            return Result;//It's always true, has nothing to do with actuall outcome

            //*** Changes should be from here ***

            /*
            List<string> changedExcelFileIds = await FileMonitorOneDrive.FindChangedExcelFilesInOneDrive(state, client, log);

            // Do work on the changed files
            foreach (var file in changedExcelFileIds)
            {
                log.Info($"Processing changes in file: {file}");
                try
                {
                    string sessionId = await FileMonitorOneDrive.StartOrResumeWorkbookSessionAsync(client, file, syncStateTable, log);
                    log.Info($"File {file} is using sessionId: {sessionId}");
                    await FileMonitorOneDrive.ScanExcelFileForPlaceholdersAsync(client, file, sessionId, log);
                }
                catch (Exception ex)
                {
                    log.Info($"Exception processing file: {ex.Message}");
                }
            }

            // Update our saved state for this subscription
            state.Insert(syncStateTable);
            return true;*/
        }

        /*** SHARED CODE STARTS HERE ***/

        /// <summary>
        /// Persists information about a subscription, userId, and deltaToken state. This class is shared between the Azure Function and the bootstrap project
        /// </summary>
        public class StoredSubscriptionState : TableEntity
        {
            public StoredSubscriptionState()
            {
                this.PartitionKey = "AAA";
            }

            public string SignInUserId { get; set; }
            public string LastDeltaToken { get; set; }
            public string SubscriptionId { get; set; }
            public string ExcelSessionId { get; set; }


            public static StoredSubscriptionState CreateNew(string subscriptionId)
            {
                var newState = new StoredSubscriptionState();
                newState.RowKey = subscriptionId;
                newState.SubscriptionId = subscriptionId;
                return newState;
            }

            public void Insert(CloudTable table)
            {
                TableOperation insert = TableOperation.InsertOrReplace(this);
                table.Execute(insert);
            }

            public static StoredSubscriptionState Open(string subscriptionId, CloudTable table)
            {
                TableOperation retrieve = TableOperation.Retrieve<StoredSubscriptionState>("AAA", subscriptionId);
                TableResult results = table.Execute(retrieve);
                return (StoredSubscriptionState)results.Result;
            }
        }

        /// <summary>
        /// Keep track of file specific information for a short period of time, so we can avoid repeatedly acting on the same file
        /// </summary>
        public class FileHistory : TableEntity
        {
            public FileHistory()
            {
                this.PartitionKey = "BBB";
            }

            public string ExcelSessionId { get; set; }
            public DateTime LastAccessedDateTime { get; set; }

            public static FileHistory CreateNew(string userId, string fileId)
            {
                var newState = new FileHistory();
                newState.RowKey = $"{userId},{fileId}";
                return newState;
            }

            public void Insert(CloudTable table)
            {
                TableOperation insert = TableOperation.InsertOrReplace(this);
                table.Execute(insert);
            }

            public static FileHistory Open(string userId, string fileId, CloudTable table)
            {
                TableOperation retrieve = TableOperation.Retrieve<FileHistory>("BBB", $"{userId},{fileId}");
                TableResult results = table.Execute(retrieve);
                return (FileHistory)results.Result;
            }
        }

        /// <summary>
        /// ADAL TokenCache implementation that stores the token cache in the provided Azure CloudTable instance.
        /// This class is shared between the Azure Function and the bootstrap project.
        /// </summary>
        public class AzureTableTokenCache : TokenCache
        {
            private readonly string signInUserId;
            private readonly CloudTable tokenCacheTable;

            private TokenCacheEntity cachedEntity;      // data entity stored in the Azure Table

            public AzureTableTokenCache(string userId, CloudTable cacheTable)
            {
                signInUserId = userId;
                tokenCacheTable = cacheTable;

                this.AfterAccess = AfterAccessNotification;

                cachedEntity = ReadFromTableStorage();
                if (null != cachedEntity)
                {
                    Deserialize(cachedEntity.CacheBits);
                }
            }

            private TokenCacheEntity ReadFromTableStorage()
            {
                TableOperation retrieve = TableOperation.Retrieve<TokenCacheEntity>(TokenCacheEntity.PartitionKeyValue, signInUserId);
                TableResult results = tokenCacheTable.Execute(retrieve);
                return (TokenCacheEntity)results.Result;
            }

            private void AfterAccessNotification(TokenCacheNotificationArgs args)
            {
                if (this.HasStateChanged)
                {
                    if (cachedEntity == null)
                    {
                        cachedEntity = new TokenCacheEntity();
                    }
                    cachedEntity.RowKey = signInUserId;
                    cachedEntity.CacheBits = Serialize();
                    cachedEntity.LastWrite = DateTime.Now;

                    TableOperation insert = TableOperation.InsertOrReplace(cachedEntity);
                    tokenCacheTable.Execute(insert);

                    this.HasStateChanged = false;
                }
            }

            /// <summary>
            /// Representation of the data stored in the Azure Table
            /// </summary>
            private class TokenCacheEntity : TableEntity
            {
                public const string PartitionKeyValue = "tokenCache";
                public TokenCacheEntity()
                {
                    this.PartitionKey = PartitionKeyValue;
                }

                public byte[] CacheBits { get; set; }
                public DateTime LastWrite { get; set; }
            }

        }
    }
}