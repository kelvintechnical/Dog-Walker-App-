using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.DTOs;
using DogWalker.Core.Requests;
using DogWalkerApp.Services.Api;

namespace DogWalkerApp.ViewModels;

public partial class ClientsViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;

    public ObservableCollection<ClientDto> Clients { get; } = new();
    public IEnumerable<ClientDto> FilteredClients =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Clients
            : Clients.Where(c => c.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                 c.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClientDto? _selectedClient;

    public ClientsViewModel(IDogWalkerApi api)
    {
        _api = api;
        Title = "Clients";
    }

    [RelayCommand]
    private Task LoadAsync() => SafeExecuteAsync(async () =>
    {
        var previousSelectionId = SelectedClient?.Id;
        var clients = (await _api.GetClientsAsync())
            .OrderBy(c => c.FullName)
            .ToList();

        Clients.Clear();
        foreach (var client in clients)
        {
            Clients.Add(client);
        }

        SelectedClient = Clients.FirstOrDefault(c => c.Id == previousSelectionId) ?? Clients.FirstOrDefault();
        OnPropertyChanged(nameof(FilteredClients));
    });

    [RelayCommand]
    private Task CreateClientAsync() => SafeExecuteAsync(async () =>
    {
        var page = Application.Current?.MainPage ?? throw new InvalidOperationException("Application page unavailable.");
        var name = await page.DisplayPromptAsync("Your profile", "Full name");
        if (string.IsNullOrWhiteSpace(name)) return;
        var email = await page.DisplayPromptAsync("Your profile", "Email", keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;
        var phone = await page.DisplayPromptAsync("Your profile", "Phone", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(phone)) return;
        var address = await page.DisplayPromptAsync("Your profile", "Street address");
        if (string.IsNullOrWhiteSpace(address)) return;
        var city = await page.DisplayPromptAsync("Your profile", "City");
        if (string.IsNullOrWhiteSpace(city)) return;
        var state = await page.DisplayPromptAsync("Your profile", "State/region");
        if (string.IsNullOrWhiteSpace(state)) return;
        var postal = await page.DisplayPromptAsync("Your profile", "Postal code");
        if (string.IsNullOrWhiteSpace(postal)) return;
        var country = await page.DisplayPromptAsync("Your profile", "Country code (e.g. US)");
        if (string.IsNullOrWhiteSpace(country)) return;
        var request = new CreateClientRequest(name, email, phone, address, null, city, state, postal, country);

        var client = await _api.CreateClientAsync(request);
        Clients.Insert(0, client);
        SelectedClient = client;
        OnPropertyChanged(nameof(FilteredClients));
    });

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredClients));
    }
}
