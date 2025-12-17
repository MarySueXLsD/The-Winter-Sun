using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VisualNovel.Services;
using VisualNovel.Scenes;

namespace VisualNovel
{
    public partial class MainWindow : Window
    {
        private readonly SaveLoadService _saveLoadService;
        private readonly SoundService _soundService;
        private readonly TranslationService _translationService;
        private DispatcherTimer? _fpsTimer;
        private int _frameCount = 0;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private bool _isVideoLoading = false;
        private bool _isFirstLoad = true; // Track if this is the first load
        private bool _isVideoLoaded = false; // Track if video is already loaded and ready
        private bool _isMusicFadingOut = false; // Track if music is currently fading out
        private DispatcherTimer? _musicFadeTimer; // Timer for music fade-out
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "video_debug.log");

        private static void LogToFile(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] {message}";
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch { }
        }

        public MainWindow()
        {
            InitializeComponent();
            _saveLoadService = new SaveLoadService();
            _soundService = new SoundService();
            _translationService = TranslationService.Instance;
            
            // Initialize translation service from config
            var configService = new ConfigService();
            _translationService.InitializeFromConfig(configService);
            _translationService.LanguageChanged += TranslationService_LanguageChanged;
            
            SetupFpsCounter();
            LoadDialogueFont();
            LoadCustomCursor();
            
            // Apply translations after window is loaded
            this.Loaded += (s, e) => ApplyTranslations();
            
            // Initially hide loading overlay (will show when loading starts)
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            
            // Load video immediately after initialization
            // This ensures it loads even if Loaded event doesn't fire properly
            // Note: _isVideoLoaded will be false for new instances, so this will load once
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isVideoLoaded || BackgroundVideo.Source == null)
                {
                    LoadVideo();
                }
                else
                {
                    // Video already loaded, ensure overlay is hidden
                    HideLoadingOverlay();
                }
                // Load background music
                LoadBackgroundMusic();
            }), DispatcherPriority.Loaded);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LogToFile("Window_Loaded event fired!");
            Console.WriteLine("Window_Loaded event fired!");
            // Only load if not already loaded
            if (!_isVideoLoaded || BackgroundVideo.Source == null)
            {
                LoadVideo();
            }
            else
            {
                LogToFile("Window_Loaded: Video already loaded, skipping LoadVideo()");
                EnsureVideoPlaying();
            }
            // Load background music if not already loaded
            if (BackgroundMusic.Source == null && BackgroundMusic2.Source == null)
            {
                LoadBackgroundMusic();
            }
            // Check if there's a save file for Continue button
            CheckForContinueOption();
            
            // Pre-load snow animation frames in background so they're ready when game starts
            AnimatedChromaKeyImage.PreloadFramesAsync(
                "Assets\\Videos\\snow-falling-ambient-frames",
                System.Windows.Media.Colors.Black,
                0.3);
            
            // Pre-warm BitmapCache for transition elements to prevent first-time lag
            PreWarmTransitionCache();
        }

        private void PreWarmTransitionCache()
        {
            // Pre-warm the GPU cache by briefly showing elements with opacity 0.01
            // This forces the BitmapCache to be created and uploaded to GPU memory
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Save original states
                var overlayVisibility = TransitionDarkOverlay.Visibility;
                var overlayOpacity = TransitionDarkOverlay.Opacity;
                var titleVisibility = ChapterTitleDisplay.Visibility;
                var titleOpacity = ChapterTitleDisplay.Opacity;
                
                // Set sample text to trigger font loading and glyph caching
                ChapterTitleText.Text = "Chapter 1: Religious Nobility";
                
                // Make elements visible but nearly transparent to trigger cache creation
                // Using 0.01 instead of 0 ensures the GPU actually renders something
                TransitionDarkOverlay.Opacity = 0.01;
                TransitionDarkOverlay.Visibility = Visibility.Visible;
                ChapterTitleDisplay.Opacity = 0.01;
                ChapterTitleDisplay.Visibility = Visibility.Visible;
                
                // Force layout and render pass
                TransitionDarkOverlay.UpdateLayout();
                ChapterTitleDisplay.UpdateLayout();
                TransitionDarkOverlay.InvalidateVisual();
                ChapterTitleDisplay.InvalidateVisual();
                ChapterTitleText.InvalidateVisual();
                
                // Use a timer to ensure GPU has time to process before hiding
                var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                hideTimer.Tick += (s, args) =>
                {
                    hideTimer.Stop();
                    // Restore original states
                    TransitionDarkOverlay.Visibility = overlayVisibility;
                    TransitionDarkOverlay.Opacity = overlayOpacity;
                    ChapterTitleDisplay.Visibility = titleVisibility;
                    ChapterTitleDisplay.Opacity = titleOpacity;
                };
                hideTimer.Start();
                
            }), DispatcherPriority.Loaded);
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // When window becomes visible, ensure video is loaded and playing
            if ((bool)e.NewValue == true)
            {
                LogToFile("Window became visible!");
                Console.WriteLine("Window became visible!");
                // Window just became visible
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LogToFile($"Window_IsVisibleChanged: Source is null: {BackgroundVideo.Source == null}, IsVideoLoaded: {_isVideoLoaded}, IsLoading: {_isVideoLoading}");
                    Console.WriteLine($"Window_IsVisibleChanged: Source is null: {BackgroundVideo.Source == null}, IsVideoLoaded: {_isVideoLoaded}, IsLoading: {_isVideoLoading}");
                    if (BackgroundVideo.Source == null && !_isVideoLoading)
                    {
                        LogToFile("Window_IsVisibleChanged: Source is null, loading video");
                        _isVideoLoading = false; // Reset flag
                        LoadVideo();
                    }
                    else if (_isVideoLoaded && BackgroundVideo.Source != null)
                    {
                        LogToFile("Window_IsVisibleChanged: Video already loaded, ensuring it's playing");
                        EnsureVideoPlaying();
                    }
                    else if (_isVideoLoading)
                    {
                        LogToFile("Window_IsVisibleChanged: Video is already loading, skipping...");
                    }
                }), DispatcherPriority.Loaded);
            }
        }

        private void LoadVideo()
        {
            // Prevent multiple simultaneous loads
            if (_isVideoLoading)
            {
                LogToFile("LoadVideo() called but video is already loading, skipping...");
                return;
            }
            
            // If video is already loaded and source is set, just ensure it's playing
            if (_isVideoLoaded && BackgroundVideo.Source != null)
            {
                LogToFile("LoadVideo() called but video is already loaded, calling EnsureVideoPlaying() instead");
                HideLoadingOverlay(); // Ensure overlay is hidden
                EnsureVideoPlaying();
                return;
            }
                
            _isVideoLoading = true;
            
            // Ensure video is visible
            BackgroundVideo.Visibility = Visibility.Visible;
            
            // Load and play background video
            // Note: background_main_menu.mp4 is deprecated - video path can be configured here for future use
            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Videos", "background_main_menu.mp4");
            
            // Check if video exists (deprecated video, will fall back to image)
            if (File.Exists(videoPath))
            {
                LogToFile($"Attempting to load video from: {videoPath}");
                LogToFile($"File exists: {File.Exists(videoPath)}");
                Console.WriteLine($"Attempting to load video from: {videoPath}");
                Console.WriteLine($"File exists: {File.Exists(videoPath)}");
            }
            else
            {
                // Video not found - fall back to image (expected behavior for deprecated video)
                LogToFile($"Background video not found (deprecated), using background image instead");
                BackgroundVideo.Visibility = Visibility.Collapsed;
                LoadBackgroundImage();
                _isVideoLoading = false;
                HideLoadingOverlay();
                return;
            }
            
            if (File.Exists(videoPath))
            {
                // Verify it's actually a video file (check extension)
                string extension = Path.GetExtension(videoPath).ToLower();
                if (extension != ".mp4" && extension != ".wmv" && extension != ".avi" && extension != ".mov")
                {
                    LogToFile($"File at {videoPath} is not a recognized video format: {extension}");
                    Console.WriteLine($"File at {videoPath} is not a recognized video format: {extension}");
                    BackgroundVideo.Visibility = Visibility.Collapsed;
                    _isVideoLoading = false;
                    return;
                }
                
                // Remove existing event handlers to avoid duplicates
                BackgroundVideo.MediaOpened -= BackgroundVideo_MediaOpened;
                BackgroundVideo.MediaFailed -= BackgroundVideo_MediaFailed;
                
                // Handle media opened event to start playing
                BackgroundVideo.MediaOpened += BackgroundVideo_MediaOpened;
                BackgroundVideo.MediaFailed += BackgroundVideo_MediaFailed;
                
                // Show loading overlay
                ShowLoadingOverlay();
                
                // Load video immediately without delays
                try
                {
                    BackgroundVideo.Stop();
                }
                catch { }
                
                BackgroundVideo.Source = null;
                
                // Set source immediately
                try
                {
                    var uri = new Uri(videoPath, UriKind.Absolute);
                    LogToFile($"Load - Created URI: {uri}");
                    Console.WriteLine($"Load - Created URI: {uri}");
                    
                    LogToFile($"Load - Setting video source to: {uri}");
                    Console.WriteLine($"Load - Setting video source to: {uri}");
                    BackgroundVideo.Source = uri;
                    LogToFile($"Load - Video source set. Waiting for MediaOpened event...");
                    Console.WriteLine($"Load - Video source set. Waiting for MediaOpened event...");
                }
                catch (Exception ex)
                {
                    LogToFile($"Error setting video source: {ex.Message}");
                    LogToFile($"Stack trace: {ex.StackTrace}");
                    Console.WriteLine($"Error setting video source: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    _isVideoLoading = false;
                    HideLoadingOverlay();
                }
            }
            else
            {
                // Fallback if video not found - load image instead
                LogToFile($"Video file not found at: {videoPath}");
                Console.WriteLine($"Video file not found at: {videoPath}");
                BackgroundVideo.Visibility = Visibility.Collapsed;
                LoadBackgroundImage();
                _isVideoLoading = false;
                HideLoadingOverlay();
            }
        }

        private void LoadBackgroundImage()
        {
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "main_menu_background.png");
                if (File.Exists(imagePath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    BackgroundImage.Source = bitmap;
                    BackgroundImage.Visibility = Visibility.Visible;
                    LogToFile($"Background image loaded: {imagePath}");
                }
                else
                {
                    LogToFile($"Background image file not found: {imagePath}");
                    BackgroundImage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading background image: {ex.Message}");
                BackgroundImage.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadVideoIfNeeded()
        {
            LogToFile($"LoadVideoIfNeeded called. Source is null: {BackgroundVideo.Source == null}, IsLoading: {_isVideoLoading}, IsVideoLoaded: {_isVideoLoaded}");
            LogToFile($"MediaElement Visibility: {BackgroundVideo.Visibility}, IsLoaded: {BackgroundVideo.IsLoaded}");
            Console.WriteLine($"LoadVideoIfNeeded called. Source is null: {BackgroundVideo.Source == null}, IsLoading: {_isVideoLoading}, IsVideoLoaded: {_isVideoLoaded}");
            Console.WriteLine($"MediaElement Visibility: {BackgroundVideo.Visibility}, IsLoaded: {BackgroundVideo.IsLoaded}");
            
            // If video is already loaded and playing, no need to do anything
            if (_isVideoLoaded && BackgroundVideo.Source != null)
            {
                LogToFile("Video is already loaded and source exists, ensuring it's playing");
                Console.WriteLine("Video is already loaded and source exists, ensuring it's playing");
                EnsureVideoPlaying();
                return; // Return early to prevent further processing
            }
            
            // Public method to allow external calls to load video
            if (BackgroundVideo.Source == null && !_isVideoLoading)
            {
                LogToFile("Calling LoadVideo() from LoadVideoIfNeeded");
                Console.WriteLine("Calling LoadVideo() from LoadVideoIfNeeded");
                LoadVideo();
            }
            else if (BackgroundVideo.Source != null)
            {
                LogToFile("Source exists, calling EnsureVideoPlaying()");
                Console.WriteLine("Source exists, calling EnsureVideoPlaying()");
                EnsureVideoPlaying();
            }
            else
            {
                LogToFile("Video is already loading, skipping...");
                Console.WriteLine("Video is already loading, skipping...");
            }
        }
        
        public bool IsVideoLoaded()
        {
            return _isVideoLoaded && BackgroundVideo.Source != null;
        }

        private void EnsureVideoPlaying()
        {
            if (BackgroundVideo.Source == null)
                return;
                
            try
            {
                BackgroundVideo.Visibility = Visibility.Visible;
                
                // Only reset position if this is the initial load (video hasn't started yet)
                // Check if video position is at the start or if it's the first time
                if (BackgroundVideo.Position == TimeSpan.Zero || !_isVideoLoaded)
                {
                    BackgroundVideo.Position = TimeSpan.Zero;
                }
                
                // Only stop and restart if video hasn't loaded yet or has ended
                if (!_isVideoLoaded || 
                    (BackgroundVideo.NaturalDuration.HasTimeSpan && 
                     BackgroundVideo.Position >= BackgroundVideo.NaturalDuration.TimeSpan))
                {
                    BackgroundVideo.Stop();
                    BackgroundVideo.Play();
                }
                else
                {
                    // Just ensure it's playing, preserve current position
                    var currentPosition = BackgroundVideo.Position;
                    BackgroundVideo.Play();
                    // Restore position in case Play() reset it
                    if (currentPosition < BackgroundVideo.NaturalDuration.TimeSpan)
                    {
                        BackgroundVideo.Position = currentPosition;
                    }
                }
                
                // Double-check after a short delay
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (BackgroundVideo.Source != null && _isVideoLoaded)
                        {
                            // Only resume if paused, don't reset position
                            var currentPos = BackgroundVideo.Position;
                            BackgroundVideo.Play();
                            if (currentPos < BackgroundVideo.NaturalDuration.TimeSpan)
                            {
                                BackgroundVideo.Position = currentPos;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error in EnsureVideoPlaying retry: {ex.Message}");
                        Console.WriteLine($"Error in EnsureVideoPlaying retry: {ex.Message}");
                    }
                }), DispatcherPriority.Input);
            }
            catch (Exception ex)
            {
                LogToFile($"Error in EnsureVideoPlaying: {ex.Message}");
                Console.WriteLine($"Error in EnsureVideoPlaying: {ex.Message}");
            }
        }

        private void BackgroundVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            LogToFile("MediaOpened event fired!");
            Console.WriteLine("MediaOpened event fired!");
            
            // Mark first load as complete only after MediaOpened fires
            if (_isFirstLoad)
            {
                _isFirstLoad = false;
                LogToFile("First load completed - MediaOpened fired successfully");
                Console.WriteLine("First load completed - MediaOpened fired successfully");
            }
            
            // Ensure video is visible and starts playing when media is opened
            BackgroundVideo.Visibility = Visibility.Visible;
            BackgroundImage.Visibility = Visibility.Collapsed; // Hide image when video loads
            BackgroundVideo.Position = TimeSpan.Zero;
            _isVideoLoading = false; // Video loaded successfully
            _isVideoLoaded = true; // Mark video as successfully loaded
            
            LogToFile($"MediaOpened - Video URI: {BackgroundVideo.Source}");
            Console.WriteLine($"MediaOpened - Video URI: {BackgroundVideo.Source}");
            
            // Force play immediately
            try
            {
                LogToFile("Attempting to play video in MediaOpened...");
                Console.WriteLine("Attempting to play video in MediaOpened...");
                BackgroundVideo.Position = TimeSpan.Zero;
                BackgroundVideo.Play();
                LogToFile("Play() called successfully");
                Console.WriteLine("Play() called successfully");
            }
            catch (Exception ex)
            {
                LogToFile($"Error playing video in MediaOpened: {ex.Message}");
                LogToFile($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Error playing video in MediaOpened: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            // Verify video is actually playing with a retry
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (BackgroundVideo.Source != null)
                    {
                        BackgroundVideo.Visibility = Visibility.Visible;
                        BackgroundVideo.Play();
                        LogToFile("Retry play() called");
                        Console.WriteLine("Retry play() called");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Error in MediaOpened retry: {ex.Message}");
                    Console.WriteLine($"Error in MediaOpened retry: {ex.Message}");
                }
            }), DispatcherPriority.Loaded);
            
            // Hide loading overlay with fade animation
            HideLoadingOverlay();
        }

        private void BackgroundVideo_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            LogToFile($"Video failed to load: {e.ErrorException?.Message ?? "Unknown error"}");
            Console.WriteLine($"Video failed to load: {e.ErrorException?.Message ?? "Unknown error"}");
            BackgroundVideo.Visibility = Visibility.Collapsed;
            _isVideoLoading = false;
            _isVideoLoaded = false;
            HideLoadingOverlay();
            // Fallback to image
            LoadBackgroundImage();
        }
        
        private void ShowLoadingOverlay()
        {
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Opacity = 1;
                LoadingOverlay.Visibility = Visibility.Visible;
            }
        }
        
        private void HideLoadingOverlay()
        {
            if (LoadingOverlay != null)
            {
                var storyboard = LoadingOverlay.Resources["FadeOutStoryboard"] as Storyboard;
                if (storyboard != null)
                {
                    storyboard.Completed += (s, e) =>
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                    };
                    storyboard.Begin(LoadingOverlay);
                }
                else
                {
                    // Fallback if storyboard not found
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // When window is activated (e.g., after closing a modal dialog),
            // don't restart the video - it should continue playing in the background
            BackgroundVideo.Visibility = Visibility.Visible;
            
            // Only handle video if it's not already loaded and playing
            if (BackgroundVideo.Source == null && !_isVideoLoading)
            {
                // If source is null and not loading, load video
                LogToFile("OnActivated: Source is null, attempting to load video");
                _isVideoLoading = false; // Reset flag before loading
                LoadVideo();
            }
            else if (BackgroundVideo.Source != null && _isVideoLoaded)
            {
                // Video is loaded - check if it has ended and needs to loop
                try
                {
                    if (BackgroundVideo.NaturalDuration.HasTimeSpan && 
                        BackgroundVideo.Position >= BackgroundVideo.NaturalDuration.TimeSpan)
                    {
                        // Video has ended, loop it
                        BackgroundVideo.Position = TimeSpan.Zero;
                        BackgroundVideo.Play();
                    }
                    // Otherwise, video is playing - don't interfere with it
                    // The video continues playing even when modal dialogs are open
                }
                catch
                {
                    // If there's an error, try to reload
                    LogToFile("OnActivated: Error checking video, attempting to reload");
                    _isVideoLoading = false;
                    _isVideoLoaded = false;
                    LoadVideo();
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Load video when window source is initialized (only if not already loaded)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isVideoLoaded || BackgroundVideo.Source == null)
                {
                    LoadVideo();
                }
                else
                {
                    LogToFile("OnSourceInitialized: Video already loaded, skipping LoadVideo()");
                    EnsureVideoPlaying();
                }
            }), DispatcherPriority.Loaded);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            // Ensure video starts playing after content is rendered
            if (BackgroundVideo.Source != null)
            {
                EnsureVideoPlaying();
            }
            else if (!_isVideoLoading)
            {
                // If source is null and not loading, try to load video
                LogToFile("OnContentRendered: Source is null, attempting to load video");
                _isVideoLoading = false; // Reset flag before loading
                LoadVideo();
            }
            else
            {
                LogToFile("OnContentRendered: Video is already loading, skipping...");
            }
        }

        private void CheckForContinueOption()
        {
            var allSaves = _saveLoadService.GetAllSaves();
            if (allSaves.Count > 0)
            {
                // Find the most recent save
                var latestSave = allSaves[0];
                foreach (var save in allSaves)
                {
                    if (save.SaveDate > latestSave.SaveDate)
                    {
                        latestSave = save;
                    }
                }
                ContinueButton.Visibility = Visibility.Visible;
            }
        }

        private void LoadDialogueFont()
        {
            try
            {
                // Use FontHelper to load Dialogues Latin font with fallback
                var fontFamily = FontHelper.LoadDialogueFontWithFallback();
                        Resources["MinecraftFont"] = fontFamily;
                LogToFile($"Dialogues Latin font with fallback loaded successfully. Font source: {fontFamily.Source}");
                        
                        // Apply font directly to all text elements
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                ApplyFontToElements(fontFamily);
                            }
                            catch (Exception ex)
                            {
                                LogToFile($"Error applying font to UI elements: {ex.Message}");
                            }
                        }), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading Dialogues Latin font: {ex.Message}");
                LogToFile($"Stack trace: {ex.StackTrace}");
                Resources["MinecraftFont"] = new FontFamily("Arial");
            }
        }

        private void LoadCustomCursor()
        {
            try
            {
                string cursorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "AOM_Titans Cursor.cur");
                if (File.Exists(cursorPath))
                {
                    this.Cursor = new Cursor(cursorPath);
                    LogToFile($"Custom cursor loaded: {cursorPath}");
                }
                else
                {
                    LogToFile($"Custom cursor file not found: {cursorPath}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading custom cursor: {ex.Message}");
            }
        }
        
        private void ApplyFontToElements(FontFamily fontFamily)
        {
            // Apply to title
            if (GameTitle != null)
            {
                GameTitle.FontFamily = fontFamily;
            }
            
            // Find and apply to subtitle (the text below the title)
            // The subtitle is in the same StackPanel as GameTitle
            var stackPanel = GameTitle?.Parent as StackPanel;
            if (stackPanel != null)
            {
                // Find the TextBlock that comes after GameTitle in the StackPanel
                bool foundTitle = false;
                foreach (var child in stackPanel.Children)
                {
                    if (child == GameTitle)
                    {
                        foundTitle = true;
                        continue;
                    }
                    if (foundTitle && child is TextBlock subtitle)
                    {
                        subtitle.FontFamily = fontFamily;
                        break;
                    }
                }
            }
            
            // Apply to all buttons via the style
            var buttons = new[] { ContinueButton, NewGameButton, LoadGameButton, OptionsButton, CreditsButton, QuitButton };
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    // Find the TextBlock in the button template and set its font
                    var textBlock = FindVisualChild<TextBlock>(button);
                    if (textBlock != null)
                    {
                        textBlock.FontFamily = fontFamily;
                    }
                }
            }
            
            // Apply to loading text
            var loadingText = FindVisualChild<TextBlock>(LoadingOverlay, tb => tb.Text == "Loading...");
            if (loadingText != null)
            {
                loadingText.FontFamily = fontFamily;
            }
            
            // Apply to version text
            if (VersionText != null)
            {
                VersionText.FontFamily = fontFamily;
            }
            
            // Apply to FPS counter
            if (FPSCounter != null)
            {
                FPSCounter.FontFamily = fontFamily;
            }
            
            // Apply to subtitle and add letter spacing
            if (SubtitleText != null)
            {
                SubtitleText.FontFamily = fontFamily;
                ApplyLetterSpacing(SubtitleText, 5.0); // 5 pixels spacing between characters
            }
        }
        
        private void ApplyLetterSpacing(TextBlock textBlock, double spacing)
        {
            if (textBlock.Text == null || textBlock.Text.Length == 0) return;
            
            // Rebuild the text with Runs that have spacing between characters
            var text = textBlock.Text;
            textBlock.Inlines.Clear();
            
            for (int i = 0; i < text.Length; i++)
            {
                var run = new Run(text[i].ToString())
                {
                    FontFamily = textBlock.FontFamily,
                    FontSize = textBlock.FontSize,
                    Foreground = textBlock.Foreground,
                    FontStyle = textBlock.FontStyle
                };
                textBlock.Inlines.Add(run);
                
                // Add spacing after each character except the last one and spaces/commas
                if (i < text.Length - 1 && text[i] != ' ' && text[i] != ',')
                {
                    // Add a thin space character to create letter spacing
                    var spaceRun = new Run("\u2009") // Thin space (U+2009)
                    {
                        FontSize = spacing
                    };
                    textBlock.Inlines.Add(spaceRun);
                }
            }
        }
        
        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    if (predicate == null || predicate(result))
                    {
                        return result;
                    }
                }
                
                var childOfChild = FindVisualChild<T>(child, predicate);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
        
        private static void FindAllVisualChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            if (parent == null) return;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    results.Add(result);
                }
                FindAllVisualChildren(child, results);
            }
        }

        private void SetupFpsCounter()
        {
            _fpsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            // Count frames using CompositionTarget
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            _frameCount++;
        }

        private void FpsTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastFpsUpdate).TotalSeconds;
            if (elapsed > 0)
            {
                var fps = (int)(_frameCount / elapsed);
                FPSCounter.Text = $"FPS: {fps}";
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
        }

        private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the video
            BackgroundVideo.Position = TimeSpan.Zero;
            BackgroundVideo.Play();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                QuitButton_Click(sender, e);
            }
            else if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                // Activate the first visible button
                if (ContinueButton.Visibility == Visibility.Visible)
                {
                    ContinueButton_Click(sender, e);
                }
                else
                {
                    NewGameButton_Click(sender, e);
                }
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            var allSaves = _saveLoadService.GetAllSaves();
            if (allSaves.Count > 0)
            {
                // Find the most recent save
                var latestSave = allSaves[0];
                foreach (var save in allSaves)
                {
                    if (save.SaveDate > latestSave.SaveDate)
                    {
                        latestSave = save;
                    }
                }

                // Start fading music immediately (don't wait)
                FadeOutMusic(null);
                
                // Use the chapter title transition for loading saved games too
                TransitionToGameSceneWithChapterTitle(
                    _translationService.GetTranslation("Chapter1_Title"), 
                    null, 
                    latestSave.CurrentDialogueIndex, 
                    latestSave.GameState);
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            
            // Start fading music immediately (don't wait)
            FadeOutMusic(null);
            
            // Start the new transition sequence with chapter title
            // Pass null to indicate new game - GameScene will be created and preloaded during transition
            var translationService = TranslationService.Instance;
            var configService = new ConfigService();
            translationService.InitializeFromConfig(configService);
            TransitionToGameSceneWithChapterTitle(translationService.GetTranslation("Chapter1_Title"), null);
        }

        private void LoadGameButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            var loadMenu = new SaveLoadMenu(null, _saveLoadService, isLoadMenu: true);
            loadMenu.ShowDialog();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            
            // Always create fresh - the optimizations in SettingsMenu make it fast enough
            // Preloading was causing issues with ShowDialog on previously shown windows
            var configService = new ConfigService();
            var settingsMenu = new SettingsMenu(configService);
            settingsMenu.Owner = this;
            settingsMenu.ShowDialog();
            
            // Apply settings after settings menu closes
            ApplySettingsFromConfig();
        }
        

        private void ApplySettingsFromConfig()
        {
            try
            {
                var configService = new ConfigService();
                var config = configService.GetConfig();
                double musicVolume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
                
                if (BackgroundMusic != null)
                {
                    BackgroundMusic.Volume = musicVolume;
                }
                if (BackgroundMusic2 != null)
                {
                    BackgroundMusic2.Volume = musicVolume;
                }
            }
            catch { }
        }

        private void CreditsButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            var creditsDialog = new Dialogs.GameDialog(
                _translationService.GetTranslation("Dialog_Credits_Title"),
                _translationService.GetTranslation("Dialog_Credits_Message"),
                Dialogs.DialogType.OK);
            creditsDialog.Owner = this;
            creditsDialog.ShowDialog();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            var quitDialog = new Dialogs.GameDialog(
                _translationService.GetTranslation("Dialog_QuitGame_Title"),
                _translationService.GetTranslation("Dialog_QuitGame_Message"),
                Dialogs.DialogType.YesNo);
            quitDialog.Owner = this;
            quitDialog.ShowDialog();
            
            if (quitDialog.DialogResult == true)
            {
                // Shutdown immediately - no lag
                Application.Current.Shutdown();
            }
        }

        private void LoadBackgroundMusic()
        {
            try
            {
                // Apply volume settings from config
                var configService = new ConfigService();
                var config = configService.GetConfig();
                double musicVolume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
                
                // Load autumn_ambient.mp3
                string autumnMusicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Music", "autumn_ambient.mp3");
                if (File.Exists(autumnMusicPath))
                {
                    BackgroundMusic.Source = new Uri(autumnMusicPath, UriKind.Absolute);
                    BackgroundMusic.Volume = musicVolume; // Use volume from config
                    BackgroundMusic.Play();
                    LogToFile($"Background music 1 loaded: {autumnMusicPath} at volume {musicVolume}");
                }
                else
                {
                    LogToFile($"Background music file not found: {autumnMusicPath}");
                }

                // Load city_ambient.mp3
                string cityMusicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Music", "city_ambient.mp3");
                if (File.Exists(cityMusicPath))
                {
                    BackgroundMusic2.Source = new Uri(cityMusicPath, UriKind.Absolute);
                    BackgroundMusic2.Volume = musicVolume; // Use volume from config
                    BackgroundMusic2.Play();
                    LogToFile($"Background music 2 loaded: {cityMusicPath} at volume {musicVolume}");
                }
                else
                {
                    LogToFile($"Background music file not found: {cityMusicPath}");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading background music: {ex.Message}");
            }
        }

        private void BackgroundMusic_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the autumn ambient music
            if (!_isMusicFadingOut && BackgroundMusic.Source != null)
            {
                BackgroundMusic.Position = TimeSpan.Zero;
                BackgroundMusic.Play();
            }
        }

        private void BackgroundMusic2_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the city ambient music
            if (!_isMusicFadingOut && BackgroundMusic2.Source != null)
            {
                BackgroundMusic2.Position = TimeSpan.Zero;
                BackgroundMusic2.Play();
            }
        }

        private void FadeOutMusic(Action? onComplete)
        {
            if (_isMusicFadingOut || (BackgroundMusic.Source == null && BackgroundMusic2.Source == null))
            {
                onComplete?.Invoke();
                return;
            }

            _isMusicFadingOut = true;
            double initialVolume1 = BackgroundMusic.Source != null ? BackgroundMusic.Volume : 0;
            double initialVolume2 = BackgroundMusic2.Source != null ? BackgroundMusic2.Volume : 0;
            double fadeDuration = 1.0; // 1 second fade-out
            int steps = 20; // Number of steps for smooth fade
            double stepInterval = fadeDuration / steps;
            double volumeStep1 = initialVolume1 / steps;
            double volumeStep2 = initialVolume2 / steps;

            _musicFadeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(stepInterval)
            };

            int currentStep = 0;
            _musicFadeTimer.Tick += (s, e) =>
            {
                currentStep++;
                double newVolume1 = Math.Max(0, initialVolume1 - (volumeStep1 * currentStep));
                double newVolume2 = Math.Max(0, initialVolume2 - (volumeStep2 * currentStep));
                
                if (BackgroundMusic.Source != null)
                {
                    BackgroundMusic.Volume = newVolume1;
                }
                if (BackgroundMusic2.Source != null)
                {
                    BackgroundMusic2.Volume = newVolume2;
                }

                if (currentStep >= steps || (newVolume1 <= 0 && newVolume2 <= 0))
                {
                    _musicFadeTimer.Stop();
                    if (BackgroundMusic.Source != null)
                    {
                        BackgroundMusic.Stop();
                        BackgroundMusic.Volume = initialVolume1; // Reset volume for next time
                    }
                    if (BackgroundMusic2.Source != null)
                    {
                        BackgroundMusic2.Stop();
                        BackgroundMusic2.Volume = initialVolume2; // Reset volume for next time
                    }
                    _isMusicFadingOut = false;
                    onComplete?.Invoke();
                }
            };

            _musicFadeTimer.Start();
        }

        private void TransitionToGameScene(Action createGameScene)
        {
            FadeOutMusic(() =>
            {
                // Music has faded out, now transition to game scene
                // Note: Game music loading will be handled in GameScene when music file is added
                createGameScene?.Invoke();
            });
        }

        private void TransitionToGameSceneWithChapterTitle(string chapterName, Action? createGameScene, int? loadFromIndex = null, Models.GameState? gameState = null)
        {
            // Hide menu elements and sidebar immediately
            if (MenuSidebar != null)
            {
                MenuSidebar.Visibility = Visibility.Collapsed;
            }
            
            // Create GameScene early (hidden) so it can preload everything while chapter title is showing
            GameScene? preloadedGameScene = null;
            
            // Create GameScene now (for new game or loading saved game)
            if (createGameScene == null)
            {
                try
                {
                    // Create GameScene but don't show it yet
                    // We'll show it off-screen when chapter title appears
                    preloadedGameScene = new GameScene(loadFromIndex, gameState);
                    // Configure it to be hidden and off-screen
                    // Keep opacity at 1 to prevent white blinking - we'll control visibility instead
                    preloadedGameScene.Left = -10000; // Move off-screen
                    preloadedGameScene.Top = -10000;
                    preloadedGameScene.Opacity = 1; // Keep at 1 to prevent white flash
                    preloadedGameScene.ShowInTaskbar = false;
                    preloadedGameScene.WindowState = WindowState.Normal; // Don't maximize yet
                    preloadedGameScene.Visibility = Visibility.Hidden; // Hide using Visibility instead
                    // Don't call Show() yet - we'll show it when chapter title appears
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create game scene: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                        "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            
            // Step 1: Fade out the menu (dark overlay)
            TransitionDarkOverlay.Visibility = Visibility.Visible;
            TransitionDarkOverlay.Opacity = 0;
            
            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            // Set desired frame rate for smooth GPU-accelerated animation
            Timeline.SetDesiredFrameRate(fadeOutAnimation, 60);
            Storyboard.SetTarget(fadeOutAnimation, TransitionDarkOverlay);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            
            fadeOutStoryboard.Completed += (s, e) =>
            {
                // Step 2: Show chapter title (GameScene is loading in background)
                // Now show the GameScene off-screen so it can load (it's already created)
                if (preloadedGameScene != null)
                {
                    // Show the window but keep it hidden and off-screen so it can render
                    preloadedGameScene.Show();
                    preloadedGameScene.Visibility = Visibility.Hidden;
                    preloadedGameScene.UpdateLayout();
                    // Force a render pass to ensure background is ready
                    preloadedGameScene.InvalidateVisual();
                }
                
                ChapterTitleText.Text = chapterName;
                ChapterTitleDisplay.Visibility = Visibility.Visible;
                ChapterTitleDisplay.Opacity = 0;
                
                // Start pulsing animation immediately (it will be visible once opacity fades in)
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StartChapterTitlePulse();
                }), DispatcherPriority.Loaded);
                
                var fadeInTitleStoryboard = new Storyboard();
                var fadeInTitleAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                // Set desired frame rate for smooth GPU-accelerated animation
                Timeline.SetDesiredFrameRate(fadeInTitleAnimation, 60);
                Storyboard.SetTarget(fadeInTitleAnimation, ChapterTitleDisplay);
                Storyboard.SetTargetProperty(fadeInTitleAnimation, new PropertyPath(UIElement.OpacityProperty));
                fadeInTitleStoryboard.Children.Add(fadeInTitleAnimation);
                
                fadeInTitleStoryboard.Completed += (s2, e2) =>
                {
                    
                    // Step 3: Wait for frames to load OR minimum 3 seconds, then fade out chapter title
                    var startTime = DateTime.Now;
                    var minWaitTime = TimeSpan.FromSeconds(3.0); // Minimum 3 seconds
                    var checkInterval = TimeSpan.FromMilliseconds(100); // Check every 100ms
                    
                    // Check cache directly first (before GameScene is fully initialized)
                    bool framesCached = AnimatedChromaKeyImage.AreFramesCached("Assets\\Videos\\snow-falling-ambient-frames");
                    
                    var waitTimer = new DispatcherTimer
                    {
                        Interval = checkInterval
                    };
                    waitTimer.Tick += (s3, e3) =>
                    {
                        var elapsed = DateTime.Now - startTime;
                        // Check both cache and GameScene instance
                        bool framesLoaded = framesCached || (preloadedGameScene?.IsSnowVideoFramesLoaded() ?? false);
                        bool minTimeElapsed = elapsed >= minWaitTime;
                        
                        if (framesLoaded && minTimeElapsed)
                        {
                            waitTimer.Stop();
                            
                            var fadeOutTitleStoryboard = new Storyboard();
                            var fadeOutTitleAnimation = new DoubleAnimation
                            {
                                From = 1,
                                To = 0,
                                Duration = TimeSpan.FromSeconds(0.5),
                                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                            };
                            // Set desired frame rate for smooth GPU-accelerated animation
                            Timeline.SetDesiredFrameRate(fadeOutTitleAnimation, 60);
                            Storyboard.SetTarget(fadeOutTitleAnimation, ChapterTitleDisplay);
                            Storyboard.SetTargetProperty(fadeOutTitleAnimation, new PropertyPath(UIElement.OpacityProperty));
                            fadeOutTitleStoryboard.Children.Add(fadeOutTitleAnimation);
                            
                            fadeOutTitleStoryboard.Completed += (s4, e4) =>
                            {
                                // Stop pulsing animation
                                StopChapterTitlePulse();
                                ChapterTitleDisplay.Visibility = Visibility.Collapsed;
                                
                                // Step 4: Show the preloaded GameScene (everything is already loaded)
                                if (preloadedGameScene != null)
                                {
                                    // First, ensure the window is fully rendered off-screen before moving it
                                    preloadedGameScene.UpdateLayout();
                                    preloadedGameScene.InvalidateVisual();
                                    
                                    // Wait a frame to ensure rendering is complete
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        // Now move it on-screen and make it visible
                                        preloadedGameScene.Left = 0;
                                        preloadedGameScene.Top = 0;
                                        preloadedGameScene.WindowState = WindowState.Maximized;
                                        preloadedGameScene.ShowInTaskbar = true;
                                        preloadedGameScene.Visibility = Visibility.Visible;
                                        preloadedGameScene.Opacity = 1; // Already at 1, but ensure it stays
                                        preloadedGameScene.Activate();
                                        preloadedGameScene.Focus();
                                        
                                        // Force another render pass after making visible
                                        preloadedGameScene.UpdateLayout();
                                        preloadedGameScene.InvalidateVisual();
                                        
                                        // Wait for GameScene to be fully rendered before closing MainWindow
                                        Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            // Force multiple render passes to ensure GameScene is fully covering screen
                                            preloadedGameScene.UpdateLayout();
                                            preloadedGameScene.InvalidateVisual();
                                            
                                            // Wait longer to ensure window is fully covering screen and rendered
                                            var delayTimer = new DispatcherTimer
                                            {
                                                Interval = TimeSpan.FromMilliseconds(100)
                                            };
                                            delayTimer.Tick += (s5, e5) =>
                                            {
                                                delayTimer.Stop();
                                                
                                                // GameScene is now fully visible and covering screen
                                                // Close MainWindow - the dark overlay will disappear but GameScene is already there
                                                // No white blink because GameScene (with dark background) is covering everything
                                                this.Close();
                                            };
                                            delayTimer.Start();
                                        }), DispatcherPriority.Render);
                                    }), DispatcherPriority.Loaded);
                                }
                                else
                                {
                                    // Fallback: create normally if preloading failed
                                    createGameScene?.Invoke();
                                }
                            };
                            
                            fadeOutTitleStoryboard.Begin();
                        }
                    };
                    waitTimer.Start();
                };
                
                fadeInTitleStoryboard.Begin();
            };
            
            fadeOutStoryboard.Begin();
        }

        public void FadeOutMusicAndClose(Action createGameScene)
        {
            TransitionToGameScene(createGameScene);
        }

        /// <summary>
        /// Load a saved game with chapter title transition (called from SaveLoadMenu)
        /// </summary>
        public void LoadGameWithChapterTitle(int dialogueIndex, Models.GameState gameState)
        {
            // Start fading music immediately
            FadeOutMusic(null);
            
            // Use the chapter title transition
            TransitionToGameSceneWithChapterTitle(
                _translationService.GetTranslation("Chapter1_Title"), 
                null, 
                dialogueIndex, 
                gameState);
        }

        private void TranslationService_LanguageChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyTranslations();
            }), DispatcherPriority.Normal);
        }

        private void ApplyTranslations()
        {
            try
            {
                if (ContinueButton != null)
                    ContinueButton.Content = _translationService.GetTranslation("MainMenu_Continue");
                if (NewGameButton != null)
                    NewGameButton.Content = _translationService.GetTranslation("MainMenu_NewGame");
                if (LoadGameButton != null)
                    LoadGameButton.Content = _translationService.GetTranslation("MainMenu_LoadGame");
                if (OptionsButton != null)
                    OptionsButton.Content = _translationService.GetTranslation("MainMenu_Options");
                if (CreditsButton != null)
                    CreditsButton.Content = _translationService.GetTranslation("MainMenu_Credits");
                if (QuitButton != null)
                    QuitButton.Content = _translationService.GetTranslation("MainMenu_Quit");
                if (GameTitle != null)
                    GameTitle.Text = _translationService.GetTranslation("MainMenu_GameTitle");
                if (SubtitleText != null)
                    SubtitleText.Text = _translationService.GetTranslation("MainMenu_Subtitle");
                // Update chapter title subtitle (used in transitions)
                if (ChapterTitleText != null)
                    ChapterTitleText.Text = _translationService.GetTranslation("Chapter1_Subtitle");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying translations: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _fpsTimer?.Stop();
            _musicFadeTimer?.Stop();
            BackgroundVideo?.Close();
            BackgroundMusic?.Close();
            BackgroundMusic2?.Close();
            if (_translationService != null)
            {
                _translationService.LanguageChanged -= TranslationService_LanguageChanged;
            }
            base.OnClosed(e);
            
            // If no other main windows are open, shut down the application
            // This handles the case when MainWindow is closed via X button or after transitioning
            bool hasOtherWindows = false;
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this && window.IsVisible && !(window is SaveLoadMenu) && !(window is Dialogs.GameDialog) && !(window is SettingsMenu))
                {
                    hasOtherWindows = true;
                    break;
                }
            }
            
            if (!hasOtherWindows)
            {
                Application.Current.Shutdown();
            }
        }

        private void StartChapterTitlePulse()
        {
            // Stop any existing animation
            StopChapterTitlePulse();

            // Check if elements are available
            if (ChapterTitleScale == null || ChapterTitleText == null)
            {
                return;
            }

            // Create very subtle pulsing animation that scales from 0.98 to 1.02 (2% variation)
            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.98,
                To = 1.02,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.98,
                To = 1.02,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            // Set desired frame rate for smooth GPU-accelerated animation
            Timeline.SetDesiredFrameRate(scaleXAnimation, 60);
            Timeline.SetDesiredFrameRate(scaleYAnimation, 60);

            // Apply animations directly to the ScaleTransform
            ChapterTitleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            ChapterTitleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        private void StopChapterTitlePulse()
        {
            // Stop animations and reset scale to 1.0
            if (ChapterTitleScale != null)
            {
                ChapterTitleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                ChapterTitleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                ChapterTitleScale.ScaleX = 1.0;
                ChapterTitleScale.ScaleY = 1.0;
            }
        }
    }
}
