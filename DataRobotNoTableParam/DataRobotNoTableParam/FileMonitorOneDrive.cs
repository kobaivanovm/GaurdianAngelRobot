using Microsoft.Azure.WebJobs.Host;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataRobotNoTableParam.DataRobotNoTableParamFunction;

namespace DataRobotNoTableParam
{
    public static class FileMonitorOneDrive
    {
        private const string FileTriggerWord = DataRobotNoTableParamFunction.TriggerWord;
        //public const string idaClientId = "6b03b509-64e8-4773-99a3-0df397284fd7";
        //public const string idaClientSecret = "I7FvOsEeO9+Om58MkHa5/jcLznGLRbUw1QBfw/aF39s=";
        //public const string idaAuthorityUrl = "https://login.microsoftonline.com/common";
        //public const string idaMicrosoftGraphUrl = "https://graph.microsoft.com";
        private const string FileStorageConnentionString = DataRobotNoTableParamFunction.StorageConnentionString;

        public static async Task<bool> ProccessChangesInFiles(CloudTable FilesMonitorTable, GraphServiceClient client, StoredSubscriptionState state, string subscriptionId, CloudTable syncStateTable, CloudTable tokenCacheTable, TraceWriter log)
        {            
            bool result = true;

            List<DriveItem> ChangedFilesDriveItems;
            List<DriveItem> HarmedDriveItems = new List<DriveItem>();

            // Query for items that have changed since the last notification was received
            ChangedFilesDriveItems = await FindChangedAnyFilesInOneDrive(FilesMonitorTable, subscriptionId, state, client, log);

            // Get list of required files:
            FileTypesList filesTypes = new FileTypesList();
            List<string> TypeList = filesTypes.GetListOfFiles();
            List<string> RansomwareExtensions = filesTypes.GetListOfRansomwares();

            // Go over files and sort them:
            foreach (DriveItem item in ChangedFilesDriveItems)
            {
                int indexOfDotType = item.Name.LastIndexOf('.');
                //log.Info($"For debugging: indexOfDotType is {indexOfDotType}");//TODO remove
                if (item.Name.Length <= (indexOfDotType + 1))
                {
                    // There is a dot char in the end of the name. Not a legal file.
                    log.Info($"For debugging: {item.Name} is an illegal file.");
                    //ChangedFilesDriveItems.Remove(item);
                    continue;
                }
                string FileExtension = item.Name.Substring(indexOfDotType);
                log.Info($"For debugging: FileExtension is {FileExtension}");//TODO remove
                if (RansomwareExtensions.Contains(FileExtension))
                {
                    log.Info($"For debugging: This is in ecrypted by a ransomware!!!");//TODO remove
                    // This file is suspected to be ransomware encrypted:
                    HarmedDriveItems.Add(item);
                    //ChangedFilesDriveItems.Remove(item);
                }
            }

            // Remove suspected items from list:
            foreach (DriveItem item in HarmedDriveItems)
            {
                // Intentionally another foreach to avoid exceptions.
                ChangedFilesDriveItems.Remove(item);
            }

            // Do work on suspected files to be encrypted. Do not add them to storage!
            ////////////////Should be here, not yet///////////////////////////
            bool IsBeingAttacked = await DoWorkOnSuspectedFiles(HarmedDriveItems, log);
            if (IsBeingAttacked == true)
            {
                // Do something to stop it.
            }

            // Do work on changed files such as security checks:
            ////////////////Should be here, not yet///////////////////////////
            IsBeingAttacked = await DoWorkOnChangedFiles(FilesMonitorTable, ChangedFilesDriveItems, subscriptionId, log);
            if (IsBeingAttacked == true)
            {
                // Do something to stop it.
            }

            // Add to storage if needed
            bool WereFiledAdded = await AddFilesToStorageTableIfNeeded(FilesMonitorTable, subscriptionId, ChangedFilesDriveItems, log);

            // Update our saved state for this subscription
            state.Insert(syncStateTable);
            return ((result == true) ? result : true);
        }

        public static async Task<bool> DoWorkOnSuspectedFiles(List<DriveItem> SuspectedFilesList, TraceWriter log)
        {
            bool result = true;

            //Send Email to user or shut OneDrive down.

            return result;
        }

