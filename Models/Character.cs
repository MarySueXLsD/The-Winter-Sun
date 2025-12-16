namespace VisualNovel.Models
{
    /// <summary>
    /// Represents a character with all their mood images
    /// </summary>
    public class Character
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = ""; // Translation key for character name
        public string NormalImage { get; set; } = "";
        public string AngryImage { get; set; } = "";
        public string SadImage { get; set; } = "";
        public string HappyImage { get; set; } = "";
        public string SurprisedImage { get; set; } = "";

        /// <summary>
        /// Get image path for a specific mood
        /// </summary>
        public string GetImagePath(CharacterMood mood)
        {
            return mood switch
            {
                CharacterMood.Normal => NormalImage,
                CharacterMood.Angry => AngryImage,
                CharacterMood.Sad => SadImage,
                CharacterMood.Happy => HappyImage,
                CharacterMood.Surprised => SurprisedImage,
                _ => NormalImage
            };
        }
    }

    /// <summary>
    /// Character mood/emotion types
    /// </summary>
    public enum CharacterMood
    {
        Normal,
        Angry,
        Sad,
        Happy,
        Surprised
    }
}

