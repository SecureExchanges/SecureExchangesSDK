using SecureExchangesSDK;
using System;
using System.Configuration;
using System.Diagnostics;

namespace ConsoleSample
{
  internal static class Config
  {
    private static Uri _endpoint = null;
    private static bool? _isSandBox = null;
    private static string _culture = null;

    private static Guid? _serialNumber = null;
    private static Guid? _apiUser = null;
    private static Guid? _apiPsw = null;

    public static bool IsSandBox
    {
      get
      {
        if(_isSandBox == null)
        {
          string configValue = ConfigurationManager.AppSettings["IsSandBox"];
          bool.TryParse(configValue, out bool isSandBox);
          _isSandBox = isSandBox;
        }

        return _isSandBox.Value;
      }
    }

    public static Uri EndPoint
    {
      get
      {
        if (_endpoint == null)
        {
          if (IsSandBox)
          {
            // By default the SDK know the production fileHandler and SEMS handler. In sandbox mode we must specify.
            SettingsHelper.CustomFileHandler = "https://preview.secure-exchanges.com/Handler/ReadFileTob64.ashx"; // Set the file handler to sandbox
            SettingsHelper.CustomSEMSEndpoint = "https://previewsems.secure-exchanges.com/SEMS_api.asmx"; // Set the SEMS endpoint to sandbox
                                                                                                          // Set the Sandbox URI and the SANDBOX handler
            _endpoint = new Uri("https://preview.secure-exchanges.com/_api/0217/0217.asmx");
          }
          else
          {
            _endpoint = new Uri("https://www.secure-exchanges.com/_api/0217/0217.asmx");
          }
        }

        return _endpoint;        
      }
    }

    public static string Culture
    {
      get
      {
        if(_culture == null)
        {
          string configValue = ConfigurationManager.AppSettings["Culture"];
          _culture = !string.IsNullOrWhiteSpace(configValue) ? configValue : "fr-CA";
        }

        return _culture;
      }
    }

    /// <summary>
    /// API serial number
    /// </summary>
    public static Guid SerialNumber
    {
      get
      {
        if (_serialNumber == null)
        {
          string configValue = ConfigurationManager.AppSettings["Serial"];
          Guid.TryParse(configValue, out Guid result);
          _serialNumber = result;
        }
        return _serialNumber.Value;
      }
    }

    /// <summary>
    /// API user number
    /// </summary>
    public static Guid APIUser
    {
      get
      {
        if (_apiUser == null)
        {
          string configValue = ConfigurationManager.AppSettings["APIUser"];
          Guid.TryParse(configValue, out Guid result);
          _apiUser = result;
        }
        return _apiUser.Value;
      }
    }

    /// <summary>
    /// API psw
    /// </summary>
    public static Guid APIPsw
    {
      get
      {
        if (_apiPsw == null)
        {
          string configValue = ConfigurationManager.AppSettings["APIPsw"];
          Guid.TryParse(configValue, out Guid result);
          _apiPsw = result;
        }
        return _apiPsw.Value;
      }
    }
  }
}
