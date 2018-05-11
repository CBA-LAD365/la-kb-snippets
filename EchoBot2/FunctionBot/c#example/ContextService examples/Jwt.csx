using System;
using System.Collections.Generic;
using Jose;
using System.Security.Cryptography.X509Certificates;


/// <summary>
/// Utility class for constructing a Signed JWT
/// </summary>
public class Jwt
{
    /// <summary>
    /// Create a JWT using provided context data and sign using private key from certBase64.
    /// Note: The matching public must be provisioned to Live Assist via the Live Assist Management Portal 
    /// </summary>
    /// <param name="contextId"> ID for the context data, as provided by Live Assist Chat SDK</param>
    /// <param name="contextData">Context data, provided by Chat visitor </param>
    /// <returns></returns>
    public static String Create(string contextId, ContextData contextData)
    {
        var payload = new Dictionary<string, object>()
            {
                { "contextId", contextId},
                { "contextData", contextData }
            };

        JWT.DefaultSettings.JsonMapper = new NewtonsoftMapper();

        X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        certStore.Open(OpenFlags.ReadOnly);
        X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, "AA78DF903BDFB734BEA89EE4C535F4BBE85A1A5C", false) ;
        X509Certificate2 cert = null;
        if (certCollection.Count > 0)
        {
            cert = certCollection[0];
        }
        certStore.Close();

        var privateKey = cert.GetRSAPrivateKey();
        string token = JWT.Encode(payload, privateKey, JwsAlgorithm.RS256);
        return token;
    }
}