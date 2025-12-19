using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VisualNovel.Models;
using VisualNovel.Services;
using System.Windows.Media.Imaging;

namespace VisualNovel
{
    public partial class GameScene : Window
    {
        private readonly StoryService _storyService;
        private readonly SaveLoadService _saveLoadService;
        private readonly ConfigService _configService;
        private readonly SoundService _soundService;
        private readonly TranslationService _translationService;
        private int _currentDialogueIndex;
        private DispatcherTimer _typingTimer;
        private DispatcherTimer _timeBlinkTimer;
        private DispatcherTimer? _fpsTimer;
        private string _currentFullText = "";
        private int _currentCharIndex = 0;
        private bool _isTyping = false;
        private int _frameCount = 0;
        private string? _currentLeftImagePath = null;
        private string? _currentRightImagePath = null;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private int? _currentLeftSpot = null;
        private int? _currentRightSpot = null;
        private GameCamera _camera;
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dialogues.log");
        
        // Character slot tracking for multiple characters (by UI slot index 0-5)
        private Dictionary<int, (Border border, Image image, ScaleTransform scale, string? imagePath, int? spot)> _characterSlots = new Dictionary<int, (Border, Image, ScaleTransform, string?, int?)>();
        
        // Track which character image is at which spot (1-6) from previous dialogue
        private Dictionary<int, string?> _previousSpotCharacters = new Dictionary<int, string?>();
        
        // Flag to defer character positioning until window is ready (prevents using wrong dimensions)
        private bool _deferCharacterPositioning = false;
        
        // Character spot positions (1-6, as percentage of screen width from left)
        // Evenly distributed across 6 columns
        private static readonly double[] SpotPositions = { 0.083, 0.25, 0.417, 0.583, 0.75, 0.917 }; // Spot 1-6 (6-column grid)
        private const double CharacterWidthPercentage = 0.225; // 22.5% of window width (0.75 of 30%)
        private const double CharacterHeightPercentage = 0.675; // 67.5% of window height (0.75 of 90%)
        private const double DialogueWindowHeight = 260; // Height of dialogue window (matches XAML)
        private const double CharacterBottomOffset = 180; // Position characters a bit lower (above dialogue window with some space)
        
        /// <summary>
        /// Get character width based on current window width
        /// </summary>
        private double GetCharacterWidth()
        {
            return this.ActualWidth * CharacterWidthPercentage;
        }
        
        /// <summary>
        /// Get character height based on current window height
        /// </summary>
        private double GetCharacterHeight()
        {
            return this.ActualHeight * CharacterHeightPercentage;
        }
        
        // Parallax effect variables
        private TranslateTransform? _backgroundParallaxTransform;
        private TranslateTransform? _characterParallaxTransform;
        private Point _lastMousePosition;
        private Point _targetParallaxPosition;
        private DispatcherTimer? _parallaxUpdateTimer;
        private double _windowCenterX = 0;
        private double _windowCenterY = 0;
        private double _inverseCenterX = 1.0;
        private double _inverseCenterY = 1.0;
        private double BackgroundParallaxSpeed = 0.15; // Background moves slower (15% of mouse movement)
        private double CharacterParallaxSpeed = 0.5; // Characters move faster (50% of mouse movement)
        private double MaxParallaxOffset = 100.0; // Maximum offset in pixels
        private const double VerticalParallaxReduction = 0.5; // Less vertical movement
        private int ParallaxUpdateInterval = 16; // Update parallax at ~60 FPS (16ms)
        private const double EasingFactor = 0.2; // Slightly faster response (was 0.15)
        private const double MinUpdateThreshold = 0.1; // Skip update if change is too small (pixels)
        private bool _parallaxEnabled = true;

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

        public GameScene(int? loadFromIndex = null, GameState? gameState = null)
        {
            InitializeComponent();
            
            // Start with window fully opaque - opacity will be controlled by MainWindow during transition
            // This prevents white blinking during scene transitions
            this.Opacity = 1;
            
            // Load and apply Dialogues Latin font to all text elements
            LoadDialogueFont();
            
            // Load custom cursor
            LoadCustomCursor();
            
            _saveLoadService = new SaveLoadService();
            _configService = new ConfigService();
            _soundService = new SoundService();
            _translationService = TranslationService.Instance;
            _translationService.InitializeFromConfig(_configService);
            _translationService.LanguageChanged += TranslationService_LanguageChanged;
            
            // Initialize StoryService with saved game state or new state
            if (gameState != null)
            {
                _storyService = new StoryService(gameState);
                _storyService.LoadScene(gameState.CurrentSceneId);
                _currentDialogueIndex = loadFromIndex ?? gameState.CurrentDialogueIndex;
            }
            else
            {
                _storyService = new StoryService();
                _currentDialogueIndex = loadFromIndex ?? 0;
                // Set chapter title for new game
                UpdateChapterTitle();
            }
            
            // Load background image for the whole scene
            LoadSceneBackground();
            
            // Load game music (placeholder - will be implemented when music file is added)
            LoadGameMusic();
            
            // Start loading snow falling video frames immediately (in background) - don't wait for window to load
            // FramesPath is already set in XAML, so we just need to trigger loading early
            LoadSnowVideoEarly();
            
            // Update location display
            UpdateLocationDisplay();
            
            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_configService.GetConfig().TextSpeed)
            };
            _typingTimer.Tick += TypingTimer_Tick;
            
