using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using WebSocketManager;

namespace fileuploadtest
{
    public class StreamMessageHandler : WebSocketHandler
    {
        public StreamMessageHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override async Task ReceiveAsync(WebSocket socket, Stream stream)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            var message = $"{socketId} send {stream.Length} bytes";
            Debug.WriteLine(message);
        }
    }
}
