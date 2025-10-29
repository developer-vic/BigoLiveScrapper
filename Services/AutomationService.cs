using System.Collections.ObjectModel; 
using BigoLiveScrapper.Automations;
using BigoLiveScrapper.Platforms.Android;

namespace BigoLiveScrapper.Services
{
    /// <summary>
    /// Centralized service to manage all social media posting automations
    /// </summary>
    public class AutomationService
    {
        private readonly AutomationAccessibilityService _accessibilityService;
        private BigoLiveAutomation? _bigoLiveAutomation;

        public ObservableCollection<AutomationStatusItem> StatusItems { get; private set; }

        public AutomationService()
        {
            _accessibilityService = AutomationAccessibilityService.Instance 
                ?? throw new InvalidOperationException("Accessibility service is not running");
            
            StatusItems = new ObservableCollection<AutomationStatusItem>
            {
                new AutomationStatusItem { Platform = "BigoLive", Status = "Pending", Icon = "📘" }
            };
        }

        /// <summary>
        /// Initialize all automation instances
        /// </summary>
        public void Initialize()
        {
            _bigoLiveAutomation = new BigoLiveAutomation(_accessibilityService);
        }

        /// <summary>
        /// Run all automations sequentially with cancellation support
        /// </summary>
        public async Task RunAllAutomationsAsync(string caption, string? mediaPath = null, bool isVideo = false, CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine("AutomationService: Starting all automations");

            // Run BigoLive automation
            if (!cancellationToken.IsCancellationRequested)
            {
                await RunAutomationAsync(
                    "BigoLive",
                    async () => await (_bigoLiveAutomation?.PostAsync(caption, mediaPath, isVideo, cancellationToken) 
                        ?? Task.FromResult((false, "BigoLive automation not initialized"))),
                    cancellationToken
                );

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                }
            }
 
            if (cancellationToken.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine("AutomationService: Automations cancelled by user");
                
                // Mark remaining pending items as cancelled
                foreach (var item in StatusItems.Where(i => i.Status == "Pending" || i.Status == "Running..."))
                {
                    item.Status = "Cancelled";
                    item.StatusColor = "Gray";
                    item.Message = "Stopped by user";
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("AutomationService: All automations completed");
            }
        }

        /// <summary>
        /// Run a single platform automation with cancellation support
        /// </summary>
        private async Task RunAutomationAsync(string platformName, Func<Task<(bool success, string message)>> automationFunc, CancellationToken cancellationToken = default)
        {
            var statusItem = StatusItems.FirstOrDefault(s => s.Platform == platformName);
            if (statusItem == null) return;

            try
            {
                // Check if cancelled before starting
                if (cancellationToken.IsCancellationRequested)
                {
                    statusItem.Status = "Cancelled";
                    statusItem.StatusColor = "Gray";
                    statusItem.Message = "Stopped by user";
                    return;
                }

                // Set status to Running when actually starting this platform
                statusItem.Status = "Running...";
                statusItem.StatusColor = "Orange";
                statusItem.Message = string.Empty;

                System.Diagnostics.Debug.WriteLine($"AutomationService: Running {platformName} automation");
                
                var result = await automationFunc();
                
                // Check if cancelled after completion
                if (cancellationToken.IsCancellationRequested)
                {
                    statusItem.Status = "Cancelled";
                    statusItem.StatusColor = "Gray";
                    statusItem.Message = "Stopped by user";
                    return;
                }
                
                if (result.success)
                {
                    statusItem.Status = "Success ✓";
                    statusItem.StatusColor = "Green";
                    statusItem.Message = result.message;
                }
                else
                {
                    statusItem.Status = "Failed ✗";
                    statusItem.StatusColor = "Red";
                    statusItem.Message = result.message;
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationService: {platformName} cancelled");
                statusItem.Status = "Cancelled";
                statusItem.StatusColor = "Gray";
                statusItem.Message = "Stopped by user";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationService: Error in {platformName} - {ex.Message}");
                statusItem.Status = "Error ✗";
                statusItem.StatusColor = "Red";
                statusItem.Message = $"Exception: {ex.Message}";
            }
        }

        /// <summary>
        /// Reset all status items
        /// </summary>
        public void ResetStatuses()
        {
            foreach (var item in StatusItems)
            {
                item.Status = "Pending";
                item.StatusColor = "Gray";
                item.Message = string.Empty;
            }
        }
    }

    /// <summary>
    /// Represents the status of an automation for a specific platform
    /// </summary>
    public class AutomationStatusItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _status = "Pending";
        private string _statusColor = "Gray";
        private string _message = string.Empty;

        public string Platform { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string StatusColor
        {
            get => _statusColor;
            set
            {
                if (_statusColor != value)
                {
                    _statusColor = value;
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
