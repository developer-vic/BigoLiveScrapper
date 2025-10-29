using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using BigoLiveScrapper.Data;
using BigoLiveScrapper.Platforms.Android;
using Android.Views.Accessibility;

namespace BigoLiveScrapper.Automations
{
    /// <summary>
    /// Bigo Live scraping automation using Accessibility Service
    /// </summary>
    public class BigoLiveAutomation
    {
        private readonly AutomationAccessibilityService _accessibilityService;

        public BigoLiveAutomation(AutomationAccessibilityService accessibilityService)
        {
            _accessibilityService = accessibilityService;
        }

        /// <summary>
        /// Scrape contribution ranking data from Bigo Live app
        /// </summary>
        public async Task<(bool success, string message, string jsonData)> ScrapeAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"BigoLiveAutomation: Starting scraping for user ID: {userId}");

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Launch Bigo Live app
                bool launched = _accessibilityService.CheckForegroundAndLaunchApp(BigoLiveSConstants.PACKAGE_NAME);
                if (!launched)
                {
                    return (false, "Failed to launch Bigo Live app", "");
                }

                await Task.Delay(3000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Navigate to home/feed
                _accessibilityService.GoBack(10, stopAtHome: true);
                await Task.Delay(2000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 1: Find and click search button (sg.bigo.live:id/iv_search)
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Looking for search button");
                var searchButton = _accessibilityService.WaitForElementByResourceId(BigoLiveSConstants.SEARCH_BUTTON_ID, BigoLiveSConstants.SHORT_TIMEOUT);
                if (searchButton == null)
                {
                    return (false, "Could not find search button", "");
                }
                _accessibilityService.ClickNode(searchButton);
                await Task.Delay(2000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 2: Find search input (sg.bigo.live:id/searchInput) - First android.widget.EditText
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Looking for search input");
                var searchInput = _accessibilityService.WaitForElementByResourceId(BigoLiveSConstants.SEARCH_INPUT_ID, BigoLiveSConstants.SHORT_TIMEOUT);
                if (searchInput == null)
                {
                    // Fallback: try to find first EditText
                    var editTexts = _accessibilityService.FindNodesByClassName("android.widget.EditText");
                    if (editTexts == null || editTexts.Count == 0)
                    {
                        return (false, "Could not find search input field", "");
                    }
                    searchInput = editTexts[0];
                }

                // Click and enter username
                _accessibilityService.ClickNode(searchInput);
                await Task.Delay(500, cancellationToken);
                _accessibilityService.InputText(userId, 0);
                await Task.Delay(2000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 3: Back(1) to hide keyboard
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Hiding keyboard");
                _accessibilityService.GoBack(1, stopAtHome: false);
                await Task.Delay(500, cancellationToken);
                _accessibilityService.ClickByResourceId(BigoLiveSConstants.SEARCH_CONFIRM_ID);
                await Task.Delay(1500, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 4: Click first search result (sg.bigo.live:id/searchOptimizeHotId)
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Looking for search result");
                var searchResult = _accessibilityService.WaitForElementByResourceId(BigoLiveSConstants.SEARCH_RESULT_ID, BigoLiveSConstants.SHORT_TIMEOUT);
                if (searchResult == null)
                {
                    return (false, "Could not find search result", "");
                }
                _accessibilityService.ClickNode(searchResult);
                await Task.Delay(3000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 5: Click contribution entry (sg.bigo.live:id/fl_contrib_entry -> sg.bigo.live:id/tv_contribute)
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Looking for contribution entry");
                var contribEntry = _accessibilityService.WaitForElementByResourceId(BigoLiveSConstants.CONTRIB_ENTRY_ID, BigoLiveSConstants.SHORT_TIMEOUT);
                if (contribEntry == null)
                {
                    // Try clicking the text directly
                    var contribText = _accessibilityService.WaitForElementByResourceId(BigoLiveSConstants.CONTRIB_TEXT_ID, BigoLiveSConstants.SHORT_TIMEOUT);
                    if (contribText == null)
                    {
                        return (false, "Could not find contribution entry", "");
                    }
                    _accessibilityService.ClickNode(contribText);
                }
                else
                {
                    _accessibilityService.ClickNode(contribEntry);
                }
                await Task.Delay(3000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 6: Find all 4 tabs (sg.bigo.live:id/uiTabTitle) - Daily, Weekly, Monthly, Overall
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Looking for tabs");
                var rootNode = _accessibilityService.GetRootInActiveWindow();
                List<AccessibilityNodeInfo> tabs = new List<AccessibilityNodeInfo>();

                if (rootNode != null)
                {
                    // Try finding by resource ID directly first
                    var tabNodes = rootNode.FindAccessibilityNodeInfosByViewId(BigoLiveSConstants.TAB_TITLE_ID);
                    if (tabNodes != null && tabNodes.Count >= 4)
                    {
                        tabs = tabNodes.ToList();
                        System.Diagnostics.Debug.WriteLine($"BigoLiveAutomation: Found {tabs.Count} tabs by resource ID");
                    }
                    else
                    {
                        // Fallback: try finding by class name and resource ID filter
                        var allTextViews = _accessibilityService.FindNodesByClassName("android.widget.TextView", rootNode);
                        tabs = allTextViews
                            .Where(n => n.ViewIdResourceName?.Contains("uiTabTitle") == true)
                            .ToList();
                        System.Diagnostics.Debug.WriteLine($"BigoLiveAutomation: Found {tabs.Count} tabs by class name filter");
                    }
                }

                if (tabs.Count < 4)
                {
                    return (false, $"Could not find all 4 tabs. Found {tabs.Count} tabs.", "");
                }

                var scrapedData = new Dictionary<string, List<Dictionary<string, object>>>();

                // Define tab order and max items
                var tabConfigs = new[]
                {
                    new { Name = "Daily", MaxItems = 3 },
                    new { Name = "Weekly", MaxItems = 3 },
                    new { Name = "Monthly", MaxItems = 3 },
                    new { Name = "Overall", MaxItems = 10 }
                };

                for (int tabIndex = 0; tabIndex < Math.Min(tabs.Count, 4); tabIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var tabConfig = tabConfigs[tabIndex];

                    System.Diagnostics.Debug.WriteLine($"BigoLiveAutomation: Scraping {tabConfig.Name} tab");

                    // Click the tab
                    _accessibilityService.ClickNode(tabs[tabIndex]);
                    await Task.Delay(2000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Scrape data for this tab
                    var tabData = await ScrapeTabData(tabConfig.MaxItems, cancellationToken);
                    scrapedData[tabConfig.Name] = tabData;

                    await Task.Delay(1000, cancellationToken);
                }

                // Convert to JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string jsonData = JsonSerializer.Serialize(scrapedData, jsonOptions);

                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Scraping completed successfully");
                return (true, "Successfully scraped data", jsonData);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("BigoLiveAutomation: Operation cancelled by user");
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BigoLiveAutomation: Error - {ex.Message}");
                return (false, $"Error: {ex.Message}", "");
            }
            finally
            {
                // Navigate back to home
                _accessibilityService.GoBack(10, stopAtHome: false);
            }
        }

        /// <summary>
        /// Scrape data from a specific tab
        /// </summary>
        private async Task<List<Dictionary<string, object>>> ScrapeTabData(int maxItems, CancellationToken cancellationToken)
        {
            var results = new List<Dictionary<string, object>>();

            await Task.Delay(1000, cancellationToken); // Wait for tab content to load

            // Find all contribution rows
            var rootNode = _accessibilityService.GetRootInActiveWindow();
            if (rootNode == null)
                return results;

            // Get all user names
            var userNameNodes = rootNode.FindAccessibilityNodeInfosByViewId(BigoLiveSConstants.USER_NAME_ID);
            var contributionNodes = rootNode.FindAccessibilityNodeInfosByViewId(BigoLiveSConstants.CONTRIBUTION_AMOUNT_ID);
            var levelNodes = rootNode.FindAccessibilityNodeInfosByViewId(BigoLiveSConstants.USER_LEVEL_ID);

            int itemCount = Math.Min(maxItems, userNameNodes?.Count ?? 0);

            for (int i = 0; i < itemCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var userData = new Dictionary<string, object>();

                // Get user_id (tv_name)
                if (userNameNodes != null && i < userNameNodes.Count)
                {
                    var userNameNode = userNameNodes[i];
                    userData["user_id"] = userNameNode.Text?.ToString() ?? "";

                    // Try to get profile picture URL from the node's parent structure
                    // This is a simplified approach - you may need to adjust based on actual UI structure
                    userData["profile_picture_url"] = ExtractProfilePictureUrl(userNameNode.Text?.ToString() ?? "") ?? "";
                }

                // Get amount (tv_contribution)
                if (contributionNodes != null && i < contributionNodes.Count)
                {
                    var contributionNode = contributionNodes[i];
                    userData["amount"] = contributionNode.Text?.ToString() ?? "";
                }

                // Get rank_position (index + 1)
                userData["rank_position"] = i + 1;

                // Get user_level (tv_user_level)
                if (levelNodes != null && i < levelNodes.Count)
                {
                    var levelNode = levelNodes[i];
                    userData["user_level"] = levelNode.Text?.ToString() ?? "";
                }

                results.Add(userData);
            }

            return results;
        }

        /// <summary>
        /// Extract profile picture URL from node structure
        /// Looks for ImageView nodes in the parent hierarchy that might contain the URL
        /// </summary>
        private string? ExtractProfilePictureUrl(string user_id)
        {
            try
            {
                // Try to find image node in parent hierarchy
                var parent = node.Parent;
                int depth = 0;
                while (parent != null && depth < 10)
                {
                    // Look for ImageView nodes in parent and its children
                    var imageNodes = _accessibilityService.FindNodesByClassName("android.widget.ImageView", parent);
                    foreach (var imgNode in imageNodes)
                    {
                        // Try to get content description that might contain URL
                        var contentDesc = imgNode.ContentDescription?.ToString();
                        if (!string.IsNullOrEmpty(contentDesc))
                        {
                            // Check if it contains http or bigo.tv
                            if (contentDesc.Contains("http") || contentDesc.Contains("bigo.tv"))
                            {
                                var url = contentDesc.Split('?')[0].Trim(); // Remove query parameters if exists
                                if (!string.IsNullOrEmpty(url))
                                {
                                    return url;
                                }
                            }
                        }

                        // Also check if parent has WebView that might contain HTML
                        // This is a fallback for web-based content
                        var webViewParent = parent;
                        int webViewDepth = 0;
                        while (webViewParent != null && webViewDepth < 3)
                        {
                            if (webViewParent.ClassName?.Contains("WebView") == true)
                            {
                                // If WebView, try to extract from content description
                                var webViewDesc = webViewParent.ContentDescription?.ToString();
                                if (!string.IsNullOrEmpty(webViewDesc) && webViewDesc.Contains("bigo.tv"))
                                {
                                    // Extract URL pattern from content description
                                    // This is a simplified extraction - may need adjustment
                                    var urlMatch = System.Text.RegularExpressions.Regex.Match(webViewDesc, @"https?://[^\s\?]+");
                                    if (urlMatch.Success)
                                    {
                                        return urlMatch.Value.Split('?')[0].Trim();
                                    }
                                }
                            }
                            webViewParent = webViewParent.Parent;
                            webViewDepth++;
                        }
                    }
                    parent = parent.Parent;
                    depth++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting profile picture URL: {ex.Message}");
            }

            return null;
        }
    }
}
