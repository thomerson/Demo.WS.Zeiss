using Demo.WS.Core.option;
using Demo.WS.Core.protocol;

namespace Demo.WS.Zeiss.Server.mid
{
    public static class WebSocketConnectionsExtensions
    {
        public static IApplicationBuilder UseWebSocketConnections(this IApplicationBuilder app, PathString pathMatch)
        {
            var textWebSocketSubprotocol = new PlainTextWebSocketSubprotocol();
            var options = new WebSocketConnectionsOptions()
            {
                AllowedOrigins = new HashSet<string> { "http://localhost:37626" }, //CORS
                SupportedSubProtocols = new List<ITextWebSocketSubprotocol>
                {
                    new JsonWebSocketSubprotocol(),
                    textWebSocketSubprotocol
                },
                DefaultSubProtocol = textWebSocketSubprotocol,
                SendSegmentSize = 4 * 1024
            };

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20)
            };

            app
                .UseWebSockets(webSocketOptions)
                .Map(pathMatch, configuration =>
                {
                    configuration.UseMiddleware<WebSocketConnectionsMiddleware>(options);
                });
            return app;
        }
    }
}
