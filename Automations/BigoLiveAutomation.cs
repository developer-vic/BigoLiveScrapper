using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigoLiveScrapper.Data;
using BigoLiveScrapper.Platforms.Android;

namespace BigoLiveScrapper.Automations
{
    /// <summary>
    /// Facebook posting automation using Accessibility Service
    /// </summary>
    public class BigoLiveAutomation
    {
        private readonly AutomationAccessibilityService _accessibilityService;

        public BigoLiveAutomation(AutomationAccessibilityService accessibilityService)
        {
            _accessibilityService = accessibilityService;
        }

        /// <summary>
        /// Automate posting to Facebook with caption and optional media
        /// </summary>
        public async Task<(bool success, string message)> PostAsync(string caption, string? mediaPath = null, bool isVideo = false, CancellationToken cancellationToken = default)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("FacebookAutomation: Starting Facebook post automation");

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Launch Facebook app
                bool launched = _accessibilityService.CheckForegroundAndLaunchApp(FacebookConstants.PACKAGE_NAME);
                if (!launched)
                {
                    return (false, "Failed to launch Facebook app");
                }

                await Task.Delay(3000, cancellationToken);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Navigate to home/feed
                _accessibilityService.GoBack(10, stopAtHome: true);
                await Task.Delay(2000, cancellationToken);

                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Find and click "What's on your mind?" or create post button
                bool createPostClicked = false;

                // Try by text first
                createPostClicked = _accessibilityService.ClickByText(FacebookConstants.CREATE_POST_TEXT_EN) ||
                                    _accessibilityService.ClickByText(FacebookConstants.CREATE_POST_TEXT_DE);
                if (!createPostClicked)
                {
                    // Try by another text option
                    createPostClicked = _accessibilityService.ClickByText(FacebookConstants.WHATS_ON_YOUR_MIND_TEXT_EN) ||
                                        _accessibilityService.ClickByText(FacebookConstants.WHATS_ON_YOUR_MIND_TEXT_DE);
                }
                if (!createPostClicked)
                {
                    return (false, "Could not find create post button");
                }

                await Task.Delay(2000, cancellationToken);
                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // If media path is provided, attach media
                if (!string.IsNullOrEmpty(mediaPath))
                {
                    // Click photo/video button
                    bool photoClicked = _accessibilityService.ClickByText(FacebookConstants.PHOTO_VIDEO_TEXT_EN) ||
                                        _accessibilityService.ClickByText(FacebookConstants.PHOTO_VIDEO_TEXT_DE);

                    if (!photoClicked)
                    {
                        System.Diagnostics.Debug.WriteLine("FacebookAutomation: Could not click photo button, continuing without media");
                    }
                    else
                    {
                        await Task.Delay(2000, cancellationToken);

                        // Check cancellation
                        cancellationToken.ThrowIfCancellationRequested();

                        // Select first photo or video from gallery
                        if (isVideo)
                        {
                            photoClicked = _accessibilityService.ClickByText(FacebookConstants.SELECT_VIDEO_TEXT_EN) ||
                                           _accessibilityService.ClickByText(FacebookConstants.SELECT_VIDEO_TEXT_DE);
                        }
                        else
                        {
                            photoClicked = _accessibilityService.ClickByText(FacebookConstants.SELECT_PHOTO_TEXT_EN) ||
                                           _accessibilityService.ClickByText(FacebookConstants.SELECT_PHOTO_TEXT_DE);
                        }

                        if (!photoClicked)
                        {
                            return (false, "Could not select media from gallery");
                        }
                        await Task.Delay(1000, cancellationToken);
                    }
                }
                await Task.Delay(1000, cancellationToken);
                // Check cancellation before posting
                cancellationToken.ThrowIfCancellationRequested();


                //find first android.widget.AutoCompleteTextView
                var textField = _accessibilityService.FindNodesByClassName(
                    "android.widget.AutoCompleteTextView").FirstOrDefault();
                if (textField != null) _accessibilityService.ClickNode(textField);
                await Task.Delay(1000, cancellationToken);

                // Input caption text
                bool captionEntered = _accessibilityService.InputText(caption, 0);
                if (!captionEntered)
                {
                    return (false, "Could not enter caption text");
                }

                await Task.Delay(1500, cancellationToken);
                // Check cancellation
                cancellationToken.ThrowIfCancellationRequested();


                // Click NEXT button
                bool nextClicked = _accessibilityService.ClickByText(FacebookConstants.NEXT_TEXT_EN, true) ||
                                  _accessibilityService.ClickByText(FacebookConstants.NEXT_TEXT_DE, true);
                if (!nextClicked)
                {
                    return (false, "Could not find NEXT button");
                }
                await Task.Delay(1000, cancellationToken);

                if (!VConstants.IS_TEST_MODE)
                {
                    // Click POST button
                    bool postClicked = _accessibilityService.ClickByText(FacebookConstants.POST_TEXT_EN, true) ||
                                       _accessibilityService.ClickByText(FacebookConstants.POST_TEXT_DE, true);
                    if (!postClicked)
                    {
                        // Try "Share now" button as alternative
                        postClicked = _accessibilityService.ClickByText(FacebookConstants.SHARE_NOW_TEXT_EN, true) ||
                                      _accessibilityService.ClickByText(FacebookConstants.SHARE_NOW_TEXT_DE, true);
                    }

                    if (!postClicked)
                    {
                        return (false, "Could not find POST/SHARE button");
                    }
                    await Task.Delay(3000, cancellationToken);
                }

                System.Diagnostics.Debug.WriteLine("FacebookAutomation: Post completed successfully");
                return (true, "Successfully posted to Facebook");
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("FacebookAutomation: Operation cancelled by user");
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FacebookAutomation: Error - {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
            finally
            {
                // Navigate back to home
                _accessibilityService.GoBack(10, stopAtHome: false);
            }
        }
    }
}
