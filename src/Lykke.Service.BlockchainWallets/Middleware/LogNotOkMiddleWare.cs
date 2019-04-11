using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace Lykke.Service.BlockchainWallets.Middleware
{
    public class LogNotOkMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILog _log;

        public LogNotOkMiddleWare(RequestDelegate next, ILogFactory logFactory)
        {
            _next = next;
            _log = logFactory.CreateLog(this);
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = new Stopwatch();
            Exception ex = null;
            sw.Start();

            using (var memStream = new MemoryStream())
            {
                Stream originalBody = context.Response.Body;
                context.Response.Body = memStream;

                context.Request.EnableRewind();
                try
                {
                    await _next.Invoke(context);
                }
                catch (Exception e)
                {
                    ex = e;
                    throw;
                }
                finally
                {
                    sw.Stop();

                    if (context.Response.StatusCode != (int)HttpStatusCode.OK
                        && context.Response.StatusCode != (int)HttpStatusCode.NoContent)
                    {
                        var request = context.Request;
                        var response = context.Response;
                        var formattedRequest = await FormatRequest(request);

                        memStream.Position = 0;
                        string responseBody = new StreamReader(memStream).ReadToEnd();
                        var formattedResponse = $"{response.StatusCode}: {responseBody}";

                        var logContent = new { Request = formattedRequest, Response = formattedResponse };

                        _log.Info(
                            $"{request.Host}{request.Path}{request.QueryString}",
                            $"Not successful response: {context.Response.StatusCode}",
                            logContent,
                            ex);
                    }

                    memStream.Position = 0;
                    await memStream.CopyToAsync(originalBody);
                    context.Response.Body = originalBody;
                }
            }
        }

            private async Task<string> FormatRequest(HttpRequest request)
            {
                var body = request.Body;
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var bodyAsText = Encoding.UTF8.GetString(buffer);
                request.Body = body;

                return $"[{request.Method}] {request.Scheme}://{request.Host}{request.Path}{request.QueryString}, Body:{bodyAsText}";
            }
        }
    }
