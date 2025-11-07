# BigoLiveScrapper

Modern .NET MAUI utility that automates the Bigo Live Android app to capture contribution leaderboards and return a structured JSON summary. The Android automation is powered by an accessibility service that navigates the app UI, opens a target host profile, iterates contribution tabs (Daily, Weekly, Monthly, Overall), and enriches each entry with profile metadata fetched over HTTPS.

## Features

- Android accessibility automation that launches Bigo Live, searches for a host, and drills into contribution rankings automatically.
- JSON output that includes the searched username, per-tab contributor lists, ranking positions, contribution amounts, and profile image URLs.
- Built-in viewer for formatted JSON with copy/share tools, plus an inline preview on the main page.
- One-tap shortcut to Android accessibility settings so users can enable or disable the helper service without leaving the app.

## Requirements

- .NET 9 SDK with the .NET MAUI workload (`dotnet workload install maui`).
- Android SDK platform 25 or newer and build tools that support API level 36 (configured in the project file).
- A physical or virtual Android device with developer mode enabled.
- Permission to enable the custom accessibility service on the device you plan to automate.

> Windows builds are optional; the project targets `net9.0-android` by default, with `net9.0-windows10.0.19041.0` available when building from Windows.

## Getting Started

```bash
git clone https://github.com/<your-org>/BigoLiveScrapper.git
cd BigoLiveScrapper
dotnet restore
dotnet build
```

If you have not installed the MAUI workload yet:

```bash
dotnet workload install maui
```

### Configure Android signing (optional)

The project includes release signing placeholders in `BigoLiveScrapper.csproj`. Override these values via environment variables or a separate `Directory.Build.props` file before creating production builds to avoid checking secrets into source control.

## Running on Android

1. Connect an Android device or start an emulator.
2. Verify the device is visible to `adb` (`adb devices`).
3. Deploy the app:
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```
4. Allow the app to install and launch on the device.

## In-App Workflow

1. **Enable the accessibility service** – tap the `Enable` button. The app opens Android Accessibility Settings so you can toggle on `Bigo Live Scrapper`.
2. **Enter a Bigo Live user ID** – the automation uses this ID to search for the host profile.
3. **Start scraping** – tap `START`. The automation:
   - Launches (or resumes) the Bigo Live app.
   - Searches for the specified user and opens the host tab.
   - Navigates to the contribution rankings and iterates the Daily, Weekly, Monthly, and Overall tabs.
   - Captures the top entries (3 per tab in test mode, up to 10 overall in production).
4. **View results** – the JSON response appears inline. Use `📋 Copy` to share or `🔍 Full Screen` for a formatted view with syntax highlighting.

While scraping is in progress the UI displays a spinner and the `START` button changes to `🛑 Stop`, allowing you to cancel mid-run.

## JSON Output

The automation produces an object shaped like:

```json
{
  "searched_username": "exampleHost",
  "summary": {
    "total_users_scraped": 12,
    "total_tabs_scraped": 4,
    "tabs": {
      "Daily": 3,
      "Weekly": 3,
      "Monthly": 3,
      "Overall": 3
    }
  },
  "data": {
    "Daily": [
      {
        "user_id": "RA_H2019",
        "username": "Sample Fan",
        "amount": "12,345",
        "rank_position": 1,
        "user_level": "Lv 32",
        "profile_picture_url": "https://.../avatar.png"
      }
      // ...
    ]
    // Additional tabs...
  }
}
```

All emoji or escaped characters in usernames are decoded, and profile pictures are fetched from `https://www.bigo.tv/user/{user_id}` when possible.

## Project Layout

- `Pages/` – MAUI UI screens (`MainPage`, `JsonViewerPage`) and their code-behind logic.
- `Services/AutomationService.cs` – orchestration layer that ties UI actions to the Android automation worker.
- `Automations/BigoLiveAutomation.cs` – step-by-step UI automation and JSON assembly logic.
- `Platforms/Android/` – accessibility service implementation plus platform-specific helpers.
- `Data/` – selectors and configuration constants shared by automation components.

## Troubleshooting

- If the `START` button stays disabled, the accessibility service is not active. Tap `Enable`, toggle the service in system settings, then return to the app.
- When the automation cannot find expected UI elements, verify the Bigo Live app version and ensure the device language matches the selectors in `Data/BigoLiveSConstants.cs`.
- Network failures while fetching profile pictures are non-fatal; missing URLs simply default to empty strings.
- For best reliability, grant the app overlay permissions and disable battery optimizations for the session.

## Security & Ethics

Use this tool only on accounts and data you are authorized to process. Automating third-party apps may violate their terms of service, and scraping personal data can carry legal obligations in your jurisdiction.


