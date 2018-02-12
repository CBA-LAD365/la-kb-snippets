X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    certStore.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, <String - Thumbprint for the certificate>, false);
    X509Certificate2 cert = null;
    if (certCollection.Count > 0)
    {
        cert = certCollection[0];
    }
    certStore.Close();
 
    var privateKey = cert.GetRSAPrivateKey();