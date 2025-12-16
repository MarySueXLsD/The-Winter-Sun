using System.Collections.Generic;

namespace VisualNovel.Models
{
    public class StoryData
    {
        public List<DialogueLine> Dialogues { get; set; } = new List<DialogueLine>();
    }

    /// <summary>
    /// Represents a character positioned at a specific spot on screen
    /// </summary>
    public class CharacterSlot
    {
        public string CharacterImage { get; set; } = "";
        public int Spot { get; set; } // 1-6, where 1 is far left, 6 is far right
        public string Facing { get; set; } = "Right"; // "Left" or "Right"
    }

    public class DialogueLine
    {
        public string CharacterName { get; set; } = "";
        public string Text { get; set; } = "";
        public string BackgroundImage { get; set; } = "";
        public string CharacterImage { get; set; } = ""; // Left character image (backward compatibility)
        public string CharacterImageRight { get; set; } = ""; // Right character image (backward compatibility)
        public List<Choice> Choices { get; set; } = new List<Choice>(); // Choices for this dialogue
        public bool HasChoices => Choices != null && Choices.Count > 0;
        
        // Multiple character support - list of all characters on screen
        public List<CharacterSlot> CharacterSlots { get; set; } = new List<CharacterSlot>();
        
        // Character positioning (1-6, where 1 is far left, 6 is far right) - backward compatibility
        public int? CharacterSpot { get; set; } // Spot for left character (Malgorzata)
        public string CharacterFacing { get; set; } = "Right"; // "Left" or "Right" for left character
        public int? CharacterSpotRight { get; set; } // Spot for right character (Agata)
        public string CharacterFacingRight { get; set; } = "Left"; // "Left" or "Right" for right character
        public double? CameraZoom { get; set; } // Optional zoom level (1.0 = normal, >1.0 = zoom in)
    }

    /// <summary>
    /// Represents a choice option that can affect flags, variables, and scene progression
    /// </summary>
    public class Choice
    {
        public string Text { get; set; } = ""; // The choice text displayed to the player
        public Dictionary<string, bool> SetFlags { get; set; } = new Dictionary<string, bool>(); // Flags to set when chosen
        public Dictionary<string, int> ModifyVariables { get; set; } = new Dictionary<string, int>(); // Variables to modify (add/subtract)
        public string? NextSceneId { get; set; } // Optional: jump to a specific scene after choice
        public int? JumpToDialogueIndex { get; set; } // Optional: jump to a specific dialogue in current scene
    }
}