        public static async Task<bool> DoWorkOnChangedFiles(CloudTable FilesMonitorTable, List<DriveItem> ChangedFilesList, string subscriptionId, TraceWriter log)
        {
            bool IsAttackInPlace = false;

            long? SuspectsNumber = null;

            // Do work on the changed files
            foreach (DriveItem item in ChangedFilesList)
            {
                log.Info($"Processing changes in file: {item.Id}");
                try
                {
                    SuspectsNumber = await FileCloudTable.FindSuspectsNumber(FilesMonitorTable, subscriptionId);
                    if (SuspectsNumber == null)
                    {
                        log.Info($"FileCloudTable.FindSuspectsNumber(FilesMonitorTable) is null. This is realy bad");
                        SuspectsNumber = 1;
                        await FileCloudTable.InsertSuspectsNumber(FilesMonitorTable, subscriptionId, SuspectsNumber);
                    }
                    else if (SuspectsNumber == MaxNumberOfSuspects)
                    {
                        log.Info($"Reached MaxNumberOfSuspects");
                        // An attack is under way
                        IsAttackInPlace = true;
                    }
                    log.Info($"Doing security checks on file: {item.Name}");
                    TableEntity entity = await FileCloudTable.Find(FilesMonitorTable, subscriptionId, item.Id);
                    if (entity == null)
                    {
                        log.Info($"This is a new file");
                        // File is not in table (a new file)
                        continue;
                    }
                    else if (((entity.RowKey == item.Id) && (entity.PartitionKey != subscriptionId)) || 
                        ((entity.PartitionKey == subscriptionId) && (entity.RowKey != item.Id)))
                    {
                        log.Info($"** Something funky is going on **");
                    }
                    else
                    {
                        log.Info($"Checking magic number");
                        // This is a modified previously existing file.

                        // Check if the magic number is legall

                    }
                    if (item.Size > MaxFileSize || item.Size < MinFileSize)
                    {
                        log.Info($"Size is too small or too large");
                        SuspectsNumber++;
                        bool InsertResult = await FileCloudTable.InsertSuspectsNumber(FilesMonitorTable, subscriptionId, SuspectsNumber);
                        log.Info($"For debugging: tried to insert");// TODO remove
                        continue;
                    }
                    if (entity != null)//Make sure to avoid bugs
                    {
                        // Check statistical match between existing version and new version.

                        // Check entropy of file.
                    }

                }
                catch (Exception ex)
                {
                    log.Info($"Exception processing file: {ex.Message}");
                    IsAttackInPlace = false;
                }
                if (SuspectsNumber == MaxNumberOfSuspects)
                {
                    // An attack is under way
                    //needed outside of loop too
                    IsAttackInPlace = true;
                }
            }
            return IsAttackInPlace;
        }

        /*public static async Task<bool> DoWorkOnChangedExcelFiles(CloudTable syncStateTable, GraphServiceClient client, string subscriptionId, List<string> ChangedFilesIds, TraceWriter log)
        {
            bool result = true;
            // Do work on the changed excel files
            foreach (var ChangedFile in ChangedFilesIds)
            {
                log.Info($"Processing changes in file: {ChangedFile}");
                try
                {
                    string sessionId = await StartOrResumeWorkbookSessionAsync(client, ChangedFile, syncStateTable, log);
                    log.Info($"File {ChangedFile} is using sessionId: {sessionId}");
                    await ScanExcelFileForPlaceholdersAsync(client, ChangedFile, sessionId, log);
                }
                catch (Exception ex)
                {
                    log.Info($"Exception processing file: {ex.Message}");
                    result = false;
                }
            }
            return result;
        }*/

