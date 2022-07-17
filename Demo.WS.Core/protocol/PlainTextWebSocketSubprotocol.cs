namespace Demo.WS.Core.protocol
{
    public class PlainTextWebSocketSubprotocol : TextWebSocketSubprotocolBase, ITextWebSocketSubprotocol
    {
        public string SubProtocol => "aspnetcore-ws.plaintext";
    }
}
