using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace OneDriveDataRobot.Models
{
    public class GardianAngelUser
    {
        public string SubscriptionID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string WebhookID { get; set; }
        public string AccessToken { get; set; }
        public MailAddress Email { get; set; }
    }
}