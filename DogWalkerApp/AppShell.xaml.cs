namespace DogWalkerApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("booking-messages", typeof(DogWalkerApp.Views.MessagesPage));
        Routing.RegisterRoute("booking-tracker", typeof(DogWalkerApp.Views.WalkTrackerPage));
    }
}
