using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebApiOwinSelfHostReverseProxy.MessageHandlers
{
    public class ProxyDelegatingHandler :
            DelegatingHandler
    {
        private readonly HttpClient _client;
        private readonly Uri _uri;

        public ProxyDelegatingHandler(
            string baseAddress,
            HttpClient client)
        {
            _client = client;
            _uri = new Uri(baseAddress);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var context = request.GetOwinContext();

            request.RequestUri = new Uri(_uri, request.RequestUri.LocalPath);

            Console.WriteLine(request.RequestUri);

            request.Headers.Add("X-Forwarded-For", context.Request.RemoteIpAddress);
            request.Headers.Add("X-Real-IP", context.Request.RemoteIpAddress);
            request.Headers.Add("X-Forwarded-Proto", _uri.Scheme);

            request.Headers.Host = _uri.Host;

            if (request.Method == HttpMethod.Get)
            {
                request.Content = null;
            }

            return await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _client.Dispose();
        }
    }
}