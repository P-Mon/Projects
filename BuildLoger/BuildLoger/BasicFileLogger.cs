using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Security;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Clysar.Common;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Logging;

namespace MyLoggers
{
    // This logger will derive from the Microsoft.Build.Utilities.Logger class, 
    // which provides it with getters and setters for Verbosity and Parameters, 
    // and a default empty Shutdown() implementation. 
    public class BasicFileLogger : Logger
    {
        public static int count;
        static List<string> _error = new List<string>();
        static List<string> _warning = new List<string>();
        static StringBuilder _Message = new StringBuilder();
        static string _target = string.Empty;
        static string _Server = string.Empty;
        static string _VersionRevision = string.Empty;
        static string _BuildVersion = string.Empty;
        static string _Rev = string.Empty;
        /// <summary> 
        /// Initialize is guaranteed to be called by MSBuild at the start of the build 
        /// before any events are raised. 
        /// </summary> 
        public override void Initialize(IEventSource eventSource)
        {
            count = 0;

            // The name of the log file should be passed as the first item in the 
            // "parameters" specification in the /logger switch.  It is required
            // to pass a log file to this logger. Other loggers may have zero or more than  
            // one parameters. 
            //Console.WriteLine("params" + Parameters); Console.ReadKey();

            if (null != Parameters)
            {
                string[] parameters = Parameters.Split(';');

                foreach (string s in parameters.ToList())
                {

                    if (s.Split('*')[0] == Constant.LoggerConstent.CPRM_ENVIRONMENT_REVISION)
                    {
                        _Rev = s.Split('*')[1];
                    }
                }
            }


            eventSource.ProjectStarted += new ProjectStartedEventHandler(eventSource_ProjectStarted);
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(eventSource_ProjectFinished);
        }



        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {

            _error.Add(string.Format("{0} {1} - {2}/{3} [{4}]", e.Message, e.File, e.LineNumber, e.ColumnNumber, e.Code));
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            _warning.Add(string.Format("{0} {1} - {2}/{3} [{4}]", e.Message, e.File, e.LineNumber, e.ColumnNumber, e.Code));
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
            // Let's take account of the verbosity setting we've been passed in deciding whether to log the message 
            if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast(LoggerVerbosity.Minimal))
                || (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast(LoggerVerbosity.Normal))
                || (e.Importance == MessageImportance.Low && IsVerbosityAtLeast(LoggerVerbosity.Detailed))
                )
            {
                _Message.AppendLine(e.Message);
            }
        }



        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            // ProjectStartedEventArgs adds ProjectFile, TargetNames 
            // Just the regular message string is good enough here, so just display that.
            if (string.IsNullOrWhiteSpace(_target) && !string.IsNullOrWhiteSpace(e.TargetNames))
            {
                _target = e.TargetNames;
            }

            _Message.AppendLine(e.Message);
            indent++;
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            // The regular message string is good enough here too.
            indent--;
        }

        /// <summary> 
        /// Write a line to the log, adding the SenderName and Message 
        /// (these parameters are on all MSBuild event argument objects) 
        /// </summary> 
        private void WriteLineWithSenderAndMessage(string line, BuildEventArgs e)
        {
            if (0 == String.Compare(e.SenderName, "MSBuild", true /*ignore case*/))
            {
                // Well, if the sender name is MSBuild, let's leave it out for prettiness
                WriteLine(line, e);
            }
            else
            {
                WriteLine(e.SenderName + ": " + line, e);
            }
        }

        /// <summary> 
        /// Just write a line to the log 
        /// </summary> 
        private void WriteLine(string line, BuildEventArgs e)
        {
            if (Utility.GetSettingValue<bool>(Constant.K_IS_CONSOL_LOG))
            {
                for (int i = indent; i > 0; i--)
                {
                    //    streamWriter.Write("\t");
                    Console.WriteLine("\t");
                }
                if (e != null)
                    Console.WriteLine(line + e.Message);
                else
                    Console.WriteLine(line);
            }
        }


        /// <summary> 
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all  
        /// events have been raised. 
        /// </summary> 
        public override void Shutdown()
        {
            try
            {
                if (_target != "Clean")
                {

                    Mailer mailer = new Mailer();
                    var email = Utility.GetRecipient();

                    string BuildMessage = MailTemplet.GetMailBody(_Rev, _error.ToArray(), _warning.ToArray());

                    mailer.ComposeMail("Clysar Build", BuildMessage);
                    string smtpUser = Utility.GetSettingValue<string>(Constant.K_SMTP_USER);
                    int port = Utility.GetSettingValue<int>(Constant.K_SMTP_SERVER_PORT);
                    string server = Utility.GetSettingValue<string>(Constant.K_SMTP_SERVER);
                    string pass = Utility.GetSettingValue<string>(Constant.K_SMTP_USER_PASS);
                    mailer.SendMail(smtpUser, email, port, server, smtpUser, pass);
                }
            }
            catch (Exception exception)
            {
                WriteLine(exception.Message, null);
            }

        }

        private int indent;
    }

    public class Utility
    {
        public Utility()
        {

        }
        public static T GetSettingValue<T>(string key, string tag = "settings")
        {
            try
            {
                var _assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;

                var _path = System.IO.Path.GetDirectoryName(_assembly);
                XDocument xdoc = XDocument.Load(_path + @"\Config.xml"); //you'll have to edit your path

                //Run query
                var lv1s = (from lv1 in xdoc.Root.Descendants(tag).First().Descendants("add")
                            where lv1.Attribute("key") != null && lv1.Attribute("value") != null
                            select new
                            {
                                Key = lv1.Attribute("key").Value,
                                Value = lv1.Attribute("value").Value
                            }).ToList();

                var val = lv1s.FirstOrDefault(p => p.Key == key).Value.ToString();
                TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
                return (T)tc.ConvertFrom(val);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return default(T);
            }
        }
        public static string[] GetRecipient()
        {
            try
            {
                var _assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;

                var _path = System.IO.Path.GetDirectoryName(_assembly);
                XDocument xdoc = XDocument.Load(_path + @"\Config.xml"); //you'll have to edit your path

                //Run query
                var lv1s = (xdoc.Root.Descendants("mail").First().Descendants("add").Select(p => p.Attribute("email").Value)).ToArray();

                return lv1s;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            return null;
        }

        public static Dictionary<string, string> GetEnvironmentSettins()
        {
            try
            {
                var _assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;

                var _path = System.IO.Path.GetDirectoryName(_assembly);
                XDocument xdoc = XDocument.Load(_path + @"\Config.xml"); //you'll have to edit your path

                //Run query
                var lv1s = (xdoc.Root.Descendants("environment").First().Descendants("add").ToDictionary(p => p.Attribute("key").Value, p => p.Attribute("value").Value));

                return lv1s;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

            }
            return null;
        }

    }

    public class Constant
    {
        public const string K_IS_CONSOL_LOG = "IsConsoleLog";
        public const string K_SMTP_USER = "SMTPServerUserName";
        public const string K_SMTP_SERVER_PORT = "SMTPServerPort";
        public const string K_SMTP_SERVER = "SMTPServer";
        public const string K_SMTP_USER_PASS = "SMTPServerPassword";
        public const string K_RECIPIENT = "mail";

        public class LoggerConstent
        {
            /*prefix CPRM : constants for Console Params*/
            public const string CPRM_ENVIRONMENT_REVISION = "REV";

            public const string CPRM_ENVIRONMENT_PROJECT = "PROJ";

            public const string CPRM_ENVIRONMENT_TARGET = "SERVER";

            public const string CPRM_URL = "URL";

        }
    }

    class MailTemplet
    {
        /*0:= Project on server ,1:= Revison; 2:= build version; 3:= warning error count; 4: warning /error ;5 and 6:url*/
        const string _mailTempletSuccess = "<div class=\"ii gt m149a2fd92e3e1d94 adP adO\" id=\":gx\"><div style=\"overflow: hidden;\" class=\"a3s\" id=\":gy\"><div dir=\"ltr\"><div><div><div><div><div><div><div>Hello All<br></div>Build Success for <br></div><b>{0}</b> of revision <span style=\"color:rgb(0,0,255)\">{1}</span><br></div>Build version <span style=\"color:rgb(0,0,255)\">{2}</span><br></div>Build Success but still have following <span style=\"color:rgb(255,0,0)\">{3}</span> warnings <br></div><span style=\"color:rgb(255,0,0)\">{4}</span><br><br></div>You can login to <a target=\"_blank\" href=\"{5}\">{6}</a><br><br></div>Thanks<div class=\"yj6qo\"></div><div class=\"adL\"><br></div></div><div class=\"adL\"></div></div></div>";
        const string _mailTempletFail = "<div class=\"ii gt m149a2fd92e3e1d94 adP adO\" id=\":gx\"><div style=\"overflow: hidden;\" class=\"a3s\" id=\":gy\"><div dir=\"ltr\"><div><div><div><div><div><div><div>Hello All<br></div>Build Failed for <br></div><b>{0}</b> of revision <span style=\"color:rgb(0,0,255)\">{1}</span><br></div>Build version <span style=\"color:rgb(0,0,255)\">{2}</span><br></div>Build field fo  following <span style=\"color:rgb(255,0,0)\">{3}</span> errors <br></div><span style=\"color:rgb(255,0,0)\">{4}</span><br><br></div><br></div>Thanks<div class=\"yj6qo\"></div><div class=\"adL\"><br></div></div><div class=\"adL\"></div></div></div>";
        internal static string GetMailBody(string revision, string[] error, string[] warning)
        {
            var settings = Utility.GetEnvironmentSettins();
            var project = settings[Constant.LoggerConstent.CPRM_ENVIRONMENT_PROJECT];
            var url = settings[Constant.LoggerConstent.CPRM_URL];
            var targetServer = settings[Constant.LoggerConstent.CPRM_ENVIRONMENT_TARGET];
            string msg = string.Empty;
            if (error.Length > 0)
            {
                msg = string.Format(_mailTempletFail, project, revision, "", error.Length, string.Join("<br/>", error));
            }
            else
            {
                msg = string.Format(_mailTempletSuccess, project, revision, "", warning.Length, string.Join("<br/>", warning), url, url);
            }
            return msg;
        }
    }
}