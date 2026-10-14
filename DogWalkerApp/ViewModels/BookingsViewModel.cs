using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;
using DogWalkerApp.Services.Api;
using DogWalkerApp.Services;

namespace DogWalkerApp.ViewModels;

public partial class BookingsViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;
    private readonly IAppSettingsProvider _settings;

    public ObservableCollection<BookingDto> History { get; } = new();
    public ObservableCollection<DogDto> Dogs { get; } = new();

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private TimeSpan _selectedTime = TimeSpan.FromHours(9);

    [ObservableProperty]
    private ServiceType _selectedService = ServiceType.ThirtyMinuteWalk;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateBookingCommand))]
    private DogDto? _selectedDog;

    public BookingsViewModel(IDogWalkerApi api, IAppSettingsProvider settings)
    {
        _api = api;
        _settings = settings;
        Title = "Bookings";
    }

    [RelayCommand]
    private Task LoadDataAsync() => SafeExecuteAsync(async () =>
    {
        var previousSelectionId = SelectedDog?.Id;
        var dogs = (await _api.GetDogsAsync()).OrderBy(d => d.Name).ToList();

        Dogs.Clear();
        foreach (var dog in dogs)
        {
            Dogs.Add(dog);
        }

        SelectedDog = Dogs.FirstOrDefault(d => d.Id == previousSelectionId) ?? Dogs.FirstOrDefault();

        History.Clear();
        var account = await _api.GetAccountAsync();
        var bookings = account.WalkerId is Guid walkerId
            ? await _api.GetWalkerBookingsAsync(walkerId)
            : account.ClientId is Guid clientId ? await _api.GetClientBookingsAsync(clientId) : [];
        foreach (var booking in bookings.OrderByDescending(b => b.StartTimeUtc))
        {
            History.Add(booking);
        }
    });

    [RelayCommand(CanExecute = nameof(CanCreateBooking))]
    private Task CreateBookingAsync() => SafeExecuteAsync(async () =>
    {
        if (SelectedDog is null)
        {
            throw new InvalidOperationException("Select a dog profile first.");
        }

        var account = await _api.GetAccountAsync();
        var walkerId = account.WalkerId ?? (await _settings.GetAsync()).DefaultWalkerId
            ?? throw new InvalidOperationException("Select/configure an existing caregiver before booking.");
        var localStart = DateTime.SpecifyKind(SelectedDate + SelectedTime, DateTimeKind.Unspecified);
        if (TimeZoneInfo.Local.IsInvalidTime(localStart) || TimeZoneInfo.Local.IsAmbiguousTime(localStart))
            throw new InvalidOperationException("Choose an unambiguous local booking time.");
        var start = new DateTimeOffset(localStart, TimeZoneInfo.Local.GetUtcOffset(localStart)).ToUniversalTime();
        var request = new CreateBookingRequest(
            ClientId: SelectedDog.ClientId,
            WalkerId: walkerId,
            DogId: SelectedDog.Id,
            ServiceType: SelectedService,
            StartTimeUtc: start,
            EndTimeUtc: start.AddMinutes(SelectedService == ServiceType.ThirtyMinuteWalk ? 30 : 60),
            Notes: Notes);

        var booking = await _api.CreateBookingAsync(request);
        History.Insert(0, booking);
        Notes = string.Empty;
    });

    [RelayCommand]
    private Task OpenMessagesAsync(BookingDto booking) => SafeExecuteAsync(() =>
        Shell.Current.GoToAsync("booking-messages", new Dictionary<string, object> { ["bookingId"] = booking.Id }));

    [RelayCommand]
    private Task OpenTrackerAsync(BookingDto booking) => SafeExecuteAsync(() =>
        Shell.Current.GoToAsync("booking-tracker", new Dictionary<string, object> { ["bookingId"] = booking.Id }));

    private bool CanCreateBooking() => SelectedDog is not null && !IsBusy;
}
