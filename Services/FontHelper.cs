using System;
using System.IO;
using System.Windows.Media;

namespace VisualNovel.Services
{
    public static class FontHelper
    {
        private static FontFamily? _cachedMinecraftFont = null;
        private static FontFamily? _cachedMinecraftFontWithFallback = null;

        public static FontFamily? LoadMinecraftFont()
        {
            if (_cachedMinecraftFont != null)
                return _cachedMinecraftFont;

            try
            {
                string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Minecraft.ttf");
                
                if (!File.Exists(fontPath))
                {
                    return null;
                }
                
                FontFamily? fontFamily = null;
                
                // Method 1: Try with absolute URI and font family name
                try
                {
                    var fontUri = new Uri(fontPath, UriKind.Absolute);
                    // Try common font family names that might be in the TTF file
                    string[] possibleNames = { "Minecraft", "Minecraft Regular", "Minecraft-Regular" };
                    
                    foreach (var name in possibleNames)
                    {
                        try
                        {
                            fontFamily = new FontFamily(fontUri, $"./#{name}");
                            // Verify it works by checking typefaces
                            var typefaces = fontFamily.GetTypefaces();
                            if (typefaces != null && typefaces.Count > 0)
                            {
                                _cachedMinecraftFont = fontFamily;
                                return fontFamily;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    // If none of the names worked, try without specifying family name
                    if (fontFamily == null)
                    {
                        fontFamily = new FontFamily(fontUri.ToString());
                        _cachedMinecraftFont = fontFamily;
                        return fontFamily;
                    }
                }
                catch
                {
                    // Method 1 failed, try Method 2
                }
                
                // Method 2: Try relative path format
                if (fontFamily == null)
                {
                    try
                    {
                        fontFamily = new FontFamily("./Assets/Fonts/#Minecraft");
                        _cachedMinecraftFont = fontFamily;
                        return fontFamily;
                    }
                    catch
                    {
                        // Method 2 also failed
                    }
                }
                
                _cachedMinecraftFont = fontFamily;
                return fontFamily;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads Minecraft font with Arial as fallback for unsupported characters
        /// WPF will automatically fallback to system fonts for characters not in Minecraft font
        /// This method returns Minecraft font, and WPF handles fallback automatically
        /// </summary>
        public static FontFamily LoadMinecraftFontWithFallback()
        {
            if (_cachedMinecraftFontWithFallback != null)
                return _cachedMinecraftFontWithFallback;

            var minecraftFont = LoadMinecraftFont();
            if (minecraftFont != null)
            {
                // WPF automatically falls back to system fonts for unsupported characters
                // We return the Minecraft font and WPF handles the fallback
                _cachedMinecraftFontWithFallback = minecraftFont;
                return minecraftFont;
            }
            
            // If Minecraft font not available, return Arial
            _cachedMinecraftFontWithFallback = new FontFamily("Arial");
            return _cachedMinecraftFontWithFallback;
        }
    }
}

