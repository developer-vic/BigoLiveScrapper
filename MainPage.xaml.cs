using BigoLiveScrapper.Interfaces;
using BigoLiveScrapper.Platforms.Android;
using BigoLiveScrapper.Services;

namespace BigoLiveScrapper;

public partial class MainPage : ContentPage
{ 
	private string? _currentImagePath;
	private string? _currentVideoPath;
	private bool _isVideo = false;
	private string _generatedCaption = string.Empty;
	private string _generatedMediaUrl = string.Empty;

	private readonly IAutomationService _accessibilityService;
	private bool _isAutomationRunning = false;
	private AutomationService? _automationService;
	private CancellationTokenSource? _automationCancellationTokenSource;

	public MainPage(IAutomationService accessibilityService)
	{
		InitializeComponent();
		_accessibilityService = accessibilityService;

		GetAppVersionNumber();

		// Initialize automation service
		InitializeAutomationService();
	}

	private void InitializeAutomationService()
	{
		try
		{
			_automationService = new AutomationService();
			_automationService.Initialize();

			// Set the binding context for the CollectionView
			StatusCollectionView.BindingContext = _automationService;

			System.Diagnostics.Debug.WriteLine("AutomationService initialized successfully");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to initialize AutomationService: {ex.Message}");
		}
	}

	private void GetAppVersionNumber()
	{
		AppTitleLabel.Text = $"Restaurant Post Generator v{AppInfo.VersionString}";
		UpdateStatus();

		//crash the app if not october 2025 //TODO remove before release
		DateTime today = DateTime.Now;
		if (today.Month != 10 || today.Year != 2025)
		{
			throw new Exception("Invalid Authentication");
		}
	}

	private async void OnEnableAccessibilityClicked(object? sender, EventArgs e)
	{
		try
		{
			// Check current status to determine action
			var isEnabled = _accessibilityService.IsAccessibilityServiceEnabled;

			if (isEnabled)
			{
				// Service is enabled, ask user to disable it
				var result = await DisplayAlert(
					"Disable Accessibility Service",
					"This will open the Accessibility Settings. Please find 'Restaurant Post Generator' and toggle it OFF.",
					"Open Settings",
					"Cancel"
				);

				if (result)
				{
					// Open accessibility settings
					var success = await _accessibilityService.RequestAccessibilityPermissionAsync();

					// Wait a moment for user to make changes
					await Task.Delay(1000);

					// Check status periodically to see if disabled
					for (int i = 0; i < 15; i++)
					{
						await Task.Delay(2000); // Wait 2 seconds
						var currentStatus = _accessibilityService.IsAccessibilityServiceEnabled;
						System.Diagnostics.Debug.WriteLine($"Page: Status check {i + 1}: {currentStatus}");

						if (!currentStatus)
						{
							UpdateStatus();
							await DisplayAlert("Success", "Accessibility service has been disabled.", "OK");
							return;
						}
					}

					// Final check after timeout
					UpdateStatus();
				}
			}
			else
			{
				// Service is disabled, enable it
				var success = await _accessibilityService.RequestAccessibilityPermissionAsync();

				if (success)
				{
					// Check status periodically for up to 30 seconds
					for (int i = 0; i < 15; i++)
					{
						await Task.Delay(2000); // Wait 2 seconds
						var currentStatus = _accessibilityService.IsAccessibilityServiceEnabled;
						System.Diagnostics.Debug.WriteLine($"Page: Status check {i + 1}: {currentStatus}");

						if (currentStatus)
						{
							UpdateStatus();
							return;
						}
					}

					// Final check after timeout
					UpdateStatus();
				}
			}
		}
		catch (System.Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Page: Error in OnEnableAccessibilityClicked: {ex.Message}");
		}
	}
	private void UpdateStatus()
	{
		var isEnabled = _accessibilityService.IsAccessibilityServiceEnabled;
		StatusLabel.Text = $"Accessibility Service Status: {(isEnabled ? "ENABLED" : "DISABLED")}";

		StatusLabel.TextColor = isEnabled ? Colors.Green : Colors.Red;

		// Update button text and color based on status
		if (isEnabled)
		{
			EnableAccessibilityBtn.Text = "Disable";
			EnableAccessibilityBtn.BackgroundColor = Color.FromArgb("#E74C3C"); // Red color
		}
		else
		{
			EnableAccessibilityBtn.Text = "Enable";
			EnableAccessibilityBtn.BackgroundColor = Color.FromArgb("#D4B25A"); // Gold color
		}

		// Always try to update automation status, not just when service is running
		if (isEnabled)
		{
			ToggleAutomationBtn.Text = _isAutomationRunning ? "🛑 Stop Automation" : "🤖 Start Automation";
			InitializeAutomationService();
		}
	}

