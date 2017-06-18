using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OneDriveDataRobot.Utils
{
    public class UserSubscriptionState : StoredSubscriptionState
    {
        public UserSubscriptionState()
        {
            this.PartitionKey = "PartKey";
        }
        internal new void Delete(CloudTable syncStateTable)
        {
            try
            {
                TableOperation remove = TableOperation.Delete(this);
                syncStateTable.Execute(remove);
            }
            catch { }
        }
        public new static UserSubscriptionState CreateNew(string subscriptionId)
        {
            var newState = new UserSubscriptionState();
            if (newState.PartitionKey == null)
            {
                newState.PartitionKey = "PartKey";
            }
            newState.RowKey = subscriptionId;
            newState.SubscriptionId = subscriptionId;
            newState.SubscriptionID = subscriptionId;
            return newState;
        }
        public new void Insert(CloudTable table)
        {
            TableOperation insert = TableOperation.InsertOrReplace(this);
            table.Execute(insert);
        }

        public new static UserSubscriptionState FindUser(string SignInUserId, CloudTable table)
        {
            try
            {
                var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PartKey");
                var userIdFilter = TableQuery.GenerateFilterCondition("SignInUserId", QueryComparisons.Equal, SignInUserId);
                string filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, userIdFilter);

                var query = new TableQuery<UserSubscriptionState>().Where(filter).Take(1);
                var matchingEntry = table.ExecuteQuery(query).FirstOrDefault();
                return matchingEntry;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while finding existing user subscription: {ex.Message}.");
            }
            return null;
        }
        public void AddAllFieldsFromUser(Microsoft.Graph.User newUser)
        {
            this.Email = newUser.Mail;
            this.FirstName = newUser.GivenName;
            this.LastName = newUser.Surname;
        }

        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SubscriptionID { get; set; }
        public string WebhookID { get; set; }
        public string AccessToken { get; set; }
        public string AuthenticationToken { get; set; }
        /*public static UserSubscriptionState FindUser(string userId, CloudTable table)
        {
            try
            {
                var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PartKey");
                var userIdFilter = TableQuery.GenerateFilterCondition("SignInUserId", QueryComparisons.Equal, userId);
                string filter = TableQuery.CombineFilters(partitionFilter, TableOperators.And, userIdFilter);

                var query = new TableQuery<UserSubscriptionState>().Where(filter).Take(1);
                var matchingEntry = table.ExecuteQuery(query).FirstOrDefault();
                return matchingEntry;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error while finding existing user subscription: {ex.Message}.");
            }
            return null;
        }*/
        /*public static UserSubscriptionState CreateNew(string subscriptionId)
        {
            var newState = new UserSubscriptionState();
            newState.RowKey = subscriptionId;
            newState.SubscriptionID = subscriptionId;
            return newState;
        }*/


    }
}