using Gradio.Net.Models;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.JSInterop;
using Microsoft.Maui.Storage;
using Microsoft.Net.Http.Headers;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Windows.Foundation;
using Windows.Storage.Streams;
using static System.Collections.Specialized.BitVector32;
using WebView2Control = Microsoft.UI.Xaml.Controls.WebView2;

namespace Gradio.Net
{
    internal partial class GradioBlazorWebViewHandler
    {
        private GradioApp _gradioApp;
        private Thread _gradioAppEventThread;

        protected override void ConnectHandler(WebView2Control platformView)
        {
            base.ConnectHandler(platformView);
            platformView.CoreWebView2Initialized += CoreWebView2Initialized;
        }

        protected override void DisconnectHandler(WebView2Control platformView)
        {
            platformView.CoreWebView2Initialized -= CoreWebView2Initialized;
            base.DisconnectHandler(platformView);
        }

        private void CoreWebView2Initialized(WebView2Control sender, Microsoft.UI.Xaml.Controls.CoreWebView2InitializedEventArgs args)
        {
            var webview2 = sender.CoreWebView2;
            _gradioApp = this.Services!.GetRequiredService<GradioApp>();
            _gradioAppEventThread = new Thread(async () =>
            {
                await _gradioApp.StartEventLoopAsync(CancellationToken.None);
            });
            _gradioAppEventThread.Start();
            webview2.WebResourceRequested += WebView2WebResourceRequested;
        }

        private async IAsyncEnumerable<string> GradioAppQueryData(GradioApp gradioApp, string sessionHash)
        {
            await foreach (var sseMessage in gradioApp.QueueData(sessionHash, CancellationToken.None))
            {
                yield return sseMessage.ProcessMsg();
            }
            yield return new CloseStreamMessage().ProcessMsg();
            gradioApp.CloseSession(sessionHash);
        }

        private async IAsyncEnumerable<string> GradioAppUploadProgress()
        {
            yield return await Task.FromResult(new DoneMessage().ProcessMsg());
        }

        private async Task<object> ApiHandle(CoreWebView2WebResourceRequest request, GradioApp gradioApp, CoreWebView2WebResourceRequestedEventArgs args)
        {
            var rootUrl = "https://0.0.0.0";
            var uri = new Uri(request.Uri);
            const string FILE_URL = "/file=";
            switch (uri.AbsolutePath)
            {
                case "/config":
                case "/info":
                    var ret = gradioApp.GetApiInfo();
                    return ret;
                case "/queue/join":
                    {
                        using var contentStream = request.Content.AsStreamForRead();
                        return await gradioApp.QueueJoin(rootUrl, (await JsonSerializer.DeserializeAsync<PredictBodyIn>(contentStream))!);
                    }
                case "/queue/data":
                    {
                        var querys = HttpUtility.ParseQueryString(uri.Query);
                        var sessionHash = querys["session_hash"]!;
                        return new SSEStream()
                        {
                            Contents = GradioAppQueryData(gradioApp, sessionHash),
                            ContentType = "text/event-stream"
                        };
                    }
                //case "/upload":
                //    {
                //        var querys = HttpUtility.ParseQueryString(uri.Query);
                //        var uploadId = querys["upload_id"]!;
                //        var boundary = GetBoundary(MediaTypeHeaderValue.Parse(request.Headers.First(x => x.Key.Equals("Content-Type", StringComparison.CurrentCultureIgnoreCase)).Value));
                //        using var stream = request.Content.AsStreamForRead();
                //        var reader = new MultipartReader(boundary, stream);
                //        var files = new List<(Stream Stream, string Name)>();
                //        var section = await reader.ReadNextSectionAsync();
                //        while (section != null)
                //        {
                //            var contentDisposition = section.GetContentDispositionHeader();
                //            if (contentDisposition != null && contentDisposition.IsFileDisposition() && contentDisposition.FileName.Value != null)
                //                files.Add((section.Body, contentDisposition.FileName.Value));
                //            section = await reader.ReadNextSectionAsync();
                //        }
                //        return await gradioApp.Upload(uploadId, files);
                //    }
                case "/upload_progress":
                    return new SSEStream()
                    {
                        Contents = GradioAppUploadProgress(),
                        ContentType = "text/event-stream"
                    };
                default:
                    if (uri.AbsolutePath.StartsWith(FILE_URL))
                    {
                        (string filePath, string contentType) = await gradioApp.GetUploadedFile(uri.AbsolutePath.Substring(FILE_URL.Length));
                        return new ApiReturnFile()
                        {
                            File = new PhysicalFileInfo(new FileInfo(filePath)),
                            ContentType = contentType
                        };
                    }
                    throw new NotFoundException();
            }
        }

