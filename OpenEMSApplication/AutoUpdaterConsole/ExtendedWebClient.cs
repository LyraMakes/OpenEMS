using System.Diagnostics.CodeAnalysis;
using System.Net;
#pragma warning disable SYSLIB0014

namespace OpenEMSApplication.AutoUpdaterConsole;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public class ExtendedWebClient : WebClient
{
    private readonly CookieContainer _cookieContainer = new CookieContainer();
    
    public int Timeout { get; set; }
    
    public new bool AllowWriteStreamBuffering { get; set; }
    
    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest? request = base.GetWebRequest(address);
        if (request == null) return null!;

        request.Timeout = Timeout;
        IWebProxy systemWebProxy = WebRequest.GetSystemWebProxy();
        systemWebProxy.Credentials = CredentialCache.DefaultNetworkCredentials;
        request.Proxy = systemWebProxy;
        request.UseDefaultCredentials = true;

        if (request is not HttpWebRequest webRequest) return request;
        
        webRequest.AllowWriteStreamBuffering = AllowWriteStreamBuffering;
        webRequest.CookieContainer = _cookieContainer;
        webRequest.AllowAutoRedirect = true;
        webRequest.ServicePoint.Expect100Continue = false;
        webRequest.KeepAlive = true;
        webRequest.Headers[HttpRequestHeader.AcceptEncoding] = "deflate, gzip";
        webRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        return request;
    }

    public ExtendedWebClient() => Timeout = 100000;
}