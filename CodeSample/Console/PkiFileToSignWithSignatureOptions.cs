using SecureExchangesSDK.Helpers;
using SecureExchangesSDK.Models;
using SecureExchangesSDK.Models.Args;
using SecureExchangesSDK.Models.JSON;
using SecureExchangesSDK.Models.Entity;
using SecureExchangesSDK.Models.Transport;
using SecureExchangesSDK.SecureExchanges;
using SecureExchangesSignatureSDK.Helpers;
using SecureExchangesSignatureSDK.Models.Args;
using SecureExchangesSignatureSDK.Models.Transport;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;
using SecureExchangesSDK.Models.Enum;

namespace ConsoleSample
{
  internal static class PkiFileToSignWithSignatureOptions
  {
    public static async Task Send()
    {
      // Indique que nous utilisons le mode PKI pour la certification des PDFs
      bool isPki = true;

      // ============================================================
      // 1. CHARGEMENT ET HASHAGE DU FICHIER
      // ============================================================
      // Chemin du fichier local à signer
      string pdfPath = @"c:\temp\contrat.pdf";
      string pdfName = Path.GetFileName(pdfPath);
      byte[] pdfBytes = File.ReadAllBytes(pdfPath);

      // Calcul de l'empreinte numérique (Hash SHA512) pour garantir l'intégrité du fichier
      // Ce hash servira d'identifiant unique pour lier les zones au fichier
      string sha512 = CryptoHelper.GetSHA512HashOfBytes(pdfBytes);


      // ============================================================
      // 2. DÉFINITION DES ZONES DE SIGNATURE
      // ============================================================
      SignZoneDetails[] zones;
      // Utilisation du ZoneBuilder pour créer les coordonnées X,Y sur le PDF
      using (ZoneBuilder zoneBuilder = new ZoneBuilder(pdfPath, isPki))
      {
        zones = new SignZoneDetails[]
        {
          zoneBuilder.CreateFieldSignZone(1, "signature1", 60, 750, 200, 50),
          zoneBuilder.CreateFieldDateSignZone(1, "date1", 60, 820, 100, 25, 12)
        };
      }

      // ============================================================
      // 3. ASSOCIATION DES ZONES AU DESTINATAIRE
      // ============================================================
      string recipientEmail = "signataire@exemple.com";
      var recipient = new RecipientInfo { Email = recipientEmail };

      // Création du dictionnaire qui lie le destinataire aux définitions de zones du fichier
      var recipientIndex = new Dictionary<RecipientInfo, List<FileZoneDefinition>>
      {
        {
          recipient,
          new List<FileZoneDefinition>
          {
            new FileZoneDefinition
            {
              // Lien via le Hash du fichier calculé plus tôt
              UniqueName = sha512,
              ZonesDef = new SignZoneDefinition { Mode = "define", Zones = zones }
            }
          }
        }
      };

      var recipients = new List<RecipientInfo> { recipient };

      // ============================================================
      // 4. OBTENTION DU JETON DE CERTIFICATION
      // ============================================================
      // Prépare la demande pour obtenir l'autorisation de certifier ce hash spécifique
      var certificationTokenArgs = new GetCertificationTokenSDKArgs(
          Config.EndPoint,
          Config.SerialNumber,
          Config.APIUser,
          Config.APIPsw,
          sha512,
          CertificationTokenType.CertifyDocument,
          Config.Culture
      );

      // Appel API pour récupérer les données de certification
      var certificationTokenResponse = SignHelper.GetCertificationToken(certificationTokenArgs);

      // ============================================================
      // 5. CERTIFICATION DU PDF
      // ============================================================
      var certifyPdfArgs = new CertifyPdfArgs
      {
        File = new FileArgs(pdfBytes, pdfName, FileHelper.GetMimeType(pdfName)),
        // Conversion du dictionnaire de zones pour l'argument
        RecipientZoneDefinitions = SignHelper.ConvertRecipientIndexToList(recipientIndex),
        Recipients = recipients.Select(r => r.Email).ToList(),
        CertifyData = certificationTokenResponse.CertificationData, // Données reçues à l'étape 4
        DetectExistingFields = true, // Tente de détecter les champs AcroForm existants

        // Logique pour assigner les zones au destinataire spécifique dans le processus de certification
        // Optionnel si un seul destinataire utilise les champs existants du PDF.
        DefineRecipients = (IReadOnlyDictionary<string, List<ZoneDetails>> zonesDictionary, IReadOnlyList<ZoneDetails> definedZones) =>
        {
          // On assigne les zones définies à l'email du destinataire
          zonesDictionary[recipientEmail].AddRange(definedZones);
          return zonesDictionary;
        }
      };

      CertifiedPdfContainer certifiedPdf;
      using (var pkiHelper = new PkiFileHelper(Config.IsSandBox, Config.Culture))
      {
        try
        {
          // Le helper peut etre reutilise pour plusieurs PDFs
          certifiedPdf = await pkiHelper.CertifyPkiPdf(certifyPdfArgs);
        }
        catch (Exception ex)
        {
          throw new Exception($"Echec de la certification PDF: {ex.Message}");
        }
      }

      // ============================================================
      // 6. CONFIGURATION DE L'ENVOI (MESSAGE ET OPTIONS)
      // ============================================================
      string message = "Veuillez signer ce contrat";
      string subject = "Contrat a signer";
      string password = null; // Mot de passe optionnel pour ouvrir le message
      List<FileArgs> fileList = null; // Utilisé pour les attachements ou la signature non pki, null car le fichier est envoyé via CertifiedPdfs plus bas
      SendMethodEnum sendMethod = SendMethodEnum.onlyEmail;
      string culture = Config.Culture; // Langue de l'interface de signature
      bool getBackHtml = false; // Retourne le HTML du courriel pour l'envoyer via votre propre serveur SMTP
      bool showSubject = true; // DEPRECATED
      bool getNotify = true; // Active la notification d'ouverture
      int maximumOpenTime = 5; // Nombre d'ouvertures max
      int minutesExpiration = 10080; // 1 semaine (en minutes)

      // --- Options de signature ---
      var signatureOptions = new SignOptions()
      {
        // Restreindre le mode de signature : Ici, seulement "Trace" (dessin à la main/souris) autorisé
        SignatureRestriction = SignatureModeRestriction.Trace,

        // Options pour demander au signataire de téléverser un fichier en retour
        UploadFileOptions = new SignOptionsFileUpload()
        {
          AuthorizedExtensions = new string[] { ".pdf", ".png", ".jpg" },
          NoEditor = false,
          // Oblige l'utilisateur à uploader un fichier avant de pouvoir terminer
          UploadMode = ReplyFileUploadMode.UploadMandatory,
          Messages = new List<ReplyOptionsMessage>()
          {
            new ReplyOptionsMessage()
            {
              CultureID = "fr-CA",
              Message = "Veuillez joindre votre spéciment chèque en retour."
            },
            new ReplyOptionsMessage()
            {
              CultureID = "en-CA",
              Message = "Please attach your null check return."
            }
          }
        }
      };

      // --- Paramètres de rappel (Callback) ---
      // Récupération des infos de licence pour obtenir les clés contextuelles
      // Le paramètre de rappel peut être n'importe quelle chaîne de caractères, mais pour la compatibilité avec Outlook Classique il faut récupérer les clés contextuelles
      var serialInfoArgs = new GetSerialInfoArgs(
        Config.EndPoint,
        Config.SerialNumber,
        Config.APIUser,
        Config.APIPsw
      );

      var licence = LicenceHelper.GetSerialInfo(serialInfoArgs);

      // Transformation des clés contextuelles en paramètres de callback
      List<CallBackParameter> callBackParameters = licence.ContextualKeys
        .Select(contextualKey => new CallBackParameter(contextualKey.Key, "votre valeur"))
        .ToList();
       
      var args = new MutliRecipientArgs(
        Config.EndPoint,
        Config.SerialNumber,
        Config.APIUser,
        Config.APIPsw,
        recipients,
        message,
        subject,
        password,
        fileList,
        sendMethod,
        getBackHtml,
        showSubject,
        getNotify,
        Config.Culture,
        maximumOpenTime,
        minutesExpiration
      )
      {
        OwnerDontNeedToSign = true, // L'expéditeur n'a pas besoin de signer
        CertifiedPdfs = new List<CertifiedPdfContainer> { certifiedPdf }, // On joint le PDF certifié
        SignatureOptions = signatureOptions,
        // Sérialisation des paramètres de callback en JSON
        CallBackParameters = SerializationHelper.SerializeToJson(callBackParameters)
      };

      // ============================================================
      // 7. ENVOI ET RÉSULTAT
      // ============================================================
      var response = MessageHelper.MultiRecipientMessage(args);

      if (response.Status == 200)
      {
        Console.WriteLine("Document PKI envoyé pour signature avec succès.");
        // Récupération de l'URL unique pour le premier signataire
        var signerAnswer = response.RecipientsAnswer[0].Answer;
        Console.WriteLine($"URL de signature: {signerAnswer.URL}");
        Console.WriteLine($"Code de sécurité: {signerAnswer.DefaultOpeningCode}");
      }
      else
      {
        Console.WriteLine($"Erreur lors de l'envoi: {response.Data}");
      }
    }
  }
}
