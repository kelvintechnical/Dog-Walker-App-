using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;
using DogWalker.Core.Configuration;
using DogWalkerApp.Services;
using DogWalkerApp.Services.Api;
using DogWalkerApp.Services.Messaging;

namespace DogWalkerApp.ViewModels;

public partial class MessagesViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;
    private readonly IRealTimeMessagingService _messaging;
    private readonly IAppSettingsProvider _settings;
    private readonly Guid _clientId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private readonly Guid _walkerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    public ObservableCollection<MessageDto> Messages { get; } = new();

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    public MessagesViewModel(IDogWalkerApi api, IRealTimeMessagingService messaging, IAppSettingsProvider settings)
    {
        _api = api;
        _messaging = messaging;
        _settings = settings;
        Title = "Messaging";
        _messaging.MessageReceived += (_, message) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Messages.Add(message));
        };
    }

    [RelayCommand]
    private Task InitializeAsync() => SafeExecuteAsync(async () =>
    {
        Messages.Clear();
        var history = await _api.GetMessagesAsync(_clientId, _walkerId);
        foreach (var message in history.OrderBy(m => m.SentAtUtc))
        {
            Messages.Add(message);
        }

        var appSettings = await _settings.GetAsync();
        await _messaging.ConnectAsync(appSettings.SignalRHubUrl);
        await _messaging.JoinBookingAsync(Guid.Empty);
    });

    [RelayCommand]
    private Task SendAsync() => SafeExecuteAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(DraftMessage))
        {
            return;
        }

        var request = new SendMessageRequest(
            ClientId: _clientId,
            WalkerId: _walkerId,
            BookingId: null,
            Direction: MessageDirection.WalkerToClient,
            Message: DraftMessage);

        var sent = await _api.SendMessageAsync(request);
        Messages.Add(sent);
        DraftMessage = string.Empty;
    });
}
