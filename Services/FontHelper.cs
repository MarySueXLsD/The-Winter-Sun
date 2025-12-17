using System;
using System.IO;
using System.Windows.Media;

namespace VisualNovel.Services
{
    public static class FontHelper
    {
        private static FontFamily? _cachedDialogueFont = null;
        private static FontFamily? _cachedDialogueFontWithFallback = null;

        public static FontFamily? LoadDialogueFont()
        {
            if (_cachedDialogueFont != null)
                return _cachedDialogueFont;

            try
            {
                string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "Dialogues_latin.ttf");
                
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
                    string[] possibleNames = { "Dialogues Latin", "Dialogues_latin", "Dialogues", "Dialogues-Latin" };
                    
                    foreach (var name in possibleNames)
                    {
                        try
                        {
                            fontFamily = new FontFamily(fontUri, $"./#{name}");
                            // Verify it works by checking typefaces
                            var typefaces = fontFamily.GetTypefaces();
                            if (typefaces != null && typefaces.Count > 0)
                            {
                                _cachedDialogueFont = fontFamily;
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
                        _cachedDialogueFont = fontFamily;
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
                        fontFamily = new FontFamily("./Assets/Fonts/#Dialogues Latin");
                        _cachedDialogueFont = fontFamily;
                        return fontFamily;
                    }
                    catch
                    {
                        // Method 2 also failed
                    }
                }
                
                _cachedDialogueFont = fontFamily;
                return fontFamily;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Loads Dialogues Latin font with Arial as fallback for unsupported characters
        /// WPF will automatically fallback to system fonts for characters not in the font
        /// This method returns Dialogues Latin font, and WPF handles fallback automatically
        /// </summary>
        public static FontFamily LoadDialogueFontWithFallback()
        {
            if (_cachedDialogueFontWithFallback != null)
                return _cachedDialogueFontWithFallback;

            var dialogueFont = LoadDialogueFont();
            if (dialogueFont != null)
            {
                // WPF automatically falls back to system fonts for unsupported characters
                // We return the Dialogues Latin font and WPF handles the fallback
                _cachedDialogueFontWithFallback = dialogueFont;
                return dialogueFont;
            }
            
            // If Dialogues Latin font not available, return Arial
            _cachedDialogueFontWithFallback = new FontFamily("Arial");
            return _cachedDialogueFontWithFallback;
        }

        // Legacy method names for backward compatibility
        public static FontFamily? LoadMinecraftFont() => LoadDialogueFont();
        public static FontFamily LoadMinecraftFontWithFallback() => LoadDialogueFontWithFallback();
    }
}

