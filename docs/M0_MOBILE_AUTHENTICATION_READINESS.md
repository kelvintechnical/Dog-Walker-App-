# M0 mobile authentication readiness — 2026-10-14

Repository-specific baseline: `DogWalkerApp/Services/Authentication/AccessTokenProvider.cs`, `AuthenticatedApiHandler.cs`, `MauiProgram.cs`, `Services/Messaging/RealTimeMessagingService.cs`, Android MainActivity/manifest and iOS AppDelegate/Info.plist. Current Android debug compilation produces an APK. Pure-client transport code is linked into automated .NET tests. Neither compilation nor those tests demonstrates an actual platform sign-in.

## What has been verified without a device

- Secure token-provider interface and MAUI SecureStorage wrapper compile; transport reads the configured token, uses HTTPS/Bearer and clears it after API 401.
- Refit points at configured API settings; account/booking contexts no longer use fabricated identities.
- Real JWT validation and SQL account isolation are tested through API integration, including SignalR membership/revocation. RSA test tokens are not provider-issued tokens.
- Android C#/XAML/assets/package dependencies build with .NET 8 SDK, MAUI Android workload, JDK17 and Android34. Debug signing is generated locally; no production keystore or distribution signing was used.
- Refresh-token acquisition/rotation, a browser sign-in adapter and secure cross-launch provider cache are **not implemented or verified**. AccessTokenProvider reads one stored access token; clearing on 401 is not token refresh. Current mobile UI does not provide a complete customer login/registration journey.

## Work after provider approval (code and configuration)

- Add a maintained provider-compatible MSAL/OIDC adapter using authorization-code + PKCE, state/nonce validation and the operating system browser, not a password collection form or embedded webview.
- Separate public native-client registration from API audience/delegated scopes; keep client secret/refresh tokens out of packaged configuration and logs.
- Configure exact callback/logout URLs and API discovery/issuer/audience. Preserve strict server validation and SQL permission binding. Implement login, account bootstrap, sign-out, recovery/expired session UX and account switching.
- Implement secure persistent cache and silent acquisition/refresh with single-flight concurrency; handle refresh rejection/revocation, rotated credentials, interrupted network calls, sign-in cancellation and cleanup on logout. MSAL manages token refresh; do not invent a separate proprietary authentication server.
- Reauthorize SignalR reconnect and stop/clear subscriptions after logout or account switch. Access-token expiry must not keep granting hub membership.
- Review Android backup (`android:allowBackup="true"` currently), reinstall/restored cache behavior and iOS Keychain persistence/deletion policy. Avoid assuming platform storage wrappers alone prove safe session semantics.

## Android emulator and physical-device checks

- An Android SDK/emulator or device plus a reachable development HTTPS API and approved identity tenant are required. Phone/emulator localhost is not the host API; use a properly reachable test hostname/certificate and appropriate development networking. Never bypass TLS certificate validation.
- `Platforms/Android/MainActivity.cs` is presently an ordinary MAUI activity; the manifest has no provider callback intent filter. Add the chosen adapter's callback activity/intent filter and register exactly the same URI with the provider. Review package name and any signature-hash requirements.
- Test system-browser launch/return, cancellation, wrong/duplicate callback/state, cold-start/deep-link return, MFA/recovery, offline retry, process death, app relaunch, token expiry/refresh, account switching and logout/revocation.
- Test camera/location denial separately from identity; broad existing permissions do not establish working background tracking.
- Emulator checks do not replace physical-device browser/cache/biometric/network behavior. APK install and physical authentication were not run here.

## iOS simulator, device and signing checks

- A compatible Mac, Xcode, supported .NET/MAUI iOS workload and simulator are needed for code-level iOS build and browser-callback checks. This Linux environment cannot provide that verification.
- `Platforms/iOS/AppDelegate.cs` currently has no provider URL callback handler, and Info.plist has no authentication CFBundleURLSchemes. Add the chosen adapter's URL handling and exact registered callback URI. If Keychain sharing is needed (e.g. MSAL), configure appropriate entitlements/groups; these must agree with signing/provisioning.
- Run simulator sign-in/refresh/logout/error handling against the approved test tenant and HTTPS API. A simulator can exercise much of the flow without App Store publication; some Keychain/entitlement/signing behavior still needs real-device validation.
- Physical devices require the appropriate Apple development identity/provisioning workflow. TestFlight/App Store distribution requires paid Apple Developer enrollment in normal circumstances; no membership was purchased. A Personal Team may support limited local-device testing, subject to Apple's limitations; do not assume it supplies distribution readiness.
- Confirm Keychain behavior across relaunch/reinstall, browser return and cancellation, MFA/recovery, background interruption, logout/account switch, and loss of connectivity on a real iPhone/iPad.

## Completion gates

M0's backend build/ownership/database checks can pass independently of provider activation. Full mobile authentication acceptance requires approved provider configuration, implemented browser/session lifecycle, Android and iOS platform checks, physical-device evidence, and resolution of unsupported MAUI 8/current SDK/store compatibility. The framework architecture remains MAUI/ASP.NET Core/SQL Server; do not silently upgrade major versions or start M1. Current commercial launch readiness is not certified.
