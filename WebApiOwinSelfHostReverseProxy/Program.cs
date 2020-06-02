using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;
using WebApiOwinSelfHostReverseProxy.MessageHandlers;

namespace WebApiOwinSelfHostReverseProxy
{
    class Program
    {
        static readonly AutoResetEvent WaitEvent = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            var options = new StartOptions(args[0]);

            var client = new HttpClient(new HttpClientHandler {UseCookies = false}, true);

            using (WebApp.Start(options, builder =>
            {
                var configuration = new HttpConfiguration(); 

                configuration.Routes.MapHttpRoute(
                    "Proxy",
                    "{*path}",
                    new
                    {
                        path = RouteParameter.Optional
                    },
                    null,
                    new ProxyDelegatingHandler(args[1], client)
                );

                builder.UseWebApi(configuration);
            })) 
            {
                Console.WriteLine("Now listening on: {0}", args[0]);
                Console.WriteLine("Pass to: {0}", args[1]);
                Console.WriteLine("Application started. Press Ctrl+C to shut down.");
                Console.WriteLine();

                WaitEvent.WaitOne();
            }
        }

        static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            WaitEvent.Set();

            e.Cancel = true;
        }
    }
}
