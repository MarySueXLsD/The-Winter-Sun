namespace VisualNovel.Models
{
    public class GameConfig
    {
        // Audio Settings
        public double MasterVolume { get; set; } = 1.0;
        public double MusicVolume { get; set; } = 0.5;
        public double SoundEffectsVolume { get; set; } = 0.7;
        public bool MuteMusic { get; set; } = false;
        public bool MuteSoundEffects { get; set; } = false;

        // Text Settings
        public int TextSpeed { get; set; } = 30; // milliseconds per character (lower = faster)
        public int FontSize { get; set; } = 32;
        public bool AutoAdvance { get; set; } = false;
        public int AutoAdvanceDelay { get; set; } = 3000; // milliseconds
        public bool SkipUnreadText { get; set; } = true;
        public bool SkipReadText { get; set; } = false;

        // Display Settings
        public bool Fullscreen { get; set; } = true;
        public int WindowWidth { get; set; } = 1920;
        public int WindowHeight { get; set; } = 1080;
        public bool VSync { get; set; } = true;
        public int TargetFPS { get; set; } = 60;
        public bool ShowFPS { get; set; } = true;

        // Optimization Settings (formerly Graphics)
        public string GraphicsQuality { get; set; } = "High"; // Low, Medium, High
        public bool EnableEffects { get; set; } = true;
        public bool EnableShadows { get; set; } = true;
        public double BackgroundOpacity { get; set; } = 0.7;
        
        // Parallax and Movement Settings
        public bool EnableParallax { get; set; } = true;
        public bool EnableMovingSprites { get; set; } = true;
        public double ParallaxIntensity { get; set; } = 1.0; // 0.0 to 2.0 (multiplier for parallax movement)
        public double CursorMovementSensitivity { get; set; } = 1.0; // 0.0 to 2.0 (how much cursor movement affects parallax)
        
        // FPS Optimization Settings
        public bool ReduceParticleEffects { get; set; } = false;
        public bool LowerAnimationQuality { get; set; } = false;
        public bool DisableBackgroundVideos { get; set; } = false;
        public bool ReduceTextureQuality { get; set; } = false;
        public bool ReduceUIEffects { get; set; } = false;
        public int ParallaxUpdateRate { get; set; } = 16; // milliseconds between parallax updates (lower = smoother but more CPU)

        // Gameplay Settings
        public bool AutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 300; // seconds
        public bool ConfirmOnExit { get; set; } = true;
        public bool ShowSkipIndicator { get; set; } = true;
        public bool ShowLocationTime { get; set; } = true;
        public bool ShowChapterTitle { get; set; } = true;

        // Key Bindings
        public string SkipKey { get; set; } = "Enter";
        public string SaveKey { get; set; } = "Ctrl+S";
        public string LoadKey { get; set; } = "Ctrl+L";
        public string MenuKey { get; set; } = "Escape";
        public string AutoAdvanceToggleKey { get; set; } = "A";

        // Language Settings
        public string Language { get; set; } = "English";
        public bool ShowSubtitles { get; set; } = true;

        // Accessibility Settings
        public bool HighContrast { get; set; } = false;
        public bool LargeText { get; set; } = false;
        public bool ReduceAnimations { get; set; } = false;
    }
}

