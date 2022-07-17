﻿using Microsoft.Extensions.Hosting;

namespace Demo.WS.Core.service
{
    public class HeartbeatService : IHostedService
    {
        private const string HEARTBEAT_MESSAGE = "server: Heartbeat";
        private readonly IWebSocketConnectionsService _webSocketConnectionsService;

        private Task _heartbeatTask;
        private CancellationTokenSource _cancellationTokenSource;

        public HeartbeatService(IWebSocketConnectionsService webSocketConnectionsService)
        {
            _webSocketConnectionsService = webSocketConnectionsService;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _heartbeatTask = HeartbeatAsync(_cancellationTokenSource.Token);

            return _heartbeatTask.IsCompleted ? _heartbeatTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_heartbeatTask != null)
            {
                _cancellationTokenSource.Cancel();

                await Task.WhenAny(_heartbeatTask, Task.Delay(-1, cancellationToken));

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task HeartbeatAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _webSocketConnectionsService.SendToAllAsync(HEARTBEAT_MESSAGE, cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(120), cancellationToken);
            }
        }
    }
}
