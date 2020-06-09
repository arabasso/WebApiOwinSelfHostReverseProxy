using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApiOwinSelfHostReverseProxy.MessageHandlers
{
    public class ProxyDelegatingHandler :
            DelegatingHandler
    {
        private readonly HttpClient _client;
        private readonly CookieContainer _cookieContainer;
        private readonly Uri _uri;

        public ProxyDelegatingHandler(string baseAddress,
            HttpClient client, CookieContainer cookieContainer)
        {
            _client = client;
            _cookieContainer = cookieContainer;

            _uri = new Uri(baseAddress);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.RequestUri = new Uri(_uri, request.RequestUri.PathAndQuery);

            var context = request.GetOwinContext();

            request.Headers.Add("X-Forwarded-For", context.Request.RemoteIpAddress);
            request.Headers.Add("X-Real-IP", context.Request.RemoteIpAddress);
            request.Headers.Add("X-Forwarded-Proto", _uri.Scheme);

            request.Headers.Host = _uri.Host;
            request.Headers.Remove("Origin");
            request.Headers.Add("Origin", _uri.OriginalString);

            if (request.Method == HttpMethod.Get)
            {
                request.Content = null;
            }

            return await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _client.Dispose();
        }
    }
}