            // Setup time blink timer for analogue watch effect
            _timeBlinkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Blink every second
            };
            _timeBlinkTimer.Tick += TimeBlinkTimer_Tick;
            _timeBlinkTimer.Start();
            
            // Setup FPS counter
            SetupFpsCounter();
            
            // Setup choice menu
            ChoiceMenuControl.ChoiceSelected += OnChoiceSelected;
            
            // Initialize camera
            _camera = new GameCamera(
                CameraZoomTransform,
                CameraPanTransform,
                CameraContainer,
                CharacterContainer,
                this);
            
            // Initialize character slots dictionary
            InitializeCharacterSlots();
            
            // Update character sizes when window loads
            this.Loaded += (s, e) => UpdateCharacterSizes();
            
            // Handle window size changes to update character positions and sizes
            this.SizeChanged += GameScene_SizeChanged;
            
            // Load dialogue - set flag to defer character positioning until window is ready
            _deferCharacterPositioning = true;
            LoadDialogue(_currentDialogueIndex);
            
            // Fade in the window when it's shown
            this.Loaded += GameScene_Loaded;
            
            // Initialize parallax after window is loaded
            this.Loaded += (s, e) => InitializeParallax();
            
            // Apply translations after window is loaded
            this.Loaded += (s, e) => ApplyTranslations();
        }

        private void GameScene_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure window is visible and ready
            this.Visibility = Visibility.Visible;
            
            // Start playing the snow video animation (frames should already be loading from constructor)
            LoadSnowVideo();
            
            // Apply settings from config after window is fully loaded
            ApplySettings();
            
            // Ensure characters and camera are positioned correctly after window is fully loaded
            // This fixes the issue where characters are misplaced when loading saved games
            // Use a timer to wait for window to be fully maximized and laid out
            var repositionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            int repositionAttempts = 0;
            bool hasRepositioned = false;
            
            repositionTimer.Tick += (timerSender, timerArgs) =>
            {
                repositionAttempts++;
                
                // Wait until window has actual dimensions (is fully maximized)
                if (!hasRepositioned && this.ActualWidth > 100 && this.ActualHeight > 100)
                {
                    hasRepositioned = true;
                    repositionTimer.Stop();
                    
                    // Clear deferred positioning flag now that window is ready
                    _deferCharacterPositioning = false;
                    
                    // Update character sizes and positions based on actual window dimensions
                    UpdateCharacterSizes();
                    
                    var dialogue = _storyService.GetDialogue(_currentDialogueIndex);
                    if (dialogue != null)
                    {
                        // Reposition all characters to correct positions using actual window dimensions
                        RepositionAllCharacters(dialogue);
                    }
                    
                    LogToFile($"Characters repositioned after {repositionAttempts} attempts, window size: {this.ActualWidth}x{this.ActualHeight}");
                    
                    // After repositioning, fade out the dark overlay
                    var delayTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(200)
                    };
                    delayTimer.Tick += (s2, e2) =>
                    {
                        delayTimer.Stop();
                        FadeOutSceneOverlay();
                    };
                    delayTimer.Start();
                }
                else if (repositionAttempts >= 20) // Give up after 2 seconds
                {
                    repositionTimer.Stop();
                    LogToFile($"Failed to reposition characters after {repositionAttempts} attempts");
                    
                    // Still fade out overlay even if repositioning failed
                    FadeOutSceneOverlay();
                }
            };
            
            repositionTimer.Start();
        }

        private void FadeOutSceneOverlay()
        {
            if (SceneTransitionOverlay == null) return;
            
            // Create fade-out animation for the overlay
            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(0.6),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(fadeOutAnimation, SceneTransitionOverlay);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);
            
            fadeOutStoryboard.Completed += (s, e) =>
            {
                // Hide the overlay after fade completes
                SceneTransitionOverlay.Visibility = Visibility.Collapsed;
            };
            
            fadeOutStoryboard.Begin();
        }

        private void GameScene_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Update cached window center for parallax
            UpdateWindowCenterCache();
            
            // Update character sizes when window size changes
            UpdateCharacterSizes();
            
            // Reposition characters when window size changes (but don't re-apply zoom)
            var dialogue = _storyService.GetDialogue(_currentDialogueIndex);
            if (dialogue != null)
            {
                // Temporarily store zoom to prevent re-applying
                double? savedZoom = dialogue.CameraZoom;
                dialogue.CameraZoom = null; // Temporarily remove zoom to prevent double zoom
                PositionCharacters(dialogue);
                dialogue.CameraZoom = savedZoom; // Restore zoom value
            }
        }
        
        /// <summary>
        /// Update all character border sizes based on current window dimensions
        /// </summary>
        private void UpdateCharacterSizes()
        {
            if (this.ActualWidth == 0 || this.ActualHeight == 0)
                return; // Window not yet initialized
                
            double charWidth = GetCharacterWidth();
            double charHeight = GetCharacterHeight();
            
            // Update all character slot borders
            CharacterImageLeftBorder.Width = charWidth;
            CharacterImageLeftBorder.Height = charHeight;
            
            CharacterImageRightBorder.Width = charWidth;
            CharacterImageRightBorder.Height = charHeight;
            
            CharacterImageSlot3Border.Width = charWidth;
            CharacterImageSlot3Border.Height = charHeight;
            
            CharacterImageSlot4Border.Width = charWidth;
            CharacterImageSlot4Border.Height = charHeight;
            
            CharacterImageSlot5Border.Width = charWidth;
            CharacterImageSlot5Border.Height = charHeight;
            
            CharacterImageSlot6Border.Width = charWidth;
            CharacterImageSlot6Border.Height = charHeight;
        }

        /// <summary>
        /// Clear all character slots and hide all character borders.
        /// Used before reloading dialogue to reset state.
        /// </summary>
        private void ClearAllCharacterSlots()
        {
            // Hide all character borders
            CharacterImageLeftBorder.Visibility = Visibility.Collapsed;
            CharacterImageRightBorder.Visibility = Visibility.Collapsed;
            CharacterImageSlot3Border.Visibility = Visibility.Collapsed;
            CharacterImageSlot4Border.Visibility = Visibility.Collapsed;
            CharacterImageSlot5Border.Visibility = Visibility.Collapsed;
            CharacterImageSlot6Border.Visibility = Visibility.Collapsed;
            
            // Clear the character slots dictionary
            foreach (var kvp in _characterSlots)
            {
                kvp.Value.border.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Reposition all visible characters to their correct positions based on current window dimensions.
        /// This is used after window load to fix positions that were calculated with incorrect dimensions.
        /// </summary>
        private void RepositionAllCharacters(DialogueLine dialogue)
        {
            if (this.ActualWidth == 0 || this.ActualHeight == 0)
                return;
            
            // Use the window's actual width directly to ensure correct positioning
            // This is critical after loading because _camera.GetWindowWidth() may return PrimaryScreenWidth
            // which can be different from actual window width due to DPI scaling or taskbar
            double windowWidth = this.ActualWidth;
            double charWidth = windowWidth * CharacterWidthPercentage;
            
            LogToFile($"RepositionAllCharacters: windowWidth={windowWidth}, charWidth={charWidth}");
            
            // Reposition characters using CharacterSlots (multiple character system)
            if (dialogue.CharacterSlots != null && dialogue.CharacterSlots.Count > 0)
            {
                foreach (var kvp in _characterSlots)
                {
                    int slotIndex = kvp.Key;
                    var slot = kvp.Value;
                    
                    if (slot.border.Visibility == Visibility.Visible && slot.spot.HasValue)
                    {
                        int spotNumber = slot.spot.Value;
                        if (spotNumber >= 1 && spotNumber <= 6)
                        {
                            double position = SpotPositions[spotNumber - 1] * windowWidth - (charWidth / 2);
                            position = Math.Max(0, Math.Min(position, windowWidth - charWidth));
                            
                            // Set position directly without animation
                            Canvas.SetLeft(slot.border, position);
                            Canvas.SetBottom(slot.border, CharacterBottomOffset);
                            
                            // Update size
                            slot.border.Width = charWidth;
                            slot.border.Height = GetCharacterHeight();
                        }
                    }
                }
            }
            
            // Also reposition legacy left/right characters if visible
            if (CharacterImageLeftBorder.Visibility == Visibility.Visible && _currentLeftSpot.HasValue)
            {
                int spot = _currentLeftSpot.Value;
                if (spot >= 1 && spot <= 6)
                {
                    double position = SpotPositions[spot - 1] * windowWidth - (charWidth / 2);
                    position = Math.Max(0, Math.Min(position, windowWidth - charWidth));
                    Canvas.SetLeft(CharacterImageLeftBorder, position);
                }
            }
            
            if (CharacterImageRightBorder.Visibility == Visibility.Visible && _currentRightSpot.HasValue)
            {
                int spot = _currentRightSpot.Value;
                if (spot >= 1 && spot <= 6)
                {
                    double position = SpotPositions[spot - 1] * windowWidth - (charWidth / 2);
                    position = Math.Max(0, Math.Min(position, windowWidth - charWidth));
                    Canvas.SetLeft(CharacterImageRightBorder, position);
                }
            }
            
            LogToFile($"RepositionAllCharacters: windowWidth={windowWidth}, charWidth={charWidth}");
        }

        private void LoadSceneBackground()
        {
            string backgroundPath;
            
            // Check current scene and load appropriate background
            var currentScene = _storyService.GetCurrentScene();
            if (currentScene != null && currentScene.SceneId == "Scene1")
            {
                // Use village.png for Scene1 (winter sun story)
                backgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "Scenes", "village.png");
            }
            else
            {
                // Default fallback
                backgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "Untitled1.png");
            }
            
            if (File.Exists(backgroundPath))
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(backgroundPath, UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    // Get image dimensions
                    double imageWidth = bitmap.PixelWidth;
                    double imageHeight = bitmap.PixelHeight;
                    
                    BackgroundImage.Source = bitmap;
                    BackgroundImage.Visibility = Visibility.Visible;
                    
                    // Center camera on wider image after window is loaded
                    // Wait for window to be ready so we can get accurate viewport dimensions
                    this.Loaded += (s, e) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (_camera != null && BackgroundImage.Source != null)
                            {
                                double viewportWidth = CameraContainer.ActualWidth > 0 ? CameraContainer.ActualWidth : this.ActualWidth;
                                double viewportHeight = CameraContainer.ActualHeight > 0 ? CameraContainer.ActualHeight : this.ActualHeight;
                                
                                if (viewportWidth > 0 && viewportHeight > 0)
                                {
                                    // Calculate the pan offset needed to center the wider image
                                    double imageAspectRatio = imageWidth / imageHeight;
                                    double viewportAspectRatio = viewportWidth / viewportHeight;
                                    
                                    if (imageAspectRatio > viewportAspectRatio)
                                    {
                                        // Image is wider - calculate pan to center
                                        double renderedWidth = viewportHeight * imageAspectRatio;
                                        double excessWidth = renderedWidth - viewportWidth;
                                        double panX = -excessWidth / 2.0;
                                        
                                        // Set this as the default pan offset for when no characters are visible
                                        _camera.SetDefaultPanOffset(panX);
                                        
                                        // Apply the centering now
                                        _camera.CenterOnBackgroundImage(imageWidth, imageHeight, viewportWidth, viewportHeight);
                                        LogToFile($"Centered camera on background image: {Path.GetFileName(backgroundPath)} (Image: {imageWidth}x{imageHeight}, Viewport: {viewportWidth}x{viewportHeight}, Pan: {panX})");
                                    }
                                    else
                                    {
                                        // Image is not wider, reset default pan to 0
                                        _camera.SetDefaultPanOffset(0);
                                    }
                                }
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    };
                }
                catch (Exception ex)
                {
                    LogToFile($"Error loading scene background image: {ex.Message}");
                    BackgroundImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                LogToFile($"Scene background image not found: {backgroundPath}");
                BackgroundImage.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Start loading frames early in the constructor (before window is shown)
        /// FramesPath is already set in XAML, so frames will start loading when control is initialized
        /// </summary>
        private void LoadSnowVideoEarly()
        {
            try
            {
                if (SnowVideo == null)
                {
                    LogToFile("LoadSnowVideoEarly: SnowVideo is null!");
                    return;
                }

                // FramesPath is already set in XAML, so frames should start loading automatically
                // We just need to ensure the control triggers loading
                // The AnimatedChromaKeyImage will start loading frames when it's initialized
                LogToFile("LoadSnowVideoEarly: Triggering early frame loading (frames will load in background)");
                
                // Frames will start loading automatically when the control is loaded
                // We'll call Play() later when the window is actually shown
            }
            catch (Exception ex)
            {
                LogToFile($"LoadSnowVideoEarly: Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if the snow video frames are fully loaded
        /// </summary>
        public bool IsSnowVideoFramesLoaded()
        {
            try
            {
                if (SnowVideo == null)
                {
                    // Check cache directly if control isn't available yet
                    string framesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Videos", "snow-falling-ambient-frames");
                    return AnimatedChromaKeyImage.AreFramesCached("Assets\\Videos\\snow-falling-ambient-frames");
                }
                return SnowVideo.IsFramesLoaded;
            }
            catch
            {
                return false;
            }
        }

        private void LoadSnowVideo()
        {
            try
            {
                if (SnowVideo == null)
                {
                    LogToFile("LoadSnowVideo: SnowVideo is null!");
                    return;
                }

                // AnimatedChromaKeyImage loads frames automatically when FramesPath is set in XAML
                // Just ensure it's visible and playing
                string framesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Videos", "snow-falling-ambient-frames");
                
                LogToFile($"LoadSnowVideo: Checking for frames directory at: {framesPath}");
                LogToFile($"LoadSnowVideo: Directory exists: {Directory.Exists(framesPath)}");
                
                if (Directory.Exists(framesPath))
                {
                    LogToFile($"LoadSnowVideo: Frames directory found, animation should start automatically");
                    SnowVideo.Visibility = Visibility.Visible;
                    SnowVideo.Play();
                }
                else
                {
                    LogToFile($"LoadSnowVideo: Frames directory not found at: {framesPath}");
                    SnowVideo.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"LoadSnowVideo: Error loading snow animation: {ex.Message}");
                LogToFile($"LoadSnowVideo: Stack trace: {ex.StackTrace}");
                if (SnowVideo != null)
                {
                    SnowVideo.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void UpdateLocationDisplay()
        {
            var currentScene = _storyService.GetCurrentScene();
            if (currentScene != null)
            {
                LocationText.Text = currentScene.SceneName.ToUpper();
            }
            else
            {
                LocationText.Text = _translationService.GetTranslation("GameScene_UnknownLocation");
            }
        }

        private void LoadDialogueFont()
        {
            var dialogueFont = FontHelper.LoadDialogueFontWithFallback();
                Resources["MinecraftFont"] = dialogueFont;
                
                // Apply font to all text elements
                CharacterNameText.FontFamily = dialogueFont;
                CharacterStatusText.FontFamily = dialogueFont;
                DialogueText.FontFamily = dialogueFont;
                SkipIndicator.FontFamily = dialogueFont;
                LocationText.FontFamily = dialogueFont;
                TimeHours.FontFamily = dialogueFont;
                TimeColon.FontFamily = dialogueFont;
                TimeMinutes.FontFamily = dialogueFont;
                ChapterTitleText.FontFamily = dialogueFont;
                VersionText.FontFamily = dialogueFont;
                FPSCounter.FontFamily = dialogueFont;
                SaveButton.FontFamily = dialogueFont;
                LoadButton.FontFamily = dialogueFont;
                MenuButton.FontFamily = dialogueFont;
            
            // Apply translations after font is loaded
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyTranslations();
            }), DispatcherPriority.Loaded);
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

        private void ApplySettings()
        {
            try
            {
                var config = _configService?.GetConfig();
                if (config == null)
                {
                    LogToFile("Warning: Config is null, using defaults");
                    return;
                }
            
                // Apply audio settings
                if (GameBackgroundMusic != null)
                {
                    GameBackgroundMusic.Volume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
                }
            
            // Apply text settings
            if (DialogueText != null)
            {
                DialogueText.FontSize = config.FontSize;
            }
            if (CharacterNameText != null)
            {
                CharacterNameText.FontSize = config.FontSize;
            }
            
            // Apply display settings
            if (FPSCounter != null)
            {
                FPSCounter.Visibility = config.ShowFPS ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (LocationText != null && TimeContainer != null)
            {
                LocationText.Visibility = config.ShowLocationTime ? Visibility.Visible : Visibility.Collapsed;
                TimeContainer.Visibility = config.ShowLocationTime ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (ChapterTitleText != null && ChapterTitleLine != null)
            {
                ChapterTitleText.Visibility = config.ShowChapterTitle ? Visibility.Visible : Visibility.Collapsed;
                ChapterTitleLine.Visibility = config.ShowChapterTitle ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (SkipIndicator != null)
            {
                SkipIndicator.Visibility = config.ShowSkipIndicator ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Apply background opacity
            if (BackgroundImage != null)
            {
                BackgroundImage.Opacity = config.BackgroundOpacity;
            }
            
            // Apply optimization settings (with safe defaults for old config files)
            try
            {
                _parallaxEnabled = config.EnableParallax;
                
                // Get parallax intensity and cursor sensitivity with safe defaults
                double parallaxIntensity = config.ParallaxIntensity > 0 ? config.ParallaxIntensity : 1.0;
                double cursorSensitivity = config.CursorMovementSensitivity > 0 ? config.CursorMovementSensitivity : 1.0;
                
                // Update parallax speeds based on intensity and cursor sensitivity
                double parallaxMultiplier = parallaxIntensity * cursorSensitivity;
                BackgroundParallaxSpeed = 0.15 * parallaxMultiplier;
                CharacterParallaxSpeed = 0.5 * parallaxMultiplier;
                MaxParallaxOffset = 100.0 * parallaxIntensity;
                
                // Update parallax update interval with safe default
                ParallaxUpdateInterval = config.ParallaxUpdateRate > 0 ? config.ParallaxUpdateRate : 16;
                if (_parallaxUpdateTimer != null)
                {
                    _parallaxUpdateTimer.Interval = TimeSpan.FromMilliseconds(ParallaxUpdateInterval);
                }
                
                // Disable parallax if setting is off
                if (!_parallaxEnabled && _parallaxUpdateTimer != null)
                {
                    _parallaxUpdateTimer.Stop();
                    // Reset parallax transforms to center
                    if (_backgroundParallaxTransform != null)
                    {
                        _backgroundParallaxTransform.X = 0;
                        _backgroundParallaxTransform.Y = 0;
                    }
                    if (_characterParallaxTransform != null)
                    {
                        _characterParallaxTransform.X = 0;
                        _characterParallaxTransform.Y = 0;
                    }
                }
                else if (_parallaxEnabled && _parallaxUpdateTimer != null && _parallaxUpdateTimer.IsEnabled == false)
                {
                    _parallaxUpdateTimer.Start();
                }
                
                // Apply FPS optimization settings
                if (SnowVideo != null)
                {
                    SnowVideo.Visibility = config.DisableBackgroundVideos ? Visibility.Collapsed : Visibility.Visible;
                }
                
                // Reduce texture quality
                if (config.ReduceTextureQuality)
                {
                    RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.LowQuality);
                }
                else
                {
                    RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
                }
                
                // Reduce UI effects
                if (config.ReduceUIEffects)
                {
                    // Disable drop shadows and effects on UI elements
                    if (SkipIndicator != null)
                    {
                        SkipIndicator.Effect = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // If optimization settings fail to load, use safe defaults
                LogToFile($"Error applying optimization settings: {ex.Message}");
                _parallaxEnabled = true;
                BackgroundParallaxSpeed = 0.15;
                CharacterParallaxSpeed = 0.5;
                MaxParallaxOffset = 100.0;
                ParallaxUpdateInterval = 16;
            }
            }
            catch (Exception ex)
            {
                // If ApplySettings fails completely, log and continue
                LogToFile($"Error in ApplySettings: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void LoadGameMusic()
        {
            try
            {
                // Check if we're in Scene1 and load autumn_ambient.mp3
                var currentScene = _storyService.GetCurrentScene();
                if (currentScene != null && currentScene.SceneId == "Scene1")
                {
                    string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Music", "autumn_ambient.mp3");
                    if (File.Exists(musicPath))
                    {
                        var config = _configService.GetConfig();
                        double targetVolume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
                        
                        GameBackgroundMusic.Source = new Uri(musicPath, UriKind.Absolute);
                        GameBackgroundMusic.Volume = 0; // Start at 0 for fade in
                        GameBackgroundMusic.Play();
                        LogToFile($"Game background music loaded: {musicPath} with target volume {targetVolume}");
                        
                        // Only fade in if target volume is greater than 0
                        if (targetVolume > 0)
                        {
                            // Fade in music slowly
                            FadeInGameMusic();
                        }
                        else
                        {
                            // If volume is 0, keep it at 0
                            GameBackgroundMusic.Volume = 0;
                        }
                    }
                    else
                    {
                        LogToFile($"Game background music file not found: {musicPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error loading game background music: {ex.Message}");
            }
        }

        private void FadeInGameMusic()
        {
            if (GameBackgroundMusic.Source == null) return;
            
            var config = _configService.GetConfig();
            double targetVolume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
            
            // If target volume is 0, don't fade in
            if (targetVolume <= 0)
            {
                GameBackgroundMusic.Volume = 0;
                return;
            }
            
            double fadeDuration = 2.0; // 2 seconds fade in
            int steps = 40; // Number of steps for smooth fade
            double stepInterval = fadeDuration / steps;
            double volumeStep = targetVolume / steps;

            var musicFadeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(stepInterval)
            };

            int currentStep = 0;
            musicFadeTimer.Tick += (s, e) =>
            {
                currentStep++;
                double newVolume = Math.Min(targetVolume, volumeStep * currentStep);
                GameBackgroundMusic.Volume = newVolume;

                if (currentStep >= steps || newVolume >= targetVolume)
                {
                    musicFadeTimer.Stop();
                    GameBackgroundMusic.Volume = targetVolume;
                }
            };

            musicFadeTimer.Start();
        }

        private void GameBackgroundMusic_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Loop the game background music
            if (GameBackgroundMusic.Source != null)
            {
                GameBackgroundMusic.Position = TimeSpan.Zero;
                GameBackgroundMusic.Play();
            }
        }


        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            // Type dialogue text
            if (_currentCharIndex < _currentFullText.Length)
            {
                DialogueText.Text = _currentFullText.Substring(0, _currentCharIndex + 1);
                _currentCharIndex++;
            }
            else
            {
                // Done typing
                _typingTimer.Stop();
                _isTyping = false;
                
                // Check if this dialogue has choices
                var dialogue = _storyService.GetDialogue(_currentDialogueIndex);
                if (dialogue != null && dialogue.HasChoices)
                {
                    ShowChoices(dialogue.Choices);
                }
            }
        }

        private void TimeBlinkTimer_Tick(object? sender, EventArgs e)
        {
            // Toggle colon visibility for analogue watch effect
            TimeColon.Visibility = TimeColon.Visibility == Visibility.Visible 
                ? Visibility.Hidden 
                : Visibility.Visible;
        }

        private void LoadDialogue(int index)
        {
            LogToFile($"=== LoadDialogue START: index={index} ===");
            
            // Update current dialogue index immediately
            int previousIndex = _currentDialogueIndex;
            _currentDialogueIndex = index;
            LogToFile($"Updated _currentDialogueIndex from {previousIndex} to {index}");
            
            var dialogue = _storyService.GetDialogue(index);
            if (dialogue == null)
            {
                LogToFile($"LoadDialogue: Dialogue at index {index} is null, showing end of story");
                // End of story or invalid index
                ShowEndOfStory();
                return;
            }
            
            string textPreview = dialogue.Text != null && dialogue.Text.Length > 50 ? dialogue.Text.Substring(0, 50) + "..." : (dialogue.Text ?? "");
            LogToFile($"LoadDialogue: Dialogue loaded - CharacterName='{dialogue.CharacterName}', Text='{textPreview}', SpeakingCharacterSpot={dialogue.SpeakingCharacterSpot}, CharacterSlots.Count={dialogue.CharacterSlots?.Count ?? 0}");
            
            // Check if any characters are entering (for logging purposes)
            if (dialogue.CharacterSlots != null && dialogue.CharacterSlots.Count > 0)
            {
                foreach (var characterSlot in dialogue.CharacterSlots)
                {
                    string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterSlot.CharacterImage);
                    
                    // Check if this is a new character
                    if (!_previousSpotCharacters.Values.Contains(characterPath))
                    {
                        LogToFile($"New character detected entering: {characterPath}");
                    }
                }
            }

            // Handle per-dialogue background image changes
            if (!string.IsNullOrEmpty(dialogue.BackgroundImage))
            {
                string backgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dialogue.BackgroundImage);
                if (File.Exists(backgroundPath))
                {
                    try
                    {
                        BackgroundImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(backgroundPath, UriKind.Absolute));
                        BackgroundImage.Visibility = Visibility.Visible;
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error loading dialogue background image: {ex.Message}");
                    }
                }
            }
            
            // Handle character name (can be empty for text-only dialogues)
            if (!string.IsNullOrEmpty(dialogue.CharacterName))
            {
                CharacterNameText.Text = dialogue.CharacterName.ToUpper();
                CharacterNameText.Visibility = Visibility.Visible;
            }
            else
            {
                CharacterNameText.Text = "";
                CharacterNameText.Visibility = Visibility.Collapsed;
            }
            
            // Show status for Agata - check both translated name and English for compatibility
            string agataName = _translationService.GetTranslation("Scene1_Character_Agata");
            if (!string.IsNullOrEmpty(dialogue.CharacterName) && 
                (dialogue.CharacterName.Contains(agataName, StringComparison.OrdinalIgnoreCase) ||
                dialogue.CharacterName.Contains("Agata", StringComparison.OrdinalIgnoreCase)))
            {
                var gameState = _storyService.GetGameState();
                int sympathy = gameState.GetVariable("agataSympathy", 0);
                int obedience = gameState.GetVariable("agataObedience", 0);
                
                string sympathyText = _translationService.GetTranslation("GameScene_Sympathy");
                string obedienceText = _translationService.GetTranslation("GameScene_Obedience");
                CharacterStatusText.Text = $"- {sympathyText}, {obedienceText}";
                CharacterStatusText.Visibility = Visibility.Visible;
            }
            else
            {
                CharacterStatusText.Visibility = Visibility.Collapsed;
            }
            
            _currentFullText = dialogue.Text ?? "";
            
            _currentCharIndex = 0;
            _isTyping = true;

            DialogueText.Text = "";
            
            // Check if we're using the new multiple character system (CharacterSlots)
            if (dialogue.CharacterSlots != null && dialogue.CharacterSlots.Count > 0)
            {
                // If deferring character positioning (window not ready yet), get dimensions from window
                // This ensures we use proper dimensions when window is fully laid out
                double windowWidth = _deferCharacterPositioning ? this.ActualWidth : _camera.GetWindowWidth();
                double charWidth = _deferCharacterPositioning ? this.ActualWidth * CharacterWidthPercentage : GetCharacterWidth();
                
                // If window isn't ready yet, use screen width as fallback but log a warning
                if (windowWidth <= 0)
                {
                    windowWidth = SystemParameters.PrimaryScreenWidth;
                    charWidth = windowWidth * CharacterWidthPercentage;
                    LogToFile($"LoadDialogue: Window not ready, using PrimaryScreenWidth={windowWidth}");
                }
                
                // Build a set of current character images and their new spots
                var currentCharacterSpots = new Dictionary<string, int>();
                foreach (var characterSlot in dialogue.CharacterSlots)
                {
                    string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterSlot.CharacterImage);
                    currentCharacterSpots[characterPath] = characterSlot.Spot;
                }
                
                // Categorize what's happening to each previous character
                var slotsExiting = new HashSet<int>();
                var charactersMoving = new Dictionary<string, (int oldSpot, int newSpot, string facing)>();
                
                // Check previous characters for exits and movements
                foreach (var kvp in _previousSpotCharacters)
                {
                    int oldSpot = kvp.Key;
                    string? characterPath = kvp.Value;
                    
                    if (string.IsNullOrEmpty(characterPath)) continue;
                    
                    if (!currentCharacterSpots.ContainsKey(characterPath))
                    {
                        // Character is exiting
                        slotsExiting.Add(oldSpot);
                    }
                    else if (currentCharacterSpots[characterPath] != oldSpot)
                    {
                        // Character is moving to a different spot
                        // Get the facing direction from the new dialogue
                        var newCharSlot = dialogue.CharacterSlots.FirstOrDefault(cs => 
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cs.CharacterImage) == characterPath);
                        string facing = newCharSlot?.Facing ?? "Right";
                        charactersMoving[characterPath] = (oldSpot, currentCharacterSpots[characterPath], facing);
                    }
                }
                
                // Animate exits for characters that are leaving
                // Find the actual slot that contains this character (may not be spot-1 after movements)
                foreach (int exitSpot in slotsExiting)
                {
                    // Find which slot actually has this character (by checking the spot tracking in the tuple)
                    int? foundSlotIndex = null;
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.spot == exitSpot && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            foundSlotIndex = slotKvp.Key;
                            break;
                        }
                    }
                    
                    // Fallback to default slot index if not found
                    int slotIndex = foundSlotIndex ?? (exitSpot - 1);
                    
                    if (slotIndex >= 0 && slotIndex < _characterSlots.Count)
                    {
                        var slot = _characterSlots[slotIndex];
                        if (slot.border.Visibility == Visibility.Visible)
                        {
                            LogToFile($"Character at spot {exitSpot} (slot {slotIndex}) is exiting to the right");
                            AnimateCharacterExit(slot.border);
                        }
                    }
                }
                
                // Determine which slot INDICES are in use by current dialogue or animations
                var slotIndicesInUse = new HashSet<int>();
                
                // Slots used by characters that are staying or entering (find by spot tracking)
                foreach (var characterSlot in dialogue.CharacterSlots)
                {
                    string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterSlot.CharacterImage);
                    
                    // If this character was in previous dialogue, find which slot they're in
                    bool foundInPrevious = false;
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.imagePath == characterPath && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            slotIndicesInUse.Add(slotKvp.Key);
                            foundInPrevious = true;
                            break;
                        }
                    }
                    
                    // If new character, they'll use the default slot for their spot
                    if (!foundInPrevious)
                    {
                        slotIndicesInUse.Add(characterSlot.Spot - 1);
                    }
                }
                
                // Slots with exiting characters (animating out)
                foreach (int exitSpot in slotsExiting)
                {
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.spot == exitSpot && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            slotIndicesInUse.Add(slotKvp.Key);
                            break;
                        }
                    }
                }
                
                // Slots that are source of movement (will be animating)
                foreach (var moving in charactersMoving.Values)
                {
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.spot == moving.oldSpot && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            slotIndicesInUse.Add(slotKvp.Key);
                            break;
                        }
                    }
                }
                
                // Hide slots that are not in use
                for (int slotIndex = 0; slotIndex < _characterSlots.Count; slotIndex++)
                {
                    if (!slotIndicesInUse.Contains(slotIndex))
                    {
                        _characterSlots[slotIndex].border.Visibility = Visibility.Collapsed;
                    }
                }
                
                // Process MOVING characters - use OLD slot for animation, keep in old slot at new position
                foreach (var movingKvp in charactersMoving)
                {
                    string characterPath = movingKvp.Key;
                    int oldSpot = movingKvp.Value.oldSpot;
                    int newSpot = movingKvp.Value.newSpot;
                    string facing = movingKvp.Value.facing;
                    
                    // Find which slot actually has this character (may not be oldSpot-1 after previous movements)
                    int? foundSlotIndex = null;
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.spot == oldSpot && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            foundSlotIndex = slotKvp.Key;
                            break;
                        }
                    }
                    
                    int oldSlotIndex = foundSlotIndex ?? (oldSpot - 1);
                    
                    if (oldSlotIndex < 0 || oldSlotIndex >= _characterSlots.Count) continue;
                    
                    var oldUiSlot = _characterSlots[oldSlotIndex];
                    
                    // Calculate target position (where the new spot is)
                    double targetPosition = SpotPositions[newSpot - 1] * windowWidth - (charWidth / 2);
                    targetPosition = Math.Max(0, Math.Min(targetPosition, windowWidth - charWidth));
                    
                    LogToFile($"ANIMATION START: CharacterPosition - from spot={oldSpot} to spot={newSpot}, slotIndex={oldSlotIndex}, targetPosition={targetPosition}, facing={facing}, imagePath={characterPath}");
                    
                    // Update facing direction on the old slot (the one we're animating)
                    bool facingRight = facing.Equals("Right", StringComparison.OrdinalIgnoreCase);
                    oldUiSlot.scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    oldUiSlot.scale.ScaleX = facingRight ? -1 : 1; // REVERSED logic
                    
                    // Animate the OLD slot to the new position
                    // Character stays in the old slot (at new position) - no swapping needed
                    AnimateCharacterPosition(oldUiSlot.border, targetPosition);
                    
                    // Update tracking - this slot now represents the character at newSpot
                    _characterSlots[oldSlotIndex] = (oldUiSlot.border, oldUiSlot.image, oldUiSlot.scale, characterPath, newSpot);
                }
                
                // Load NON-MOVING characters into their slots
                foreach (var characterSlot in dialogue.CharacterSlots)
                {
                    string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterSlot.CharacterImage);
                    
                    // Skip if this character is moving (handled above)
                    if (charactersMoving.ContainsKey(characterPath)) continue;
                    
                    // Find which slot this character is in (may not be spot-1 after previous movements)
                    int? foundSlotIndex = null;
                    foreach (var slotKvp in _characterSlots)
                    {
                        if (slotKvp.Value.imagePath == characterPath && slotKvp.Value.border.Visibility == Visibility.Visible)
                        {
                            foundSlotIndex = slotKvp.Key;
                            break;
                        }
                    }
                    
                    // Use found slot or default to spot-1 for new characters
                    int slotIndex = foundSlotIndex ?? (characterSlot.Spot - 1);
                    if (slotIndex < 0 || slotIndex >= _characterSlots.Count) continue;
                    
                    var uiSlot = _characterSlots[slotIndex];
                    
                    if (File.Exists(characterPath))
                    {
                        try
                        {
                            var processedImage = ImageHelper.LoadImageWithoutArtifacts(characterPath);
                            if (processedImage != null)
                            {
                                uiSlot.image.Source = processedImage;
                            }
                            else
                            {
                                uiSlot.image.Source = new BitmapImage(new Uri(characterPath, UriKind.Absolute));
                            }
                            
                            // Update facing direction
                            bool facingRight = characterSlot.Facing?.Equals("Right", StringComparison.OrdinalIgnoreCase) ?? true;
                            uiSlot.scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                            uiSlot.scale.ScaleX = facingRight ? -1 : 1; // REVERSED logic
                            
                            // Calculate target position
                            double targetPosition = SpotPositions[characterSlot.Spot - 1] * windowWidth - (charWidth / 2);
                            targetPosition = Math.Max(0, Math.Min(targetPosition, windowWidth - charWidth));
                            Canvas.SetBottom(uiSlot.border, CharacterBottomOffset);
                            
                            bool isNewCharacter = !_previousSpotCharacters.Values.Contains(characterPath);
                            
                            if (isNewCharacter)
                            {
                                // Character is appearing for the first time
                                bool fromLeft = characterSlot.Spot == 1;
                                LogToFile($"ANIMATION START: CharacterEntrance - spot={characterSlot.Spot}, slotIndex={slotIndex}, fromLeft={fromLeft}, targetPosition={targetPosition}, imagePath={characterPath}");
                                AnimateCharacterEntrance(uiSlot.border, targetPosition, fromLeft);
                            }
                            else
                            {
                                // Character stays at same spot, no animation needed
                                LogToFile($"Character staying at spot - spot={characterSlot.Spot}, slotIndex={slotIndex}, position={targetPosition}, imagePath={characterPath}");
                                Canvas.SetLeft(uiSlot.border, targetPosition);
                            }
                            
                            uiSlot.border.Visibility = Visibility.Visible;
                            LogToFile($"Character visibility set - spot={characterSlot.Spot}, slotIndex={slotIndex}, visible=true");
                            
                            // Update tracking
                            _characterSlots[slotIndex] = (uiSlot.border, uiSlot.image, uiSlot.scale, characterPath, characterSlot.Spot);
                            LogToFile($"Character slot tracking updated - slotIndex={slotIndex}, spot={characterSlot.Spot}, imagePath={characterPath}");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error loading character slot {slotIndex} image: {ex.Message}");
                            uiSlot.border.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        LogToFile($"Character slot {slotIndex} image not found: {characterPath}");
                        uiSlot.border.Visibility = Visibility.Collapsed;
                    }
                }
                
                // Update previous spot tracking for next dialogue
                _previousSpotCharacters.Clear();
                foreach (var characterSlot in dialogue.CharacterSlots)
                {
                    string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterSlot.CharacterImage);
                    _previousSpotCharacters[characterSlot.Spot] = characterPath;
                }
            }
            else
            {
                // Fall back to old system (backward compatibility)
                // Hide all additional slots
                for (int i = 2; i < _characterSlots.Count; i++)
                {
                    _characterSlots[i].border.Visibility = Visibility.Collapsed;
                }
                
                // Load left character image if specified
                if (!string.IsNullOrEmpty(dialogue.CharacterImage))
            {
                string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dialogue.CharacterImage);
                
                // Only reload if the image path changed (avoid unnecessary processing)
                if (characterPath != _currentLeftImagePath)
                {
                    _currentLeftImagePath = characterPath;
                    
                    if (File.Exists(characterPath))
                    {
                        try
                        {
                            // Use ImageHelper to remove edge artifacts (now cached)
                            var processedImage = ImageHelper.LoadImageWithoutArtifacts(characterPath);
                            if (processedImage != null)
                            {
                                CharacterImageLeft.Source = processedImage;
                            }
                            else
                            {
                                // Fallback to regular loading if processing fails
                                CharacterImageLeft.Source = new BitmapImage(new Uri(characterPath, UriKind.Absolute));
                            }
                            CharacterImageLeftBorder.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error loading left character image: {ex.Message}");
                            CharacterImageLeftBorder.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        LogToFile($"Left character image not found: {characterPath}");
                        CharacterImageLeftBorder.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    // Same image, just ensure visibility
                    CharacterImageLeftBorder.Visibility = Visibility.Visible;
                }
                }
                else
                {
                    CharacterImageLeftBorder.Visibility = Visibility.Collapsed;
                    _currentLeftImagePath = null;
                }

                // Load right character image if specified
                if (!string.IsNullOrEmpty(dialogue.CharacterImageRight))
            {
                string characterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dialogue.CharacterImageRight);
                
                // Only reload if the image path changed (avoid unnecessary processing)
                if (characterPath != _currentRightImagePath)
                {
                    _currentRightImagePath = characterPath;
                    
                    if (File.Exists(characterPath))
                    {
                        try
                        {
                            // Use ImageHelper to remove edge artifacts (now cached)
                            var processedImage = ImageHelper.LoadImageWithoutArtifacts(characterPath);
                            if (processedImage != null)
                            {
                                CharacterImageRight.Source = processedImage;
                            }
                            else
                            {
                                // Fallback to regular loading if processing fails
                                CharacterImageRight.Source = new BitmapImage(new Uri(characterPath, UriKind.Absolute));
                            }
                            CharacterImageRightBorder.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error loading right character image: {ex.Message}");
                            CharacterImageRightBorder.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        LogToFile($"Right character image not found: {characterPath}");
                        CharacterImageRightBorder.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    // Same image, just ensure visibility
                    CharacterImageRightBorder.Visibility = Visibility.Visible;
                }
            }
                else
                {
                    CharacterImageRightBorder.Visibility = Visibility.Collapsed;
                    _currentRightImagePath = null;
                }
            }

            // Position characters based on dialogue settings
            LogToFile($"Calling PositionCharacters - dialogue.CharacterSlots.Count={dialogue.CharacterSlots?.Count ?? 0}");
            PositionCharacters(dialogue);
            LogToFile($"PositionCharacters completed - visible characters: {string.Join(", ", _characterSlots.Where(kvp => kvp.Value.border.Visibility == Visibility.Visible).Select(kvp => $"slot{kvp.Key}(spot={kvp.Value.spot}, image={Path.GetFileName(kvp.Value.imagePath ?? "")})"))}");

            // Start typing animation
            LogToFile($"Starting typing animation timer");
            _typingTimer.Start();
            LogToFile($"=== LoadDialogue END: index={index} ===");
        }

        private void PositionCharacters(DialogueLine dialogue)
        {
            // If using CharacterSlots, reposition them based on current window dimensions
            if (dialogue.CharacterSlots != null && dialogue.CharacterSlots.Count > 0)
            {
                // Actually reposition the characters
                RepositionAllCharacters(dialogue);
                
                var characterSlotSpots = dialogue.CharacterSlots.Select(s => s.Spot).ToList();
                
                // Update camera after layout is complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (characterSlotSpots.Count > 0)
                    {
                        if (dialogue.CameraZoom.HasValue)
                        {
                            _camera.SetCharacterSpotsWithoutAdjustment(characterSlotSpots);
                            _camera.ApplyZoom(dialogue.CameraZoom);
                        }
                        else
                        {
                            _camera.UpdateCharacterSpots(characterSlotSpots);
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
                return;
            }
            
            // Track previous spots BEFORE updating to detect changes
            int? previousLeftSpot = _currentLeftSpot;
            int? previousRightSpot = _currentRightSpot;
            
            // Get container width for calculations - use camera's window width method
            double windowWidth = _camera.GetWindowWidth();
            
            // Position left character (Malgorzata)
            if (dialogue.CharacterSpot.HasValue && dialogue.CharacterSpot.Value >= 1 && dialogue.CharacterSpot.Value <= 6)
            {
                int spot = dialogue.CharacterSpot.Value;
                double charWidth = GetCharacterWidth();
                double position = SpotPositions[spot - 1] * windowWidth - (charWidth / 2); // Center character
                // Ensure character stays on screen - clamp to visible area
                position = Math.Max(0, Math.Min(position, windowWidth - charWidth));
                LogToFile($"Positioning Malgorzata at spot {spot}, windowWidth={windowWidth}, calculated position={position}, clamped position={position}");
                
                // Set bottom position - characters positioned a bit lower
                Canvas.SetBottom(CharacterImageLeftBorder, CharacterBottomOffset);
                
                // Animate movement if spot changed or character is appearing
                bool isNewCharacter = !CharacterImageLeftBorder.IsVisible;
                if (_currentLeftSpot != spot || isNewCharacter)
                {
                    if (isNewCharacter)
                    {
                        // Character is appearing - animate from left if spot 1, otherwise from right
                        bool fromLeft = spot == 1;
                        AnimateCharacterEntrance(CharacterImageLeftBorder, position, fromLeft);
                    }
                    else
                    {
                        AnimateCharacterPosition(CharacterImageLeftBorder, position);
                    }
                    _currentLeftSpot = spot;
                }
                else
                {
                    Canvas.SetLeft(CharacterImageLeftBorder, position);
                }
                
                // Set facing direction (Right = 1, Left = -1)
                // Note: ScaleX = 1 means facing right (original), ScaleX = -1 means facing left (flipped)
                // REVERSED: If dialogue says "Right", we flip to face left, and vice versa
                bool facingRight = dialogue.CharacterFacing?.Equals("Right", StringComparison.OrdinalIgnoreCase) ?? true;
                
                // Ensure the scale animation doesn't interfere
                CharacterImageLeftScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                CharacterImageLeftScale.ScaleX = facingRight ? -1 : 1; // REVERSED
            }
            else if (!string.IsNullOrEmpty(dialogue.CharacterImage))
            {
                // Default position (spot 5 - right side) if not specified
                double charWidth = GetCharacterWidth();
                double position = SpotPositions[4] * windowWidth - (charWidth / 2);
                Canvas.SetBottom(CharacterImageLeftBorder, CharacterBottomOffset);
                if (!_currentLeftSpot.HasValue)
                {
                    AnimateCharacterEntrance(CharacterImageLeftBorder, position, false);
                    _currentLeftSpot = 5;
                }
                else if (_currentLeftSpot != 5)
                {
                    AnimateCharacterPosition(CharacterImageLeftBorder, position);
                    _currentLeftSpot = 5;
                }
            }

            // Position right character (Agata)
            if (dialogue.CharacterSpotRight.HasValue && dialogue.CharacterSpotRight.Value >= 1 && dialogue.CharacterSpotRight.Value <= 6)
            {
                int spot = dialogue.CharacterSpotRight.Value;
                double charWidth = GetCharacterWidth();
                double position = SpotPositions[spot - 1] * windowWidth - (charWidth / 2); // Center character
                // Ensure character stays on screen - clamp to visible area
                position = Math.Max(0, Math.Min(position, windowWidth - charWidth));
                LogToFile($"Positioning Agata at spot {spot}, windowWidth={windowWidth}, calculated position={position}, clamped position={position}");
                
                // Set bottom position - characters positioned a bit lower
                Canvas.SetBottom(CharacterImageRightBorder, CharacterBottomOffset);
                
                // Animate movement if spot changed or character is appearing
                bool isNewCharacter = !CharacterImageRightBorder.IsVisible;
                if (_currentRightSpot != spot || isNewCharacter)
                {
                    if (isNewCharacter)
                    {
                        // Character is appearing - animate from left if spot 1, otherwise from right
                        bool fromLeft = spot == 1;
                        AnimateCharacterEntrance(CharacterImageRightBorder, position, fromLeft);
                    }
                    else
                    {
                        AnimateCharacterPosition(CharacterImageRightBorder, position);
                    }
                    _currentRightSpot = spot;
                }
                else
                {
                    Canvas.SetLeft(CharacterImageRightBorder, position);
                }
                
                // Set facing direction (Right = 1, Left = -1)
                // Note: ScaleX = 1 means facing right (original), ScaleX = -1 means facing left (flipped)
                // REVERSED: If dialogue says "Left", we flip to face right, and vice versa
                bool facingRight = dialogue.CharacterFacingRight?.Equals("Right", StringComparison.OrdinalIgnoreCase) ?? false;
                
                // Ensure the scale animation doesn't interfere
                CharacterImageRightScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                CharacterImageRightScale.ScaleX = facingRight ? -1 : 1; // REVERSED
            }
            else if (!string.IsNullOrEmpty(dialogue.CharacterImageRight))
            {
                // Default position (spot 5 - right side) if not specified
                double charWidth = GetCharacterWidth();
                double position = SpotPositions[4] * windowWidth - (charWidth / 2);
                Canvas.SetBottom(CharacterImageRightBorder, CharacterBottomOffset);
                if (!_currentRightSpot.HasValue)
                {
                    AnimateCharacterEntrance(CharacterImageRightBorder, position, false);
                    _currentRightSpot = 5;
                    // Default facing for right character
                    CharacterImageRightScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    CharacterImageRightScale.ScaleX = 1; // REVERSED - default facing right
                }
                else if (_currentRightSpot != 5)
                {
                    AnimateCharacterPosition(CharacterImageRightBorder, position);
                    _currentRightSpot = 5;
                    CharacterImageRightScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    CharacterImageRightScale.ScaleX = 1; // REVERSED
                }
            }

            // Check if character spots actually changed
            bool spotsChanged = (previousLeftSpot != _currentLeftSpot) || (previousRightSpot != _currentRightSpot);
            
            // Collect all active character spots
            var activeSpots = new List<int>();
            if (_currentLeftSpot.HasValue)
            {
                activeSpots.Add(_currentLeftSpot.Value);
            }
            if (_currentRightSpot.HasValue)
            {
                activeSpots.Add(_currentRightSpot.Value);
            }
            
            // Update camera after layout is complete to ensure correct window width calculations
            if (spotsChanged)
            {
                // Spots changed - update camera with new positions
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Only update camera if we have active spots
                    if (activeSpots.Count > 0)
                    {
                        // If manual zoom is specified, apply it directly without automatic adjustment
                        if (dialogue.CameraZoom.HasValue)
                        {
                            // Update spots list in camera but skip automatic adjustment
                            // We'll apply manual zoom instead
                            _camera.SetCharacterSpotsWithoutAdjustment(activeSpots);
                            _camera.ApplyZoom(dialogue.CameraZoom);
                        }
                        else
                        {
                            // No manual zoom - use automatic adjustment
                            _camera.UpdateCharacterSpots(activeSpots);
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            else if (dialogue.CameraZoom.HasValue)
            {
                // Spots haven't changed, but manual zoom is specified
                // Only apply if zoom value is different from current
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    double currentZoom = _camera.CurrentZoom;
                    if (Math.Abs(currentZoom - dialogue.CameraZoom.Value) > 0.01)
                    {
                        // Zoom changed, apply it (but don't update spots since they haven't changed)
                        _camera.ApplyZoom(dialogue.CameraZoom);
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            // If neither spots nor zoom changed, don't update camera at all
        }

        private void InitializeCharacterSlots()
        {
            // Map slot indices to UI elements
            // Slot 0 = Left (CharacterImageLeftBorder)
            // Slot 1 = Right (CharacterImageRightBorder)
            // Slot 2-5 = Additional slots (CharacterImageSlot3-6)
            _characterSlots[0] = (CharacterImageLeftBorder, CharacterImageLeft, CharacterImageLeftScale, null, null);
            _characterSlots[1] = (CharacterImageRightBorder, CharacterImageRight, CharacterImageRightScale, null, null);
            _characterSlots[2] = (CharacterImageSlot3Border, CharacterImageSlot3, CharacterImageSlot3Scale, null, null);
            _characterSlots[3] = (CharacterImageSlot4Border, CharacterImageSlot4, CharacterImageSlot4Scale, null, null);
            _characterSlots[4] = (CharacterImageSlot5Border, CharacterImageSlot5, CharacterImageSlot5Scale, null, null);
            _characterSlots[5] = (CharacterImageSlot6Border, CharacterImageSlot6, CharacterImageSlot6Scale, null, null);
        }

        private void AnimateCharacterPosition(Border characterBorder, double targetX, Action? onComplete = null)
        {
            try
            {
                var config = _configService.GetConfig();
                
                // If moving sprites are disabled, just set position directly
                if (!config.EnableMovingSprites)
                {
                    Canvas.SetLeft(characterBorder, targetX);
                    onComplete?.Invoke();
                    return;
                }
            }
            catch (Exception ex)
            {
                // If config access fails, just set position directly
                LogToFile($"Error in AnimateCharacterPosition: {ex.Message}");
                Canvas.SetLeft(characterBorder, targetX);
                onComplete?.Invoke();
                return;
            }
            
            double currentX = Canvas.GetLeft(characterBorder);
            if (double.IsNaN(currentX)) currentX = 0;

            var animation = new DoubleAnimation
            {
                From = currentX,
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            if (onComplete != null)
            {
                animation.Completed += (s, e) =>
                {
                    LogToFile($"ANIMATION COMPLETE: CharacterPosition - targetX={targetX}");
                    onComplete();
                };
            }
            else
            {
                animation.Completed += (s, e) => LogToFile($"ANIMATION COMPLETE: CharacterPosition - targetX={targetX}");
            }

            LogToFile($"ANIMATION BEGIN: CharacterPosition - from={currentX}, to={targetX}, duration=500ms");
            characterBorder.BeginAnimation(Canvas.LeftProperty, animation);
        }

        /// <summary>
        /// Animate character entrance from the left or right side of the screen
        /// </summary>
        private void AnimateCharacterEntrance(Border characterBorder, double targetX, bool fromLeft = false)
        {
            try
            {
                var config = _configService.GetConfig();
                
                // If moving sprites are disabled, just set position directly
                if (!config.EnableMovingSprites)
                {
                    Canvas.SetLeft(characterBorder, targetX);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error in AnimateCharacterEntrance: {ex.Message}");
                Canvas.SetLeft(characterBorder, targetX);
                return;
            }
            
            double windowWidth = _camera.GetWindowWidth();
            double charWidth = GetCharacterWidth();
            double startX;
            
            if (fromLeft)
            {
                // Start from off-screen left
                startX = -charWidth;
            }
            else
            {
                // Start from off-screen right (window width + character width)
                startX = windowWidth + charWidth;
            }
            
            // Set initial position
            Canvas.SetLeft(characterBorder, startX);
            
            // Animate to target position
            var animation = new DoubleAnimation
            {
                From = startX,
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) => LogToFile($"ANIMATION COMPLETE: CharacterEntrance - targetX={targetX}, fromLeft={fromLeft}");
            LogToFile($"ANIMATION BEGIN: CharacterEntrance - from={startX}, to={targetX}, fromLeft={fromLeft}, duration=600ms");
            characterBorder.BeginAnimation(Canvas.LeftProperty, animation);
        }

        /// <summary>
        /// Animate character exit to the right side of the screen
        /// </summary>
        private void AnimateCharacterExit(Border characterBorder, Action? onComplete = null)
        {
            try
            {
                var config = _configService.GetConfig();
                
                // If moving sprites are disabled, just hide immediately
                if (!config.EnableMovingSprites)
                {
                    characterBorder.Visibility = Visibility.Collapsed;
                    onComplete?.Invoke();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error in AnimateCharacterExit: {ex.Message}");
                characterBorder.Visibility = Visibility.Collapsed;
                onComplete?.Invoke();
                return;
            }
            
            double currentX = Canvas.GetLeft(characterBorder);
            if (double.IsNaN(currentX)) currentX = 0;
            
            // Exit to off-screen right
            double windowWidth = _camera.GetWindowWidth();
            double charWidth = GetCharacterWidth();
            double targetX = windowWidth + charWidth;
            
            var animation = new DoubleAnimation
            {
                From = currentX,
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            animation.Completed += (s, e) =>
            {
                LogToFile($"ANIMATION COMPLETE: CharacterExit - targetX={targetX}");
                characterBorder.Visibility = Visibility.Collapsed;
                onComplete?.Invoke();
            };

            LogToFile($"ANIMATION BEGIN: CharacterExit - from={currentX}, to={targetX}, duration=500ms");
            characterBorder.BeginAnimation(Canvas.LeftProperty, animation);
        }

        private void ShowEndOfStory()
        {
            CharacterNameText.Text = _translationService.GetTranslation("GameScene_TheEnd");
            DialogueText.Text = _translationService.GetTranslation("GameScene_ThankYou");
            _isTyping = false;
            _typingTimer.Stop();
        }

        private void AdvanceDialogue()
        {
            // Don't advance if choices are showing
            if (ChoiceMenuControl.Visibility == Visibility.Visible)
            {
                return;
            }

            if (_isTyping)
            {
                // Skip typing animation
                _typingTimer.Stop();
                DialogueText.Text = _currentFullText;
                _currentCharIndex = _currentFullText.Length;
                _isTyping = false;
                
                // Check if this dialogue has choices after skipping
                var dialogue = _storyService.GetDialogue(_currentDialogueIndex);
                if (dialogue != null && dialogue.HasChoices)
                {
                    ShowChoices(dialogue.Choices);
                }
            }
            else
            {
                // Move to next dialogue
                _currentDialogueIndex++;
                if (_currentDialogueIndex >= _storyService.GetTotalDialogues())
                {
                    // End of current scene - try to advance to next scene
                    if (_storyService.TryAdvanceToNextScene())
                    {
                        // New scene loaded, reset dialogue index and reload background
                        _currentDialogueIndex = 0;
                        LoadSceneBackground();
                        UpdateLocationDisplay();
                        LoadDialogue(_currentDialogueIndex);
                    }
                    else
                    {
                        // No more scenes - end of story
                        ShowEndOfStory();
                    }
                }
                else
                {
                    LoadDialogue(_currentDialogueIndex);
                }
            }
        }

        private void ShowChoices(List<Choice> choices)
        {
            ChoiceMenuControl.ShowChoices(choices);
            ChoiceMenuControl.Visibility = Visibility.Visible;
        }

        private void OnChoiceSelected(Choice choice)
        {
            // Play click sound
            _soundService.PlayClickSound();
            
            // Hide choice menu
            ChoiceMenuControl.Hide();
            
            // Apply choice effects
            var gameState = _storyService.GetGameState();
            
            // Set flags
            foreach (var flag in choice.SetFlags)
            {
                gameState.SetFlag(flag.Key, flag.Value);
            }
            
            // Modify variables
            foreach (var variable in choice.ModifyVariables)
            {
                int currentValue = gameState.GetVariable(variable.Key, 0);
                gameState.SetVariable(variable.Key, currentValue + variable.Value);
            }
            
            // Handle scene jump or dialogue jump
            if (!string.IsNullOrEmpty(choice.NextSceneId))
            {
                // Jump to a different scene
                _storyService.LoadScene(choice.NextSceneId);
                _currentDialogueIndex = 0;
                LoadSceneBackground();
                UpdateLocationDisplay();
                LoadDialogue(_currentDialogueIndex);
            }
            else if (choice.JumpToDialogueIndex.HasValue)
            {
                // Jump to a specific dialogue in current scene
                _currentDialogueIndex = choice.JumpToDialogueIndex.Value;
                LoadDialogue(_currentDialogueIndex);
            }
            else
            {
                // Normal advance to next dialogue
                AdvanceDialogue();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                AdvanceDialogue();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ShowMainMenu();
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowSaveMenu();
            }
            else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowLoadMenu();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                AdvanceDialogue();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Only update target position, actual parallax update happens on timer
            _targetParallaxPosition = e.GetPosition(this);
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // Reset parallax to center when mouse leaves window (use cached center)
            _targetParallaxPosition = new Point(_windowCenterX, _windowCenterY);
        }

        private void InitializeParallax()
        {
            // Find the parallax transforms from the XAML
            _backgroundParallaxTransform = this.FindName("BackgroundParallaxTransform") as TranslateTransform;
            _characterParallaxTransform = this.FindName("CharacterParallaxTransform") as TranslateTransform;
            
            // Update cached window center values
            UpdateWindowCenterCache();
            
            // Initialize mouse position to center
            _lastMousePosition = new Point(_windowCenterX, _windowCenterY);
            _targetParallaxPosition = _lastMousePosition;
            
            // Setup throttled parallax update timer
            _parallaxUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ParallaxUpdateInterval)
            };
            _parallaxUpdateTimer.Tick += (s, e) => UpdateParallax(_targetParallaxPosition);
            _parallaxUpdateTimer.Start();
            
            if (_backgroundParallaxTransform == null || _characterParallaxTransform == null)
            {
                LogToFile("Warning: Parallax transforms not found during initialization");
            }
        }

        private void UpdateWindowCenterCache()
        {
            _windowCenterX = this.ActualWidth / 2.0;
            _windowCenterY = this.ActualHeight / 2.0;
            
            // Cache inverse values to avoid division in hot path
            _inverseCenterX = _windowCenterX > 0 ? 1.0 / _windowCenterX : 1.0;
            _inverseCenterY = _windowCenterY > 0 ? 1.0 / _windowCenterY : 1.0;
        }

        private void UpdateParallax(Point mousePosition)
        {
            if (!_parallaxEnabled)
                return;
                
            if (_backgroundParallaxTransform == null || _characterParallaxTransform == null)
                return;

            // Update cached center if window size changed (should be rare)
            if (Math.Abs(_windowCenterX - this.ActualWidth / 2.0) > 1.0 || 
                Math.Abs(_windowCenterY - this.ActualHeight / 2.0) > 1.0)
            {
                UpdateWindowCenterCache();
            }

            // Calculate offset from center (normalized to -1 to 1) using cached inverse
            double offsetX = (mousePosition.X - _windowCenterX) * _inverseCenterX;
            double offsetY = (mousePosition.Y - _windowCenterY) * _inverseCenterY;

            // Clamp offsets to prevent excessive movement (using Math.Clamp for .NET 8)
            offsetX = Math.Clamp(offsetX, -1.0, 1.0);
            offsetY = Math.Clamp(offsetY, -1.0, 1.0);

            // Pre-calculate common values
            double offsetXScaled = offsetX * MaxParallaxOffset;
            double offsetYScaled = offsetY * MaxParallaxOffset * VerticalParallaxReduction;

            // Calculate target positions
            double targetBackgroundX = offsetXScaled * BackgroundParallaxSpeed;
            double targetBackgroundY = offsetYScaled * BackgroundParallaxSpeed;
            double targetCharacterX = offsetXScaled * CharacterParallaxSpeed;
            double targetCharacterY = offsetYScaled * CharacterParallaxSpeed;

            // Get current positions
            double currentBgX = _backgroundParallaxTransform.X;
            double currentBgY = _backgroundParallaxTransform.Y;
            double currentCharX = _characterParallaxTransform.X;
            double currentCharY = _characterParallaxTransform.Y;

            // Calculate deltas
            double deltaBgX = targetBackgroundX - currentBgX;
            double deltaBgY = targetBackgroundY - currentBgY;
            double deltaCharX = targetCharacterX - currentCharX;
            double deltaCharY = targetCharacterY - currentCharY;

            // Early exit if changes are too small (performance optimization)
            double maxDelta = Math.Max(Math.Max(Math.Abs(deltaBgX), Math.Abs(deltaBgY)),
                                     Math.Max(Math.Abs(deltaCharX), Math.Abs(deltaCharY)));
            if (maxDelta < MinUpdateThreshold)
                return;

            // Interpolate with easing factor
            double newBgX = currentBgX + deltaBgX * EasingFactor;
            double newBgY = currentBgY + deltaBgY * EasingFactor;
            double newCharX = currentCharX + deltaCharX * EasingFactor;
            double newCharY = currentCharY + deltaCharY * EasingFactor;

            // Apply transforms
            _backgroundParallaxTransform.X = newBgX;
            _backgroundParallaxTransform.Y = newBgY;
            _characterParallaxTransform.X = newCharX;
            _characterParallaxTransform.Y = newCharY;

            _lastMousePosition = mousePosition;
        }

        private void SnowVideo_MediaEnded(object? sender, RoutedEventArgs e)
        {
            // Not used for AnimatedChromaKeyImage - animation loops automatically
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            ShowMainMenu();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            ShowSaveMenu();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            ShowLoadMenu();
        }


        private void ShowMainMenu()
        {
            // Auto-save before returning to menu
            _saveLoadService.AutoSave(_currentDialogueIndex, _storyService);
            
            var mainMenu = new MainWindow();
            mainMenu.Show();
            mainMenu.Activate();
            mainMenu.Focus();
            
            // Force video to load after window is shown and activated
            // Use a timer to ensure window is fully rendered - try multiple times
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            int attempts = 0;
            timer.Tick += (s, e) =>
            {
                attempts++;
                LogToFile($"Timer tick {attempts}: Attempting to load video...");
                Console.WriteLine($"Timer tick {attempts}: Attempting to load video...");
                mainMenu.LoadVideoIfNeeded();
                
                // Stop early if video is already loaded
                if (mainMenu.IsVideoLoaded())
                {
                    timer.Stop();
                    LogToFile($"Timer stopped early - video is loaded after {attempts} attempts");
                    Console.WriteLine($"Timer stopped early - video is loaded after {attempts} attempts");
                    return;
                }
                
                // Try up to 5 times
                if (attempts >= 5)
                {
                    timer.Stop();
                    LogToFile($"Timer stopped after {attempts} attempts");
                    Console.WriteLine($"Timer stopped after {attempts} attempts");
                }
            };
            timer.Start();
            
            // Close this window after a small delay to ensure MainWindow is fully activated
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Close();
            }), DispatcherPriority.Background);
        }

        private void ShowSaveMenu()
        {
            var saveMenu = new SaveLoadMenu(_currentDialogueIndex, _saveLoadService, false, _storyService);
            saveMenu.Owner = this;
            saveMenu.ShowDialog();
        }

        private void ShowLoadMenu()
        {
            var loadMenu = new SaveLoadMenu(_currentDialogueIndex, _saveLoadService, true, _storyService);
            loadMenu.Owner = this;
            loadMenu.ShowDialog();
            
            // If a game was loaded, the SaveLoadMenu will handle opening the new GameScene
            // and closing this one, so we don't need to do anything here
        }

        public void LoadFromIndex(int index)
        {
            _currentDialogueIndex = index;
            LoadDialogue(_currentDialogueIndex);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Stop time blink timer
            _timeBlinkTimer?.Stop();
            
            // Stop FPS counter
            if (_fpsTimer != null)
            {
                _fpsTimer.Stop();
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }
            
            // Stop parallax update timer
            _parallaxUpdateTimer?.Stop();
            
            // Auto-save when closing the game scene
            _saveLoadService.AutoSave(_currentDialogueIndex, _storyService);
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_translationService != null)
            {
                _translationService.LanguageChanged -= TranslationService_LanguageChanged;
            }
            
            // Stop all timers and cleanup
            _typingTimer?.Stop();
            _timeBlinkTimer?.Stop();
            _fpsTimer?.Stop();
            _parallaxUpdateTimer?.Stop();
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            GameBackgroundMusic?.Close();
            
            base.OnClosed(e);
            
            // If no other main windows are open, shut down the application
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

        private void ChapterTitlePanel_Loaded(object sender, RoutedEventArgs e)
        {
            // Measure the text width and set the line width to extend beyond it
            ChapterTitleText.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            ChapterTitleText.Arrange(new Rect(ChapterTitleText.DesiredSize));
            
            double textWidth = ChapterTitleText.ActualWidth;
            if (textWidth == 0)
            {
                // If not yet measured, use a default calculation
                textWidth = ChapterTitleText.Text.Length * 14; // Approximate width
            }
            
            // Set line width to extend 30 pixels on each side
            ChapterTitleLine.Width = textWidth + 60;
            ChapterTitleLine.Margin = new Thickness(-30, 8, -30, 0);
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
                string fpsLabel = _translationService.GetTranslation("GameScene_FPS");
                FPSCounter.Text = $"{fpsLabel}: {fps}";
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
        }

        private void TranslationService_LanguageChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyTranslations();
                // Update chapter title
                if (ChapterTitleText != null)
                {
                    ChapterTitleText.Text = _translationService.GetTranslation("Chapter1_Title");
                }
                // Update character status if visible
                var dialogue = _storyService.GetDialogue(_currentDialogueIndex);
                if (dialogue != null)
                {
                    // Check both English and translated name for Agata
                    string agataName = _translationService.GetTranslation("Scene1_Character_Agata");
                    if (dialogue.CharacterName.Contains("Agata", StringComparison.OrdinalIgnoreCase) || 
                        dialogue.CharacterName.Contains(agataName, StringComparison.OrdinalIgnoreCase))
                    {
                        var gameState = _storyService.GetGameState();
                        string sympathyText = _translationService.GetTranslation("GameScene_Sympathy");
                        string obedienceText = _translationService.GetTranslation("GameScene_Obedience");
                        CharacterStatusText.Text = $"- {sympathyText}, {obedienceText}";
                    }
                }
                // Update location if unknown
                UpdateLocationDisplay();
                // Update FPS counter
                FpsTimer_Tick(null, EventArgs.Empty);
            }), DispatcherPriority.Normal);
        }

        private void ApplyTranslations()
        {
            try
            {
                // Update buttons - get key bindings from config
                var config = _configService.GetConfig();
                string saveText = _translationService.GetTranslation("GameScene_Save");
                string loadText = _translationService.GetTranslation("GameScene_Load");
                string menuText = _translationService.GetTranslation("GameScene_Menu");
                
                if (SaveButton != null)
                    SaveButton.Content = $"{saveText} ({config.SaveKey})";
                if (LoadButton != null)
                    LoadButton.Content = $"{loadText} ({config.LoadKey})";
                if (MenuButton != null)
                    MenuButton.Content = $"{menuText} ({config.MenuKey})";
                
                // Update skip indicator
                if (SkipIndicator != null)
                    SkipIndicator.Text = _translationService.GetTranslation("GameScene_SkipIndicator");
                
                // Update chapter title
                UpdateChapterTitle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying translations: {ex.Message}");
            }
        }

        private void UpdateChapterTitle()
        {
            if (ChapterTitleText != null)
            {
                ChapterTitleText.Text = _translationService.GetTranslation("Chapter1_Title");
            }
        }
    }
}
