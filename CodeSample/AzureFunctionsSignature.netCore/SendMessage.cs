using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SecureExchangesSDK.Helpers;
using SecureExchangesSDK.Models;
using SecureExchangesSDK.Models.Answer;
using SecureExchangesSDK.Models.Args;
using SecureExchangesSDK.Models.Entity;
using SecureExchangesSDK.Models.JSON;
using SecureExchangesSDK.Models.Transport;
using SecureExchangesSDK.SecureExchanges;
using SecureExchangesSignatureSDK.Helpers;
using SecureExchangesSignatureSDK.Models.Args;
using System.Net;

namespace AzureFunctionsSignature.netCore;

public class SendMessage
{
  private readonly ILogger<SendMessage> _logger;
  private readonly PkiFileHelper _pkiFileHelper;

  public SendMessage(ILogger<SendMessage> logger, PkiFileHelper pkiFileHelper)
  {
    _logger = logger;
    _pkiFileHelper = pkiFileHelper;
  }


  [Function("SendMessage")]
  public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
  {
    var response = req.CreateResponse(HttpStatusCode.BadRequest);
    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

    try
    {
      Uri Uri = new Uri("https://www.secure-exchanges.com/_api/0217/0217.asmx");
      Guid Serial = new Guid("00000000-0000-0000-0000-000000000000");
      Guid ApiUser = new Guid("00000000-0000-0000-0000-000000000000");
      Guid ApiPassword = new Guid("00000000-0000-0000-0000-000000000000");

      // Define recipients
      string email = "recipient@example.com";

      List<RecipientInfo> recipients = new List<RecipientInfo>()
      {
        new RecipientInfo()
        {
          Email = email,
          Phone = null
        }
      };

      // PDFs preparation
      string filePath = Path.Combine(AppContext.BaseDirectory, "document_pdfa.pdf");
      string pkiFile = Path.GetFileName(filePath);
      byte[] bytesPkiFile = await File.ReadAllBytesAsync(filePath);
      string sha512PkiFile = CryptoHelper.GetSHA512HashOfBytes(bytesPkiFile);

      // Get certification token for each pdf
      var certificationTokenArgs = new GetCertificationTokenSDKArgs(
        Uri,
        Serial,
        ApiUser,
        ApiPassword,
        sha512PkiFile,
        CertificationTokenType.CertifyDocument,
        "en-CA"
      );

      var certificationTokenResponse = SignHelper.GetCertificationToken(certificationTokenArgs);
      if (certificationTokenResponse.Status != 200)
        throw new InvalidOperationException($"Retrieving certification token failed: {certificationTokenResponse.Data}");

      // List of unexisting zones to be added to pdf during certification
      // Zones must be already distributed across recipients
      ZoneBuilder zoneBuilder = new ZoneBuilder(bytesPkiFile, true);
      var zonesPkiFile = new SignZoneDetails[]
      {
        zoneBuilder.CreateFieldSignZone(1, "signature_1", 50, 76, 185, 90)
      };
      var recipientIndex = new Dictionary<RecipientInfo, List<FileZoneDefinition>>
      {
        {
          recipients[0],
          new List<FileZoneDefinition>
          {
            new FileZoneDefinition
            {
              UniqueName = sha512PkiFile,
              ZonesDef = new SignZoneDefinition { Mode = "define", Zones = zonesPkiFile }
            }
          }
        }
      };

      // Create arguments for PDF certification
      CertifyPdfArgs certifyPdfArgs = new CertifyPdfArgs()
      {
        File = new FileArgs(bytesPkiFile, pkiFile, FileHelper.GetMimeType(pkiFile)),
        RecipientZoneDefinitions = SignHelper.ConvertRecipientIndexToList(recipientIndex),
        Recipients = recipients.Select(recipient => recipient.Email).ToList(),
        CertifyData = certificationTokenResponse.CertificationData,
        DetectExistingFields = false
      };

      CertifiedPdfContainer certifyPkiPdfResult;

      try
      {
        // Certifying PDFs, helper can be reused for multiple pdfs
        certifyPkiPdfResult = await _pkiFileHelper.CertifyPkiPdf(certifyPdfArgs);
        if (certifyPkiPdfResult == null)
          throw new InvalidOperationException("Certification result is null");
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"CertifyPdf failed: {ex.Message}", ex);
      }

      // Send Message
      string HTMLBody = "Message Body";
      string EmailSubject = $"File to sign - Test";
      string psw = "1234";
      // Let you send the mail with your own smtp server
      bool sendWitMyOwnSMPTServer = false;

      //Create the message args
      var mutliRecipientArgs = new MutliRecipientArgs(
        Uri,
        Serial,
        ApiUser,
        ApiPassword,
        recipients,
        HTMLBody,
        EmailSubject,
        psw,
        null,
        SendMethodEnum.onlyEmail,
        sendWitMyOwnSMPTServer,
        true,
        true,
        "en-CA",
        1,
        5000)
      {
        CertifiedPdfs = new List<CertifiedPdfContainer>() { certifyPkiPdfResult },
        OwnerDontNeedToSign = true,
      };

      // Call the multicecipient method 
      MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(mutliRecipientArgs);
      if (answer.Status != 200)
        throw new InvalidOperationException("Failed to send message");

      string emailUrl = answer.RecipientsAnswer[0].Answer.URL;

      response.StatusCode = HttpStatusCode.OK;
      await response.WriteStringAsync($"The message was successfully sent to {email}.\n");
      await response.WriteStringAsync($"Message url: ${emailUrl}");
    }
    catch (InvalidOperationException ex)
    {
      response.StatusCode = HttpStatusCode.BadRequest;
      await response.WriteStringAsync(ex.Message);
    }
    catch (Exception ex)
    {
      await response.WriteStringAsync($"Error occured: {ex.Message}");
    }

    _logger.LogInformation("C# HTTP trigger function processed a request.");
    return response;
  }
}