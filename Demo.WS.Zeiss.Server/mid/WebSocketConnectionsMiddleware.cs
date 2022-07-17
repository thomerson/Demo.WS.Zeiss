using Demo.WS.Core.protocol;
using Demo.WS.Core;
using System.Net.WebSockets;
using Demo.WS.Core.option;
using Demo.WS.Zeiss.Model;
using Demo.WS.Core.contract;

namespace Demo.WS.Zeiss.Server.mid
{
    internal class WebSocketConnectionsMiddleware
    {
        private readonly WebSocketConnectionsOptions _options;
        private readonly IWebSocketConnectionsService _connectionsService;
        private readonly IMachineService _machineService;

        public WebSocketConnectionsMiddleware(RequestDelegate next,
            WebSocketConnectionsOptions options,
            IWebSocketConnectionsService connectionsService,
            IMachineService machineService
            )
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connectionsService = connectionsService ?? throw new ArgumentNullException(nameof(connectionsService));
            _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                if (ValidateOrigin(context))
                {
                    ITextWebSocketSubprotocol textSubProtocol = NegotiateSubProtocol(context.WebSockets.WebSocketRequestedProtocols);

                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext
                    {
                        SubProtocol = textSubProtocol?.SubProtocol,
                        DangerousEnableCompression = true
                    });

                    WebSocketConnection webSocketConnection = new WebSocketConnection(webSocket, textSubProtocol ?? _options.DefaultSubProtocol, _options.SendSegmentSize, _options.ReceivePayloadBufferSize);
                    webSocketConnection.ReceiveText += async (sender, message) =>
                    {
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<wsMachineMessage>(message);

                        if (data != null)
                        {
                            if (data.payload.status == MachineStatus.errored)
                            {
                                // send a repaired event
                                data._event = "repaired";
                                data.payload.status = MachineStatus.idle;

                                await webSocketConnection.SendAsync(Newtonsoft.Json.JsonConvert.SerializeObject(data), CancellationToken.None);

                            }

                            await _machineService.UpdateStatus(data.payload);
                        }
                        await webSocketConnection.SendAsync("OK", CancellationToken.None);
                    };

                    _connectionsService.AddConnection(webSocketConnection);

                    await webSocketConnection.ReceiveMessagesUntilCloseAsync();

                    if (webSocketConnection.CloseStatus.HasValue)
                    {
                        await webSocket.CloseAsync(webSocketConnection.CloseStatus.Value, webSocketConnection.CloseStatusDescription, CancellationToken.None);
                    }

                    _connectionsService.RemoveConnection(webSocketConnection.Id);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private bool ValidateOrigin(HttpContext context)
        {
            return (_options.AllowedOrigins == null) || (_options.AllowedOrigins.Count == 0) || (_options.AllowedOrigins.Contains(context.Request.Headers["Origin"].ToString()));
        }

        private ITextWebSocketSubprotocol NegotiateSubProtocol(IList<string> requestedSubProtocols)
        {
            ITextWebSocketSubprotocol subProtocol = null;

            foreach (ITextWebSocketSubprotocol supportedSubProtocol in _options.SupportedSubProtocols)
            {
                if (requestedSubProtocols.Contains(supportedSubProtocol.SubProtocol))
                {
                    subProtocol = supportedSubProtocol;
                    break;
                }
            }

            return subProtocol;
        }

    }
}