        public static async Task<bool> AddFilesToStorageTableIfNeeded(CloudTable FilesMonitorTable, string subscriptionId, List<DriveItem> NewFileItems, TraceWriter log)
        {
            bool result = true;
            //CloudTableControl FilesMonitorHistoryTable = new CloudTableControl(FilesTable, StorageConnentionString);


            foreach (DriveItem DriveItemFile in NewFileItems)
            {
                log.Info($"Adding file {DriveItemFile.Name}, file ID {DriveItemFile.Id}, from OneDrive to storage table.");
                FileEntity tmpFile = new FileEntity(subscriptionId, DriveItemFile.Id);
                //log.Info($"For debugging: Activated file entity");//TODO remove
                tmpFile.AddParametersFromDriveItem(DriveItemFile);
                //log.Info($"For debugging: Added parameters to file entity");//TODO remove
                ////////////////Testing:
                if (DriveItemFile.Content != null)//TODO remove
                {
                    log.Info($"(DriveItemFile.Content != null): {DriveItemFile.Content != null}");
                    using (var reader = new System.IO.StreamReader(DriveItemFile.Content))
                    {
                        string stam = reader.ReadToEnd();
                        log.Info($"Content is: {stam}");
                    }
                }
                //////////////////////
                TableEntity FindResult = await FileCloudTable.Find(FilesMonitorTable, subscriptionId, DriveItemFile.Id);
                if (FindResult == null)
                {
                    await FileCloudTable.Insert(FilesMonitorTable, tmpFile);
                    log.Info($"Added file from OneDrive to storage table: {tmpFile.FileId}");
                }
                else
                {
                    log.Info($"File is already in storage table: {tmpFile.FileId}");
                    result = false;
                }
            }
            return result;
        }

        /*public static async void AddFilesToStorageTableIfNeeded(CloudTable FilesMonitorTable, string subscriptionId, List<string> NewFileIds, TraceWriter log)
        {
            CloudTableControl FilesMonitorHistoryTable = new CloudTableControl(FilesTable, StorageConnentionString);
            foreach (string FileID in NewFileIds)
            {
                FileEntity tmpFile = new FileEntity(subscriptionId, FileID);
                if (FilesMonitorHistoryTable.FindEntityInTable(tmpFile) == null)
                {
                    log.Info($"Adding file to OneDrive: {FileID}");
                    FilesMonitorHistoryTable.Insert(tmpFile);
                }
                else
                {
                    log.Info($"File is already in OneDrive: {FileID}");
                }
            }
        }*/

        public static async Task<bool> RemoveFilesFromStorageTable(CloudTable FilesMonitorTable, string subscriptionId, List<string> RemoveFileIds, TraceWriter log)
        {
            bool result = true;

            foreach (string FileID in RemoveFileIds)
            {
                if (await FileCloudTable.Delete(FilesMonitorTable, subscriptionId, FileID) == false)
                {
                    log.Info($"File isn't in the table, can't delete: {FileID}");
                    result = false;
                }
                else
                {
                    log.Info($"File was found and removed: {FileID}");
                }
            }
            return result;
        }

