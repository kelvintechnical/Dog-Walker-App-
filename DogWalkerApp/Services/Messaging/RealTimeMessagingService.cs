using DogWalker.Core.DTOs;
using DogWalkerApp.Services.Authentication;
using Microsoft.AspNetCore.SignalR.Client;

namespace DogWalkerApp.Services.Messaging;

public interface IRealTimeMessagingService
{
    Task ConnectAsync(string hubUrl, CancellationToken cancellationToken = default);
    Task JoinBookingAsync(Guid bookingId);
    event EventHandler<MessageDto>? MessageReceived;
}

public class RealTimeMessagingService(IAccessTokenProvider tokens) : IRealTimeMessagingService
{
    private HubConnection? _connection;

    public event EventHandler<MessageDto>? MessageReceived;

    public async Task ConnectAsync(string hubUrl, CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        if (new Uri(hubUrl).Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(await tokens.GetAsync()))
            throw new InvalidOperationException("Sign in using a secure endpoint before connecting to messages.");
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.AccessTokenProvider = tokens.GetAsync)
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
