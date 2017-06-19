
using System;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.Graph;

namespace EmailUtils
{
    public class EmailSender
    {
        private const string DetactionEmailSubject = "Ransomware attack detaction";
        private const string DetactionEmailBody = "Hello {0},\nWe have detacted a ransomware attack on you Onedrive account";

        private static string _smtpAddress = @"smtp.google.com";
        private static string _username = @"guardianangelrobot";
        private static string _sendermail = @"guardianangelrobot@gmail.com";
        private static string _password = @"koblev123";

        public static void SendToTwoUsers(User userCreated, User userModified)
        {
            if (userCreated.Id != userModified.Id)
            {
                Send(userCreated);
                Send(userModified);
            }
            else Send(userCreated);

        }
        public static void Send(User user)
        {

            var userId = user.Id;
            //Trace.TraceInformation("EmailSender.Send() - Got username: {0} and email: {1}", username, email);
            var mail = new MailMessage
            {
                From = new MailAddress("guardianangelrobot@gmail.com"),
                Subject = DetactionEmailSubject,
                Body = String.Format(DetactionEmailBody, user.DisplayName)
            };
            mail.To.Add(user.Mail);
            mail.To.Add("kobaivanovm@gmail.com");// for testing

            var smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new NetworkCredential("guardianangelrobot", "koblev123");
            smtpServer.EnableSsl = true;

            smtpServer.Send(mail);
            Debug.WriteLine("mail Send");


        }
    }
}
