### SandBox

If you use the sandbox "preview", you must specify the file Handler and the SEMS Handler for the sandbox environment.
Those Settings are global to the application.

> SettingsHelper.CustomFileHandler = "https://preview.secure-exchanges.com/Handler/ReadFileTob64.ashx";
> SettingsHelper.CustomSEMSEndpoint = "https://previewsems.secure-exchanges.com/SEMS_api.asmx";

** PLEASE NOTES THAT THOSES LINES MUST NOT BE PRESENT FOR PRODUCTION

### How to get files signed ###
You can define template of signature zones online and then use it with the API.
If you send file witout defined signature zones ie.(Signature, initial, text, date, signature date, number) your recipient will be allowed to sign where he want.

If you need to sign the file after all the recipients signs, you will need to set the value of the args.OwnerDontNeedToSign to "false". See code sample bellow or see the complete example in the **SendSignFile** method in the **[MessageHelperTest.cs](https://github.com/cboivin80/SecureExchangesSDK/blob/master/CodeSample/CodeSample/MessageHelperTest.cs#L185)** class.
>       //Create the message args
>       var args = new MutliRecipientArgs(
>          EndPointURI,      /// The endpoint configuration name of Secure Exchanges
>          TestSerialNumber, /// Specify the serial of the licence owner
>          TestAPIUser,      /// Specify the API user retreived from Secure Exchanges
>          TestAPIPsw,       /// Specify the API password retreived from Secure Exchanges
>          recipients,       /// Specify the list of recipent
>          HTMLBody,         /// Specify the message to Secure (must be in clear) the helper will encrypt the message
>          EmailSubject,     /// Specify the subject of the message
>          psw,              /// The password to secure the message. The password will be share between all the Recipient. Set it As null if your don't need a password
>          null,             /// The list of files (memory) to attach to the message
>          SecureExchangesSDK.SecureExchanges.SendMethodEnum.onlyEmail, /// The SendMethod mode, OnlyEmail, EmailWithSMS. If EmailWithSMS or SMSwithEmail specify, all the recipient must have a cell number in the RecipientInfo
>          sendWitMyOwnSMPTServer, /// Specify if the user will send the message by it'self if is set to false, the SEMS (Secure Exchanges Mail System) will send the message
>          true,             /// Specify if the subject need to be show
>          true,             /// Specify if the owner must be notify by email when the message is open 
>          "fr-CA",          /// Specify the culture of the message send to the recipient
>          1,                /// Specify the number of open time before the message is destroy value must be between 1 - 99
>          5                 /// Specify the number of minutes before the message expire. Default value 14 days. 50400
>       )
>       { FilesPath = files }; /// The list of files path to attach to the message
>       
>       // Because we have some file to sign, we add them to the args
>       // Notes that you can send files witout signature only to the first recipient, if you have multiple recipient.
>       if (signFiles.Count > 0)
>       {
>         // Set the files to be sign
>         args.FileToSign = signFiles;
>         // If the owner of the licence need to sign the file, set the value to false.
>         // If it's set to false, the licence owner will receive an email when the file will be ready to sign by him
>         args.OwnerDontNeedToSign = true;
>         // Set the recipient zone definition
>         args.SignRecipientsZoneDef = SignHelper.ConvertRecipientIndexToList(recipientIndex);
>       }
> 
>       // Call the multicecipient method 
>       MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args);

### How to send a message with events ###

