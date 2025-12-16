using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using VisualNovel.Models;
using VisualNovel.Services;

namespace VisualNovel
{
    public partial class SettingsMenu : Window
    {
        private readonly ConfigService _configService;
        private readonly SoundService _soundService;
        private readonly TranslationService _translationService;
        private GameConfig _currentConfig;
        private GameConfig _originalConfig; // Store original to compare changes
        private Button? _activeTabButton;
        private bool _hasUnsavedChanges = false;
        
        // Cache styles for faster tab switching
        private Style? _tabButtonStyle;
        private Style? _activeTabButtonStyle;

        public SettingsMenu(ConfigService configService)
        {
            try
            {
                // Make window transparent initially to prevent flash of untranslated content and delay
                // Using Opacity instead of Visibility so ShowDialog() works correctly
                this.Opacity = 0;
                
                InitializeComponent();
                _configService = configService;
                _soundService = new SoundService();
                _translationService = TranslationService.Instance;
                _translationService.InitializeFromConfig(_configService);
                _translationService.LanguageChanged += TranslationService_LanguageChanged;
                
                // Create a deep copy of the config to avoid modifying the original until Apply is clicked
                try
                {
                    var originalConfig = _configService.GetConfig();
                    if (originalConfig != null)
                    {
                        string json = JsonConvert.SerializeObject(originalConfig);
                        _currentConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
                        _originalConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
                    }
                    else
                    {
                        _currentConfig = new GameConfig();
                        _originalConfig = new GameConfig();
                    }
                }
                catch
                {
                    _currentConfig = new GameConfig();
                    _originalConfig = new GameConfig();
                }

                // Wait for window to be fully loaded before accessing UI elements
                // Do initialization synchronously in Loaded event to prevent delay
                this.Loaded += SettingsMenu_Loaded;
            }
            catch (Exception ex)
            {
                // Make sure window is visible even if constructor fails
                this.Opacity = 1;
                var errorDialog = new Dialogs.GameDialog(
                    _translationService?.GetTranslation("Dialog_Error_Title") ?? "Error",
                    $"Error: {ex.Message}",
                    Dialogs.DialogType.OK);
                // Only set Owner if window has been shown (not just loaded)
                // Window must be visible and have been shown via ShowDialog/Show
                try
                {
                    if (this.IsLoaded && this.IsVisible)
                    {
                        errorDialog.Owner = this;
                    }
                }
                catch
                {
                    // If setting Owner fails, just continue without it
                }
                errorDialog.ShowDialog();
                throw;
            }
        }

        private void SettingsMenu_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Do all initialization synchronously (not with BeginInvoke) to prevent delay
                // Window is transparent, so user won't see English text flash
                try
                {
                    // Load Minecraft font with fallback (cached, so very fast)
                    var minecraftFont = Services.FontHelper.LoadMinecraftFontWithFallback();
                    Resources["MinecraftFont"] = minecraftFont;
                    // Apply font recursively to ensure all elements get it (backup for elements not using styles)
                    ApplyFontToChildren(this, minecraftFont);

                    // Setup ComboBox font application when dropdowns open
                    SetupComboBoxFonts(minecraftFont);

                    // Cache styles for faster tab switching
                    if (Resources.Contains("TabButtonStyle"))
                        _tabButtonStyle = (Style)Resources["TabButtonStyle"];
                    if (Resources.Contains("ActiveTabButtonStyle"))
                        _activeTabButtonStyle = (Style)Resources["ActiveTabButtonStyle"];

                    // Load custom cursor (fast file check)
                    LoadCustomCursor();

                    // Set initial active tab
                    if (AudioTabButton != null)
                    {
                        _activeTabButton = AudioTabButton;
                        SwitchTab(AudioTabButton);
                    }

                    // Load current settings
                    LoadSettings();
                    
                    // Apply translations BEFORE showing window
                    ApplyTranslations();
                    
                    // Set up change tracking after settings are loaded
                    SetupChangeTracking();
                    
                    // Update Apply button state (should be disabled initially)
                    UpdateApplyButtonState();
                    
                    // Now make the window visible - all translations are already applied
                    this.Opacity = 1;
                }
                catch (Exception ex)
                {
                    // If initialization fails, still show the window
                    this.Opacity = 1;
                    var errorDialog = new Dialogs.GameDialog(
                        _translationService.GetTranslation("Dialog_Error_Title"),
                        $"{_translationService.GetTranslation("Dialog_Error_Title")}: {ex.Message}",
                        Dialogs.DialogType.OK);
                    // Only set Owner if window has been shown
                    if (this.IsLoaded)
                    {
                        errorDialog.Owner = this;
                    }
                    errorDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // Make sure window is visible even if there's an error
                this.Opacity = 1;
                var errorDialog = new Dialogs.GameDialog(
                    _translationService.GetTranslation("Dialog_Error_Title"),
                    $"{_translationService.GetTranslation("Dialog_Error_Title")}: {ex.Message}",
                    Dialogs.DialogType.OK);
                // Only set Owner if window has been shown (not just loaded)
                // Window must be visible and have been shown via ShowDialog/Show
                try
                {
                    if (this.IsLoaded && this.IsVisible)
                    {
                        errorDialog.Owner = this;
                    }
                }
                catch
                {
                    // If setting Owner fails, just continue without it
                }
                errorDialog.ShowDialog();
            }
        }

