﻿namespace Demo.WS.Core.protocol
{
    public interface ITextWebSocketSubprotocol
    {
        string SubProtocol { get; }

        Task SendAsync(string message, Func<byte[], CancellationToken, Task> sendMessageBytesAsync, CancellationToken cancellationToken);

        string Read(string rawMessage);
    }
}
