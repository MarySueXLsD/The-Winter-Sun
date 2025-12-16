using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VisualNovel
{
    /// <summary>
    /// Fast animated image control with chroma key (black to transparent).
    /// Uses parallel processing to load and process all frames quickly.
    /// </summary>
    public class AnimatedChromaKeyImage : Image
    {
        private DispatcherTimer? _animationTimer;
        private BitmapSource[]? _frames;
        private int _currentFrameIndex = 0;
        private bool _isLoading = false;
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "video_debug.log");
        
        // Static cache for frames (keyed by directory path)
        private static readonly Dictionary<string, BitmapSource[]> _frameCache = new Dictionary<string, BitmapSource[]>();
        private static readonly object _cacheLock = new object();

        public AnimatedChromaKeyImage()
        {
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Start loading if path is set
            if (!string.IsNullOrEmpty(FramesPath) && _frames == null && !_isLoading)
            {
                _ = LoadFramesAsync();
            }
        }

        #region Dependency Properties

        public static readonly DependencyProperty FramesPathProperty =
            DependencyProperty.Register(nameof(FramesPath), typeof(string), typeof(AnimatedChromaKeyImage),
                new PropertyMetadata(null, OnFramesPathChanged));

        public static readonly DependencyProperty ColorKeyProperty =
            DependencyProperty.Register(nameof(ColorKey), typeof(Color), typeof(AnimatedChromaKeyImage),
                new PropertyMetadata(Colors.Black));

        public static readonly DependencyProperty ToleranceProperty =
            DependencyProperty.Register(nameof(Tolerance), typeof(double), typeof(AnimatedChromaKeyImage),
                new PropertyMetadata(0.3));

        public static readonly DependencyProperty FrameRateProperty =
            DependencyProperty.Register(nameof(FrameRate), typeof(double), typeof(AnimatedChromaKeyImage),
                new PropertyMetadata(30.0, OnFrameRateChanged));

        public string? FramesPath
        {
            get => (string?)GetValue(FramesPathProperty);
            set => SetValue(FramesPathProperty, value);
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

        public double FrameRate
        {
            get => (double)GetValue(FrameRateProperty);
            set => SetValue(FrameRateProperty, value);
        }

        #endregion

        #region Property Change Handlers

        private static void OnFramesPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedChromaKeyImage control && e.NewValue is string path && !string.IsNullOrEmpty(path))
            {
                LogToFile($"AnimatedChromaKeyImage: FramesPath changed to: {path}");
                _ = control.LoadFramesAsync();
            }
        }

        private static void OnFrameRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AnimatedChromaKeyImage control)
            {
                control.UpdateTimerInterval();
            }
        }

        #endregion

        private async Task LoadFramesAsync()
        {
            if (string.IsNullOrEmpty(FramesPath) || _isLoading)
                return;

            _isLoading = true;
            var startTime = DateTime.Now;

            // Capture color key values on UI thread
            byte keyR = ColorKey.R;
            byte keyG = ColorKey.G;
            byte keyB = ColorKey.B;
            int tolerance = (int)(Tolerance * 255.0);

            try
            {
                // Resolve path
                string directory = Path.IsPathRooted(FramesPath) 
                    ? FramesPath 
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FramesPath);

                LogToFile($"AnimatedChromaKeyImage: Loading frames from: {directory}");

                // Check cache first
                lock (_cacheLock)
                {
                    if (_frameCache.TryGetValue(directory, out var cached) && cached.Length > 0)
                    {
                        var cacheTime = (DateTime.Now - startTime).TotalMilliseconds;
                        LogToFile($"AnimatedChromaKeyImage: Using {cached.Length} cached frames (took {cacheTime:F0}ms)");
                        _frames = cached;
                        _currentFrameIndex = 0;
                        Source = _frames[0];
                        Visibility = Visibility.Visible;
                        StartAnimation();
                        _isLoading = false;
                        return;
                    }
                }

                if (!Directory.Exists(directory))
                {
                    LogToFile($"AnimatedChromaKeyImage: Directory not found: {directory}");
                    Visibility = Visibility.Collapsed;
                    _isLoading = false;
                    return;
                }

                // Get frame files
                var frameFiles = Directory.GetFiles(directory, "frame_*.png")
                    .OrderBy(f => f)
                    .ToArray();

                if (frameFiles.Length == 0)
                {
                    LogToFile($"AnimatedChromaKeyImage: No frame files found");
                    Visibility = Visibility.Collapsed;
                    _isLoading = false;
                    return;
                }

                LogToFile($"AnimatedChromaKeyImage: Found {frameFiles.Length} frames, loading progressively...");

                // Load first frame immediately on UI thread for instant display
                var firstFrame = await Task.Run(() => LoadAndProcessSingleFrame(frameFiles[0], keyR, keyG, keyB, tolerance));
                if (firstFrame != null)
                {
                    Source = firstFrame;
                    Visibility = Visibility.Visible;
                    LogToFile($"AnimatedChromaKeyImage: First frame displayed in {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");
                }

                // Load remaining frames in background with lower priority
                var loadedFrames = await Task.Run(() => LoadAndProcessFramesParallel(frameFiles, keyR, keyG, keyB, tolerance));

                if (loadedFrames == null || loadedFrames.Length == 0)
                {
                    LogToFile($"AnimatedChromaKeyImage: Failed to load any frames");
                    Visibility = Visibility.Collapsed;
                    _isLoading = false;
                    return;
                }

                // Cache
                lock (_cacheLock)
                {
                    _frameCache[directory] = loadedFrames;
                }

                _frames = loadedFrames;
                _currentFrameIndex = 0;

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                LogToFile($"AnimatedChromaKeyImage: Loaded and processed {loadedFrames.Length} frames in {elapsed:F0}ms");

                // Start animation (first frame already shown)
                StartAnimation();
                
                FramesLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogToFile($"AnimatedChromaKeyImage: Error loading frames: {ex.Message}");
                Visibility = Visibility.Collapsed;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private BitmapSource[] LoadAndProcessFramesParallel(string[] frameFiles, byte keyR, byte keyG, byte keyB, int tolerance)
        {
            var frames = new BitmapSource?[frameFiles.Length];
            
            // Use half CPU cores to avoid starving UI thread
            int maxParallelism = Math.Max(1, Environment.ProcessorCount / 2);
            
            Parallel.For(0, frameFiles.Length, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, i =>
            {
                try
                {
                    frames[i] = LoadAndProcessSingleFrame(frameFiles[i], keyR, keyG, keyB, tolerance);
                }
                catch (Exception ex)
                {
                    LogToFile($"AnimatedChromaKeyImage: Error loading frame {i}: {ex.Message}");
                }
            });

            // Filter out any null entries (failed loads) and return
            return frames.Where(f => f != null).Cast<BitmapSource>().ToArray();
        }

        private BitmapSource? LoadAndProcessSingleFrame(string filePath, byte keyR, byte keyG, byte keyB, int tolerance)
        {
            try
            {
                // Read file bytes
                var bytes = File.ReadAllBytes(filePath);
                
                    // Decode image at reduced resolution for speed
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(bytes);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bitmap.DecodePixelWidth = 800; // Reduced for speed
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    // Convert to Bgra32 for processing
                    var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                
                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                converted.CopyPixels(pixels, stride, 0);
                
                // Apply chroma key - make matching pixels transparent
                // Process in chunks for better cache performance
                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = rowOffset + x * 4;
                        
                        byte b = pixels[idx];
                        byte g = pixels[idx + 1];
                        byte r = pixels[idx + 2];
                        
                        // Check if pixel matches key color within tolerance
                        int diffR = r - keyR;
                        int diffG = g - keyG;
                        int diffB = b - keyB;
                        
                        // Use squared distance for speed (avoid Math.Abs)
                        if (diffR < 0) diffR = -diffR;
                        if (diffG < 0) diffG = -diffG;
                        if (diffB < 0) diffB = -diffB;
                        
                        int maxDiff = diffR > diffG ? diffR : diffG;
                        if (diffB > maxDiff) maxDiff = diffB;
                        
                        if (maxDiff <= tolerance)
                        {
                            // Make transparent
                            pixels[idx + 3] = 0;
                        }
                    }
                }
                
                // Create result bitmap
                var result = BitmapSource.Create(
                    width, height, 96, 96,
                    PixelFormats.Bgra32, null,
                    pixels, stride);
                result.Freeze();
                
                return result;
            }
            catch
            {
                return null;
            }
        }

        private void StartAnimation()
        {
            if (_frames == null || _frames.Length == 0)
                return;

            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Tick -= OnAnimationTick;
            }

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / FrameRate)
            };
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();

            LogToFile($"AnimatedChromaKeyImage: Animation started at {FrameRate} FPS with {_frames.Length} frames");
        }

        private void UpdateTimerInterval()
        {
            if (_animationTimer != null)
            {
                _animationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / FrameRate);
            }
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            if (_frames == null || _frames.Length == 0)
                return;

            _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Length;
            Source = _frames[_currentFrameIndex];
        }

        public void Play()
        {
            if (_frames != null && _frames.Length > 0)
            {
                StartAnimation();
            }
            else if (!string.IsNullOrEmpty(FramesPath) && !_isLoading)
            {
                _ = LoadFramesAsync();
            }
        }

        public void Stop()
        {
            _animationTimer?.Stop();
        }

        public bool IsFramesLoaded => _frames != null && _frames.Length > 0;

        public event EventHandler? FramesLoaded;

        /// <summary>
        /// Check if frames are cached for a given path
        /// </summary>
        public static bool AreFramesCached(string framesPath)
        {
            if (string.IsNullOrEmpty(framesPath))
                return false;
            
            string resolvedPath = Path.IsPathRooted(framesPath)
                ? framesPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, framesPath);
            
            lock (_cacheLock)
            {
                return _frameCache.TryGetValue(resolvedPath, out var frames) && frames != null && frames.Length > 0;
            }
        }

        /// <summary>
        /// Pre-load frames in background so they're ready when needed.
        /// Call this from main menu to eliminate loading time on chapter screen.
        /// </summary>
        public static void PreloadFramesAsync(string framesPath, Color colorKey, double tolerance)
        {
            if (string.IsNullOrEmpty(framesPath) || AreFramesCached(framesPath))
                return;

            string directory = Path.IsPathRooted(framesPath)
                ? framesPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, framesPath);

            if (!Directory.Exists(directory))
                return;

            byte keyR = colorKey.R;
            byte keyG = colorKey.G;
            byte keyB = colorKey.B;
            int tol = (int)(tolerance * 255.0);

            // Load in background with low priority
            Task.Run(() =>
            {
                try
                {
                    var startTime = DateTime.Now;
                    var frameFiles = Directory.GetFiles(directory, "frame_*.png").OrderBy(f => f).ToArray();
                    if (frameFiles.Length == 0) return;

                    var frames = new BitmapSource?[frameFiles.Length];
                    int maxParallelism = Math.Max(1, Environment.ProcessorCount / 2);

                    Parallel.For(0, frameFiles.Length, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, i =>
                    {
                        frames[i] = LoadAndProcessSingleFrameStatic(frameFiles[i], keyR, keyG, keyB, tol);
                    });

                    var result = frames.Where(f => f != null).Cast<BitmapSource>().ToArray();
                    if (result.Length > 0)
                    {
                        lock (_cacheLock)
                        {
                            _frameCache[directory] = result;
                        }
                        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                        LogToFile($"AnimatedChromaKeyImage: Pre-loaded {result.Length} frames in {elapsed:F0}ms");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"AnimatedChromaKeyImage: Pre-load error: {ex.Message}");
                }
            });
        }

        private static BitmapSource? LoadAndProcessSingleFrameStatic(string filePath, byte keyR, byte keyG, byte keyB, int tolerance)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.DecodePixelWidth = 800;
                bitmap.EndInit();
                bitmap.Freeze();

                var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];
                converted.CopyPixels(pixels, stride, 0);

                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = rowOffset + x * 4;
                        byte b = pixels[idx];
                        byte g = pixels[idx + 1];
                        byte r = pixels[idx + 2];

                        int diffR = r - keyR; if (diffR < 0) diffR = -diffR;
                        int diffG = g - keyG; if (diffG < 0) diffG = -diffG;
                        int diffB = b - keyB; if (diffB < 0) diffB = -diffB;
                        int maxDiff = diffR > diffG ? diffR : diffG;
                        if (diffB > maxDiff) maxDiff = diffB;

                        if (maxDiff <= tolerance) pixels[idx + 3] = 0;
                    }
                }

                var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
                result.Freeze();
                return result;
            }
            catch { return null; }
        }

        private static void LogToFile(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(LogFilePath, $"[{timestamp}] {message}" + Environment.NewLine);
            }
            catch { }
        }
    }
}