        private void LoadCustomCursor()
        {
            try
            {
                string cursorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "AOM_Titans Cursor.cur");
                if (System.IO.File.Exists(cursorPath))
                {
                    this.Cursor = new System.Windows.Input.Cursor(cursorPath);
                }
            }
            catch { }
        }

        private void ApplyFontToChildren(DependencyObject parent, System.Windows.Media.FontFamily font)
        {
            try
            {
                int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                    
                    // Apply font to TextBlocks
                    if (child is TextBlock textBlock)
                    {
                        textBlock.FontFamily = font;
                    }
                    // Apply font to Buttons
                    else if (child is Button button)
                    {
                        button.FontFamily = font;
                    }
                    // Apply font to TextBoxes
                    else if (child is TextBox textBox)
                    {
                        textBox.FontFamily = font;
                    }
                    // Apply font to CheckBoxes
                    else if (child is CheckBox checkBox)
                    {
                        checkBox.FontFamily = font;
                    }
                    // Apply font to ComboBoxes
                    else if (child is ComboBox comboBox)
                    {
                        comboBox.FontFamily = font;
                    }
                    // Apply font to ComboBoxItems
                    else if (child is ComboBoxItem comboBoxItem)
                    {
                        comboBoxItem.FontFamily = font;
                    }
                    
                    // Recursively apply to children
                    ApplyFontToChildren(child, font);
                }
            }
            catch { }
        }

        /// <summary>
        /// Refreshes settings from config file - call this when showing the window again
        /// </summary>
        public void RefreshSettings()
        {
            try
            {
                // Reload config from file
                var originalConfig = _configService.GetConfig();
                if (originalConfig != null)
                {
                    string json = JsonConvert.SerializeObject(originalConfig);
                    _currentConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
                    _originalConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
                }
                else
                {
                    _currentConfig = new GameConfig();
                    _originalConfig = new GameConfig();
                }
                
                // Update translation service
                _translationService.InitializeFromConfig(_configService);
                
                // Reload settings into UI
                LoadSettings();
                
                // Reapply translations in case language changed
                ApplyTranslations();
                
                // Reset change tracking
                _hasUnsavedChanges = false;
                UpdateApplyButtonState();
            }
            catch (Exception ex)
            {
                // Silently fail - if refresh fails, use existing settings
                System.Diagnostics.Debug.WriteLine($"Error refreshing settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (_currentConfig == null)
                {
                    _currentConfig = new GameConfig();
                }

                // Audio Settings (convert from 0-1 to 0-100 for UI)
                if (MasterVolumeSlider != null) MasterVolumeSlider.Value = _currentConfig.MasterVolume * 100;
                if (MusicVolumeSlider != null) MusicVolumeSlider.Value = _currentConfig.MusicVolume * 100;
                if (SoundEffectsVolumeSlider != null) SoundEffectsVolumeSlider.Value = _currentConfig.SoundEffectsVolume * 100;
                if (MuteMusicCheckBox != null) MuteMusicCheckBox.IsChecked = _currentConfig.MuteMusic;
                if (MuteSoundEffectsCheckBox != null) MuteSoundEffectsCheckBox.IsChecked = _currentConfig.MuteSoundEffects;
                UpdateVolumeLabels();

                // Text Settings
                if (TextSpeedSlider != null) TextSpeedSlider.Value = _currentConfig.TextSpeed;
                if (FontSizeSlider != null) FontSizeSlider.Value = _currentConfig.FontSize;
                if (AutoAdvanceCheckBox != null) AutoAdvanceCheckBox.IsChecked = _currentConfig.AutoAdvance;
                if (AutoAdvanceDelaySlider != null) AutoAdvanceDelaySlider.Value = _currentConfig.AutoAdvanceDelay;
                if (SkipUnreadTextCheckBox != null) SkipUnreadTextCheckBox.IsChecked = _currentConfig.SkipUnreadText;
                if (SkipReadTextCheckBox != null) SkipReadTextCheckBox.IsChecked = _currentConfig.SkipReadText;
                UpdateTextLabels();

                // Display Settings
                if (FullscreenCheckBox != null) FullscreenCheckBox.IsChecked = _currentConfig.Fullscreen;
                if (WindowWidthSlider != null) WindowWidthSlider.Value = _currentConfig.WindowWidth;
                if (WindowHeightSlider != null) WindowHeightSlider.Value = _currentConfig.WindowHeight;
                if (VSyncCheckBox != null) VSyncCheckBox.IsChecked = _currentConfig.VSync;
                if (TargetFPSSlider != null) TargetFPSSlider.Value = _currentConfig.TargetFPS;
                if (ShowFPSCheckBox != null) ShowFPSCheckBox.IsChecked = _currentConfig.ShowFPS;
                UpdateDisplayLabels();

                // Graphics Settings
                if (GraphicsQualityComboBox != null)
                {
                    var selectedItem = GraphicsQualityComboBox.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content?.ToString() == _currentConfig.GraphicsQuality);
                    if (selectedItem != null)
                    {
                        GraphicsQualityComboBox.SelectedItem = selectedItem;
                    }
                }
                if (EnableEffectsCheckBox != null) EnableEffectsCheckBox.IsChecked = _currentConfig.EnableEffects;
                if (EnableShadowsCheckBox != null) EnableShadowsCheckBox.IsChecked = _currentConfig.EnableShadows;
                // Graphics Settings (convert from 0-1 to 0-100 for UI)
                if (BackgroundOpacitySlider != null) BackgroundOpacitySlider.Value = _currentConfig.BackgroundOpacity * 100;
                
                // Optimization Settings
                if (EnableParallaxCheckBox != null) EnableParallaxCheckBox.IsChecked = _currentConfig.EnableParallax;
                if (EnableMovingSpritesCheckBox != null) EnableMovingSpritesCheckBox.IsChecked = _currentConfig.EnableMovingSprites;
                if (ParallaxIntensitySlider != null) ParallaxIntensitySlider.Value = _currentConfig.ParallaxIntensity * 100; // Convert 0-2 to 0-200
                if (CursorMovementSensitivitySlider != null) CursorMovementSensitivitySlider.Value = _currentConfig.CursorMovementSensitivity * 100; // Convert 0-2 to 0-200
                if (ReduceParticleEffectsCheckBox != null) ReduceParticleEffectsCheckBox.IsChecked = _currentConfig.ReduceParticleEffects;
                if (LowerAnimationQualityCheckBox != null) LowerAnimationQualityCheckBox.IsChecked = _currentConfig.LowerAnimationQuality;
                if (DisableBackgroundVideosCheckBox != null) DisableBackgroundVideosCheckBox.IsChecked = _currentConfig.DisableBackgroundVideos;
                if (ReduceTextureQualityCheckBox != null) ReduceTextureQualityCheckBox.IsChecked = _currentConfig.ReduceTextureQuality;
                if (ReduceUIEffectsCheckBox != null) ReduceUIEffectsCheckBox.IsChecked = _currentConfig.ReduceUIEffects;
                if (ParallaxUpdateRateSlider != null) ParallaxUpdateRateSlider.Value = _currentConfig.ParallaxUpdateRate;
                UpdateGraphicsLabels();
                UpdateOptimisationLabels();

                // Gameplay Settings
                if (AutoSaveCheckBox != null) AutoSaveCheckBox.IsChecked = _currentConfig.AutoSave;
                if (AutoSaveIntervalSlider != null) AutoSaveIntervalSlider.Value = _currentConfig.AutoSaveInterval;
                if (ConfirmOnExitCheckBox != null) ConfirmOnExitCheckBox.IsChecked = _currentConfig.ConfirmOnExit;
                if (ShowSkipIndicatorCheckBox != null) ShowSkipIndicatorCheckBox.IsChecked = _currentConfig.ShowSkipIndicator;
                if (ShowLocationTimeCheckBox != null) ShowLocationTimeCheckBox.IsChecked = _currentConfig.ShowLocationTime;
                if (ShowChapterTitleCheckBox != null) ShowChapterTitleCheckBox.IsChecked = _currentConfig.ShowChapterTitle;
                UpdateGameplayLabels();

                // Controls Settings
                if (SkipKeyTextBox != null) SkipKeyTextBox.Text = _currentConfig.SkipKey ?? "Enter";
                if (SaveKeyTextBox != null) SaveKeyTextBox.Text = _currentConfig.SaveKey ?? "Ctrl+S";
                if (LoadKeyTextBox != null) LoadKeyTextBox.Text = _currentConfig.LoadKey ?? "Ctrl+L";
                if (MenuKeyTextBox != null) MenuKeyTextBox.Text = _currentConfig.MenuKey ?? "Escape";
                if (AutoAdvanceToggleKeyTextBox != null) AutoAdvanceToggleKeyTextBox.Text = _currentConfig.AutoAdvanceToggleKey ?? "A";

                // Accessibility Settings
                if (HighContrastCheckBox != null) HighContrastCheckBox.IsChecked = _currentConfig.HighContrast;
                if (LargeTextCheckBox != null) LargeTextCheckBox.IsChecked = _currentConfig.LargeText;
                if (ReduceAnimationsCheckBox != null) ReduceAnimationsCheckBox.IsChecked = _currentConfig.ReduceAnimations;
                
                // Language Settings
                if (LanguageComboBox != null)
                {
                    var selectedItem = LanguageComboBox.Items
                        .Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Tag?.ToString() == _currentConfig.Language);
                    if (selectedItem != null)
                    {
                        LanguageComboBox.SelectedItem = selectedItem;
                    }
                    else
                    {
                        // Default to English if language not found
                        var englishItem = LanguageComboBox.Items
                            .Cast<ComboBoxItem>()
                            .FirstOrDefault(item => item.Tag?.ToString() == "English");
                        if (englishItem != null)
                        {
                            LanguageComboBox.SelectedItem = englishItem;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new Dialogs.GameDialog(
                    _translationService.GetTranslation("Dialog_Error_Title"),
                    $"{_translationService.GetTranslation("Dialog_Error_Title")}: {ex.Message}",
                    Dialogs.DialogType.OK);
                // Only set Owner if window has been shown
                if (this.IsLoaded && this.IsVisible)
                {
                    errorDialog.Owner = this;
                }
                errorDialog.ShowDialog();
            }
        }

        private void SetupChangeTracking()
        {
            // Track all control changes
            if (MasterVolumeSlider != null) MasterVolumeSlider.ValueChanged += OnSettingChanged;
            if (MusicVolumeSlider != null) MusicVolumeSlider.ValueChanged += OnSettingChanged;
            if (SoundEffectsVolumeSlider != null) SoundEffectsVolumeSlider.ValueChanged += OnSettingChanged;
            if (MuteMusicCheckBox != null) { MuteMusicCheckBox.Checked += OnSettingChanged; MuteMusicCheckBox.Unchecked += OnSettingChanged; }
            if (MuteSoundEffectsCheckBox != null) { MuteSoundEffectsCheckBox.Checked += OnSettingChanged; MuteSoundEffectsCheckBox.Unchecked += OnSettingChanged; }
            if (TextSpeedSlider != null) TextSpeedSlider.ValueChanged += OnSettingChanged;
            if (FontSizeSlider != null) FontSizeSlider.ValueChanged += OnSettingChanged;
            if (AutoAdvanceCheckBox != null) { AutoAdvanceCheckBox.Checked += OnSettingChanged; AutoAdvanceCheckBox.Unchecked += OnSettingChanged; }
            if (AutoAdvanceDelaySlider != null) AutoAdvanceDelaySlider.ValueChanged += OnSettingChanged;
            if (SkipUnreadTextCheckBox != null) { SkipUnreadTextCheckBox.Checked += OnSettingChanged; SkipUnreadTextCheckBox.Unchecked += OnSettingChanged; }
            if (SkipReadTextCheckBox != null) { SkipReadTextCheckBox.Checked += OnSettingChanged; SkipReadTextCheckBox.Unchecked += OnSettingChanged; }
            if (FullscreenCheckBox != null) { FullscreenCheckBox.Checked += OnSettingChanged; FullscreenCheckBox.Unchecked += OnSettingChanged; }
            if (WindowWidthSlider != null) WindowWidthSlider.ValueChanged += OnSettingChanged;
            if (WindowHeightSlider != null) WindowHeightSlider.ValueChanged += OnSettingChanged;
            if (VSyncCheckBox != null) { VSyncCheckBox.Checked += OnSettingChanged; VSyncCheckBox.Unchecked += OnSettingChanged; }
            if (TargetFPSSlider != null) TargetFPSSlider.ValueChanged += OnSettingChanged;
            if (ShowFPSCheckBox != null) { ShowFPSCheckBox.Checked += OnSettingChanged; ShowFPSCheckBox.Unchecked += OnSettingChanged; }
            if (GraphicsQualityComboBox != null) GraphicsQualityComboBox.SelectionChanged += OnSettingChanged;
            if (EnableEffectsCheckBox != null) { EnableEffectsCheckBox.Checked += OnSettingChanged; EnableEffectsCheckBox.Unchecked += OnSettingChanged; }
            if (EnableShadowsCheckBox != null) { EnableShadowsCheckBox.Checked += OnSettingChanged; EnableShadowsCheckBox.Unchecked += OnSettingChanged; }
            if (BackgroundOpacitySlider != null) BackgroundOpacitySlider.ValueChanged += OnSettingChanged;
            if (EnableParallaxCheckBox != null) { EnableParallaxCheckBox.Checked += OnSettingChanged; EnableParallaxCheckBox.Unchecked += OnSettingChanged; }
            if (EnableMovingSpritesCheckBox != null) { EnableMovingSpritesCheckBox.Checked += OnSettingChanged; EnableMovingSpritesCheckBox.Unchecked += OnSettingChanged; }
            if (ParallaxIntensitySlider != null) ParallaxIntensitySlider.ValueChanged += OnSettingChanged;
            if (CursorMovementSensitivitySlider != null) CursorMovementSensitivitySlider.ValueChanged += OnSettingChanged;
            if (ReduceParticleEffectsCheckBox != null) { ReduceParticleEffectsCheckBox.Checked += OnSettingChanged; ReduceParticleEffectsCheckBox.Unchecked += OnSettingChanged; }
            if (LowerAnimationQualityCheckBox != null) { LowerAnimationQualityCheckBox.Checked += OnSettingChanged; LowerAnimationQualityCheckBox.Unchecked += OnSettingChanged; }
            if (DisableBackgroundVideosCheckBox != null) { DisableBackgroundVideosCheckBox.Checked += OnSettingChanged; DisableBackgroundVideosCheckBox.Unchecked += OnSettingChanged; }
            if (ReduceTextureQualityCheckBox != null) { ReduceTextureQualityCheckBox.Checked += OnSettingChanged; ReduceTextureQualityCheckBox.Unchecked += OnSettingChanged; }
            if (ReduceUIEffectsCheckBox != null) { ReduceUIEffectsCheckBox.Checked += OnSettingChanged; ReduceUIEffectsCheckBox.Unchecked += OnSettingChanged; }
            if (ParallaxUpdateRateSlider != null) ParallaxUpdateRateSlider.ValueChanged += OnSettingChanged;
            if (AutoSaveCheckBox != null) { AutoSaveCheckBox.Checked += OnSettingChanged; AutoSaveCheckBox.Unchecked += OnSettingChanged; }
            if (AutoSaveIntervalSlider != null) AutoSaveIntervalSlider.ValueChanged += OnSettingChanged;
            if (ConfirmOnExitCheckBox != null) { ConfirmOnExitCheckBox.Checked += OnSettingChanged; ConfirmOnExitCheckBox.Unchecked += OnSettingChanged; }
            if (ShowSkipIndicatorCheckBox != null) { ShowSkipIndicatorCheckBox.Checked += OnSettingChanged; ShowSkipIndicatorCheckBox.Unchecked += OnSettingChanged; }
            if (ShowLocationTimeCheckBox != null) { ShowLocationTimeCheckBox.Checked += OnSettingChanged; ShowLocationTimeCheckBox.Unchecked += OnSettingChanged; }
            if (ShowChapterTitleCheckBox != null) { ShowChapterTitleCheckBox.Checked += OnSettingChanged; ShowChapterTitleCheckBox.Unchecked += OnSettingChanged; }
            if (SkipKeyTextBox != null) SkipKeyTextBox.TextChanged += OnSettingChanged;
            if (SaveKeyTextBox != null) SaveKeyTextBox.TextChanged += OnSettingChanged;
            if (LoadKeyTextBox != null) LoadKeyTextBox.TextChanged += OnSettingChanged;
            if (MenuKeyTextBox != null) MenuKeyTextBox.TextChanged += OnSettingChanged;
            if (AutoAdvanceToggleKeyTextBox != null) AutoAdvanceToggleKeyTextBox.TextChanged += OnSettingChanged;
            if (HighContrastCheckBox != null) { HighContrastCheckBox.Checked += OnSettingChanged; HighContrastCheckBox.Unchecked += OnSettingChanged; }
            if (LargeTextCheckBox != null) { LargeTextCheckBox.Checked += OnSettingChanged; LargeTextCheckBox.Unchecked += OnSettingChanged; }
            if (ReduceAnimationsCheckBox != null) { ReduceAnimationsCheckBox.Checked += OnSettingChanged; ReduceAnimationsCheckBox.Unchecked += OnSettingChanged; }
            if (LanguageComboBox != null) LanguageComboBox.SelectionChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object? sender, EventArgs e)
        {
            _hasUnsavedChanges = true;
            UpdateApplyButtonState();
        }

        private bool HasChanges()
        {
            try
            {
                // Create a temporary config with current UI values
                var tempConfig = new GameConfig();
                
                // Audio Settings
                if (MasterVolumeSlider != null) tempConfig.MasterVolume = MasterVolumeSlider.Value / 100.0;
                if (MusicVolumeSlider != null) tempConfig.MusicVolume = MusicVolumeSlider.Value / 100.0;
                if (SoundEffectsVolumeSlider != null) tempConfig.SoundEffectsVolume = SoundEffectsVolumeSlider.Value / 100.0;
                if (MuteMusicCheckBox != null) tempConfig.MuteMusic = MuteMusicCheckBox.IsChecked ?? false;
                if (MuteSoundEffectsCheckBox != null) tempConfig.MuteSoundEffects = MuteSoundEffectsCheckBox.IsChecked ?? false;
                
                // Text Settings
                if (TextSpeedSlider != null) tempConfig.TextSpeed = (int)TextSpeedSlider.Value;
                if (FontSizeSlider != null) tempConfig.FontSize = (int)FontSizeSlider.Value;
                if (AutoAdvanceCheckBox != null) tempConfig.AutoAdvance = AutoAdvanceCheckBox.IsChecked ?? false;
                if (AutoAdvanceDelaySlider != null) tempConfig.AutoAdvanceDelay = (int)AutoAdvanceDelaySlider.Value;
                if (SkipUnreadTextCheckBox != null) tempConfig.SkipUnreadText = SkipUnreadTextCheckBox.IsChecked ?? false;
                if (SkipReadTextCheckBox != null) tempConfig.SkipReadText = SkipReadTextCheckBox.IsChecked ?? false;
                
                // Display Settings
                if (FullscreenCheckBox != null) tempConfig.Fullscreen = FullscreenCheckBox.IsChecked ?? false;
                if (WindowWidthSlider != null) tempConfig.WindowWidth = (int)WindowWidthSlider.Value;
                if (WindowHeightSlider != null) tempConfig.WindowHeight = (int)WindowHeightSlider.Value;
                if (VSyncCheckBox != null) tempConfig.VSync = VSyncCheckBox.IsChecked ?? false;
                if (TargetFPSSlider != null) tempConfig.TargetFPS = (int)TargetFPSSlider.Value;
                if (ShowFPSCheckBox != null) tempConfig.ShowFPS = ShowFPSCheckBox.IsChecked ?? false;
                
                // Graphics Settings
                if (GraphicsQualityComboBox != null && GraphicsQualityComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    tempConfig.GraphicsQuality = selectedItem.Content?.ToString() ?? "High";
                }
                if (EnableEffectsCheckBox != null) tempConfig.EnableEffects = EnableEffectsCheckBox.IsChecked ?? false;
                if (EnableShadowsCheckBox != null) tempConfig.EnableShadows = EnableShadowsCheckBox.IsChecked ?? false;
                if (BackgroundOpacitySlider != null) tempConfig.BackgroundOpacity = BackgroundOpacitySlider.Value / 100.0;
                
                // Optimization Settings
                if (EnableParallaxCheckBox != null) tempConfig.EnableParallax = EnableParallaxCheckBox.IsChecked ?? false;
                if (EnableMovingSpritesCheckBox != null) tempConfig.EnableMovingSprites = EnableMovingSpritesCheckBox.IsChecked ?? false;
                if (ParallaxIntensitySlider != null) tempConfig.ParallaxIntensity = ParallaxIntensitySlider.Value / 100.0; // Convert 0-200 to 0-2
                if (CursorMovementSensitivitySlider != null) tempConfig.CursorMovementSensitivity = CursorMovementSensitivitySlider.Value / 100.0; // Convert 0-200 to 0-2
                if (ReduceParticleEffectsCheckBox != null) tempConfig.ReduceParticleEffects = ReduceParticleEffectsCheckBox.IsChecked ?? false;
                if (LowerAnimationQualityCheckBox != null) tempConfig.LowerAnimationQuality = LowerAnimationQualityCheckBox.IsChecked ?? false;
                if (DisableBackgroundVideosCheckBox != null) tempConfig.DisableBackgroundVideos = DisableBackgroundVideosCheckBox.IsChecked ?? false;
                if (ReduceTextureQualityCheckBox != null) tempConfig.ReduceTextureQuality = ReduceTextureQualityCheckBox.IsChecked ?? false;
                if (ReduceUIEffectsCheckBox != null) tempConfig.ReduceUIEffects = ReduceUIEffectsCheckBox.IsChecked ?? false;
                if (ParallaxUpdateRateSlider != null) tempConfig.ParallaxUpdateRate = (int)ParallaxUpdateRateSlider.Value;
                
                // Gameplay Settings
                if (AutoSaveCheckBox != null) tempConfig.AutoSave = AutoSaveCheckBox.IsChecked ?? false;
                if (AutoSaveIntervalSlider != null) tempConfig.AutoSaveInterval = (int)AutoSaveIntervalSlider.Value;
                if (ConfirmOnExitCheckBox != null) tempConfig.ConfirmOnExit = ConfirmOnExitCheckBox.IsChecked ?? false;
                if (ShowSkipIndicatorCheckBox != null) tempConfig.ShowSkipIndicator = ShowSkipIndicatorCheckBox.IsChecked ?? false;
                if (ShowLocationTimeCheckBox != null) tempConfig.ShowLocationTime = ShowLocationTimeCheckBox.IsChecked ?? false;
                if (ShowChapterTitleCheckBox != null) tempConfig.ShowChapterTitle = ShowChapterTitleCheckBox.IsChecked ?? false;
                
                // Controls Settings
                if (SkipKeyTextBox != null) tempConfig.SkipKey = SkipKeyTextBox.Text ?? "Enter";
                if (SaveKeyTextBox != null) tempConfig.SaveKey = SaveKeyTextBox.Text ?? "Ctrl+S";
                if (LoadKeyTextBox != null) tempConfig.LoadKey = LoadKeyTextBox.Text ?? "Ctrl+L";
                if (MenuKeyTextBox != null) tempConfig.MenuKey = MenuKeyTextBox.Text ?? "Escape";
                if (AutoAdvanceToggleKeyTextBox != null) tempConfig.AutoAdvanceToggleKey = AutoAdvanceToggleKeyTextBox.Text ?? "A";
                
                // Accessibility Settings
                if (HighContrastCheckBox != null) tempConfig.HighContrast = HighContrastCheckBox.IsChecked ?? false;
                if (LargeTextCheckBox != null) tempConfig.LargeText = LargeTextCheckBox.IsChecked ?? false;
                if (ReduceAnimationsCheckBox != null) tempConfig.ReduceAnimations = ReduceAnimationsCheckBox.IsChecked ?? false;
                
                // Language Settings
                if (LanguageComboBox != null && LanguageComboBox.SelectedItem is ComboBoxItem selectedLanguageItem)
                {
                    string? selectedLanguage = selectedLanguageItem.Tag?.ToString();
                    if (!string.IsNullOrEmpty(selectedLanguage))
                    {
                        tempConfig.Language = selectedLanguage;
                    }
                }
                
                // Compare with original
                string originalJson = JsonConvert.SerializeObject(_originalConfig);
                string currentJson = JsonConvert.SerializeObject(tempConfig);
                return originalJson != currentJson;
            }
            catch
            {
                return _hasUnsavedChanges;
            }
        }

        private void UpdateApplyButtonState()
        {
            if (ApplyButton != null)
            {
                ApplyButton.IsEnabled = HasChanges();
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Audio Settings (convert from 0-100 to 0-1 for config)
                if (MasterVolumeSlider != null) _currentConfig.MasterVolume = MasterVolumeSlider.Value / 100.0;
                if (MusicVolumeSlider != null) _currentConfig.MusicVolume = MusicVolumeSlider.Value / 100.0;
                if (SoundEffectsVolumeSlider != null) _currentConfig.SoundEffectsVolume = SoundEffectsVolumeSlider.Value / 100.0;
                if (MuteMusicCheckBox != null) _currentConfig.MuteMusic = MuteMusicCheckBox.IsChecked ?? false;
                if (MuteSoundEffectsCheckBox != null) _currentConfig.MuteSoundEffects = MuteSoundEffectsCheckBox.IsChecked ?? false;

                // Text Settings
                if (TextSpeedSlider != null) _currentConfig.TextSpeed = (int)TextSpeedSlider.Value;
                if (FontSizeSlider != null) _currentConfig.FontSize = (int)FontSizeSlider.Value;
                if (AutoAdvanceCheckBox != null) _currentConfig.AutoAdvance = AutoAdvanceCheckBox.IsChecked ?? false;
                if (AutoAdvanceDelaySlider != null) _currentConfig.AutoAdvanceDelay = (int)AutoAdvanceDelaySlider.Value;
                if (SkipUnreadTextCheckBox != null) _currentConfig.SkipUnreadText = SkipUnreadTextCheckBox.IsChecked ?? false;
                if (SkipReadTextCheckBox != null) _currentConfig.SkipReadText = SkipReadTextCheckBox.IsChecked ?? false;

                // Display Settings
                if (FullscreenCheckBox != null) _currentConfig.Fullscreen = FullscreenCheckBox.IsChecked ?? false;
                if (WindowWidthSlider != null) _currentConfig.WindowWidth = (int)WindowWidthSlider.Value;
                if (WindowHeightSlider != null) _currentConfig.WindowHeight = (int)WindowHeightSlider.Value;
                if (VSyncCheckBox != null) _currentConfig.VSync = VSyncCheckBox.IsChecked ?? false;
                if (TargetFPSSlider != null) _currentConfig.TargetFPS = (int)TargetFPSSlider.Value;
                if (ShowFPSCheckBox != null) _currentConfig.ShowFPS = ShowFPSCheckBox.IsChecked ?? false;

                // Graphics Settings
                if (GraphicsQualityComboBox != null && GraphicsQualityComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    _currentConfig.GraphicsQuality = selectedItem.Content?.ToString() ?? "High";
                }
                if (EnableEffectsCheckBox != null) _currentConfig.EnableEffects = EnableEffectsCheckBox.IsChecked ?? false;
                if (EnableShadowsCheckBox != null) _currentConfig.EnableShadows = EnableShadowsCheckBox.IsChecked ?? false;
                // Graphics Settings (convert from 0-100 to 0-1 for config)
                if (BackgroundOpacitySlider != null) _currentConfig.BackgroundOpacity = BackgroundOpacitySlider.Value / 100.0;

                // Optimization Settings
                if (EnableParallaxCheckBox != null) _currentConfig.EnableParallax = EnableParallaxCheckBox.IsChecked ?? false;
                if (EnableMovingSpritesCheckBox != null) _currentConfig.EnableMovingSprites = EnableMovingSpritesCheckBox.IsChecked ?? false;
                if (ParallaxIntensitySlider != null) _currentConfig.ParallaxIntensity = ParallaxIntensitySlider.Value / 100.0; // Convert 0-200 to 0-2
                if (CursorMovementSensitivitySlider != null) _currentConfig.CursorMovementSensitivity = CursorMovementSensitivitySlider.Value / 100.0; // Convert 0-200 to 0-2
                if (ReduceParticleEffectsCheckBox != null) _currentConfig.ReduceParticleEffects = ReduceParticleEffectsCheckBox.IsChecked ?? false;
                if (LowerAnimationQualityCheckBox != null) _currentConfig.LowerAnimationQuality = LowerAnimationQualityCheckBox.IsChecked ?? false;
                if (DisableBackgroundVideosCheckBox != null) _currentConfig.DisableBackgroundVideos = DisableBackgroundVideosCheckBox.IsChecked ?? false;
                if (ReduceTextureQualityCheckBox != null) _currentConfig.ReduceTextureQuality = ReduceTextureQualityCheckBox.IsChecked ?? false;
                if (ReduceUIEffectsCheckBox != null) _currentConfig.ReduceUIEffects = ReduceUIEffectsCheckBox.IsChecked ?? false;
                if (ParallaxUpdateRateSlider != null) _currentConfig.ParallaxUpdateRate = (int)ParallaxUpdateRateSlider.Value;

                // Gameplay Settings
                if (AutoSaveCheckBox != null) _currentConfig.AutoSave = AutoSaveCheckBox.IsChecked ?? false;
                if (AutoSaveIntervalSlider != null) _currentConfig.AutoSaveInterval = (int)AutoSaveIntervalSlider.Value;
                if (ConfirmOnExitCheckBox != null) _currentConfig.ConfirmOnExit = ConfirmOnExitCheckBox.IsChecked ?? false;
                if (ShowSkipIndicatorCheckBox != null) _currentConfig.ShowSkipIndicator = ShowSkipIndicatorCheckBox.IsChecked ?? false;
                if (ShowLocationTimeCheckBox != null) _currentConfig.ShowLocationTime = ShowLocationTimeCheckBox.IsChecked ?? false;
                if (ShowChapterTitleCheckBox != null) _currentConfig.ShowChapterTitle = ShowChapterTitleCheckBox.IsChecked ?? false;

                // Controls Settings
                if (SkipKeyTextBox != null) _currentConfig.SkipKey = SkipKeyTextBox.Text ?? "Enter";
                if (SaveKeyTextBox != null) _currentConfig.SaveKey = SaveKeyTextBox.Text ?? "Ctrl+S";
                if (LoadKeyTextBox != null) _currentConfig.LoadKey = LoadKeyTextBox.Text ?? "Ctrl+L";
                if (MenuKeyTextBox != null) _currentConfig.MenuKey = MenuKeyTextBox.Text ?? "Escape";
                if (AutoAdvanceToggleKeyTextBox != null) _currentConfig.AutoAdvanceToggleKey = AutoAdvanceToggleKeyTextBox.Text ?? "A";

                // Accessibility Settings
                if (HighContrastCheckBox != null) _currentConfig.HighContrast = HighContrastCheckBox.IsChecked ?? false;
                if (LargeTextCheckBox != null) _currentConfig.LargeText = LargeTextCheckBox.IsChecked ?? false;
                if (ReduceAnimationsCheckBox != null) _currentConfig.ReduceAnimations = ReduceAnimationsCheckBox.IsChecked ?? false;

                // Language Settings
                if (LanguageComboBox != null && LanguageComboBox.SelectedItem is ComboBoxItem selectedLanguageItem)
                {
                    string? selectedLanguage = selectedLanguageItem.Tag?.ToString();
                    if (!string.IsNullOrEmpty(selectedLanguage))
                    {
                        _currentConfig.Language = selectedLanguage;
                        _translationService.SetLanguage(selectedLanguage);
                    }
                }

                // Update and save to file
                _configService.UpdateConfig(_currentConfig);
                _configService.SaveConfig();
            }
            catch (Exception ex)
            {
                var errorDialog = new Dialogs.GameDialog(
                    _translationService.GetTranslation("Dialog_Error_Title"),
                    $"{_translationService.GetTranslation("Dialog_Error_Title")}: {ex.Message}",
                    Dialogs.DialogType.OK);
                // Only set Owner if window has been shown
                if (this.IsLoaded && this.IsVisible)
                {
                    errorDialog.Owner = this;
                }
                errorDialog.ShowDialog();
            }
        }

        private void SwitchTab(Button tabButton)
        {
            if (tabButton == null) return;

            // Fast style switching using cached styles
            if (_activeTabButton != null && _tabButtonStyle != null)
            {
                _activeTabButton.Style = _tabButtonStyle;
            }

            _activeTabButton = tabButton;
            if (_activeTabButtonStyle != null)
            {
                tabButton.Style = _activeTabButtonStyle;
            }

            // Fast panel visibility switching - batch all changes
            // Hide all panels first (single pass)
            AudioSettingsPanel.Visibility = Visibility.Collapsed;
            TextSettingsPanel.Visibility = Visibility.Collapsed;
            DisplaySettingsPanel.Visibility = Visibility.Collapsed;
            OptimisationSettingsPanel.Visibility = Visibility.Collapsed;
            GameplaySettingsPanel.Visibility = Visibility.Collapsed;
            ControlsSettingsPanel.Visibility = Visibility.Collapsed;
            AccessibilitySettingsPanel.Visibility = Visibility.Collapsed;
            LanguageSettingsPanel.Visibility = Visibility.Collapsed;

            // Show selected panel (direct reference comparison - fastest)
            if (tabButton == AudioTabButton)
                AudioSettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == TextTabButton)
                TextSettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == DisplayTabButton)
                DisplaySettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == OptimisationTabButton)
                OptimisationSettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == GameplayTabButton)
                GameplaySettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == ControlsTabButton)
                ControlsSettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == AccessibilityTabButton)
                AccessibilitySettingsPanel.Visibility = Visibility.Visible;
            else if (tabButton == LanguageTabButton)
                LanguageSettingsPanel.Visibility = Visibility.Visible;
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch tab immediately for instant response
            if (sender is Button button)
            {
                SwitchTab(button);
            }
            
            // Play sound after tab switch (non-blocking, MediaElement.Play is async)
            _soundService.PlayClickSound();
        }

        private void UpdateVolumeLabels()
        {
            try
            {
                if (MasterVolumeValue != null && MasterVolumeSlider != null)
                    MasterVolumeValue.Text = $"{(int)MasterVolumeSlider.Value}%";
                if (MusicVolumeValue != null && MusicVolumeSlider != null)
                    MusicVolumeValue.Text = $"{(int)MusicVolumeSlider.Value}%";
                if (SoundEffectsVolumeValue != null && SoundEffectsVolumeSlider != null)
                    SoundEffectsVolumeValue.Text = $"{(int)SoundEffectsVolumeSlider.Value}%";
            }
            catch { }
        }

        private void UpdateTextLabels()
        {
            try
            {
                if (TextSpeedValue != null && TextSpeedSlider != null)
                    TextSpeedValue.Text = $"{(int)TextSpeedSlider.Value}ms";
                if (FontSizeValue != null && FontSizeSlider != null)
                    FontSizeValue.Text = $"{(int)FontSizeSlider.Value}";
                if (AutoAdvanceDelayValue != null && AutoAdvanceDelaySlider != null)
                    AutoAdvanceDelayValue.Text = $"{Math.Round(AutoAdvanceDelaySlider.Value / 1000, 1)}s";
            }
            catch { }
        }

        private void UpdateDisplayLabels()
        {
            try
            {
                if (WindowWidthValue != null && WindowWidthSlider != null)
                    WindowWidthValue.Text = $"{(int)WindowWidthSlider.Value}";
                if (WindowHeightValue != null && WindowHeightSlider != null)
                    WindowHeightValue.Text = $"{(int)WindowHeightSlider.Value}";
                if (TargetFPSValue != null && TargetFPSSlider != null)
                    TargetFPSValue.Text = $"{(int)TargetFPSSlider.Value}";
            }
            catch { }
        }

        private void UpdateGraphicsLabels()
        {
            try
            {
                if (BackgroundOpacityValue != null && BackgroundOpacitySlider != null)
                    BackgroundOpacityValue.Text = $"{(int)BackgroundOpacitySlider.Value}%";
            }
            catch { }
        }

        private void UpdateOptimisationLabels()
        {
            try
            {
                if (ParallaxIntensityValue != null && ParallaxIntensitySlider != null)
                    ParallaxIntensityValue.Text = $"{(int)ParallaxIntensitySlider.Value}%";
                if (CursorMovementSensitivityValue != null && CursorMovementSensitivitySlider != null)
                    CursorMovementSensitivityValue.Text = $"{(int)CursorMovementSensitivitySlider.Value}%";
                if (ParallaxUpdateRateValue != null && ParallaxUpdateRateSlider != null)
                    ParallaxUpdateRateValue.Text = $"{(int)ParallaxUpdateRateSlider.Value}ms";
            }
            catch { }
        }

        private void UpdateGameplayLabels()
        {
            try
            {
                if (AutoSaveIntervalValue != null && AutoSaveIntervalSlider != null)
                {
                    int minutes = (int)AutoSaveIntervalSlider.Value / 60;
                    AutoSaveIntervalValue.Text = $"{minutes}min";
                }
            }
            catch { }
        }

        // Volume Slider Events
        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabels();
        }

        private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabels();
        }

        private void SoundEffectsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateVolumeLabels();
        }

        // Text Settings Events
        private void TextSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTextLabels();
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTextLabels();
        }

        private void AutoAdvanceDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTextLabels();
        }

        // Display Settings Events
        private void WindowWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDisplayLabels();
        }

        private void WindowHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDisplayLabels();
        }

        private void TargetFPSSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDisplayLabels();
        }

        // Graphics Settings Events
        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGraphicsLabels();
        }

        private void ParallaxIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateOptimisationLabels();
        }

        private void CursorMovementSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateOptimisationLabels();
        }

        private void ParallaxUpdateRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateOptimisationLabels();
        }

        private void GraphicsQualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Quality change handled on apply
        }

        // Gameplay Settings Events
        private void AutoSaveIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGameplayLabels();
        }

        // CheckBox Events
        private void MuteMusicCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void MuteSoundEffectsCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void AutoAdvanceCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void SkipUnreadTextCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void SkipReadTextCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void FullscreenCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void VSyncCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ShowFPSCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void EnableEffectsCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void EnableShadowsCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void EnableParallaxCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void EnableMovingSpritesCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ReduceParticleEffectsCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void LowerAnimationQualityCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void DisableBackgroundVideosCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ReduceTextureQualityCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ReduceUIEffectsCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void AutoSaveCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ConfirmOnExitCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ShowSkipIndicatorCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ShowLocationTimeCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ShowChapterTitleCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void HighContrastCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void LargeTextCheckBox_Changed(object sender, RoutedEventArgs e) { }
        private void ReduceAnimationsCheckBox_Changed(object sender, RoutedEventArgs e) { }

        // TextBox Events
        private void SkipKeyTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void SaveKeyTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void LoadKeyTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void MenuKeyTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void AutoAdvanceToggleKeyTextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            var dialog = new Dialogs.GameDialog(
                _translationService.GetTranslation("Dialog_ResetSettings_Title"),
                _translationService.GetTranslation("Dialog_ResetSettings_Message"),
                Dialogs.DialogType.YesNo);
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                // Reset to default values
                _currentConfig = new GameConfig();
                
                // Update UI to show default values
                LoadSettings();
                
                // Save the default values to the config file
                SaveSettings();
                
                // Apply volume settings immediately
                ApplyVolumeSettings();
                
                // Update original config to match current (no unsaved changes)
                string json = JsonConvert.SerializeObject(_currentConfig);
                _originalConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
                _hasUnsavedChanges = false;
                UpdateApplyButtonState();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            
            // Check if there are unsaved changes
            if (HasChanges())
            {
                var dialog = new Dialogs.GameDialog(
                    _translationService.GetTranslation("Dialog_UnsavedChanges_Title"),
                    _translationService.GetTranslation("Dialog_UnsavedChanges_Message"),
                    Dialogs.DialogType.YesNo);
                dialog.Owner = this;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    // User wants to save
                    SaveSettings();
                    ApplyVolumeSettings();
                    _hasUnsavedChanges = false;
                }
                else
                {
                    // User doesn't want to save - revert language to original
                    if (_originalConfig != null && !string.IsNullOrEmpty(_originalConfig.Language))
                    {
                        _translationService.SetLanguage(_originalConfig.Language);
                    }
                }
            }
            else
            {
                // No changes, but language might have been changed - revert to original
                if (_originalConfig != null && !string.IsNullOrEmpty(_originalConfig.Language))
                {
                    _translationService.SetLanguage(_originalConfig.Language);
                }
            }
            
            this.Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            
            // Save language change before saving settings
            if (LanguageComboBox != null && LanguageComboBox.SelectedItem is ComboBoxItem selectedLanguageItem)
            {
                string? selectedLanguage = selectedLanguageItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(selectedLanguage))
                {
                    _translationService.SetLanguage(selectedLanguage);
                }
            }
            
            SaveSettings();
            
            // Apply volume immediately to all open windows
            ApplyVolumeSettings();
            
            // Update original config to match current
            string json = JsonConvert.SerializeObject(_currentConfig);
            _originalConfig = JsonConvert.DeserializeObject<GameConfig>(json) ?? new GameConfig();
            _hasUnsavedChanges = false;
            
            // Disable Apply button
            if (ApplyButton != null)
            {
                ApplyButton.IsEnabled = false;
            }
        }

        private void ApplyVolumeSettings()
        {
            try
            {
                var config = _configService.GetConfig();
                double musicVolume = config.MuteMusic ? 0 : config.MusicVolume * config.MasterVolume;
                
                // Apply to MainWindow if it exists
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow mainWindow)
                    {
                        var backgroundMusic = mainWindow.FindName("BackgroundMusic") as System.Windows.Controls.MediaElement;
                        var backgroundMusic2 = mainWindow.FindName("BackgroundMusic2") as System.Windows.Controls.MediaElement;
                        
                        if (backgroundMusic != null)
                        {
                            backgroundMusic.Volume = musicVolume;
                        }
                        if (backgroundMusic2 != null)
                        {
                            backgroundMusic2.Volume = musicVolume;
                        }
                    }
                    else if (window is GameScene gameScene)
                    {
                        var gameBackgroundMusic = gameScene.FindName("GameBackgroundMusic") as System.Windows.Controls.MediaElement;
                        if (gameBackgroundMusic != null)
                        {
                            gameBackgroundMusic.Volume = musicVolume;
                        }
                    }
                }
            }
            catch { }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Apply language change immediately for preview
            // If user cancels, we'll revert it in CancelButton_Click
            if (LanguageComboBox != null && LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? language = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(language))
                {
                    _translationService.SetLanguage(language);
                }
            }
        }

        private void TranslationService_LanguageChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyTranslations();
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void ApplyTranslations()
        {
            try
            {
                // Update title
                if (TitleText != null)
                    TitleText.Text = _translationService.GetTranslation("Settings_Title");

                // Update tab buttons
                if (AudioTabButton != null)
                    AudioTabButton.Content = _translationService.GetTranslation("Settings_Audio");
                if (TextTabButton != null)
                    TextTabButton.Content = _translationService.GetTranslation("Settings_Text");
                if (DisplayTabButton != null)
                    DisplayTabButton.Content = _translationService.GetTranslation("Settings_Display");
                if (OptimisationTabButton != null)
                    OptimisationTabButton.Content = _translationService.GetTranslation("Settings_Optimisation");
                if (GameplayTabButton != null)
                    GameplayTabButton.Content = _translationService.GetTranslation("Settings_Gameplay");
                if (ControlsTabButton != null)
                    ControlsTabButton.Content = _translationService.GetTranslation("Settings_Controls");
                if (AccessibilityTabButton != null)
                    AccessibilityTabButton.Content = _translationService.GetTranslation("Settings_Accessibility");
                if (LanguageTabButton != null)
                    LanguageTabButton.Content = _translationService.GetTranslation("Settings_Language");

                // Update language combo box items
                if (LanguageComboBox != null)
                {
                    foreach (ComboBoxItem item in LanguageComboBox.Items)
                    {
                        string? tag = item.Tag?.ToString();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            item.Content = _translationService.GetTranslation($"Language_{tag}");
                        }
                    }
                }

                // Update buttons
                if (ResetButton != null)
                    ResetButton.Content = _translationService.GetTranslation("Settings_ResetToDefaults");
                if (CancelButton != null)
                    CancelButton.Content = _translationService.GetTranslation("Settings_Cancel");
                if (ApplyButton != null)
                    ApplyButton.Content = _translationService.GetTranslation("Settings_Apply");

                // Update all setting labels and checkboxes
                UpdateSettingsTranslations();
            }
            catch (Exception ex)
            {
                // Silently fail - translations are not critical
                System.Diagnostics.Debug.WriteLine($"Error applying translations: {ex.Message}");
            }
        }

        private void UpdateSettingsTranslations()
        {
            // Audio Settings Labels
            if (MasterVolumeLabel != null)
                MasterVolumeLabel.Text = _translationService.GetTranslation("Settings_MasterVolume");
            if (MusicVolumeLabel != null)
                MusicVolumeLabel.Text = _translationService.GetTranslation("Settings_MusicVolume");
            if (SoundEffectsVolumeLabel != null)
                SoundEffectsVolumeLabel.Text = _translationService.GetTranslation("Settings_SoundEffectsVolume");
            if (MuteMusicCheckBox != null)
                MuteMusicCheckBox.Content = _translationService.GetTranslation("Settings_MuteMusic");
            if (MuteSoundEffectsCheckBox != null)
                MuteSoundEffectsCheckBox.Content = _translationService.GetTranslation("Settings_MuteSoundEffects");

            // Text Settings Labels
            if (TextSpeedLabel != null)
                TextSpeedLabel.Text = _translationService.GetTranslation("Settings_TextSpeed");
            if (FontSizeLabel != null)
                FontSizeLabel.Text = _translationService.GetTranslation("Settings_FontSize");
            if (AutoAdvanceCheckBox != null)
                AutoAdvanceCheckBox.Content = _translationService.GetTranslation("Settings_AutoAdvanceText");
            if (AutoAdvanceDelayLabel != null)
                AutoAdvanceDelayLabel.Text = _translationService.GetTranslation("Settings_AutoAdvanceDelay");
            if (SkipUnreadTextCheckBox != null)
                SkipUnreadTextCheckBox.Content = _translationService.GetTranslation("Settings_SkipUnreadText");
            if (SkipReadTextCheckBox != null)
                SkipReadTextCheckBox.Content = _translationService.GetTranslation("Settings_SkipReadText");

            // Display Settings Labels
            if (FullscreenCheckBox != null)
                FullscreenCheckBox.Content = _translationService.GetTranslation("Settings_Fullscreen");
            if (WindowWidthLabel != null)
                WindowWidthLabel.Text = _translationService.GetTranslation("Settings_WindowWidth");
            if (WindowHeightLabel != null)
                WindowHeightLabel.Text = _translationService.GetTranslation("Settings_WindowHeight");
            if (VSyncCheckBox != null)
                VSyncCheckBox.Content = _translationService.GetTranslation("Settings_VSync");
            if (TargetFPSLabel != null)
                TargetFPSLabel.Text = _translationService.GetTranslation("Settings_TargetFPS");
            if (ShowFPSCheckBox != null)
                ShowFPSCheckBox.Content = _translationService.GetTranslation("Settings_ShowFPSCounter");

            // Graphics Settings Labels
            if (GraphicsQualityLabel != null)
                GraphicsQualityLabel.Text = _translationService.GetTranslation("Settings_GraphicsQuality");
            if (EnableEffectsCheckBox != null)
                EnableEffectsCheckBox.Content = _translationService.GetTranslation("Settings_EnableVisualEffects");
            if (EnableShadowsCheckBox != null)
                EnableShadowsCheckBox.Content = _translationService.GetTranslation("Settings_EnableShadows");
            if (BackgroundOpacityLabel != null)
                BackgroundOpacityLabel.Text = _translationService.GetTranslation("Settings_BackgroundOpacity");
            
            // Update Graphics Quality ComboBox items
            if (GraphicsQualityComboBox != null)
            {
                foreach (ComboBoxItem item in GraphicsQualityComboBox.Items)
                {
                    string? content = item.Content?.ToString();
                    if (content == "Low")
                        item.Content = _translationService.GetTranslation("Settings_Quality_Low");
                    else if (content == "Medium")
                        item.Content = _translationService.GetTranslation("Settings_Quality_Medium");
                    else if (content == "High")
                        item.Content = _translationService.GetTranslation("Settings_Quality_High");
                }
            }

            // Gameplay Settings Labels
            if (AutoSaveCheckBox != null)
                AutoSaveCheckBox.Content = _translationService.GetTranslation("Settings_EnableAutoSave");
            if (AutoSaveIntervalLabel != null)
                AutoSaveIntervalLabel.Text = _translationService.GetTranslation("Settings_AutoSaveInterval");
            if (ConfirmOnExitCheckBox != null)
                ConfirmOnExitCheckBox.Content = _translationService.GetTranslation("Settings_ConfirmOnExit");
            if (ShowSkipIndicatorCheckBox != null)
                ShowSkipIndicatorCheckBox.Content = _translationService.GetTranslation("Settings_ShowSkipIndicator");
            if (ShowLocationTimeCheckBox != null)
                ShowLocationTimeCheckBox.Content = _translationService.GetTranslation("Settings_ShowLocationTime");
            if (ShowChapterTitleCheckBox != null)
                ShowChapterTitleCheckBox.Content = _translationService.GetTranslation("Settings_ShowChapterTitle");

            // Controls Settings Labels
            if (SkipKeyLabel != null)
                SkipKeyLabel.Text = _translationService.GetTranslation("Settings_SkipKey");
            if (SaveKeyLabel != null)
                SaveKeyLabel.Text = _translationService.GetTranslation("Settings_SaveKey");
            if (LoadKeyLabel != null)
                LoadKeyLabel.Text = _translationService.GetTranslation("Settings_LoadKey");
            if (MenuKeyLabel != null)
                MenuKeyLabel.Text = _translationService.GetTranslation("Settings_MenuKey");
            if (AutoAdvanceToggleKeyLabel != null)
                AutoAdvanceToggleKeyLabel.Text = _translationService.GetTranslation("Settings_AutoAdvanceToggleKey");

            // Accessibility Settings Labels
            if (HighContrastCheckBox != null)
                HighContrastCheckBox.Content = _translationService.GetTranslation("Settings_HighContrastMode");
            if (LargeTextCheckBox != null)
                LargeTextCheckBox.Content = _translationService.GetTranslation("Settings_LargeTextMode");
            if (ReduceAnimationsCheckBox != null)
                ReduceAnimationsCheckBox.Content = _translationService.GetTranslation("Settings_ReduceAnimations");

            // Language Settings Labels
            if (LanguageLabel != null)
                LanguageLabel.Text = _translationService.GetTranslation("Settings_Language");
        }

        // ComboBox click handler to make entire area clickable
        private void ComboBoxArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                // Find the parent ComboBox
                var comboBox = FindParent<ComboBox>(border);
                if (comboBox != null && !comboBox.IsEditable)
                {
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    e.Handled = true;
                }
            }
        }

        private void SetupComboBoxFonts(System.Windows.Media.FontFamily font)
        {
            try
            {
                // Find all ComboBoxes and setup font application
                var comboBoxes = FindVisualChildren<ComboBox>(this);
                foreach (var comboBox in comboBoxes)
                {
                    comboBox.DropDownOpened += (s, e) =>
                    {
                        try
                        {
                            // Apply font to all ComboBoxItems when dropdown opens
                            for (int i = 0; i < comboBox.Items.Count; i++)
                            {
                                var container = comboBox.ItemContainerGenerator.ContainerFromIndex(i) as ComboBoxItem;
                                if (container != null)
                                {
                                    container.FontFamily = font;
                                    container.Cursor = null; // Use custom cursor
                                    ApplyFontToChildren(container, font);
                                }
                            }
                            
                            // Set cursor on popup elements
                            var popup = FindVisualChild<Popup>(comboBox);
                            if (popup != null && popup.Child is FrameworkElement popupChild)
                            {
                                popupChild.Cursor = null;
                                ApplyCursorToChildren(popupChild, null);
                            }
                        }
                        catch { }
                    };
                }
            }
            catch { }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // Slider track dragging support
        private static Slider? _draggingSlider = null;
        private static bool _isDragging = false;
        private static Track? _draggingTrack = null;

        private void SliderTrack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is RepeatButton button)
            {
                var slider = FindParent<Slider>(button);
                var track = FindParent<Track>(button);
                if (slider != null && track != null)
                {
                    _draggingSlider = slider;
                    _draggingTrack = track;
                    _isDragging = true;
                    
                    // Capture mouse on the slider itself for better tracking
                    slider.CaptureMouse();
                    
                    // Update value to click position immediately
                    Point position = e.GetPosition(track);
                    UpdateSliderValueFromPosition(slider, track, position);
                    
                    e.Handled = true;
                }
            }
        }

        private void SliderTrack_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggingSlider != null && _draggingTrack != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point position = e.GetPosition(_draggingTrack);
                    UpdateSliderValueFromPosition(_draggingSlider, _draggingTrack, position);
                    e.Handled = true;
                }
                else
                {
                    // Mouse button released
                    if (_draggingSlider != null)
                    {
                        _draggingSlider.ReleaseMouseCapture();
                    }
                    _isDragging = false;
                    _draggingSlider = null;
                    _draggingTrack = null;
                }
            }
        }

        private void SliderTrack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingSlider != null)
            {
                _draggingSlider.ReleaseMouseCapture();
            }
            _isDragging = false;
            _draggingSlider = null;
            _draggingTrack = null;
        }

        private void Slider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggingSlider != null && _draggingTrack != null && sender == _draggingSlider)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Point position = e.GetPosition(_draggingTrack);
                    UpdateSliderValueFromPosition(_draggingSlider, _draggingTrack, position);
                    e.Handled = true;
                }
            }
        }

        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingSlider != null && sender == _draggingSlider)
            {
                _draggingSlider.ReleaseMouseCapture();
                _isDragging = false;
                _draggingSlider = null;
                _draggingTrack = null;
            }
        }

        private void Slider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider && slider == _draggingSlider)
            {
                _isDragging = false;
                _draggingSlider = null;
                _draggingTrack = null;
            }
        }

        private void UpdateSliderValueFromPosition(Slider slider, Track track, Point position)
        {
            if (track.ActualWidth > 0 && track.ActualHeight > 0)
            {
                double percentage = 0;
                
                if (slider.Orientation == Orientation.Horizontal)
                {
                    percentage = Math.Max(0, Math.Min(1, position.X / track.ActualWidth));
                }
                else
                {
                    percentage = Math.Max(0, Math.Min(1, 1 - (position.Y / track.ActualHeight)));
                }
                
                double newValue = slider.Minimum + (percentage * (slider.Maximum - slider.Minimum));
                slider.Value = newValue;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void ApplyCursorToChildren(DependencyObject parent, System.Windows.Input.Cursor? cursor)
        {
            try
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    
                    if (child is FrameworkElement element)
                    {
                        element.Cursor = cursor;
                    }
                    
                    // Recursively apply to children
                    ApplyCursorToChildren(child, cursor);
                }
            }
            catch { }
        }
    }
}

