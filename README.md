# Demo.WS.Zeiss

* Demo.WS.Zeiss.Server  websocket后端服务
* Demo.WS.Zeiss.Client websocket Client 用JavaScript连接websocket后端服务，可以打开控制台进行调试


## 配置

* 跨域配置

由于client和server不在一个域名上，需要在server配置跨域

```c#
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
			
```

* timeout设置

```c#
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20)
};

app.UseWebSockets(webSocketOptions);
			
```

* server接收到socket信息后处理

```c#
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
```

* 添加心跳连接


