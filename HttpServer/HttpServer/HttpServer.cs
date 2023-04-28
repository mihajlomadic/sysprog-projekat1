using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    internal class HttpServer
    {

        #region Response body constants

        public static readonly byte[] badRequestBody = Encoding.ASCII.GetBytes("<h1>Bad request.</h1>");
        public static readonly byte[] forbiddenRequestBody = Encoding.ASCII.GetBytes("<h1>Forbidden.</h1>");
        public static readonly byte[] notFoundRequestBody = Encoding.ASCII.GetBytes("<h1>Not found.</h1>");

        #endregion

        private void SendResponse(HttpListenerContext context, byte[] responseBody, string contentType, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            // Formiramo string koji ce biti logovan na konzoli
            // i handle-ovanju zahteva
            string logString = string.Format(
                "REQUEST:\n{0} {1} HTTP/{2}\nHost: {3}\nUser-agent: {4}\n-------------------\nRESPONSE:\nStatus: {5}\nDate: {6}\nContent-Type: {7}\nContent-Length: {8}\n",
                context.Request.HttpMethod,
                context.Request.RawUrl,
                context.Request.ProtocolVersion,
                context.Request.UserHostName,
                context.Request.UserAgent,
                statusCode,
                DateTime.Now,
                contentType,
                responseBody.Length
            );

            // Postavljamo parametre response-a i upisujemo podatke u body
            context.Response.ContentType = contentType;
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentLength64 = responseBody.Length;
            // Saljemo response i radimo cleanup objekta outputStream
            using (Stream outputStream = context.Response.OutputStream)
            {
                outputStream.Write(responseBody, 0, responseBody.Length);
            }
            Console.WriteLine(logString);
        }

        private string serverAddress;
        private uint portNumber;
        private string rootDir;
        private ReaderWriterCache<string, byte[]> cache = new ReaderWriterCache<string, byte[]>();

        public HttpServer(string serverAddress, uint portNumber, string rootDir)
        {
            this.serverAddress = serverAddress;
            this.portNumber = portNumber;
            this.rootDir = rootDir;
        }

        public void Launch()
        {

            using (HttpListener httpListener = new HttpListener())
            {
                // Kreiranje HttpListener objekta, koji nam sluzi 
                // za prihvatanje http zahteva
                httpListener.Prefixes.Add($"http://{serverAddress}:{portNumber}/");
                httpListener.Start();
                Console.WriteLine("Server has launched!\n");

                while (httpListener.IsListening)
                {
                    // 'Ulazna tacka' za http zahteve
                    // HttpListenerContext je objekat preko koga mozemo
                    // ocitati parametre http zahteva (request-a), odnosno upravljati odgovorom
                    HttpListenerContext context = httpListener.GetContext();

                    // Pri pristizanju novog zahteva, dajemo je na obradu
                    // backgroung niti iz ThreadPool-a, i nastavljamo da slusamo
                    ThreadPool.QueueUserWorkItem((object httpListenerContext) =>
                    {
                        try
                        {
                            HttpListenerContext localContext = httpListenerContext as HttpListenerContext;
                            if (localContext == null)
                                throw new ArgumentException($"{httpListenerContext} is not a reference to an HttpListenerContext object!");

                            string fileName = Path.GetFileName(localContext.Request.RawUrl);
                            string fileExtension = Path.GetExtension(fileName);

                            #region Validation

                            // Saljemo nazad BadRequest ukoliko korisnik ne navede ime fajla
                            if (fileName == string.Empty || fileExtension == string.Empty)
                            {
                                SendResponse(context, badRequestBody, "text/html", HttpStatusCode.BadRequest);
                                return;
                            }

                            // Ukoliko trazeni dokument nije zahtevanog tipa, saljemo nazad Forbidden
                            if (fileExtension != ".gif" && fileExtension != ".png")
                            {
                                SendResponse(context, forbiddenRequestBody, "text/html", HttpStatusCode.Forbidden);
                                return;
                            }

                            // Nalazimo putanju do naseg fajla (trazeci u root dir-u i svim njegovim subdir-ovima)
                            // !!! Ovo moze da bude bottleneck
                            string filePath = Directory.GetFiles(rootDir, fileName, SearchOption.AllDirectories).FirstOrDefault();

                            // Ukoliko trazeni fajl nije nadjen, saljemo nazad NotFound
                            if (filePath == null)
                            {
                                SendResponse(context, notFoundRequestBody, "text/html", HttpStatusCode.NotFound);
                                return;
                            }

                            #endregion

                            // Ukoliko je zahtev prosao validaciju, ispitujemo da li je 
                            // dati fajl vec zahtevan, odnosno da li se nalazi u cache-u
                            byte[] responseBody;

                            if (!cache.TryGetValue(filePath, out responseBody))
                            {
                                responseBody = File.ReadAllBytes(filePath);
                                cache.Add(filePath, responseBody);
                            }

                            // Saljemo nazad pribavljeni fajl
                            SendResponse(context, responseBody, "image/gif");
                        }
                        catch (Exception ex)
                        {
                            if (context.Response.OutputStream.CanWrite)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                context.Response.OutputStream.Close();
                            }
                            Console.WriteLine($"Failure occurred: {ex.Message}");
                        }
                    }, context);
                }
            }

        }

    }
}
