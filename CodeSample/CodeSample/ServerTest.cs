using System;
using System.Configuration;
using CodeSample.Helper;
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
      System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom("SecureExchangesSDK.dll");
      string version = assembly.GetName().Version.ToString();
      SecureExchangesSDK.Helpers.StateHelper.TestServer(new BaseEndpointConfiguration(EndpointHelper.EndPoint),$"SDK {version}");
    }
  }
}
