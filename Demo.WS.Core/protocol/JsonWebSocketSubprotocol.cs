using Newtonsoft.Json;

namespace Demo.WS.Core.protocol
{
    public class JsonWebSocketSubprotocol : TextWebSocketSubprotocolBase, ITextWebSocketSubprotocol
    {
        public string SubProtocol => "aspnetcore-ws.json";

        public override Task SendAsync(string message, Func<byte[], CancellationToken, Task> sendMessageBytesAsync, CancellationToken cancellationToken)
        {
            string jsonMessage = JsonConvert.SerializeObject(new { message, timestamp = DateTime.UtcNow });

            return base.SendAsync(jsonMessage, sendMessageBytesAsync, cancellationToken);
        }
    }
}
