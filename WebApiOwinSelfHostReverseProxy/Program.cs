using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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

            var cookieContainer = ReadCookiesFromDisk("Cookies.db");

            var client = new HttpClient(new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer
            }, true);

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
                    new ProxyDelegatingHandler(args[1], client, cookieContainer)
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

            WriteCookiesToDisk("Cookies.db", cookieContainer);
        }

        static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            WaitEvent.Set();

            e.Cancel = true;
        }

        static void WriteCookiesToDisk(
            string file,
            CookieContainer cookieJar)
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name);

            Directory.CreateDirectory(appData);

            using (Stream stream = File.Create(Path.Combine(appData, file)))
            {
                try
                {
                    Console.Out.Write("Writing cookies to disk... ");

                    var formatter = new BinaryFormatter();

                    formatter.Serialize(stream, cookieJar);

                    Console.Out.WriteLine("Done.");
                }

                catch (Exception e)
                {
                    Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
                }
            }
        }

        static CookieContainer ReadCookiesFromDisk(
            string file)
        {
            try
            {
                var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetExecutingAssembly().GetName().Name);

                Directory.CreateDirectory(appData);

                using (Stream stream = File.Open(Path.Combine(appData, file), FileMode.Open))
                {
                    Console.Out.Write("Reading cookies from disk... ");

                    var formatter = new BinaryFormatter();

                    Console.Out.WriteLine("Done.");

                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }

            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());

                return new CookieContainer();
            }
        }
    }
}
