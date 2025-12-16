using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VisualNovel.Services
{
    public static class ImageHelper
    {
        // Cache for processed images to avoid reprocessing on every dialogue change
        private static readonly Dictionary<string, BitmapSource> _imageCache = new Dictionary<string, BitmapSource>();
        private static readonly Dictionary<string, DateTime> _fileTimestamps = new Dictionary<string, DateTime>();

        /// <summary>
        /// Loads an image and removes visual artifacts around transparent backgrounds.
        /// This removes white/colored halos that appear around edges of transparent images.
        /// Uses caching to avoid reprocessing the same images.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>Processed BitmapSource with artifacts removed, or null if loading fails</returns>
        public static BitmapSource? LoadImageWithoutArtifacts(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return null;
                }

                // Check cache first
                var fileInfo = new FileInfo(imagePath);
                if (_imageCache.TryGetValue(imagePath, out BitmapSource? cachedImage))
                {
                    // Verify file hasn't changed by checking timestamp
                    if (_fileTimestamps.TryGetValue(imagePath, out DateTime cachedTimestamp) &&
                        fileInfo.LastWriteTime == cachedTimestamp)
                    {
                        return cachedImage;
                    }
                    else
                    {
                        // File changed, remove from cache
                        _imageCache.Remove(imagePath);
                        _fileTimestamps.Remove(imagePath);
                    }
                }

                // Load the original image
                var originalBitmap = new BitmapImage();
                originalBitmap.BeginInit();
                originalBitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
                originalBitmap.EndInit();
                originalBitmap.Freeze();

                // Convert to a format we can manipulate (BGRA32 with premultiplied alpha)
                var formatConverted = new FormatConvertedBitmap(originalBitmap, PixelFormats.Bgra32, null, 0);
                formatConverted.Freeze();

                // Create a writable copy
                int stride = (formatConverted.PixelWidth * formatConverted.Format.BitsPerPixel + 7) / 8;
                byte[] pixels = new byte[stride * formatConverted.PixelHeight];
                formatConverted.CopyPixels(pixels, stride, 0);

                // Process pixels to remove edge artifacts
                RemoveEdgeArtifacts(pixels, formatConverted.PixelWidth, formatConverted.PixelHeight, stride);

                // Create a new bitmap from the processed pixels
                var processedBitmap = BitmapSource.Create(
                    formatConverted.PixelWidth,
                    formatConverted.PixelHeight,
                    formatConverted.DpiX,
                    formatConverted.DpiY,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    stride);

                processedBitmap.Freeze();
                
                // Cache the processed image
                _imageCache[imagePath] = processedBitmap;
                _fileTimestamps[imagePath] = fileInfo.LastWriteTime;
                
                return processedBitmap;
            }
            catch (Exception)
            {
                // Log error if needed, but return null to allow fallback
                return null;
            }
        }

        /// <summary>
        /// Clears the image cache. Useful if memory becomes an issue.
        /// </summary>
        public static void ClearCache()
        {
            _imageCache.Clear();
            _fileTimestamps.Clear();
        }

        /// <summary>
        /// Removes edge artifacts from pixel data by detecting and removing white/colored halos
        /// around transparent edges.
        /// </summary>
        private static void RemoveEdgeArtifacts(byte[] pixels, int width, int height, int stride)
        {
            // Thresholds for artifact detection
            const byte alphaThreshold = 15; // Pixels with alpha below this are considered transparent
            const byte brightnessThreshold = 180; // Bright pixels near edges are likely artifacts
            const byte whiteThreshold = 200; // Very white pixels (R, G, B all high) are likely artifacts
            const int edgeRadius = 3; // Check pixels within this radius

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    byte b = pixels[index];
                    byte g = pixels[index + 1];
                    byte r = pixels[index + 2];
                    byte a = pixels[index + 3];

                    // Skip fully opaque pixels (not edges)
                    if (a == 255)
                        continue;

                    // For fully transparent or very transparent pixels, ensure they're completely transparent
                    if (a < alphaThreshold)
                    {
                        pixels[index] = 0;     // B
                        pixels[index + 1] = 0;  // G
                        pixels[index + 2] = 0;  // R
                        pixels[index + 3] = 0;  // A
                        continue;
                    }

                    // Check if this is an edge pixel (semi-transparent)
                    // and if it's near a fully transparent area
                    bool isNearTransparent = false;
                    int transparentNeighbors = 0;
                    
                    for (int dy = -edgeRadius; dy <= edgeRadius; dy++)
                    {
                        for (int dx = -edgeRadius; dx <= edgeRadius; dx++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                continue;

                            int neighborIndex = ny * stride + nx * 4;
                            byte neighborAlpha = pixels[neighborIndex + 3];
                            
                            if (neighborAlpha < alphaThreshold)
                            {
                                isNearTransparent = true;
                                transparentNeighbors++;
                            }
                        }
                    }

                    // If this pixel is near transparent area, check if it's an artifact
                    if (isNearTransparent)
                    {
                        // Calculate brightness
                        byte brightness = (byte)((r + g + b) / 3);
                        
                        // Check if it's very white (all RGB channels are high)
                        bool isVeryWhite = r > whiteThreshold && g > whiteThreshold && b > whiteThreshold;
                        
                        // Check if it's bright overall
                        bool isBright = brightness > brightnessThreshold;
                        
                        // If it's a bright/white pixel near transparent edges, it's likely an artifact
                        if (isVeryWhite || (isBright && transparentNeighbors > 2))
                        {
                            // Remove the artifact by making it fully transparent
                            pixels[index] = 0;     // B
                            pixels[index + 1] = 0;  // G
                            pixels[index + 2] = 0;  // R
                            pixels[index + 3] = 0;  // A
                        }
                        // For other semi-transparent edge pixels with low alpha, reduce their visibility
                        else if (a < 100 && transparentNeighbors > 1)
                        {
                            // Further reduce alpha for very transparent edge pixels near many transparent neighbors
                            pixels[index + 3] = (byte)(a * 0.6);
                        }
                    }
                }
            }
        }
    }
}

