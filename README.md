# Friends with Fur – Dog Walker Entrepreneur Suite

Friends with Fur is a turnkey solution for independent dog walkers who want to offer professional-grade mobile experiences to their clients. The solution pairs a .NET MAUI mobile app with an ASP.NET Core Web API and a shared domain library so you can manage bookings, GPS-tracked walks, payments, messaging, and media updates from one codebase.

## Solution Layout

```
DogWalkerEntrepreneur.sln
├── DogWalkerApp           # .NET MAUI client (iOS/Android/Mac/Windows)
├── DogWalkerApi           # ASP.NET Core Web API + SignalR Hub
└── DogWalker.Core         # Shared domain models, DTOs, enums, requests
```

### Technology Stack
- .NET 8 / C#
- .NET MAUI + CommunityToolkit.Maui + MVVM Toolkit
- ASP.NET Core Web API + Entity Framework Core + SQL Server provider
- SignalR for live walk updates and messaging
- Stripe.NET + SendGrid SDK placeholders
- Refit for typed API clients in the mobile app

## Core Capabilities

- **Service Catalog & Quick Booking** – Predefined services (30-min walk, 1-hour walk, sitting, boarding, etc.) with quick-book actions from the dashboard.
- **Booking Calendar & History** – Schedule walks, capture notes, and review historical activity with status tracking.
- **Client & Dog Profiles** – Manage client contact info and dogs (breed, weight, special needs, vet info).
- **Live GPS Tracking** – Background GPS service streams route points to the API and SignalR hub for client visibility.
- **Media Updates** – Capture photos/videos during walks and sync them to the backend for client sharing.
- **Messaging & Logs** – Real-time messaging channel between walker and clients with persisted history.
- **Payments & Tips** – Stripe payment intent orchestration (create, capture, refund) plus tip support and auto charge after completion.
- **Notifications & Email Hooks** – SendGrid placeholder for confirmation emails and notifications.
- **Environment Awareness** – Configuration for API base URLs, Stripe keys, SendGrid, and Maps baked into `appsettings`.

## Getting Started

### Prerequisites

- .NET 8 SDK (`dotnet --version`)
- Android/iOS build tools if targeting mobile (Xcode, Android SDK)
- SQL Server instance (localdb/Azure SQL) for production scenarios — development defaults to an in-memory store
- Stripe + SendGrid accounts for live payments and email (test keys provided as placeholders)

### 1. Restore Dependencies

```bash
dotnet restore DogWalkerEntrepreneur.sln
```

### 2. Configure Secrets

The API reads configuration from `appsettings.json`, `appsettings.Development.json`, and environment variables. Update the following sections before production deployment:

- `ConnectionStrings:SqlServer`
- `Stripe:SecretKey` / `Stripe:WebhookSecret`
- `App:StripePublishableKey`, `App:SendGridApiKey`, `App:MapsApiKey`

For local development you can export environment variables or use `dotnet user-secrets` on the API project:

