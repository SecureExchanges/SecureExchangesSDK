# Secure Exchanges SDK

The official .NET SDK for [Secure Exchanges](https://www.secure-exchanges.com/) — send encrypted messages, transfer large files, sign PDFs electronically, and track delivery from your applications.

## Features

- End-to-end encrypted messages over email or SMS
- File transfer up to 2.5 GB with chunked upload/download
- Electronic PDF signatures with configurable signature zones
- Delivery and read tracking (`LogsHelper`)
- Return-envelope workflow for collecting confidential replies
- SESAR webhook integration

## Packages

All packages are published on NuGet under the [Secure Exchanges publisher profile](https://www.nuget.org/profiles/SecureExchanges). Pick the row that matches your target framework — the Signature module is optional.

| SDK | Purpose | .NET Framework 4.7.2+ (Windows only) | .NET 8+ (Windows + Linux) |
|-----|---------|--------------------------------------|---------------------------|
| Main SDK | Messaging, encryption, tracking | `SecureExchanges` | `SecureExchanges.NetCore` |
| Signature (optional) | PDF certification | `SecureExchanges.Signature` | `SecureExchanges.Signature.NetCore` |

## Getting a licence

Purchase a licence at [secure-exchanges.com/tarifs](https://www.secure-exchanges.com/tarifs), or contact [support@secure-exchanges.com](mailto:support@secure-exchanges.com) to evaluate the SDK. For technical questions about integrating the SDK, reach the developer support team at [supportdev@secure-exchanges.info](mailto:supportdev@secure-exchanges.info).

## Samples in this repository

All samples share the same `CodeSample.sln` solution.

| Project | Target | Demonstrates |
|---------|--------|--------------|
| [`CodeSample/CodeSample/`](CodeSample/CodeSample/) | net4.8 MSTest | Unit tests covering messaging, signatures, and downloads — see [`MessageHelperTest.cs`](CodeSample/CodeSample/MessageHelperTest.cs) and [`ServerTest.cs`](CodeSample/CodeSample/ServerTest.cs) |
| [`CodeSample/Console/`](CodeSample/Console/) | net4.7.2 console | PDF signing and return-envelope walkthroughs using `SecureExchanges` + `SecureExchanges.Signature`. Main files: [`PkiFileToSignWithSignatureOptions.cs`](CodeSample/Console/PkiFileToSignWithSignatureOptions.cs) (most complete PKI signing flow) and [`GetEnveloppe.cs`](CodeSample/Console/GetEnveloppe.cs) (single- and multi-recipient envelope). See the dedicated sections below. |
| [`CodeSample/Tool/GenerateSESARManifestWebHook/`](CodeSample/Tool/GenerateSESARManifestWebHook/) | net4.8 console | Generator for SESAR webhook manifests |
| [`CodeSample/AzureFunctionsSignature.netCore/`](CodeSample/AzureFunctionsSignature.netCore/) | net8.0 Azure Functions v4 | Running `SecureExchanges.NetCore` + `SecureExchanges.Signature.NetCore` in an Azure Function, with a `Dockerfile` and `build-and-push.ps1` for container deployment |

## Sandbox (preview) configuration

When pointing the SDK at the `preview` sandbox, override the file handler and SEMS endpoint globally at application startup:

```csharp
SettingsHelper.CustomFileHandler  = "https://preview.secure-exchanges.com/Handler/ReadFileTob64.ashx";
SettingsHelper.CustomSEMSEndpoint = "https://previewsems.secure-exchanges.com/SEMS_api.asmx";
```

> These lines must **not** be present in production builds.

## Sending a signed message

Signature-zone templates (signature, initial, text, date, signature date, number) can be pre-defined on the Secure Exchanges portal. If you send a file without a zone definition, the recipient is free to sign anywhere. To require the licence owner to sign after every recipient, set `args.OwnerDontNeedToSign = false`.

Full working example: [`MessageHelperTest.cs` → `SendSignFile`](https://github.com/SecureExchanges/SecureExchangesSDK/blob/master/CodeSample/CodeSample/MessageHelperTest.cs).

```csharp
var args = new MutliRecipientArgs(
    EndPointURI,
    TestSerialNumber,
    TestAPIUser,
    TestAPIPsw,
    recipients,
    HTMLBody,
    EmailSubject,
    psw,                                                     // message password, or null
    null,                                                    // in-memory files
    SecureExchangesSDK.SecureExchanges.SendMethodEnum.onlyEmail,
    sendWithMyOwnSMTPServer,
    true,                                                    // show subject
    true,                                                    // notify owner on open
    "fr-CA",
    1,                                                       // max reads (1-99)
    5                                                        // expiry in minutes
)
{
    FilesPath = files
};

if (signFiles.Count > 0)
{
    args.FileToSign            = signFiles;
    args.OwnerDontNeedToSign   = true;
    args.SignRecipientsZoneDef = SignHelper.ConvertRecipientIndexToList(recipientIndex);
}

MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args);
```

## Complete PKI signing workflow

[`PkiFileToSignWithSignatureOptions.cs`](CodeSample/Console/PkiFileToSignWithSignatureOptions.cs) in the Console sample is the most complete end-to-end integration — a good starting point when building a real PDF-signing feature. It covers:

1. **Loading and hashing the PDF** — `CryptoHelper.GetSHA512HashOfBytes` produces the SHA-512 hash used to bind the signature zones to that exact file.
2. **Placing signature zones with `ZoneBuilder`** — precise X/Y coordinates for signature and date fields (`CreateFieldSignZone`, `CreateFieldDateSignZone`).
3. **Requesting a certification token** via `SignHelper.GetCertificationToken` to authorize certification of that specific hash.
4. **Certifying the PDF** with `PkiFileHelper.CertifyPkiPdf`, including auto-detection of existing AcroForm fields (`DetectExistingFields = true`) and a `DefineRecipients` delegate that maps detected zones onto a specific recipient.
5. **Rich `SignOptions`** — `SignatureRestriction` to constrain the accepted signature mode, plus `UploadFileOptions` to ask the signer to upload a file in return (mandatory upload, allowed extensions, bilingual prompts).
6. **Callback parameters from contextual keys** — `LicenceHelper.GetSerialInfo` exposes the contextual keys required for Outlook Classic compatibility; serialize them as `CallBackParameter` entries.
7. **Sending via `MutliRecipientArgs`** with the `CertifiedPdfContainer`, `SignatureOptions`, and serialized callback payload attached, then reading the signature URL and default opening code from the response.

```csharp
var signatureOptions = new SignOptions
{
    SignatureRestriction = SignatureModeRestriction.Trace,          // hand-drawn only
    UploadFileOptions = new SignOptionsFileUpload
    {
        AuthorizedExtensions = new[] { ".pdf", ".png", ".jpg" },
        UploadMode           = ReplyFileUploadMode.UploadMandatory,  // signer must attach a file
        Messages             = new List<ReplyOptionsMessage>
        {
            new ReplyOptionsMessage { CultureID = "fr-CA", Message = "Veuillez joindre votre spécimen chèque en retour." },
            new ReplyOptionsMessage { CultureID = "en-CA", Message = "Please attach your void cheque." }
        }
    }
};
```

## Sending with upload events

Hook into `FileHelper` events to observe chunked uploads:

```csharp
var fileHelper = new FileHelper();

fileHelper.Before_UploadChunksErrorRetry += (chunks, inputFile, fileIdentifier) =>
{
    // invoked before retrying failed chunks
};

fileHelper.UploadFileStart_Event += (fileName, fileSize) => { /* file starts */ };
fileHelper.UploadFileEnd_Event   += (fileName, fileSize) => { /* all chunks done */ };
fileHelper.UploadFinish_Event    += () => { /* a single chunk finished */ };

fileHelper.UploadFinishFiles_Event += (List<UploadFiles> uploadedFiles) =>
{
    foreach (var upf in uploadedFiles)
    {
        var fileName    = upf.RealFileName;
        var fileLength  = upf.BytesLenght;                   // use GetTextSize for a human-readable value
        var fileSHA512  = upf.SHA512;
        var fileHash    = upf.Hash;                          // hex SHA-512
        var totalChunks = upf.TotalPart;
        var handshakeId = upf.CryptedHandShakeID;
    }
};

MultiRecipientAnswer answer = MessageHelper.MultiRecipientMessage(args, fileHelper);
var status = answer.Status;                                  // e.g. 200, 403
```

## Downloading files

```csharp
var msg = ReadMessage(url, "", "");                          // retrieve the message

var fileHelper = new FileHelper();

fileHelper.DownloadFinish_event += (DownloadedFileWithMetaData file) =>
{
    var filePath     = file.FilePath;
    var fileName     = file.FileName;
    var state        = file.FileState;                       // Process, NotProcess, ...
    var wasDecrypted = file.Uncrypted;
    var succeeded    = file.Success;
};

fileHelper.DownloadsFinish_event += (List<DownloadedFile> files) =>
{
    var count = files.Count;
};

fileHelper.DownloadSecureFiles(msg.FilesMetaData, "./download folder");
```

## Tracking a sent message

Retrieve delivery, open, and download events for any message you sent:

```csharp
LogsHelper.GetLog(new GetLogArgs(EndPointURI, TestSerialNumber, TestAPIUser, TestAPIPsw, messageId));
```

## Collecting confidential replies — the envelope feature

Send a Secure Exchanges link to an external party through any channel; files and messages they return come back encrypted to the licence owner. Replies can also be forwarded to a callback webhook.

[`GetEnveloppe.cs`](CodeSample/Console/GetEnveloppe.cs) in the Console sample covers both the single- and multi-recipient flows:

- **`GetOne()`** — one recipient via `GetEnveloppeArgs` + `MessageHelper.GetEnveloppe`.
- **`GetMany()`** — a `List<Recipient>` via `GetEnvelopesArgs` + `MessageHelper.GetEnvelopes`.

Both methods show the patterns you will want in production:

- **Reply UI constraints** via `SeReplyOptions` — `NoEditor` to hide the message editor, `NoSign` when no signature is required, `Password` to let the responder password-protect their reply.
- **Lifecycle limits** — `ReplyExpirationHours`, `MaximumReplyOpenTime`, `NotifyWhenRead`.
- **Allowed attachment extensions** — `AuthorizedExtensions` (leave `null` to accept every extension; invalid entries are dropped).
- **Callback parameters from contextual keys** — fetched via `LicenceHelper.GetSerialInfo` and serialized as `CallBackParameter` entries. Required for Outlook Classic compatibility.

```csharp
var licence = LicenceHelper.GetSerialInfo(new GetSerialInfoArgs(
    Config.EndPoint, Config.SerialNumber, Config.APIUser, Config.APIPsw));

var callBackParameters = licence.ContextualKeys
    .Select(k => new CallBackParameter(k.Key, "your value"))
    .ToList();

var args = new GetEnveloppeArgs(
    Config.EndPoint, Config.SerialNumber, Config.APIUser, Config.APIPsw,
    "Document return", "signer@example.com", Config.Culture)
{
    MaximumReplyOpenTime = 5,
    ReplyExpirationHours = 1,
    NotifyWhenRead       = true,
    ReplyOptions         = new SeReplyOptions { NoEditor = true, NoSign = true, Password = true },
    AuthorizedExtensions = new[] { ".pdf", ".png", ".jpg" },
    CallBackParameters   = SerializationHelper.SerializeToJson(callBackParameters)
};

var response = MessageHelper.GetEnveloppe(args);
// response.Url, response.RepID, response.Recipient.Email
```

## Azure Functions + .NET 8

[`AzureFunctionsSignature.netCore`](CodeSample/AzureFunctionsSignature.netCore/) is a ready-to-deploy Azure Functions v4 app (isolated worker, .NET 8) that uses `SecureExchanges.NetCore` and `SecureExchanges.Signature.NetCore` to send signed messages. Included:

- [`SendMessage.cs`](CodeSample/AzureFunctionsSignature.netCore/SendMessage.cs) — HTTP-triggered function entry point
- [`Dockerfile`](CodeSample/AzureFunctionsSignature.netCore/Dockerfile) — Linux container image with the Chromium system dependencies needed for PDF certification
- [`build-and-push.ps1`](CodeSample/AzureFunctionsSignature.netCore/build-and-push.ps1) — helper to build the image and push it to a registry

## SESAR webhook tool

[`Tool/GenerateSESARManifestWebHook`](CodeSample/Tool/GenerateSESARManifestWebHook/) is a small console utility that generates a SESAR webhook manifest (keys, IVs, base64 headers) and can optionally fire the webhook once the manifest is ready — useful when wiring up a SESAR-capable endpoint for the first time.

## Support

- General: [support@secure-exchanges.com](mailto:support@secure-exchanges.com)
- Developer / SDK integration: [supportdev@secure-exchanges.info](mailto:supportdev@secure-exchanges.info)
- Website: [secure-exchanges.com](https://www.secure-exchanges.com)
- Help portal: [help.secure-exchanges.com](https://help.secure-exchanges.com)
