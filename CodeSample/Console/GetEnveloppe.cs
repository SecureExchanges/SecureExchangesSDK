using SecureExchangesSDK.Helpers;
using SecureExchangesSDK.Models.Args;
using SecureExchangesSDK.Models.JSON;
using SecureExchangesSDK.Models.Transport;
using Recipient = SecureExchangesSDK.SecureExchanges.Recipient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleSample
{
  internal static class GetEnveloppe
  {
    public static void GetOne()
    {
      // Configuration de base
      string subject = "Récupération des preuves";
      string destination = "email@exemple.com"; // Email du destinataire
      string culture = Config.Culture; // Langue de l'interface

      // Configuration de sécurité et de cycle de vie
      int maximumReplyOpenTime = 5; // Nombre maximum d'ouvertures autorisées pour le retour
      int hoursExpiration = 1; // Durée de validité du lien en heures (expiration)
      bool notifyWhenRead = true; // Recevoir une notification par courriel à l'ouverture (par défaut: true)
      bool replyToAPI = false; // OBSOLÈTE


      // ============================================================
      // Paramètres de rappel (Callback)
      // ============================================================
      // Récupération des infos de licence pour obtenir les clés contextuelles.
      // Le paramètre de rappel peut être n'importe quelle chaîne, mais pour la compatibilité 
      // avec Outlook Classique, l'utilisation des clés contextuelles est requise.
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


      // ============================================================
      // Options de l'interface de réponse (SeReplyOptions)
      // ============================================================
      SeReplyOptions replyOptions = new SeReplyOptions
      {
        NoEditor = true,  // Masque l'éditeur de texte (juste l'upload de fichier)
        NoSign = true,    // Le fichier ne nécessitera pas de signature
        Password = true   // Permet à l'utilisateur de protéger sa réponse par mot de passe
      };



      // ============================================================
      // Liste des extensions de fichiers autorisées.
      // Si le tableau est null, toutes les extensions sont permises.
      // Remplissez le tableau avec des extensions comme ".jpg", ".pdf", ".docx".
      // Vous pouvez aussi utiliser MimeHelper.MIMETypesDictionary pour voir les extensions disponibles.
      // Les extensions invalides seront supprimées.
      // ============================================================
      string[] authorizedExtensions = new string[] { ".pdf", ".png", ".jpg" };


      // ============================================================
      // Construction des arguments pour la demande d'enveloppe
      // ============================================================
      var args = new GetEnveloppeArgs(
        Config.EndPoint,
        Config.SerialNumber,
        Config.APIUser,
        Config.APIPsw,
        subject,
        destination,
        culture
      )
      {
        MaximumReplyOpenTime = maximumReplyOpenTime,
        ReplyExpirationHours = hoursExpiration,
        NotifyWhenRead = notifyWhenRead,
        ReplyToAPI = replyToAPI,
        // Sérialisation des paramètres de callback en JSON
        CallBackParameters = SerializationHelper.SerializeToJson(callBackParameters),
        ReplyOptions = replyOptions,
        AuthorizedExtensions = authorizedExtensions
      };

      // ============================================================
      // Appel API et Gestion de la réponse
      // ============================================================
      var response = MessageHelper.GetEnveloppe(args);

      if (response.StatusCode == 200)
      {
        Console.WriteLine("Demande d'enveloppe créée avec succès.");
        Console.WriteLine($"URL de l'enveloppe: {response.Url}");
        Console.WriteLine($"Destination: {response.Recipient.Email}");
        Console.WriteLine($"ID de réponse (Reply ID): {response.RepID}");
      }
      else
      {
        Console.WriteLine($"Erreur lors de la création: {response.Data}");
      }
    }

    public static void GetMany()
    {
      // Configuration de base
      string subject = "Récupération des preuves";
      // Email du destinataires
      List<Recipient> recipients = new List<Recipient>
      {
        new Recipient{ Email = "email1@exemple.com"},
        new Recipient{ Email = "email2@exemple.com"},
      };
      string culture = Config.Culture; // Langue de l'interface

      // Configuration de sécurité et de cycle de vie
      int maximumReplyOpenTime = 5; // Nombre maximum d'ouvertures autorisées pour le retour
      int hoursExpiration = 1; // Durée de validité du lien en heures (expiration)
      bool notifyWhenRead = true; // Recevoir une notification par courriel à l'ouverture (par défaut: true)
      bool replyToAPI = false; // OBSOLÈTE


      // ============================================================
      // Paramètres de rappel (Callback)
      // ============================================================
      // Récupération des infos de licence pour obtenir les clés contextuelles.
      // Le paramètre de rappel peut être n'importe quelle chaîne, mais pour la compatibilité 
      // avec Outlook Classique, l'utilisation des clés contextuelles est requise.
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


      // ============================================================
      // Options de l'interface de réponse (SeReplyOptions)
      // ============================================================
      SeReplyOptions replyOptions = new SeReplyOptions
      {
        NoEditor = true,  // Masque l'éditeur de texte (juste l'upload de fichier)
        NoSign = true,    // Le fichier ne nécessitera pas de signature
        Password = true   // Permet à l'utilisateur de protéger sa réponse par mot de passe
      };



      // ============================================================
      // Liste des extensions de fichiers autorisées.
      // Si le tableau est null, toutes les extensions sont permises.
      // Remplissez le tableau avec des extensions comme ".jpg", ".pdf", ".docx".
      // Vous pouvez aussi utiliser MimeHelper.MIMETypesDictionary pour voir les extensions disponibles.
      // Les extensions invalides seront supprimées.
      // ============================================================
      string[] authorizedExtensions = new string[] { ".pdf", ".png", ".jpg" };


      // ============================================================
      // Construction des arguments pour la demande d'enveloppe
      // ============================================================
      var args = new GetEnvelopesArgs(
        Config.EndPoint,
        Config.SerialNumber,
        Config.APIUser,
        Config.APIPsw,
        subject,
        recipients,
        culture
      )
      {
        MaximumReplyOpenTime = maximumReplyOpenTime,
        ReplyExpirationHours = hoursExpiration,
        NotifyWhenRead = notifyWhenRead,
        ReplyToAPI = replyToAPI,
        // Sérialisation des paramètres de callback en JSON
        CallBackParameters = SerializationHelper.SerializeToJson(callBackParameters),
        ReplyOptions = replyOptions,
        AuthorizedExtensions = authorizedExtensions
      };

      // ============================================================
      // Appel API et Gestion de la réponse
      // ============================================================
      var response = MessageHelper.GetEnvelopes(args);

      foreach (var answer in response.Answers)
      {
        if (answer.StatusCode == 200)
        {
          Console.WriteLine("Demande d'enveloppe créée avec succès.");
          Console.WriteLine($"URL de l'enveloppe: {answer.Url}");
          Console.WriteLine($"Destination: {answer.Recipient.Email}");
          Console.WriteLine($"ID de réponse (Reply ID): {answer.RepID}");
        }
        else
        {
          Console.WriteLine($"Erreur lors de la création: {answer.Data}");
        }       
      }
    }
  }
}