```bash
cd DogWalkerApi
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=.;Database=DogWalker;Trusted_Connection=True;"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

The MAUI app uses `Resources/Raw/appsettings.json` for client-side configuration. Update the URLs and keys there when pointing at hosted environments.

### 3. Run the API

```bash
cd DogWalkerApi
dotnet run
```

The API hosts:

- REST endpoints under `/api/*`
- SignalR hub at `/hubs/walks`
- Swagger UI in Development at `https://localhost:5001/swagger`

### 4. Run the MAUI App

```bash
cd DogWalkerApp
dotnet build        # or dotnet build -t:Run -f net8.0-android
```

Deploy to your target platform via Visual Studio, `dotnet`, or `maui` CLI tooling. The default configuration assumes the API runs at `https://localhost:5001`. Update `Resources/Raw/appsettings.json` or platform-specific config if you deploy elsewhere.

## Feature Highlights

### Mobile App (DogWalkerApp)

- **Dashboard Tab** – Service cards with quick-book actions, live status banner, and upcoming walk summaries.
- **Bookings Tab** – Calendar controls, service picker, notes, and walk history timeline.
- **Clients Tab** – Searchable client list with quick-add wizard.
- **Live Walk Tab** – Start/stop tracking, capture photos, and live route feed (powered by GPS tracker service + SignalR).
- **Messages Tab** – Chat-style interface with SignalR-powered updates and persisted history.
- **Infrastructure**
  - MVVM via CommunityToolkit.Mvvm with asynchronous RelayCommand helpers.
  - Refit-powered `IDogWalkerApi` client plus DI-registered services for GPS, media capture, secure storage, and SignalR messaging.
  - App-wide theming + accessibility-friendly styles and permission declarations for Android/iOS (location, camera, storage).

### Backend API (DogWalkerApi)

- **Domain Models** – Clients, Dogs, Walkers, Bookings, Payments, Messages, Media Assets, Route Points.
- **EF Core DbContext** (`DogWalkerDbContext`) with owned types for addresses/service areas and cascade rules.
- **Services**
  - Booking orchestration + pricing engine
  - Payment orchestration (Stripe intents, capture, refunds)
  - Client management + dog profiles
  - Messaging service + SendGrid/Webhook placeholders
- **Controllers**
  - `BookingsController` – create, list, status updates, GPS route ingestion, media upload, payment intents
  - `ClientsController` – CRUD clients and dogs
  - `MessagesController` – fetch/send chat threads
- **Infrastructure**
  - JWT-ready authentication middleware
  - FluentValidation for request validation
  - AutoMapper mapping profile shared across DTOs
  - Serilog logging pipeline + Swagger
  - SignalR hub (`WalkHub`) for real-time walk updates

### Shared Core (DogWalker.Core)

- Enums for services, bookings, payments, environments, messaging directions.
- DTOs + request records shared between API and MAUI client.
- Configuration objects (`AppEnvironmentOptions`) for consistent environment metadata.

## Recent UX Improvements

### Completed (Phase 1)

#### Loading Indicators
- **Added ActivityIndicator to all pages** (DashboardPage, BookingsPage, ClientsPage, WalkTrackerPage, MessagesPage)
- Bound to `IsBusy` property from BaseViewModel
- Users now see visual feedback during API calls and async operations
- Indicators display centered overlay with primary theme color

#### Error Handling & User Feedback
- **Enhanced BaseViewModel** with comprehensive error handling
  - Added `ErrorMessage` observable property to track errors
  - Updated `SafeExecuteAsync` with try-catch-finally block
  - Errors now display user-friendly alerts via `DisplayAlert`
  - Added `ShowErrorAlertAsync` and `ShowSuccessAlertAsync` helper methods
- Users no longer experience silent failures - all exceptions surface with clear messages

#### Dog Selection UI
- **Added Dog Picker to BookingsPage** (BookingsPage.xaml:22-25)
  - Binds to `Dogs` collection and `SelectedDog` property
  - Displays dog names via `ItemDisplayBinding`
  - Enables users to select which dog to book instead of using hardcoded placeholder
  - Positioned between Service picker and Notes field for logical flow

#### Command Validation
- **Added CanExecute predicates to critical commands**
  - `CreateBookingCommand` (BookingsViewModel.cs:81): Disabled when `SelectedDog` is null or `IsBusy` is true
  - `SendCommand` (MessagesViewModel.cs:74): Disabled when `DraftMessage` is empty or `IsBusy` is true
  - Added `[NotifyCanExecuteChangedFor]` attributes to trigger re-evaluation when properties change
  - Buttons now visually disable when actions aren't valid, preventing user confusion

### Remaining UX Enhancements (Phase 2)

#### High Priority
1. **Replace Hardcoded Test Data**
   - BookingsViewModel.LoadDataAsync (line 45-47) uses placeholder "Goldie" dog
   - Need to fetch real dogs from API: `await _api.GetDogsForClientAsync(...)`
   - Update ClientsViewModel to load real clients from API
   - Populate realistic demo data via seed migrations

2. **Empty State Messages**
   - Add "No bookings yet" label to BookingsPage History CollectionView
   - Add "No clients found" to ClientsPage when FilteredClients is empty
   - Add "No messages" to MessagesPage when Messages collection is empty
   - Add "No route data" to WalkTrackerPage when Route is empty
   - Use visibility converters: `IsVisible="{Binding Collection.Count, Converter={StaticResource IsZeroConverter}}"`

3. **Service Error Handling**
   - GpsTrackerService.TrackAsync (line 41-65): Wrap in try-catch, handle location permission failures
   - RealTimeMessagingService.ConnectAsync (line 19-37): Add exception handling, show connection errors
   - MediaCaptureService: Add validation for file size/format, handle permission denials
   - Add retry logic for network failures in API calls

#### Medium Priority
4. **Validation Feedback UI**
   - Add error label below Dog picker on BookingsPage when SelectedDog is null
   - Show validation messages for required fields before submission
   - Add field-level validation with red borders/labels for invalid inputs

5. **Toast/Snackbar Notifications**
   - Currently only StatusMessage on Dashboard shows feedback
   - Integrate CommunityToolkit.Maui Snackbar for success/error notifications
   - Show "Booking created successfully" after CreateBookingAsync
   - Show "Message sent" after SendAsync
   - Display network connectivity warnings

6. **Empty Collection Converters**
   - Create `CollectionToVisibilityConverter` to show/hide empty states
   - Create `IsNullOrEmptyConverter` for validation feedback
   - Add to Resources/Styles/Converters.xaml

### UI/UX Polish (Phase 3)
- Replace placeholder app icons and splash screens
- Add skeleton loaders instead of blank screens during initial load
- Implement pull-to-refresh on CollectionViews
- Add swipe actions to collection items (delete, edit)
- Improve accessibility labels and screen reader support
- Add haptic feedback for button presses
- Theme refinements (shadows, spacing, animation)

## Testing & Verification

- **API** – Run `dotnet test` once unit tests are added; manual verification via Swagger UI (`/swagger`) and standard REST clients (Insomnia/Postman).
- **Mobile** – Use MAUI Hot Reload or device/emulator builds. Ensure location and camera permissions are granted on first launch.
- **SignalR** – With API running, start a live walk in the app and watch `RoutePointUpdated` messages in console logs or additional client listeners.

## Deployment Notes

1. **Backend**
   - Provision SQL Server/Azure SQL and update connection strings.
   - Configure Stripe + SendGrid secrets via environment variables or Key Vault.
   - Deploy API to Azure App Service, container apps, or on-prem servers.
2. **Mobile**
   - Replace placeholder assets/app icons with branded versions.
   - Update bundle identifiers + signing certificates for App Store / Google Play.
   - Point `appsettings.json` at production API URLs and regenerate builds per platform.

## Next Steps / Customization Ideas

- Add push notifications (Azure Notification Hubs / Firebase) for booking confirmations.
- Integrate offline caching for bookings + media uploads.
- Extend payment flow with invoice PDFs and email receipts.
- Add multi-walker support + team management.
- Wire up SendGrid email templates for confirmations and summaries.

---

Friends with Fur gives you a production-ready starting point for a premium dog walking experience—customize branding, extend features, and deploy with confidence. 🐾
