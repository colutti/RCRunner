using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Xml.Serialization;
using RCRunner.Shared.Lib;
using RCRunner.Shared.Lib.PluginsStruct;

namespace SendEmailPlugin
{
    /// <summary>
    /// Whom to send the e-mail
    /// </summary>
    public class EmailSettingsToList
    {
        [XmlElement("Address")]
        public string Address;
    }

    /// <summary>
    /// Defines a configuration to send e-mails
    /// </summary>
    [XmlRoot("EmailSettings")]
    public class EmailSettings
    {
        /// <summary>
        /// From email address
        /// </summary>
        [XmlElement("FromAddress")]
        public string FromAddress;

        /// <summary>
        /// From Name
        /// </summary>
        [XmlElement("FromName")]
        public string FromName;

        /// <summary>
        /// The address of the SMTP Server
        /// </summary>
        [XmlElement("Host")]
        public string Host;

        /// <summary>
        /// Subject of the e-mail
        /// </summary>
        [XmlElement("Subject")]
        public string Subject;

        /// <summary>
        /// SMTP host por. Default is 25
        /// </summary>
        [XmlElement("Port")]
        public int Port;

        /// <summary>
        /// List of people who will receive the e-mail
        /// </summary>
        [XmlElement("ToListItem")]
        public List<EmailSettingsToList> ToList;

        public EmailSettings()
        {
            ToList = new List<EmailSettingsToList>();    
        }

        /// <summary>
        /// Creates a instance of the settings reading from a XML file
        /// </summary>
        /// <returns></returns>
        public static EmailSettings CreateNew()
        {
            const string emailSettingsXmlPath = "EmailSettings.xml";

            var emailSettings = new EmailSettings();

            if (!File.Exists(emailSettingsXmlPath)) return emailSettings;

            var ser = new XmlSerializer(typeof(EmailSettings));

            using (var readtext = new StreamReader(emailSettingsXmlPath))
            {
                emailSettings = (EmailSettings)ser.Deserialize(readtext);
            }

            return emailSettings;
        }

    }

    public class SendEmailPlugin : TestExecution
    {
        public override void AfterTestExecution(string idTestCase)
        {
        }

        /// <summary>
        /// Called before a test run begins, this method will send an e-mail alerting that a run will begin
        /// </summary>
        /// <param name="testCasesList"></param>
        public override void BeforeTestRun(List<TestScript> testCasesList)
        {
            var message = string.Format("{0} - Starting a new test run. {1} tests will be executed.", DateTime.Now,
                testCasesList.Count);

            SendEmail(null, message);
        }

        /// <summary>
        /// Called after a test run finishes, this method will send an e-mail alerting that the run finished with some details about the test execution
        /// </summary>
        /// <param name="testCasesList"></param>
        public override void AfterTestRun(List<TestScript> testCasesList)
        {
            var passedTestCases = testCasesList.Count(x => x.TestExecutionStatus == TestExecutionStatus.Passed);
            var failedTestCases = testCasesList.Count(x => x.TestExecutionStatus == TestExecutionStatus.Failed);

            var message =
                string.Format(
                    "{0} - The test execution finished. Ran {1} tests. {2} tests passed, {3} tests failed. More details on the spreedsheet",
                    DateTime.Now, testCasesList.Count, passedTestCases, failedTestCases);

            var excelFilePath = Path.ChangeExtension(Path.GetTempFileName(), "xlsx");

            RCRunnerAPI.ExportToExcel(excelFilePath, testCasesList);

            SendEmail(excelFilePath, message);

        }

        /// <summary>
        /// Send an email with an attachment and a specific body
        /// </summary>
        /// <param name="fileAttachement"></param>
        /// <param name="body"></param>
        private static void SendEmail(string fileAttachement, string body)
        {
            var emailSettings = EmailSettings.CreateNew();

            if (emailSettings.ToList.Count <= 0) return;

            var fromAddress = new MailAddress(emailSettings.FromAddress, emailSettings.FromName);
            var mail = new MailMessage { From = fromAddress };
            
            if (!string.IsNullOrEmpty(fileAttachement))
            {
                var anexo = new Attachment(fileAttachement);
                mail.Attachments.Add(anexo);
            }

            foreach (var to in emailSettings.ToList)
            {
                mail.To.Add(to.Address);
            }
            
            var client = new SmtpClient
            {
                Port = emailSettings.Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = emailSettings.Host
            };

            mail.Subject = emailSettings.Subject;
            mail.Body = body;
            client.Send(mail);
        }
    }
}
