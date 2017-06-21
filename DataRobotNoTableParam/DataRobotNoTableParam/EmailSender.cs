using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using static DataRobotNoTableParam.DataRobotNoTableParamFunction;

namespace DataRobotNoTableParam
{
    public static class EmailSender
    {
        private const string DetactionEmailSubject = "Ransomware attack detaction";//Your Email Headling.
        private const string DetactionEmailBody = "Hello {0},\nWe have detacted a ransomware attack on you Onedrive account. We recommend you disconnect your OneDrive imediately.";//Your Email Body

        public static void SendToTwoUsers(StoredSubscriptionState userCreatedMail, StoredSubscriptionState userModifiedMail)
        {
            if (userCreatedMail.Email != userModifiedMail.Email)
            {
                Send(userCreatedMail);
                Send(userModifiedMail);
            }
            else Send(userCreatedMail);

        }
        private static void Send(StoredSubscriptionState userEntity)
        {
            var mail = new MailMessage
            {
                From = new MailAddress(ServiceGmail),
                Subject = DetactionEmailSubject,
                Body = String.Format(DetactionEmailBody, " user")
            };
            mail.To.Add(userEntity.Email);

            var smtpServer = new SmtpClient(SmtpAddress);
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential(GmailUserName, GmailPassword);
            smtpServer.EnableSsl = true;

            smtpServer.Send(mail);

        }
    }
}
