using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SecureExchangesSDK.Models;
using SecureExchangesSDK.Models.Contract;

namespace SecureExchangesSamples
{
  [TestClass]
  public class ServerTest
  {
    [TestMethod]
    public void TestConnectionWithServer()
    {
      
      SecureExchangesSDK.Helpers.StateHelper.TestServer(new BaseEndpointConfiguration(new Uri(ConfigurationManager.AppSettings["endpointURI"].ToString())));
    }
  }
}
