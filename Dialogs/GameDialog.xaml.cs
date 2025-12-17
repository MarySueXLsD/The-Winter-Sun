using System;
using System.Windows;
using System.Windows.Media;
using VisualNovel.Services;

namespace VisualNovel.Dialogs
{
    public partial class GameDialog : Window
    {
        private readonly SoundService _soundService;
        private readonly TranslationService _translationService;
        public new bool? DialogResult { get; private set; }

        public GameDialog(string title, string content, DialogType type = DialogType.OK)
        {
            InitializeComponent();
            _soundService = new SoundService();
            _translationService = TranslationService.Instance;
            
            // Load Dialogues Latin font with fallback
            var dialogueFont = FontHelper.LoadDialogueFontWithFallback();
            Resources["MinecraftFont"] = dialogueFont;
            
            // Apply font directly to text elements
            DialogTitle.FontFamily = dialogueFont;
            DialogContent.FontFamily = dialogueFont;

            // Apply font to buttons
            OKButton.FontFamily = dialogueFont;
            YesButton.FontFamily = dialogueFont;
            NoButton.FontFamily = dialogueFont;
            CancelButton.FontFamily = dialogueFont;
            
            // Load custom cursor
            LoadCustomCursor();
            
            DialogTitle.Text = title;
            DialogContent.Text = content;
            
            // Apply translations to buttons
            ApplyTranslations();

            // Configure buttons based on dialog type
            switch (type)
            {
                case DialogType.OK:
                    OKButton.Visibility = Visibility.Visible;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;
                    break;
                case DialogType.YesNo:
                    OKButton.Visibility = Visibility.Collapsed;
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Collapsed;
                    break;
                case DialogType.YesNoCancel:
                    OKButton.Visibility = Visibility.Collapsed;
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            DialogResult = true;
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            DialogResult = true;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            DialogResult = false;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            DialogResult = null;
            this.Close();
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
            catch
            {
                // Silently fail if cursor can't be loaded
            }
        }

        private void ApplyTranslations()
        {
            try
            {
                OKButton.Content = _translationService.GetTranslation("Dialog_OK");
                YesButton.Content = _translationService.GetTranslation("Dialog_Yes");
                NoButton.Content = _translationService.GetTranslation("Dialog_No");
                CancelButton.Content = _translationService.GetTranslation("Dialog_Cancel");
            }
            catch
            {
                // Silently fail if translations can't be applied
            }
        }
    }

    public enum DialogType
    {
        OK,
        YesNo,
        YesNoCancel
    }
}