        // Request the delta stream from OneDrive to find files that have changed between notifications for this account
        public static async Task<List<DriveItem>> FindChangedAnyFilesInOneDrive(CloudTable FilesMonitorTable, string subscriptionId, StoredSubscriptionState state, GraphServiceClient client, TraceWriter log)
        {
            const string DefaultDeltaToken = idaMicrosoftGraphUrl + "/v1.0/me/drive/root/delta?token=latest";

            // We default to reading the "latest" state of the drive, so we don't have to process all the files in the drive
            // when a new subscription comes in.
            string deltaUrl = DefaultDeltaToken;
            if (!String.IsNullOrEmpty(state.LastDeltaToken))
            {
                deltaUrl = state.LastDeltaToken;
            }

            const int MaxLoopCount = 50;

            List<DriveItem> changedFileDriveItems = new List<DriveItem>();
            List<string> DeletedFileIds = new List<string>();


            IDriveItemDeltaRequest request = new DriveItemDeltaRequest(deltaUrl, client, null);

            // Only allow reading 50 pages, if we read more than that, we're going to cancel out
            for (int loopCount = 0; loopCount < MaxLoopCount && request != null; loopCount++)
            {
                log.Info($"Making request for '{state.SubscriptionId}' to '{deltaUrl}' ");
                var deltaResponse = await request.GetAsync();

                log.Verbose($"Found {deltaResponse.Count} files changed in this page.");
                try
                {
                    // Find changed files of any type
                    IEnumerable<DriveItem> ChangedDriveItemFiles = (from f in deltaResponse
                                                                    where f.File != null && f.Name != null && f.Deleted == null
                                                                    select f);
                    log.Info($"Found {ChangedDriveItemFiles.Count()} changed OneDrive files in this page.");
                    changedFileDriveItems.AddRange(ChangedDriveItemFiles);

                    // Find deleted files and remove them from table
                    IEnumerable<string> DeletedSomeFiles = (from f in deltaResponse
                                                            where f.Id != null && f.Deleted != null
                                                            select f.Id);
                    log.Info($"Found {DeletedSomeFiles.Count()} deleted files in this page.");
                    DeletedFileIds.AddRange(DeletedSomeFiles);

                    // For debugging:
                    var AllChangedFiles = (from f in deltaResponse
                                           where f.File != null && f.Id != null
                                           select f);
                    log.Info($"Found {AllChangedFiles.Count()} files change in this page by doing SELECT.");
                }
                catch (Exception ex)
                {
                    log.Info($"Exception enumerating changed files: {ex.ToString()}");
                    throw;
                }

                //**** Eitan added:
                bool result = await RemoveFilesFromStorageTable(FilesMonitorTable, subscriptionId, DeletedFileIds, log);
                //**** Eitan end

                if (null != deltaResponse.NextPageRequest)
                {
                    request = deltaResponse.NextPageRequest;
                }
                else if (null != deltaResponse.AdditionalData["@odata.deltaLink"])
                {
                    string deltaLink = (string)deltaResponse.AdditionalData["@odata.deltaLink"];
                    log.Verbose($"All changes requested, nextDeltaUrl: {deltaLink}");
                    state.LastDeltaToken = deltaLink;
                    return changedFileDriveItems;
                }
                else
                {
                    request = null;
                }
            }

            // If we exit the For loop without returning, that means we read MaxLoopCount pages without finding a deltaToken
            log.Info($"Read through MaxLoopCount pages without finding an end. Too much data has changed.");
            state.LastDeltaToken = DefaultDeltaToken;

            return changedFileDriveItems;

        }

        // Request the delta stream from OneDrive to find files that have changed between notifications for this account
        /*public static async Task<List<string>> FindChangedExcelFilesInOneDrive(StoredSubscriptionState state, GraphServiceClient client, TraceWriter log)
        {
            const string DefaultDeltaToken = idaMicrosoftGraphUrl + "/v1.0/me/drive/root/delta?token=latest";

            // We default to reading the "latest" state of the drive, so we don't have to process all the files in the drive
            // when a new subscription comes in.
            string deltaUrl = DefaultDeltaToken;
            if (!String.IsNullOrEmpty(state.LastDeltaToken))
            {
                deltaUrl = state.LastDeltaToken;
            }

            const int MaxLoopCount = 50;
            List<string> changedFileIds = new List<string>();

            IDriveItemDeltaRequest request = new DriveItemDeltaRequest(deltaUrl, client, null);

            // Only allow reading 50 pages, if we read more than that, we're going to cancel out
            for (int loopCount = 0; loopCount < MaxLoopCount && request != null; loopCount++)
            {
                log.Info($"Making request for '{state.SubscriptionId}' to '{deltaUrl}' ");
                var deltaResponse = await request.GetAsync();

                log.Verbose($"Found {deltaResponse.Count} files changed in this page.");
                try
                {
                    var changedExcelFiles = (from f in deltaResponse
                                             where f.File != null && f.Name != null && f.Name.EndsWith(".xlsx") && f.Deleted == null
                                             select f.Id);
                    log.Info($"Found {changedExcelFiles.Count()} changed Excel files in this page.");
                    changedFileIds.AddRange(changedExcelFiles);
                }
                catch (Exception ex)
                {
                    log.Info($"Exception enumerating changed files: {ex.ToString()}");
                    throw;
                }


                if (null != deltaResponse.NextPageRequest)
                {
                    request = deltaResponse.NextPageRequest;
                }
                else if (null != deltaResponse.AdditionalData["@odata.deltaLink"])
                {
                    string deltaLink = (string)deltaResponse.AdditionalData["@odata.deltaLink"];
                    log.Verbose($"All changes requested, nextDeltaUrl: {deltaLink}");
                    state.LastDeltaToken = deltaLink;
                    return changedFileIds;
                }
                else
                {
                    request = null;
                }
            }

            // If we exit the For loop without returning, that means we read MaxLoopCount pages without finding a deltaToken
            log.Info($"Read through MaxLoopCount pages without finding an end. Too much data has changed.");
            state.LastDeltaToken = DefaultDeltaToken;

            return changedFileIds;

        }*/

