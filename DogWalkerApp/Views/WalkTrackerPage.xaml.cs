using DogWalkerApp.Helpers;
using DogWalkerApp.ViewModels;

namespace DogWalkerApp.Views;

public partial class WalkTrackerPage : ContentPage
{
    public WalkTrackerPage()
    {
        InitializeComponent();
        BindingContext = ServiceHelper.GetRequiredService<WalkTrackerViewModel>();
    }
}
