using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketManager
{
    public class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next,
                                          WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);
            
            await Receive(socket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                {
                    await _webSocketHandler.ReceiveAsync(socket, buffer);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocketHandler.OnDisconnected(socket);
                }
            });

            //TODO - investigate the Kestrel exception thrown when this is the last middleware
            //await _next.Invoke(context);
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, Stream> handleMessage)
        {
            int bufferSize = 1024 * 256;
            var buffer = new byte[bufferSize];
            var offset = 0;
            var free = buffer.Length;
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), CancellationToken.None);

                offset += result.Count;
                free -= result.Count;
                if (result.EndOfMessage)
                {
                    using (var stream = new MemoryStream(buffer, 0, offset, false, true))
                    {
                        handleMessage(result, stream);
                    }
                    buffer = new byte[bufferSize];
                    offset = 0;
                    free = buffer.Length;
                }
                else if (free == 0)
                {
                    // No free space
                    // Resize the outgoing buffer
                    var newSize = buffer.Length + bufferSize;
                    // Check if the new size exceeds a limit
                    // It should suit the data it receives
                    // This limit however has a max value of 2 billion bytes (2 GB)
                    if (newSize > 1024 * 1024 * 1024)
                    {
                        throw new Exception("Maximum size exceeded");
                    }
                    var newBuffer = new byte[newSize];
                    Array.Copy(buffer, 0, newBuffer, 0, offset);
                    buffer = newBuffer;
                    free = buffer.Length - offset;
                }
            }
        }
    }
}