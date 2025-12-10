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
        var request = new CreateClientRequest(
            FullName: "New Client",
            Email: $"client+{Guid.NewGuid():N}@friendswithfur.com",
            PhoneNumber: "(555) 555-5555",
            Line1: "123 Bark Street",
            Line2: null,
            City: "Dogsville",
            State: "CA",
            PostalCode: "90210",
            Country: "US");

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
