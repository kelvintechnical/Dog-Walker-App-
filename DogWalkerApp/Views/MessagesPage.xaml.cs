using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class MessagesPage : ContentPage, IQueryAttributable
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
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("bookingId", out var value) && Guid.TryParse(value?.ToString(), out var bookingId))
            _viewModel.ActiveBookingId = bookingId;
    }
}