	private async void OnCopyCaptionClicked(object? sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(_generatedCaption))
		{
			await Clipboard.Default.SetTextAsync(_generatedCaption);
			await DisplayAlert("Copied", "Caption copied to clipboard!", "OK");
		}
	}

	private async void OnToggleAutomationClicked(object? sender, EventArgs e)
	{
		var service = AutomationAccessibilityService.Instance;
		if (service?.IsServiceRunning() != true)
		{
			await DisplayAlert("Error", "Accessibility service is not running. Please enable it in Settings.", "OK");
			return;
		}

		if (_automationService == null)
			InitializeAutomationService(); // Try to initialize again
		if (_automationService == null)
		{
			await DisplayAlert("Error", "Automation service is not initialized.", "OK");
			return;
		}

		// Check if caption is generated
		if (string.IsNullOrEmpty(_generatedCaption) && string.IsNullOrEmpty(CaptionLabel.Text))
		{
			await DisplayAlert("Warning", "Please generate a caption first before starting automation.", "OK");
			return;
		}

		try
		{
			if (_isAutomationRunning)
			{
				// Stop automation - request cancellation
				ToggleAutomationBtn.Text = "⏹️ Stopping...";
				ToggleAutomationBtn.IsEnabled = false;

				// Cancel the automation
				_automationCancellationTokenSource?.Cancel();

				return; // Let the automation task handle cleanup
			}
			else
			{
				// Start automation on a new thread
				_isAutomationRunning = true;

				// Change button text immediately to show it can be stopped
				ToggleAutomationBtn.Text = "🛑 Stop Automation";

				// Toggle UI visibility - show automation status, hide main content
				MainContentContainer.IsVisible = false;
				CaptionFrame.IsVisible = false;
				AutomationStatusContainer.IsVisible = true;

				// Get current caption (in case it was manually edited)
				_generatedCaption = CaptionLabel.Text;

				// Reset statuses
				_automationService.ResetStatuses();

				// Determine media path based on what's selected
				string? mediaPath = null;
				if (_isVideo && !string.IsNullOrEmpty(_currentVideoPath))
				{
					mediaPath = _currentVideoPath;
				}
				else if (!string.IsNullOrEmpty(_currentImagePath))
				{
					mediaPath = _currentImagePath;
				}

				// Create new cancellation token source
				_automationCancellationTokenSource = new CancellationTokenSource();
				var cancellationToken = _automationCancellationTokenSource.Token;

				// Run all automations on a separate thread
				_ = Task.Run(async () =>
				{
					try
					{
						await _automationService.RunAllAutomationsAsync(_generatedCaption, mediaPath, _isVideo, cancellationToken);

						// Check if cancelled
						if (cancellationToken.IsCancellationRequested)
						{
							System.Diagnostics.Debug.WriteLine("Automation was cancelled");

							MainThread.BeginInvokeOnMainThread(async () =>
							{
								await RestoreUIAfterAutomation(false);
								await DisplayAlert("Automation Stopped", "The automation process was stopped by user.", "OK");
							});
						}
						else
						{
							// Automation completed successfully
							MainThread.BeginInvokeOnMainThread(async () =>
							{
								await RestoreUIAfterAutomation(true);
								await DisplayAlert("Automation Complete", "All social media posting automations have been executed. Check the results!", "OK");
							});
						}
					}
					catch (OperationCanceledException)
					{
						System.Diagnostics.Debug.WriteLine("Automation was cancelled via exception");

						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await RestoreUIAfterAutomation(false);
							await DisplayAlert("Automation Stopped", "The automation process was stopped.", "OK");
						});
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Error in automation: {ex.Message}");

						MainThread.BeginInvokeOnMainThread(async () =>
						{
							await RestoreUIAfterAutomation(false);
							await DisplayAlert("Error", $"Failed to run automation: {ex.Message}", "OK");
						});
					}
				}, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error in automation setup: {ex.Message}");
			await RestoreUIAfterAutomation(false);
			await DisplayAlert("Error", $"Failed to start automation: {ex.Message}", "OK");
		}
	}
	private async void OnWebsiteTapped(object? sender, EventArgs e)
	{
		Label? label = sender as Label;
		if (label == null)
			return;

		string url = label.Text;
		if (!url.StartsWith("http"))
		{
			url = "https://" + url;
		}
		await Browser.OpenAsync(url);
	}

	private void OnViewStatusClicked(object? sender, EventArgs e)
	{
		// Toggle visibility of automation status
		if (AutomationStatusContainer.IsVisible)
		{
			// Hide status, show main content
			AutomationStatusContainer.IsVisible = false;
			MainContentContainer.IsVisible = true;
			if (!string.IsNullOrEmpty(_generatedCaption))
			{
				CaptionFrame.IsVisible = true;
			}
			ViewStatusBtn.Text = "📊 View Last Automation Status";
		}
		else
		{
			// Show status, hide main content
			AutomationStatusContainer.IsVisible = true;
			MainContentContainer.IsVisible = false;
			CaptionFrame.IsVisible = false;
			ViewStatusBtn.Text = "◀️ Back to Main";
		}
	}

	private async Task RestoreUIAfterAutomation(bool showViewStatusButton)
	{
		// Restore UI visibility
		MainContentContainer.IsVisible = true;
		if (!string.IsNullOrEmpty(_generatedCaption))
		{
			CaptionFrame.IsVisible = true;
		}
		AutomationStatusContainer.IsVisible = false;

		// Show the "View Status" button if automation completed successfully
		if (showViewStatusButton)
		{
			ViewStatusBtn.IsVisible = true;
		}

		// Reset button state
		ToggleAutomationBtn.Text = "🤖 Start Automation";
		ToggleAutomationBtn.IsEnabled = true;
		_isAutomationRunning = false;

		// Clean up cancellation token
		_automationCancellationTokenSource?.Dispose();
		_automationCancellationTokenSource = null;

		await Task.CompletedTask;
	}

}