It's very easy to send a message with file or not with Secure Exchanges =====================================================<<<<<<<<<<<<<<<<< ?????????? not in title subject
You will see an complete code sample in **[MessageHelperTest.cs](https://github.com/cboivin80/SecureExchangesSDK/blob/master/CodeSample/CodeSample/MessageHelperTest.cs)** class.
```
 var args = new MutliRecipientArgs(
         EndPointURI,
         TestSerialNumber,
         TestAPIUser,
         TestAPIPsw,
         recipients,
         HTMLBody,
         EmailSubject,
         messagePassword,
         filesArgs,
         sendingMode,
         sendMessageByMyself,
         true,
         getNotify, culture, 1, 5)
      { FilesPath = files };
      
      // Attaching events
      
      fileHelper.Before_UploadChunksErrorRetry += (IEnumerable<int> chunks, string inputFile, Guid fileIdentifier) =>
      {
          /// This is the delegate associated to the event when chunks are in errors, before retry upload.
          /// <param name="chunks">Chunks list</param>
          /// <param name="inputFile">File path</param>
          /// <param name="fileIdentifier">File ID</param>
      };
      
      fileHelper.UploadFileStart_Event += (string eventFileName, long fileSize) =>
      {
          /// This event occur before the file start to be uploaded to the server.
          var fileName = eventFileName;
          var theFileSize = fileSize;
      };

      fileHelper.UploadFileEnd_Event += (string eventFileName, long fileSize) =>
      {
          /// This event occur when all the file chunks has finish
      };
     
      fileHelper.UploadFinish_Event += () =>
      {
          /// The event call when a chuck finish upload
      };

      fileHelper.UploadFinishFiles_Event += (List<UploadFiles> uploadedFiles) =>
      {
          /// This event occur when all the files upload has finish
          for (int i = 0; i < uploadedFiles.Count; i++)
          {
              UploadFiles upf = uploadedFiles[i]; // That is the model of files chunk when file are uploaded to the Secure Exchanges server
              
              string fileName = upf.RealFileName; // Real file name of the file
              
              int fileLength = upf.BytesLenght; // The file length (the size of the file). If you need the beauty filesize, you can use the GetTextSize

              string fileSHA512 = upf.SHA512; // The file SHA512
              string fileHash = upf.Hash; // The Hex SHA512 file hash

              double originalfile_totalPart = upf.TotalPart; // The total number of chunk part that file

              Guid guid = upf.CryptedHandShakeID; // That is the cryptedHandShakeID that was use to upload the file
          }

      };
      
      // Call the multicecipient method 
      MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args, fileHelper);
      
      var status = answer.Status; // The state could be 200, 403, ....
```

### How to download a filepath and binary file
```
   ////////////////////////////// Upload Test ////////////////////////////////////

   string filePath = GlobalSettings.LargeFilePath; // This is a large file path
   string filePathBinary = GlobalSettings.smallFilePath; // This is a small binary file
   filesPath.Add(filePathBinary); // A file list (we need it later in this example to compare)
   
   // A function that uploads the file path and the binary file and generates the link
   var url = GenerateLinkForUploadedFile(filePath, filePathBinary);


   ////////////////////////////// Download Test ////////////////////////////////////

   var msg = ReadMessage(url, "", ""); // Get the message


   FileHelper down_fh = new FileHelper(); // We create a FileHelper to attach event to downloaded files
   
   // Attaching event after each file download
   down_fh.DownloadFinish_event += (DownloadedFileWithMetaData file) =>
   {               
       var filePath = file.FilePath; // The final file path downloaded
       
       var fileName = file.FileName; // The final file name downloaded       
       
       var state = file.FileState; // The file state of the downloaded file. The state could be Process, NotProcess ...       
       
       var isFileUncrypted = file.Uncrypted; // Is the file has been successfly uncrypted           
       
       bool isSuccess = file.Success; //If the file is flagged has succces                 
   };

   // Attaching event after all download finished
   down_fh.DownloadsFinish_event += (List<DownloadedFile> files) =>
   {
       var numberOfFiles = files.Count; // Get the number of files in the collection
   };
   
   //After attachement we can start the download
   down_fh.DownloadSecureFiles(msg.FilesMetaData, "./download folder");
```
            
### Get the status of your send message ###

If your code just send a message to someone, and need to get status about it

That method will let you know all the informations about the documents sent. 

When that document has been open, the hash of the files sent, if they are downloaded or not and if they are still available.
```
LogsHelper.GetLog(new GetLogArgs(EndPointURI, TestSerialNumber, TestAPIUser, TestAPIPsw, messageId));
```

### How to retreived confidential informations ###

Secure Exchanges supported up to 5 Gb in encrypted data. It's very usefull when you need to retreived some "photo copie" from someone and need to protect the privacy of the document.

You just need to send a Secure Exchanges link to the external source. The link could be send by the channel you want. Automaticly the file and message will be return to the licence owner.

We called that feature "The envelop" As a pre-stamped envelope the return envelope, will automatically be returned to the sender and license owner.

When the feature is activated, you can also retreived the return informations to a callback WS on your side.
```
 GetEnveloppeResponse response = SecureExchangesSDK.Helpers.MessageHelper.GetEnveloppe(new
       SecureExchangesSDK.Models.Args.GetEnveloppeArgs(EndPointURI, TestSerialNumber, TestAPIUser, TestAPIPsw, "Reply", "email1;email2;", "fr-CA")
      {
        // If you get a callback API activated set it to true, if not set it to no
        ReplyToAPI = false,
        // Object that you want to retreived when you will get back the message
        CallBackParameters = "Your callback parameter, xml, json what you want"
      });

```
