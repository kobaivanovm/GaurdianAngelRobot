using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.IO;
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
        private const string DetactionEmailSubject = "Ransomware Attack Detection by Guardian Angel Service";//Your Email Headline.
        private const string DetactionEmailBody = "Hello {0},\nWe have detacted a ransomware attack on you Onedrive account. We recommend you disconnect your OneDrive imediately.\n" +
            "Download this file and run it imediately: " + OneDriveDisconnetScript + " \n" +
            "First press the link, on the newly open window press \"Download\".\n" +
            "Open the file. If promted with \"Windows protected your PC\", select \"More info\", then \"Run Anyway\".";//Your Email Body

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
            string userName = " " + userEntity.FirstName + " " + userEntity.LastName;
            var mail = new MailMessage
            {
                From = new MailAddress(ServiceGmail),
                Subject = DetactionEmailSubject,
                Body = String.Format(DetactionEmailBody, userName)
            };
            mail.To.Add(userEntity.Email);

            var smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential(GmailUserName, GmailPassword);
            smtpServer.EnableSsl = true;

            smtpServer.Send(mail);

        }
    }
}
