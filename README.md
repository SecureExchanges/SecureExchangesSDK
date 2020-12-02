
# Secure Exchanges SDK

Secure Exchanges allows you to send confidential information by email. It's a end to end encryption SaaS.

A Microsoft Outlook connector exists to allow our users to have a completely transparent experience.

We have also made available to them an SDK available in Nuget at the following address https://www.nuget.org/packages/SecureExchanges/

To use our SDK you need a user license or a trial version license.

In the code examples you will find how:

- Sent a message with attachments
- Protected with a password, SMS code or both
- To be able to read a received message
- To be able to see the logs and status of a sent message

### How to get files signed ###
You can define template of signature zones online and then use it with the API.
If you send file witout defined signature zones ie.(Signature, initial, text, date, signature date, number) your recipient will be allowed to sign where he want.

If you need to sign the file after all the recipients signs, you will need to set the value of the args.OwnerDontNeedToSign to "false". See code sample bellow
See the complete example in the **SendSignFile** method in the **[MessageHelperTest.cs](https://github.com/cboivin80/SecureExchangesSDK/blob/master/CodeSample/CodeSample/MessageHelperTest.cs)** class.
>       //Create the message args
>       var args = new MutliRecipientArgs(
>           EndPointURI,
>          TestSerialNumber,
>          TestAPIUser,
>          TestAPIPsw,
>          recipients,
>          HTMLBody,
>          EmailSubject,
>          psw,
>          null,
>          SecureExchangesSDK.SecureExchanges.SendMethodEnum.onlyEmail,
>          sendWitMyOwnSMPTServer,
>          true,
>          true, "fr-CA", 1, 5)
>       { FilesPath = files };
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

### How to send a message ###

It's very easy to send a message with file or not with Secure Exchanges
You will see an complete code sample in MessageHelperTest.cs
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
      // Call the multicecipient method 
      MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args);
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