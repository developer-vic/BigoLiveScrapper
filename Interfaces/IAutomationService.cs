namespace BigoLiveScrapper.Interfaces
{
    public interface IAutomationService
    {
        bool IsAccessibilityServiceEnabled { get; }
        Task<bool> RequestAccessibilityPermissionAsync();
    }
}