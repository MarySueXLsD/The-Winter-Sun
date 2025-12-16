using System;
using System.IO;
using System.Windows.Controls;

namespace VisualNovel.Services
{
    /// <summary>
    /// Service for playing sound effects
    /// </summary>
    public class SoundService
    {
        private readonly string _clickSoundPath;

        public SoundService()
        {
            _clickSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "main_menu_clicks.mp3");
        }

        /// <summary>
        /// Plays the click sound effect
        /// </summary>
        public void PlayClickSound()
        {
            try
            {
                if (!File.Exists(_clickSoundPath))
                {
                    return; // Silently fail if file doesn't exist
                }

                // Get volume from config
                var configService = new ConfigService();
                var config = configService.GetConfig();
                double soundEffectsVolume = config.MuteSoundEffects ? 0 : config.SoundEffectsVolume * config.MasterVolume;

                // Create a new MediaElement for each sound to allow overlapping sounds
                var soundPlayer = new MediaElement
                {
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Manual,
                    Volume = soundEffectsVolume,
                    Source = new Uri(_clickSoundPath, UriKind.Absolute)
                };

                soundPlayer.MediaEnded += (s, e) =>
                {
                    soundPlayer.Close();
                };

                soundPlayer.Play();
            }
            catch
            {
                // Silently fail on errors
            }
        }
    }
}

