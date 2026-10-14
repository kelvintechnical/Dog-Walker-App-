using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class WalkTrackerPage : ContentPage, IQueryAttributable
{
    private readonly WalkTrackerViewModel _viewModel = ServiceHelper.GetRequiredService<WalkTrackerViewModel>();

    public WalkTrackerPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("bookingId", out var value) && Guid.TryParse(value?.ToString(), out var bookingId))
            _viewModel.ActiveBookingId = bookingId;
    }
}
