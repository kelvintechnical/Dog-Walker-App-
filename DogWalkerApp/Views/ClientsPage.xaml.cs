using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _viewModel = ServiceHelper.GetRequiredService<ClientsViewModel>();

    public ClientsPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
