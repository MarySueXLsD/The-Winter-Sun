using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VisualNovel
{
    /// <summary>
    /// Custom element that renders a MediaElement with chroma key (black to transparent)
    /// Uses CompositionTarget.Rendering to capture and process frames
    /// </summary>
    public class ChromaKeyVideoElement : Border
    {
        private MediaElement? _mediaElement;
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "video_debug.log");
        
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(ChromaKeyVideoElement),
                new PropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty ColorKeyProperty =
            DependencyProperty.Register(nameof(ColorKey), typeof(Color), typeof(ChromaKeyVideoElement),
                new PropertyMetadata(Colors.Black));

        public static readonly DependencyProperty ToleranceProperty =
            DependencyProperty.Register(nameof(Tolerance), typeof(double), typeof(ChromaKeyVideoElement),
                new PropertyMetadata(0.3));

        public Uri? Source
        {
            get => (Uri?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Color ColorKey
        {
            get => (Color)GetValue(ColorKeyProperty);
            set => SetValue(ColorKeyProperty, value);
        }

        public double Tolerance
        {
            get => (double)GetValue(ToleranceProperty);
            set => SetValue(ToleranceProperty, value);
        }

        public ChromaKeyVideoElement()
        {
            ClipToBounds = true;
            
            // Initialize MediaElement - make it visible
            _mediaElement = new MediaElement
            {
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Stretch = Stretch.UniformToFill,
                Visibility = Visibility.Visible
            };
            
            _mediaElement.MediaOpened += MediaElement_MediaOpened;
            _mediaElement.MediaFailed += MediaElement_MediaFailed;
            _mediaElement.MediaEnded += MediaElement_MediaEnded;
            
            // Set MediaElement as child
            Child = _mediaElement;
            
            // Subscribe to rendering events for frame processing
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

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

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChromaKeyVideoElement control)
            {
                control.LoadVideo();
            }
        }

        private void LoadVideo()
        {
            if (Source == null || _mediaElement == null) return;

            try
            {
                LogToFile($"ChromaKeyVideoElement: Loading video from {Source}");
                _mediaElement.Source = Source;
            }
            catch (Exception ex)
            {
                LogToFile($"ChromaKeyVideoElement: Error loading video: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error loading video: {ex.Message}");
            }
        }

        private void MediaElement_MediaOpened(object? sender, RoutedEventArgs e)
        {
            if (_mediaElement == null) return;
            
            LogToFile($"ChromaKeyVideoElement: Media opened, starting playback. Size: {_mediaElement.NaturalVideoWidth}x{_mediaElement.NaturalVideoHeight}");
            _mediaElement.Play();
        }

        private void MediaElement_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            LogToFile($"ChromaKeyVideoElement: Media failed: {e.ErrorException?.Message}");
            System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorException?.Message}");
        }

        private void MediaElement_MediaEnded(object? sender, RoutedEventArgs e)
        {
            // Loop the video
            if (_mediaElement != null)
            {
                _mediaElement.Position = TimeSpan.Zero;
                _mediaElement.Play();
            }
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            // For now, just let MediaElement render directly
            // We'll add chroma key processing later if needed
            // MediaElement doesn't support RenderTargetBitmap capture, so we'll use it directly
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            
            if (Parent == null)
            {
                // Cleanup when removed from visual tree
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                Stop();
            }
        }

        public void Play()
        {
            _mediaElement?.Play();
        }

        public void Pause()
        {
            _mediaElement?.Pause();
        }

        public void Stop()
        {
            _mediaElement?.Stop();
        }
    }
}
