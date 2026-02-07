using SecureExchangesSDK.Helpers;
using SecureExchangesSDK.Models.Messenging;
using SecureExchangesSDK.Models.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GenerateSESARManifestWebHook
{
  internal class Program
  {
    static void Main(string[] args)
    {

      List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();

      headers.Add(new KeyValuePair<string, string>("A", "A value"));
      headers.Add(new KeyValuePair<string, string>("B", "B value"));

      string str = SerializationHelper.SerializeToJson(headers);

      Console.WriteLine("Quel est le dossier de stockage de SESAR ?");
      string path = Console.ReadLine();

      if (!Directory.Exists(path))
      {
        Console.WriteLine("Le dossier n'existe pas");
        return;
      }

      Console.WriteLine("Quel est le répertoire ou stocker les webhook?");
      string hookDirectory = Console.ReadLine();


      Console.WriteLine("Quel est la clé en base 64 du webhook?");
      string hookKey = Console.ReadLine();

      Console.WriteLine("Quel est le iv en base 64 du webhook?");
      string hookIv = Console.ReadLine();


      Console.WriteLine("Souhaitez vous déclancher les webhook? Indique y pour lancer les webhooks");
      string launchWebhook = Console.ReadLine();
      bool webHookCall = launchWebhook.ToLower() == "y";


      bool HeadersErrors = false;
      do
      {
        HeadersErrors = false;
        Console.WriteLine("Ajouter des en-têtes à vos appels ? (tableau de paires clé/valeur)");
        string headersString = Console.ReadLine();
        try
        {
          if (!string.IsNullOrEmpty(headersString))
          {
            headers = SerializationHelper.DeserializeFromJson<List<KeyValuePair<string, string>>>(headersString);
          }
        }
        catch (Exception ex)
        {
          HeadersErrors = true;
          Console.WriteLine($"Impossible de desérializer les valeurs du headers. {ex.Message}.");
        }
      } while (HeadersErrors);

      string webhookURL = string.Empty;
      if (webHookCall)
      {
        Console.WriteLine("Quel est l'adresse du webhook?");
        webhookURL = Console.ReadLine();
      }


      ScanDir(path, hookDirectory, hookKey, hookIv);

      if (launchWebhook.ToLower() == "y")
      {
        string[] hook = Directory.GetFiles(hookDirectory);
        foreach (var h in hook)
        {
          WebHookCall(h, webhookURL, hookKey, headers);
        }
      }

    }


    private static void ScanDir(string path, string hookDirectory, string hookKey, string hookIv)
    {
      string[] directory = Directory.GetDirectories(path);

      // iterate each file, and ensure theire is no other file that is newer that the delted delay
      foreach (var dir in directory)
      {
        ScanDir(dir, hookDirectory, hookKey, hookIv);
        if (Directory.Exists(dir))
        {

          // if no files we delete
          if (Directory.GetFiles(dir).Count() == 0)
          {
            continue;
          }
          else // file are less than one hour
          {
            string[] files = Directory.GetFiles(dir, "manifest.json");
            // todo : Mettre la logique de validation, et s'assuré que tu n'est pas trop loins en arrière.
            if (files.Length == 1)
            {
              string manifestPath = files[0];

              var storeM = SerializationHelper.DeserializeFromJson<StoreManifest>(File.ReadAllText(manifestPath));

              // Write the webhook file.
              File.WriteAllText(Path.Combine(hookDirectory, Guid.NewGuid().ToString()), CryptoHelper.EncryptStringToB64(SerializationHelper.SerializeToJson(storeM), Convert.FromBase64String(hookKey), Convert.FromBase64String(hookIv)));


            }

          }
        }
      }


    }

    private static void WriteVerbose(string msg)
    {
      Console.WriteLine(msg);
    }

    private static bool WebHookCall(string filePath, string webHookURL, string hookKey, List<KeyValuePair<string, string>> headers)
    {
      try
      {
        WriteVerbose($"Send file {filePath} to web hook weebHook");
        HttpWebRequest wr = WebRequest.Create(webHookURL) as HttpWebRequest;

        SesarWebHook webHook = new SesarWebHook();
        webHook.CryptedObject = File.ReadAllText(filePath);
        webHook.HashKey = CryptoHelper.GetSHA512HashOfString(hookKey);

        wr.Timeout = 60000;
        wr.Method = "POST";
        wr.Accept = "application/json";
        wr.ContentType = "application/json";
        wr.UserAgent = "SESAR";
        wr.Headers.Add("HashKey", webHook.HashKey);

        if (headers.Any())
        {
          foreach (var h in headers)
          {
            wr.Headers.Add(h.Key, h.Value);
          }
        }

          using (StreamWriter dataStream = new StreamWriter(wr.GetRequestStream()))
          {
            dataStream.Write($"{{ \"args\" : {SerializationHelper.SerializeToJson(webHook)}}}");
          }

          using (WebResponse response = wr.GetResponse())
          {
            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
            {
              WriteVerbose($"Send file {filePath} to web hook {webHookURL} is success");
              return true;
            }
            WriteVerbose($"Send file {filePath} to web hook {webHookURL} is fail");
          }


        }
      catch (Exception ex)
      {
        WriteVerbose(ex.ToString());
      }
      return false;
      //        HttpWebResponse rep = (HttpWebResponse)wr.GetResponse();
      //        if (rep.StatusCode != HttpStatusCode.OK) throw new Exception();
    }

  }
}
