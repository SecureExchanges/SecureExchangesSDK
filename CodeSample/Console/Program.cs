using SecureExchangesSDK;
using SecureExchangesSDK.Helpers;
using SecureExchangesSDK.Models;
using SecureExchangesSDK.Models.Answer;
using SecureExchangesSDK.Models.Args;
using SecureExchangesSDK.Models.Entity;
using SecureExchangesSDK.SecureExchanges;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSample
{



  class Program
  {

    public static Uri EndPoint
    {
      get
      {
        if (ConfigurationManager.AppSettings["IsSandBox"] != null && bool.Parse(ConfigurationManager.AppSettings["IsSandBox"].ToString()))
        {
          // By default the SDK know the production fileHandler and SEMS handler. In sandbox mode we must specify.
          SettingsHelper.CustomFileHandler = "https://preview.secure-exchanges.com/Handler/ReadFileTob64.ashx"; // Set the file handler to sandbox
          SettingsHelper.CustomSEMSEndpoint = "https://previewsems.secure-exchanges.com/SEMS_api.asmx"; // Set the SEMS endpoint to sandbox
          // Set the Sandbox URI and the SANDBOX handler
          return new Uri("https://preview.secure-exchanges.com/_api/0217/0217.asmx");
        }
        return new Uri("https://www.secure-exchanges.com/_api/0217/0217.asmx");
      }
    }


    /// <summary>
    /// Put your serial number
    /// </summary>
    private static Guid TestSerialNumber = new Guid(ConfigurationManager.AppSettings["Serial"].ToString());
    /// <summary>
    /// Put your api number
    /// </summary>
    private static Guid TestAPIUser = new Guid(ConfigurationManager.AppSettings["APIUser"].ToString());
    /// <summary>
    /// put your API psw
    /// </summary>
    private static Guid TestAPIPsw = new Guid(ConfigurationManager.AppSettings["APIPsw"].ToString());

    static void Main(string[] args)
    {
       int intMode = 0;
      SendMethodEnum sendingMode = SendMethodEnum.onlyEmail;
      while (intMode <= 0 || intMode > 2)
      {
        Console.WriteLine("Choose your communication mode. Type 1 for email and 2 for SMS");
        string mode = Console.ReadLine();
        int.TryParse(mode, out intMode);
        switch (intMode)
        {
          case 1:
            sendingMode = SendMethodEnum.onlyEmail;
            break;
          case 2:
            sendingMode = SendMethodEnum.msgSMSOnly;
            break;
        }
        Console.Clear();
      }

      Console.WriteLine($"Please enter the recipient informations for mode {sendingMode.ToString()}");
      string contactInfo = Console.ReadLine();
      RecipientInfo recipient = new RecipientInfo();
      switch (sendingMode)
      {
        case SendMethodEnum.onlyEmail:
          recipient.Email = contactInfo;
          break;
        case SendMethodEnum.msgSMSOnly:
          recipient.Phone = contactInfo;
          break;
      }
      List<RecipientInfo> recipients = new List<RecipientInfo>();
      recipients.Add(recipient);
      Console.Clear();

      Console.WriteLine("Do you want to send a file ? Type (y) for yes, and (n) for no");
      string isFile = Console.ReadLine();
      string filePath = null;
      if (isFile == "y")
      {
        Console.WriteLine("Type the file path");
         filePath= Console.ReadLine();
      }
      Console.Clear();

      Console.WriteLine("Type the subject :");
      string subject = Console.ReadLine();
      Console.Clear();

      Console.WriteLine("Type the message :");
      string message = Console.ReadLine();
      Console.Clear();

      Console.WriteLine("Protect by password ? Type (y) for yes, and (n) for no");
      string isPassword = Console.ReadLine();
      string psw = null;
      if (isPassword == "y")
      {
        Console.WriteLine("Type your password :");
        psw = Console.ReadLine();
      }
      Console.Clear();
      Console.WriteLine("Send in progress...");
      MultiRecipientAnswer answer = MultiSendEmailWithLocalFiles(sendingMode,recipients,filePath, subject,message,psw);
      // if the status is set to 200 the call was made successfully. See in answer.Data if the status is not 200
      if (answer.Status == 200)
      {
        Console.WriteLine($"Send is success {answer.Data}");
        foreach (var a in answer.RecipientsAnswer)
        {
          // Here use a.Answer.HtmlMsg to send your email with your SMTP server
          // a.Answer.Guid -- this is the reference of a messageid. Keep it in your système to retreived log about this message
          Console.WriteLine($"A {sendingMode.ToString()} has been sent to {contactInfo}, the url is {a.Answer.URL}");

        }
      }
      else
      {
        Console.WriteLine($"An error has occur {answer.Data}");
      }
      Console.WriteLine("Finish");
      Console.ReadLine();
    }


    public static MultiRecipientAnswer MultiSendEmailWithLocalFiles(SendMethodEnum sendingMode, List<RecipientInfo> recipients, string filePath, string subject, string message, string psw)
    {
      // Create a files list path : physical path
      List<string> files = new List<string>();
      // Get the project path

      if (!string.IsNullOrEmpty(filePath))
      {
        files.Add(filePath);
      }
        

      // Fill this list if your have memory file to encrypt and send has attachment
      List<FileArgs> filesArgs = null;

      

      // Configure the message body, and the subject
      string HTMLBody = message;
      string EmailSubject = subject;      

      // If set to true, will return the HTML message obfuscated, and protected, you need to send it by email
      // If set to false, Secure Exchanges will send the message for you
      bool sendMessageByMyself = false;

      // Received the notify by email, when the message will be read
      bool getNotify = true;
      // The culture of the message. Currently support "fr-CA" and "en-CA"
      string culture = "fr-CA";
      //Create the message args
      var args = new MutliRecipientArgs(
         EndPoint,
         TestSerialNumber,
         TestAPIUser,
         TestAPIPsw,
         recipients,
         HTMLBody,
         EmailSubject,
         psw,
         filesArgs,
         sendingMode,
         sendMessageByMyself,
         true,
         getNotify, culture, 5, 30)
      { FilesPath = files };
      // Call the multicecipient method 
      MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args);
      
      return answer;
    }
  }
}