        private string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary))
                throw new InvalidDataException("Missing content-type boundary.");

            return boundary;
        }

        private class NotFoundException : Exception
        {
        }

        private class ApiReturnFile
        {
            public IFileInfo File { get; set; }
            public string ContentType { get; set; }
        }

        private class SSEStream
        {
            public string UUID { get; set; } = Guid.NewGuid().ToString();
            public IAsyncEnumerable<string> Contents { get; set; }
            public string ContentType { get; set; }
        }

        private void SendSSEStreamResponse(CoreWebView2 webview2, string uuid, bool final, string? content)
        {
            _currentSynchronizationContext.Post(async (e) =>
            {
                try
                {
                    var finalJS = final ? "true" : "false";
                    if (content == null)
                    {
                        await webview2.ExecuteScriptAsync($"(window.onSSEEventStream||console.log)({{uuid:'{uuid}',final:{finalJS},content:null}});");
                    }
                    else
                    {
                        var bytes = Encoding.UTF8.GetBytes(content);
                        var bytesString = "[" + String.Join(",", bytes) + "]";
                        var eval = $"(window.onSSEEventStream||console.log)({{uuid:'{uuid}',final:{finalJS},content:Uint8Array.from({bytesString})}});";
                        await webview2.ExecuteScriptAsync(eval);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }, null);
        }

        private void SendSSEMessage(CoreWebView2 webview2, SSEStream sseStream)
        {
            new Thread(async () =>
            {
                try
                {
                    await foreach (var content in sseStream.Contents)
                    {
                        SendSSEStreamResponse(webview2, sseStream.UUID, false, content);
                    }
                    SendSSEStreamResponse(webview2, sseStream.UUID, true, null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }).Start();
        }

        private async void WebView2WebResourceRequested(CoreWebView2 webview2, CoreWebView2WebResourceRequestedEventArgs args)
        {
            using var deferral = args.GetDeferral();
            string uri = args.Request.Uri;
            string subPath = new Uri(args.Request.Uri).AbsolutePath.TrimStart('/');
            var file = _gradioApp.GetFileInfo(subPath);

            if (file.Exists)
            {
                using var fileStream = file.CreateReadStream();
                args.Response = await CreateWebResourceResponse(webview2, _gradioApp.GetMimeType(file.Name), fileStream, args, subPath);
            }
            else
            {
                try
                {
                    var ret = await ApiHandle(args.Request, _gradioApp, args);
                    if (ret == null)
                    {
                        args.Response = webview2.Environment.CreateWebResourceResponse(null, 200, "OK", $"Content-Type: {_gradioApp.GetMimeType(subPath)}");
                    }
                    else if (ret is ApiReturnFile apiFile)
                    {
                        using var fileStream = apiFile.File.CreateReadStream();
                        args.Response = await CreateWebResourceResponse(webview2, apiFile.ContentType, fileStream, args, subPath);
                    }
                    else if (ret is SSEStream apiSSEStream)
                    {
                        SendSSEMessage(webview2, apiSSEStream);
                        args.Response = webview2.Environment.CreateWebResourceResponse(new InMemoryRandomAccessStream(), 200, "OK", $"Content-Type: {apiSSEStream.ContentType}{Environment.NewLine}kapp-event-stream: {apiSSEStream.UUID}");
                    }
                    else
                    {
                        var retStream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(ret));
                        IRandomAccessStream stream = await ReadStreamRange(retStream, 0, retStream.Length);
                        args.Response = webview2.Environment.CreateWebResourceResponse(stream, 200, "OK", $"Content-Type: application/json");
                    }
                }
                catch (NotFoundException)
                {
                    args.Response = webview2.Environment.CreateWebResourceResponse(null, 404, "Not Found", string.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    args.Response = webview2.Environment.CreateWebResourceResponse(null, 500, "Server Error", string.Empty);
                }
            }

            string GetHeaderString(IDictionary<string, string> headers) =>
                string.Join(Environment.NewLine, headers.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            async Task<CoreWebView2WebResourceResponse> CreateWebResourceResponse(CoreWebView2 webview2,
                string contentType,
                Stream fileStream,
                CoreWebView2WebResourceRequestedEventArgs args, string subPath)
            {
                var headers = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "Content-Type", contentType },
                    { "Cache-Control", "no-cache, max-age=0, must-revalidate, no-store" }
                };
                var length = fileStream.Length;
                long rangeStart = 0;
                long rangeEnd = length - 1;

                int statusCode = 200;
                string reasonPhrase = "OK";

                //适用于音频视频文件资源的响应
                bool partial = args.Request.Headers.Contains("Range");
                if (partial)
                {
                    statusCode = 206;
                    reasonPhrase = "Partial Content";

                    var rangeString = args.Request.Headers.GetHeader("Range");
                    var ranges = rangeString.Split('=');
                    if (ranges.Length > 1 && !string.IsNullOrEmpty(ranges[1]))
                    {
                        string[] rangeDatas = ranges[1].Split("-");
                        rangeStart = Convert.ToInt64(rangeDatas[0]);
                        if (rangeDatas.Length > 1 && !string.IsNullOrEmpty(rangeDatas[1]))
                        {
                            rangeEnd = Convert.ToInt64(rangeDatas[1]);
                        }
                        else
                        {
                            //每次加载4Mb，不能设置太多
                            rangeEnd = Math.Min(rangeEnd, rangeStart + 4 * 1024 * 1024);
                        }
                    }

                    headers.Add("Accept-Ranges", "bytes");
                    headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{length}");
                }

                headers.Add("Content-Length", (rangeEnd - rangeStart + 1).ToString());
                var headerString = GetHeaderString(headers);
                IRandomAccessStream stream = await ReadStreamRange(fileStream, rangeStart, rangeEnd);
                return webview2.Environment.CreateWebResourceResponse(stream, statusCode, reasonPhrase, headerString);
            }

            async Task<IRandomAccessStream> ReadStreamRange(Stream contentStream, long start, long end)
            {
                long length = end - start + 1;
                contentStream.Position = start;

                using var memoryStream = new MemoryStream();

                StreamCopy(contentStream, memoryStream, length);
                // 将内存流的位置重置为起始位置
                memoryStream.Seek(0, SeekOrigin.Begin);

                var randomAccessStream = new InMemoryRandomAccessStream();
                await randomAccessStream.WriteAsync(memoryStream.GetWindowsRuntimeBuffer());

                return randomAccessStream;
            }

            // 辅助方法，用于限制StreamCopy复制的数据长度
            void StreamCopy(Stream source, Stream destination, long length)
            {
                //缓冲区设为1Mb,应该是够了
                byte[] buffer = new byte[1024 * 1024];
                int bytesRead;

                while (length > 0 && (bytesRead = source.Read(buffer, 0, (int)Math.Min(buffer.Length, length))) > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                    length -= bytesRead;
                }
            }
        }
    }
}
