using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class MessagesPage : ContentPage
{
    private readonly MessagesViewModel _viewModel = ServiceHelper.GetRequiredService<MessagesViewModel>();

    public MessagesPage()
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
