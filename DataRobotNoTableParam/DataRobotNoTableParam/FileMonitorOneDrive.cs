using FileSignatures;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Graph;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
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
            List<DriveItem> UnrelatedDriveItems = new List<DriveItem>();

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
                string FileExtension = Path.GetExtension(item.Name);
                log.Info($"For debugging: FileExtension is {FileExtension}");//TODO remove
                if (RansomwareExtensions.Contains(FileExtension))
                {
                    log.Info($"For debugging: This is in ecrypted by a ransomware!!!");//TODO remove
                    // This file is suspected to be ransomware encrypted:
                    HarmedDriveItems.Add(item);
                    //ChangedFilesDriveItems.Remove(item);
                }
                else if (!TypeList.Contains(FileExtension))
                {
                    log.Info($"File {item.Name} is not of known type.");
                    UnrelatedDriveItems.Add(item);
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

            // Remove unrelated files
            foreach (DriveItem item in UnrelatedDriveItems)
            {
                ChangedFilesDriveItems.Remove(item);
            }

            // Do work on changed files such as security checks:
            ////////////////Should be here, not yet///////////////////////////
            IsBeingAttacked = await DoWorkOnChangedFiles(client, FilesMonitorTable, ChangedFilesDriveItems, subscriptionId, log);
            if (IsBeingAttacked == true)
            {
                // Do something to stop it.
            }

            // Add to storage if needed
            bool WereFiledAdded = await AddFilesToStorageTableIfNeeded(client, FilesMonitorTable, subscriptionId, ChangedFilesDriveItems, log);

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

        public static async Task<bool> DoWorkOnChangedFiles(GraphServiceClient client, CloudTable FilesMonitorTable, List<DriveItem> ChangedFilesList, string subscriptionId, TraceWriter log)
        {
            bool IsAttackInPlace = false;
            int FileIsSuspicious = 0;

            long? SuspectsNumber = null;

            // Do work on the changed files
            foreach (DriveItem item in ChangedFilesList)
            {
                FileIsSuspicious = 0;
                log.Info($"Processing changes in file: {item.Id}");
                try
                {
                    log.Info($"Doing security checks on file: {item.Name}");
                    FileEntity entity = await FileCloudTable.FindFile(FilesMonitorTable, subscriptionId, item.Id);
                    if (entity == null)
                    {
                        log.Info($"This is a new file");
                        // File is not in table (a new file)
                    }
                    else if (((entity.RowKey == item.Id) && (entity.PartitionKey != subscriptionId)) || 
                        ((entity.PartitionKey == subscriptionId) && (entity.RowKey != item.Id)))
                    {
                        log.Info($"** Something funky is going on **");
                        continue;
                    }
                    log.Info($"Checking magic number");

                    // Derive the file content:
                    Stream content;
                    if (item.Content == null)
                    {
                        content = await client.Me.Drive.Items[item.Id].Content.Request().GetAsync();
                    }
                    else
                    {
                        content = item.Content;
                    }
                    if (content == null)
                    {
                        log.Info($"Content is still null even though we retrieved it. Very strange. File name is {item.Name}");
                        continue;
                    }

                    // Check if the file is honeypot, if so was it changed:
                    bool IsWorthHoney = IsHoneypot(item);
                    if (IsWorthHoney == true)
                    {
                        if (HasHoneypotChanged(item, IsWorthHoney) == true)
                        {
                            log.Info($"Honeypot file was changed. An attack is underway.");
                            IsAttackInPlace = true;
                            break;
                        }
                        else
                        {
                            log.Info($"Honeypot file thought to bechanged, but it seems everything is alright.");
                        }
                    }

                    // Check if the magic number is legall:
                    var inspector = new FileFormatInspector();
                    var format = inspector.DetermineFileFormat(content);
                    var magic = format.Extension;
                    var extension = Path.GetExtension(item.Name);
                    if (magic == extension.Substring(1))//ignore dot
                    {
                        log.Info($"Both magic and extension are: {extension}");
                    }
                    else
                    {
                        log.Info($"File magic is {magic}, while file extension is {extension}. Suspicious!");
                        FileIsSuspicious++;
                        break;
                        //maby continue if only suspicious.
                    }

                    double entropy = (-1);//default value.
                    double previousEntropy = (-1);//default value.

                    byte[] bytesContent = ReadFully(content);

                    log.Info($"File is of size: {item.Size}");
                    // Check if file is too small to check.
                    if (item.Size < MinFileSize)
                    {
                        log.Info($"Size is too small");
                        continue;
                    }
                    //if (item.Size > MaxFileSize)
                    else if (item.Size > (MaxFileSize^2))//For debugging. Weird size problem
                    {
                        log.Info($"Size is too large");
                        // Check only a small part of file.
                        int ContentSize = bytesContent.Length;
                        int FileIntervals = ContentSize/12;
                        byte[] firstBytes = new byte[MaxProccessingSize];
                        Array.Copy(bytesContent, ContentSize/4, firstBytes, 0, FileIntervals);
                        byte[] MidBytes = new byte[MaxProccessingSize];
                        Array.Copy(bytesContent, ContentSize/2, MidBytes, 0, FileIntervals);
                        byte[] LastBytes = new byte[MaxProccessingSize];
                        Array.Copy(bytesContent, ((3*ContentSize)/4), LastBytes, 0, FileIntervals);
                        double firstPartEntropy = EntropyCalculator.Entropy(firstBytes);
                        double MidPartEntropy = EntropyCalculator.Entropy(MidBytes);
                        double LastPartEntropy = EntropyCalculator.Entropy(LastBytes);
                        entropy = ((firstPartEntropy + MidPartEntropy + LastPartEntropy)/3);
                    }
                    if (entity != null)
                    {
                        // Check statistical match between existing version and new version.
                        previousEntropy = entity.Entropy;
                        if (previousEntropy < entropy + 0.5 || previousEntropy > entropy + 2 || 
                            Path.GetExtension(item.Name) != Path.GetExtension(entity.Name) ||
                            (entity.FileMagic != null && entity.FileMagic != magic))
                        {
                            FileIsSuspicious++;
                        }
                    }

                    // Check entropy of file is legal.
                    if (entropy < 0)
                    {
                        entropy = EntropyCalculator.Entropy(bytesContent);
                    }
                    if (EntropyValue.IsFileEncrypted(entropy))
                    {
                        FileIsSuspicious++;
                    }

                    // Check if file was found to suspicious.
                    if (FileIsSuspicious > 0)
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
                            break;
                        }
                        else if (FileIsSuspicious == 3)
                        {
                            log.Info($"Found 3 suspicious attributes. Assume an attacks is underway.");
                            // An attack is under way
                            IsAttackInPlace = true;
                            break;
                        }
                        SuspectsNumber++;
                        bool InsertResult = await FileCloudTable.InsertSuspectsNumber(FilesMonitorTable, subscriptionId, SuspectsNumber);
                        log.Info($"For debugging: tried to insert");
                        if (SuspectsNumber >= MaxNumberOfSuspects)
                        {
                            IsAttackInPlace = true;
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    log.Info($"Exception processing file: {ex.Message}");
                    IsAttackInPlace = false;
                }
            }
            if (SuspectsNumber >= MaxNumberOfSuspects)
            {
                // An attack is under way
                //needed outside of loop too
                IsAttackInPlace = true;
            }
            return IsAttackInPlace;
        }

        public static bool IsHoneypot(DriveItem item)
        {
            // Check if honey pot. return true if so.
            return false;
        }
        public static bool HasHoneypotChanged(DriveItem item, bool IsWorthHoney)
        {
            // Check if honey pot is changed since previously known.
            if (IsWorthHoney == false)
            {
                return false;
            }
            return false;
        }
        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static async Task<bool> AddFilesToStorageTableIfNeeded(GraphServiceClient client, CloudTable FilesMonitorTable, string subscriptionId, List<DriveItem> NewFileItems, TraceWriter log)
        {
            bool result = true;


            foreach (DriveItem DriveItemFile in NewFileItems)
            {
                log.Info($"Adding file {DriveItemFile.Name}, file ID {DriveItemFile.Id}, from OneDrive to storage table.");
                FileEntity tmpFile = new FileEntity(subscriptionId, DriveItemFile.Id);
                //log.Info($"For debugging: Activated file entity");//TODO remove
                tmpFile.AddParametersFromDriveItem(DriveItemFile);
                //log.Info($"For debugging: Added parameters to file entity");//TODO remove
                
                // Get magic number:
                Stream content = DriveItemFile.Content;
                if (content != null)//TODO remove
                {
                    ////////////////Testing:
                    log.Info($"(DriveItemFile.Content != null): {DriveItemFile.Content != null}");
                    using (var reader = new System.IO.StreamReader(DriveItemFile.Content))
                    {
                        string stam = reader.ReadToEnd();
                        log.Info($"Content is: {stam}");
                    }
                    //////////////////////
                }
                else
                {
                    content = await client.Me.Drive.Items[DriveItemFile.Id].Content.Request().GetAsync();
                    var inspector = new FileFormatInspector();
                    var format = inspector.DetermineFileFormat(content);
                    tmpFile.FileMagic = format.Extension;
                }
                
                TableEntity FindResult = await FileCloudTable.Find(FilesMonitorTable, subscriptionId, DriveItemFile.Id);
                if (FindResult == null)
                {
                    await FileCloudTable.Insert(FilesMonitorTable, tmpFile);
                    log.Info($"Added file from OneDrive to storage table: {tmpFile.FileId}");
                }
                else
                {
                    log.Info($"File is already in storage table: {tmpFile.FileId}. Updating it's version.");
                    await FileCloudTable.InsertOrReplace(FilesMonitorTable, tmpFile);
                    result = false;
                }
            }
            return result;
        }

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
    }
}
