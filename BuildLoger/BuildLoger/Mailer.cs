using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Mail;

namespace Clysar.Common
{

    using System.Configuration;

    public class Mailer 
    {
        public class Mail
        {
            public string Subject { get; set; }
            public string Body { get; set; }

            public Mail() 
            { 
                Subject = String.Empty;
                Body = String.Empty;
            }

            public Mail(string msg)
            {
                Body = msg;
                Subject = String.Empty;
            }

            public Mail(string sub, string msg)
            {
                Subject = sub;
                Body = msg;
            }
        }

        public class MailClient : SmtpClient
        {
            public MailClient(string server, int port) : base(server, port)
            {
                base.EnableSsl = true;
                base.UseDefaultCredentials = false;
            }

            public void SetCredentials(string usr, string pwd){

                base.Credentials = new System.Net.NetworkCredential(usr, pwd);
            }
        }

        Mail mail;
        //ILogger logger;

        public Mailer()
        {
            mail = new Mail();
            //logger = log;
        }

        #region Compose Mail

        public void ComposeMail(string sub, string msg)
        {
            mail = new Mail(sub, msg);
        }

        #endregion

        #region Send Mail

        public void SendMail(string fromAddr, string[] toAddr, int port, string server, string user, string pass)
        {            
                try
                {
                    using (MailClient client = new MailClient(server, port))
                    {
                        MailAddress from = new MailAddress(fromAddr, String.Empty, System.Text.Encoding.UTF8);
                        
                        using (MailMessage message = new MailMessage())
                        {
                            message.From = from;
                            foreach (var add in toAddr)
                            {
                                message.To.Add(new MailAddress(add));
                            }
                            message.Body = mail.Body;
                            message.Subject = mail.Subject;
                            message.IsBodyHtml = true;

                            message.BodyEncoding = System.Text.Encoding.UTF8;
                            message.SubjectEncoding = System.Text.Encoding.UTF8;

                            client.EnableSsl = true;
                            client.SetCredentials(user, pass);
                            client.Send(message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message + mail.Body);
                }
        }

      
        #endregion

    }
}
