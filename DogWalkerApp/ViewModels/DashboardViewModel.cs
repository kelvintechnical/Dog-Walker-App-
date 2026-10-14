using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;
using DogWalkerApp.Models;
using DogWalkerApp.Services.Api;

namespace DogWalkerApp.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;

    public ObservableCollection<ServiceOption> ServiceOptions { get; } = new();
    public ObservableCollection<BookingDto> UpcomingBookings { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Stay pawsitive!";

    public DashboardViewModel(IDogWalkerApi api)
    {
        _api = api;
        Title = "Friends with Fur";
        PopulateServiceOptions();
    }

    [RelayCommand]
    private Task InitializeAsync() => SafeExecuteAsync(async () =>
    {
        UpcomingBookings.Clear();
        var account = await _api.GetAccountAsync();
        var bookings = account.WalkerId is Guid walkerId
            ? await _api.GetWalkerBookingsAsync(walkerId)
            : account.ClientId is Guid clientId ? await _api.GetClientBookingsAsync(clientId) : [];
        foreach (var booking in bookings)
        {
            UpcomingBookings.Add(booking);
        }

        StatusMessage = UpcomingBookings.Any()
            ? $"Next walk starts at {UpcomingBookings.First().StartTimeUtc.LocalDateTime:t}"
            : "No walks scheduled. Time to promote your services!";
    });

    [RelayCommand]
    private Task QuickBookAsync(ServiceOption option) => SafeExecuteAsync(async () =>
    {
        // Quick-book opens the real dog-selection form; never create arbitrary client/dog IDs.
        await Shell.Current.GoToAsync("//bookings");
    });

    private void PopulateServiceOptions()
    {
        ServiceOptions.Clear();
        ServiceOptions.Add(new ServiceOption("30-min Walk", "Quick stroll for energetic pups.", 25, ServiceType.ThirtyMinuteWalk));
        ServiceOptions.Add(new ServiceOption("1-hour Walk", "Extended adventure with GPS tracking.", 45, ServiceType.OneHourWalk));
        ServiceOptions.Add(new ServiceOption("Dog Sitting", "In-home care with playtime updates.", 70, ServiceType.DogSitting));
        ServiceOptions.Add(new ServiceOption("Boarding", "Overnight stay with reliable care.", 110, ServiceType.Boarding));
    }
}
