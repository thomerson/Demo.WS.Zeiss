﻿using Demo.WS.Core.protocol;
using System.Net.WebSockets;
using System.Text;

namespace Demo.WS.Core
{
    public class WebSocketConnection
    {
        public Guid Id { get; } = Guid.NewGuid();

        private readonly WebSocket _webSocket;
        private readonly ITextWebSocketSubprotocol _textSubProtocol;

        private readonly int _receivePayloadBufferSize;
        private readonly int? _sendSegmentSize;

        public WebSocketCloseStatus? CloseStatus { get; private set; } = null;
        public string CloseStatusDescription { get; private set; } = null;

        public event EventHandler<string> ReceiveText;
        public event EventHandler<byte[]> ReceiveBinary;

        public WebSocketConnection(WebSocket webSocket, ITextWebSocketSubprotocol textSubProtocol, int? sendSegmentSize, int receivePayloadBufferSize)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _textSubProtocol = textSubProtocol ?? throw new ArgumentNullException(nameof(textSubProtocol));
            _sendSegmentSize = sendSegmentSize;
            _receivePayloadBufferSize = receivePayloadBufferSize;
        }

        private Task SendTextMessageBytesAsync(byte[] messageBytes, CancellationToken cancellationToken)
        {
            return SendMessageBytesAsync(messageBytes, WebSocketMessageType.Text, cancellationToken: cancellationToken);
        }

        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            return _textSubProtocol.SendAsync(message, SendTextMessageBytesAsync, cancellationToken);
        }

        private async Task SendMessageBytesAsync(byte[] messageBytes, WebSocketMessageType messageType, bool compressMessage = true, CancellationToken cancellationToken = default)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                if (_sendSegmentSize.HasValue && (_sendSegmentSize.Value < messageBytes.Length))
                {
                    int messageOffset = 0;
                    int messageBytesToSend = messageBytes.Length;

                    while (messageBytesToSend > 0)
                    {
                        int messageSegmentSize = Math.Min(_sendSegmentSize.Value, messageBytesToSend);
                        ArraySegment<byte> messageSegment = new ArraySegment<byte>(messageBytes, messageOffset, messageSegmentSize);

                        messageOffset += messageSegmentSize;
                        messageBytesToSend -= messageSegmentSize;

                        await _webSocket.SendAsync(messageSegment, messageType, GetMessageFlags(messageBytesToSend == 0, !compressMessage), cancellationToken);
                    }
                }
                else
                {
                    ArraySegment<byte> messageSegment = new ArraySegment<byte>(messageBytes, 0, messageBytes.Length);

                    await _webSocket.SendAsync(messageSegment, messageType, GetMessageFlags(true, !compressMessage), cancellationToken);
                }
            }
        }

        private static WebSocketMessageFlags GetMessageFlags(bool endOfMessage, bool disableCompression)
        {
            WebSocketMessageFlags messageFlags = WebSocketMessageFlags.None;

            if (endOfMessage)
            {
                messageFlags |= WebSocketMessageFlags.EndOfMessage;
            }

            if (disableCompression)
            {
                messageFlags |= WebSocketMessageFlags.DisableCompression;
            }

            return messageFlags;
        }

        public async Task ReceiveMessagesUntilCloseAsync()
        {
            try
            {
                byte[] receivePayloadBuffer = new byte[_receivePayloadBufferSize];
                WebSocketReceiveResult webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receivePayloadBuffer), CancellationToken.None);
                while (webSocketReceiveResult.MessageType != WebSocketMessageType.Close)
                {
                    byte[] webSocketMessage = await ReceiveMessagePayloadAsync(webSocketReceiveResult, receivePayloadBuffer);
                    if (webSocketReceiveResult.MessageType == WebSocketMessageType.Binary)
                    {
                        OnReceiveBinary(webSocketMessage);
                    }
                    else
                    {
                        OnReceiveText(Encoding.UTF8.GetString(webSocketMessage));
                    }

                    webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receivePayloadBuffer), CancellationToken.None);
                }

                CloseStatus = webSocketReceiveResult.CloseStatus.Value;
                CloseStatusDescription = webSocketReceiveResult.CloseStatusDescription;
            }
            catch (WebSocketException wsex) when (wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            { }
        }

        private async Task<byte[]> ReceiveMessagePayloadAsync(WebSocketReceiveResult webSocketReceiveResult, byte[] receivePayloadBuffer)
        {
            byte[] messagePayload = null;

            if (webSocketReceiveResult.EndOfMessage)
            {
                messagePayload = new byte[webSocketReceiveResult.Count];
                Array.Copy(receivePayloadBuffer, messagePayload, webSocketReceiveResult.Count);
            }
            else
            {
                using (MemoryStream messagePayloadStream = new MemoryStream())
                {
                    messagePayloadStream.Write(receivePayloadBuffer, 0, webSocketReceiveResult.Count);
                    while (!webSocketReceiveResult.EndOfMessage)
                    {
                        webSocketReceiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(receivePayloadBuffer), CancellationToken.None);
                        messagePayloadStream.Write(receivePayloadBuffer, 0, webSocketReceiveResult.Count);
                    }

                    messagePayload = messagePayloadStream.ToArray();
                }
            }

            return messagePayload;
        }

        private void OnReceiveText(string webSocketMessage)
        {
            string message = _textSubProtocol.Read(webSocketMessage);

            ReceiveText?.Invoke(this, message);
        }

        private void OnReceiveBinary(byte[] webSocketMessage)
        {
            ReceiveBinary?.Invoke(this, webSocketMessage);
        }


    }
}
