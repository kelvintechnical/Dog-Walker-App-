using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class BookingsPage : ContentPage
{
    private readonly BookingsViewModel _viewModel = ServiceHelper.GetRequiredService<BookingsViewModel>();

    public BookingsPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
