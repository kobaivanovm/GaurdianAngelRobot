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
    public class FuntionsResults
    {
        public List<FileEntity> FilesList;
        public bool IsBeingAttacked;
        public FuntionsResults()
        {
            FilesList = new List<FileEntity>();
        }
    }
    public static class FileMonitorOneDrive
    {
        public static async Task<bool> ProccessChangesInFiles(string UserSubscriptionID, CloudTable UsersTable, CloudTable FilesMonitorTable, GraphServiceClient client, DataRobotNoTableParamFunction.StoredSubscriptionState state, string subscriptionId, CloudTable syncStateTable, CloudTable tokenCacheTable, TraceWriter log)
        {            
            bool result = true;

            List<DriveItem> ChangedFilesDriveItems;
            List<DriveItem> HarmedDriveItems = new List<DriveItem>();
            List<DriveItem> UnrelatedDriveItems = new List<DriveItem>();

            // Query for items that have changed since the last notification was received
            ChangedFilesDriveItems = await FindChangedAnyFilesInOneDrive(FilesMonitorTable, subscriptionId, state, client, log);

            // Update our saved state for this subscription
            state.Insert(syncStateTable);

            // Get list of required files:
            FileTypesList filesTypes = new FileTypesList();
            List<string> TypeList = filesTypes.GetListOfFiles();
            List<string> RansomwareExtensions = filesTypes.GetListOfRansomwares();

            // Go over files and sort them:
            foreach (DriveItem item in ChangedFilesDriveItems)
            {
                string FileExtension = Path.GetExtension(item.Name);
                if (RansomwareExtensions.Contains(FileExtension))
                {
                    log.Info($"This might be encrypted ecrypted by a ransomware!!!");
                    // This file is suspected to be ransomware encrypted:
                    HarmedDriveItems.Add(item);
                    log.Info($"For debugging 1");
                    continue;
                }
                else if (!(TypeList.Contains(FileExtension)))
                {
                    log.Info($"File {item.Name} is not of known type.");
                    UnrelatedDriveItems.Add(item);
                }
            }

            if (HarmedDriveItems.Count > 0)
            {
                // Do work on suspected files to be encrypted. Do not add them to storage!
                log.Info($"For debugging 3");
                bool IsBeingAttacked = await DoWorkOnSuspectedFiles(HarmedDriveItems, log);
                log.Info($"For debugging 4");
                if (IsBeingAttacked == true)
                {
                    log.Info($"For debugging 5");
                    // Do something to stop it.
                    log.Info($"HarmedDriveItems size: {HarmedDriveItems.Count}");
                    log.Info($"ChangedFilesDriveItems size: {ChangedFilesDriveItems.Count}");
                    //DriveItem tmp = HarmedDriveItems.FirstOrDefault();
                    //DriveItem tmp = ChangedFilesDriveItems.First(x => x.Deleted == null);
                    /*DriveItem tmp = HarmedDriveItems.First(x => x.Deleted == null);
                    log.Info($"tmp ID: {tmp.Id}");
                    log.Info($"For debugging- tmp.CreatedBy.User.Id is null => {tmp == null}");
                    log.Info($"For debugging- tmp.CreatedBy is null => {tmp.CreatedBy == null}");
                    log.Info($"For debugging- tmp.CreatedBy.User is null => {tmp.CreatedBy.User == null}");
                    log.Info($"For debugging- tmp.CreatedBy.User.Id is null => {tmp.CreatedBy.User.Id == null}");
                    log.Info($"For debugging- tmp.CreatedBy.User.DisplayName is null => {tmp.CreatedBy.User.DisplayName == null}");
                    log.Info($"For debugging- tmp.CreatedBy.User.DisplayName => {tmp.CreatedBy.User.DisplayName}");
                    log.Info($"For debugging- tmp.CreatedBy.User.Id is => {tmp.CreatedBy.User.Id}");
                    log.Info($"For debugging- tmp.LastModifiedBy is null => {(tmp.LastModifiedBy == null)}");
                    log.Info($"For debugging- tmp.LastModifiedBy.User is null => {(tmp.LastModifiedBy.User == null)}");
                    log.Info($"For debugging- tmp.LastModifiedBy.User.Id is null => {(tmp.LastModifiedBy.User.Id == null)}");
                    log.Info($"For debugging- tmp.LastModifiedBy.User.Id is  => {(tmp.LastModifiedBy.User.Id)}");*/
                    //LocalUserEntity entityCreatedBy = await LocalUserEntity.FindLocalUser(UsersTable, UserSubscriptionID);
                    //LocalUserEntity entityLastModifiedBy = await LocalUserEntity.FindLocalUser(UsersTable, UserSubscriptionID);
                    StoredSubscriptionState entityCreatedBy = StoredSubscriptionState.FindUser(UserSubscriptionID, UsersTable);
                    log.Info($"For debugging- UserSubscriptionID: {UserSubscriptionID}");
                    log.Info($"For debugging- entityCreatedBy is null =>: {(entityCreatedBy == null)}");
                    //LocalUserEntity entityLastModifiedBy = UserSubscriptionState.FindUser(UserSubscriptionID, UsersTable);

                    //TableEntity entityCreatedBy = await FileCloudTable.Find(UsersTable, "PartKey", tmp.CreatedBy.User.Id);
                    //TableEntity entityLastModifiedBy = await FileCloudTable.Find(UsersTable, "PartKey", tmp.LastModifiedBy.User.Id);
                    log.Info($"For debugging- is tmp null: {(entityCreatedBy == null || entityCreatedBy == null)}");
                    EmailSender.SendToTwoUsers(entityCreatedBy, entityCreatedBy);
                    //SendEmailToUser(entityCreatedBy, entityLastModifiedBy, log);
                    log.Info($"For debugging 6");
                }

                // Remove suspected items from list:
                foreach (DriveItem item in HarmedDriveItems)
                {
                    log.Info($"For debugging 2");
                    // Intentionally another foreach to avoid exceptions.
                    ChangedFilesDriveItems.Remove(item);
                }
            }

            // Remove unrelated files
            foreach (DriveItem item in UnrelatedDriveItems)
            {
                ChangedFilesDriveItems.Remove(item);
            }

            // Do work on changed files such as security checks:
            FuntionsResults Results = await DoWorkOnChangedFiles(client, FilesMonitorTable, ChangedFilesDriveItems, subscriptionId, log);
            if (Results.IsBeingAttacked == true)
            {
                // Do something to stop it.
                DriveItem tmp = ChangedFilesDriveItems.FirstOrDefault(x => x.Deleted == null);
                //LocalUserEntity entityCreatedBy = await LocalUserEntity.FindLocalUser(UsersTable, tmp.CreatedBy.User.Id);
                //LocalUserEntity entityLastModifiedBy = await LocalUserEntity.FindLocalUser(UsersTable, tmp.CreatedBy.User.Id);
                //EmailSender.SendToTwoUsers(entityCreatedBy, entityLastModifiedBy);
                //SendEmailToUser(ChangedFilesDriveItems.FirstOrDefault<DriveItem>(), log);
            }

            // Add to storage if needed
            bool WereFiledAdded = await AddFilesToStorageTableIfNeeded(client, FilesMonitorTable, subscriptionId, Results.FilesList, log);

            return result;
        }

        public static void SendEmailToUser(LocalUserEntity lastModifiedBy, LocalUserEntity createdBy, TraceWriter log)
        {
            //User lastModifiedByUser
            /*if (item == null)
            {
                log.Info($"For debugging: item is null");
            }
            else
            {
                if (item.CreatedBy == null)
                {
                    log.Info($"For debugging: CreatedByUser is null");
                }
                else
                {
                    if (item.CreatedBy == null)
                    {
                        log.Info($"For debugging: CreatedByUser.Mail is null");
                    }
                }
                if (item.LastModifiedByUser == null)
                {
                    log.Info($"For debugging: LastModifiedByUser is null");
                }
                else
                {
                    if (item.LastModifiedByUser.Mail == null)
                    {
                        log.Info($"For debugging: LastModifiedByUser.Mail is null");
                    }
                }
            }*/
            //EmailSender.SendToTwoUsers(CreatedByUser, ModifiedByUser);
        }

        public static async Task<FuntionsResults> DoWorkOnChangedFiles(GraphServiceClient client, CloudTable FilesMonitorTable, List<DriveItem> ChangedFilesList, string subscriptionId, TraceWriter log)
        {
            //bool IsAttackInPlace = false;
            int FileIsSuspicious = 0;

            long? SuspectsNumber = null;

            FuntionsResults Results = new FuntionsResults();

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
                        entity = new FileEntity(subscriptionId, item.Id);
                        entity.AddParametersFromDriveItem(item);
                        entity.IsNew = true;
                    }
                    else if (((entity.RowKey == item.Id) && (entity.PartitionKey != subscriptionId)) ||
                        ((entity.PartitionKey == subscriptionId) && (entity.RowKey != item.Id)))
                    {
                        log.Info($"** Something funky is going on **");
                        continue;
                    }

                    // Derive the file content:
                    Stream content;
                    if (item.Content == null)
                    {
                        log.Info($"item.Content is null.");
                        content = await client.Me.Drive.Items[item.Id].Content.Request().GetAsync();
                    }
                    else
                    {
                        log.Info($"item.Content is not null, so use it.");
                        content = item.Content;
                    }
                    if (content == null)
                    {
                        log.Info($"Content is still null even though we retrieved it. Very strange. File name is {item.Name}");
                        continue;
                    }

                    // Check if the file is honeypot, if so was it changed:
                    log.Info($"Checking Honeyppt things:");
                    bool IsWorthHoney = IsHoneypot(item);
                    if (IsWorthHoney == true)
                    {
                        if (HasHoneypotChanged(item, log) == true)
                        {
                            log.Info($"Honeypot file was changed. An attack is underway.");
                            //IsAttackInPlace = true;
                            Results.IsBeingAttacked = true;
                            break;
                        }
                        else
                        {
                            log.Info($"Honeypot file thought to bechanged, but it seems everything is alright.");
                            continue;
                        }
                    }

                    // Check if the magic number is legall:
                    log.Info($"Checking magic number:");
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
                        continue;
                        //maby break if not only suspicious.
                    }

                    double entropy = (-1);//default value.
                    double previousEntropy = (-1);//default value.

                    log.Info($"File is of size: {item.Size}");
                    // Check if file is too small to check.
                    if (item.Size < MinFileSize)
                    {
                        log.Info($"Size is too small");
                        continue;
                    }

                    byte[] bytesContent = ReadFully(content, log);

                    if (item.Size > MaxFileSize)//For debugging. Weird size problem
                    {
                        log.Info($"Size is too large.");
                        // Check only a small part of file.
                        //log.Info($"For debugging: content Stream Size: {content.Length}");//TODO remove
                        int ContentSize = bytesContent.Length;
                        //log.Info($"For debugging: Content Size: {ContentSize}");//TODO remove
                        int FileIntervals = ContentSize / 16;
                        //log.Info($"For debugging: File Intervals: {FileIntervals}");//TODO remove
                        byte[] firstBytes = new byte[FileIntervals];
                        Array.Copy(bytesContent, 0, firstBytes, 0, FileIntervals);
                        byte[] secondBytes = new byte[FileIntervals];
                        Array.Copy(bytesContent, ContentSize/4, secondBytes, 0, FileIntervals);
                        byte[] MidBytes = new byte[FileIntervals];
                        Array.Copy(bytesContent, ContentSize/2, MidBytes, 0, FileIntervals);
                        byte[] LastBytes = new byte[FileIntervals];
                        Array.Copy(bytesContent, ((3 * ContentSize) / 4), LastBytes, 0, FileIntervals);
                        double firstPartEntropy = EntropyCalculator.Entropy(firstBytes);
                        double secondPartEntropy = EntropyCalculator.Entropy(secondBytes);
                        double MidPartEntropy = EntropyCalculator.Entropy(MidBytes);
                        double LastPartEntropy = EntropyCalculator.Entropy(LastBytes);
                        entropy = ((firstPartEntropy + secondPartEntropy + MidPartEntropy + LastPartEntropy) / 4);
                    }
                    if (entity != null)
                    {
                        log.Info($"Check file statistics");
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
                    log.Info($"File {item.Name} has entropy: {entropy}.");

                    // Update file's entropy and magic:
                    entity.FileMagic = magic;
                    entity.Entropy = entropy;
                    Results.FilesList.Add(entity);

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
                            //IsAttackInPlace = true;
                            Results.IsBeingAttacked = true;
                            break;
                        }
                        else if (FileIsSuspicious == 3)
                        {
                            log.Info($"Found 3 suspicious attributes. Assume an attacks is underway.");
                            // An attack is under way
                            //IsAttackInPlace = true;
                            Results.IsBeingAttacked = true;
                            break;
                        }
                        SuspectsNumber++;
                        bool InsertResult = await FileCloudTable.InsertSuspectsNumber(FilesMonitorTable, subscriptionId, SuspectsNumber);
                        log.Info($"For debugging: tried to insert");
                        if (SuspectsNumber >= MaxNumberOfSuspects)
                        {
                            //IsAttackInPlace = true;
                            Results.IsBeingAttacked = true;
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    log.Info($"Exception processing file: {ex.Message}");
                    //IsAttackInPlace = false;
                }
            }
            if (SuspectsNumber >= MaxNumberOfSuspects)
            {
                // An attack is under way
                //needed outside of loop too
                //IsAttackInPlace = true;
                Results.IsBeingAttacked = true;
            }
            return Results;
        }

        public static async Task<bool> DoWorkOnSuspectedFiles(List<DriveItem> SuspectedFilesList, TraceWriter log)
        {
            var inspector = new FileFormatInspector();
            foreach (DriveItem item in SuspectedFilesList)
            {
                if (item.Content == null)
                {
                    continue;
                }
                else
                {
                    var format = inspector.DetermineFileFormat(item.Content);
                    if (format == null)
                    {
                        return false;
                    }
                }
            }
            //Send Email to user or shut OneDrive down.

            return true;
        }

        public static bool IsHoneypot(DriveItem item)
        {
            // Check if honey pot. return true if so.
            return (item.Name.Contains("honeypot"));
        }
        public static bool HasHoneypotChanged(DriveItem item, TraceWriter log)
        {
            // Check if honey pot is changed since previously known.
            bool result = false;
            if (item.Content == null)
            {
                log.Info($"Honeypot content is null. it's name: {item.Name}");
            }
            else
            {
                byte[] bytesContent = ReadFully(item.Content, log);
                double entropy = EntropyCalculator.Entropy(bytesContent);
                result = EntropyValue.IsFileEncrypted(entropy);
            }
            return result;
        }
        public static byte[] ReadFully(Stream input, TraceWriter log)    
        {
            //log.Info($"For debugging in ReadFully: Stream size is: {input.Length}");//TODO remove
            input.Position = 0;
            byte[] buffer = new byte[input.Length];
            for (int totalBytesCopied = 0; totalBytesCopied < input.Length;)
                totalBytesCopied += input.Read(buffer, totalBytesCopied, Convert.ToInt32(input.Length) - totalBytesCopied);
            //log.Info($"For debugging: buffer size is: {buffer.Length}");//TODO remove
            return buffer;
        }

        public static async Task<bool> AddFilesToStorageTableIfNeeded(GraphServiceClient client, CloudTable FilesMonitorTable, string subscriptionId, List<FileEntity> NewFileItems, TraceWriter log)
        {
            bool result = true;


            foreach (FileEntity entity in NewFileItems)
            {
                log.Info($"Adding file {entity.Name}, file ID {entity.FileId}, from OneDrive to storage table.");

                TableEntity FindResult = await FileCloudTable.Find(FilesMonitorTable, subscriptionId, entity.FileId);
                if (FindResult == null)
                {
                    await FileCloudTable.Insert(FilesMonitorTable, entity);
                    log.Info($"Added file from OneDrive to storage table: {entity.FileId}");
                }
                else
                {
                    log.Info($"File is already in storage table: {entity.FileId}. Updating it's version.");
                    await FileCloudTable.InsertOrReplace(FilesMonitorTable, entity);
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
        public static async Task<List<DriveItem>> FindChangedAnyFilesInOneDrive(CloudTable FilesMonitorTable, string subscriptionId, DataRobotNoTableParamFunction.StoredSubscriptionState state, GraphServiceClient client, TraceWriter log)
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

            //List<DriveItem> tmpList = new List<DriveItem>();//TODO remove

            IDriveItemDeltaRequest request = new DriveItemDeltaRequest(deltaUrl, client, null);

            // Only allow reading 50 pages, if we read more than that, we're going to cancel out
            for (int loopCount = 0; loopCount < MaxLoopCount && request != null; loopCount++)
            {
                log.Info($"Making request for '{state.SubscriptionId}' to '{deltaUrl}' ");
                var deltaResponse = await request.GetAsync();

                log.Verbose($"Found {deltaResponse.Count} files changed in this page.");
                try
                {
                    /////////////////////////////////////
                    /*IEnumerable<DriveItem> FilesWithID = (from f in deltaResponse
                                                                    where f.File != null && f.CreatedBy.User.Id != null && f.Deleted == null
                                                                    select f);
                    log.Info($"Found {tmpList.Count()} tmpList OneDrive files in this page.");
                    tmpList.AddRange(tmpList);*/
                    /////////////////////////////////////

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

                //DriveItem tmp = tmpList.First(x => x.Deleted == null);
                /*log.Info($"Debugging 2");
                DriveItem tmp = tmpList[0];
                log.Info($"tmp ID: {tmp.Id}");
                log.Info($"For debugging- tmp.CreatedBy.User.Id is null => {tmp == null}");
                log.Info($"For debugging- tmp.CreatedBy is null => {tmp.CreatedBy == null}");
                log.Info($"For debugging- tmp.CreatedBy.User is null => {tmp.CreatedBy.User == null}");
                log.Info($"For debugging- tmp.CreatedBy.User.Id is null => {tmp.CreatedBy.User.Id == null}");
                log.Info($"For debugging- tmp.CreatedBy.User.DisplayName is null => {tmp.CreatedBy.User.DisplayName == null}");
                log.Info($"For debugging- tmp.CreatedBy.User.DisplayName => {tmp.CreatedBy.User.DisplayName}");
                log.Info($"For debugging- tmp.CreatedBy.User.Id is => {tmp.CreatedBy.User.Id}");
                log.Info($"For debugging- tmp.LastModifiedBy is null => {(tmp.LastModifiedBy == null)}");
                log.Info($"For debugging- tmp.LastModifiedBy.User is null => {(tmp.LastModifiedBy.User == null)}");
                log.Info($"For debugging- tmp.LastModifiedBy.User.Id is null => {(tmp.LastModifiedBy.User.Id == null)}");
                log.Info($"For debugging- tmp.LastModifiedBy.User.Id is  => {(tmp.LastModifiedBy.User.Id)}");*/

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
