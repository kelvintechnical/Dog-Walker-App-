using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel = ServiceHelper.GetRequiredService<DashboardViewModel>();

    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