        /// <summary>
        /// Ensure that we're working out of a shared session if multiple updates to a file are happening frequently.
        /// This improves performance and ensures consistency of the data between requests.
        /// </summary>
        /*public static async Task<string> StartOrResumeWorkbookSessionAsync(GraphServiceClient client, string fileId, CloudTable table, TraceWriter log)
        {
            const string userId = "1234";

            var fileItem = FileHistory.Open(userId, fileId, table);
            if (null == fileItem)
            {
                log.Info($"No existing Excel session found for file: {fileId}");
                fileItem = FileHistory.CreateNew(userId, fileId);
            }

            if (!string.IsNullOrEmpty(fileItem.ExcelSessionId))
            {
                // Verify session is still available
                TimeSpan lastUsed = DateTime.UtcNow.Subtract(fileItem.LastAccessedDateTime);
                if (lastUsed.TotalMinutes < 5)
                {
                    fileItem.LastAccessedDateTime = DateTime.UtcNow;
                    try
                    {
                        // Attempt to update the cache, but if we get a conflict, just ignore it
                        fileItem.Insert(table);
                    }
                    catch { }
                    log.Info($"Reusing existing session for file: {fileId}");
                    return fileItem.ExcelSessionId;
                }
            }

            string sessionId = null;
            try
            {
                // Create a new workbook session
                var session = await client.Me.Drive.Items[fileId].Workbook.CreateSession(true).Request().PostAsync();
                fileItem.LastAccessedDateTime = DateTime.UtcNow;
                fileItem.ExcelSessionId = session.Id;
                log.Info($"Reusing existing session for file: {fileId}");
                sessionId = session.Id;
            }
            catch { }

            try
            {
                fileItem.Insert(table);
            }
            catch { }

            return sessionId;
        }*/

        // Use the Excel REST API to look for queries that we can replace with real data
        /*public static async Task ScanExcelFileForPlaceholdersAsync(GraphServiceClient client, string fileId, string workbookSession, TraceWriter log)
        {
            const string SheetName = "Sheet1";

            var dataRequest = client.Me.Drive.Items[fileId].Workbook.Worksheets[SheetName].UsedRange().Request();
            if (null != workbookSession)
            {
                dataRequest.Headers.Add(new HeaderOption("workbook-session-id", workbookSession));
            }
            var data = await dataRequest.Select("address,cellCount,columnCount,values").GetAsync();

            var usedRangeId = data.Address;
            var sendPatch = false;
            dynamic range = data.Values;

            for (int rowIndex = 0; rowIndex < range.Count; rowIndex++)
            {
                var rowValues = range[rowIndex];
                for (int columnIndex = 0; columnIndex < rowValues.Count; columnIndex++)
                {
                    var value = (string)rowValues[columnIndex];
                    if (value.StartsWith($"{FileTriggerWord} "))
                    {
                        log.Info($"Found cell [{rowIndex},{columnIndex}] with value: {value} ");
                        rowValues[columnIndex] = await ReplacePlaceholderValueAsync(value);
                        sendPatch = true;
                    }
                    else
                    {
                        // Replace the value with null so we don't overwrite anything on the PATCH
                        rowValues[columnIndex] = null;
                    }
                }
            }

            if (sendPatch)
            {
                log.Info($"Updating file {fileId} with replaced values.");
                await client.Me.Drive.Items[fileId].Workbook.Worksheets[SheetName].Range(data.Address).Request().PatchAsync(data);
            }
        }*/
        // Make a request to retrieve a response based on the input value
        /*public static async Task<string> ReplacePlaceholderValueAsync(string inputValue)
        {
            // This is merely an example. A real solution would do something much richer
            if (inputValue.StartsWith($"{FileTriggerWord} ") && inputValue.EndsWith(" stock quote"))
            {
                // For demo purposes, return a random value instead of the stock quote
                Random rndNum = new Random(int.Parse(Guid.NewGuid().ToString().Substring(0, 8), System.Globalization.NumberStyles.HexNumber));
                return rndNum.Next(20, 100).ToString();
            }

            return inputValue;
        }*/
    }
}
