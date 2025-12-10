using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;
using DogWalkerApp.Services.Api;

namespace DogWalkerApp.ViewModels;

public partial class BookingsViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;
    private readonly Guid _defaultWalkerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

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

    public BookingsViewModel(IDogWalkerApi api)
    {
        _api = api;
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
        var bookings = await _api.GetWalkerBookingsAsync(_defaultWalkerId);
        foreach (var booking in bookings.OrderByDescending(b => b.StartTimeUtc))
        {
            History.Add(booking);
        }
    });

    [RelayCommand(CanExecute = nameof(CanCreateBooking))]
    private Task CreateBookingAsync() => SafeExecuteAsync(async () =>
    {
        if (_selectedDog is null)
        {
            throw new InvalidOperationException("Select a dog profile first.");
        }

        var start = new DateTimeOffset(_selectedDate + _selectedTime, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
        var request = new CreateBookingRequest(
            clientId: _selectedDog.ClientId,
            walkerId: _defaultWalkerId,
            dogId: _selectedDog.Id,
            serviceType: _selectedService,
            startTimeUtc: start,
            endTimeUtc: start.AddMinutes(_selectedService == ServiceType.ThirtyMinuteWalk ? 30 : 60),
            notes: Notes);

        var booking = await _api.CreateBookingAsync(request);
        History.Insert(0, booking);
        Notes = string.Empty;
    });

    private bool CanCreateBooking() => SelectedDog is not null && !IsBusy;
}
