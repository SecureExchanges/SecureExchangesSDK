using SecureExchangesSDK;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSample.Helper
{
  public static class EndpointHelper
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
  }
}
