using DogWalker.Core.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace DogWalkerApp.Services.Messaging;

public interface IRealTimeMessagingService
{
    Task ConnectAsync(string hubUrl, CancellationToken cancellationToken = default);
    Task JoinBookingAsync(Guid bookingId);
    event EventHandler<MessageDto>? MessageReceived;
}

public class RealTimeMessagingService : IRealTimeMessagingService
{
    private HubConnection? _connection;

    public event EventHandler<MessageDto>? MessageReceived;

    public async Task ConnectAsync(string hubUrl, CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>("MessageReceived", dto =>
        {
            MessageReceived?.Invoke(this, dto);
        });

        await _connection.StartAsync(cancellationToken);
    }

    public Task JoinBookingAsync(Guid bookingId)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("Real-time connection not established.");
        }

        return _connection.InvokeAsync("JoinBookingGroup", bookingId);
    }
}
