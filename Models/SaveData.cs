using System;

namespace VisualNovel.Models
{
    public class SaveData
    {
        public int CurrentDialogueIndex { get; set; }
        public string CurrentSceneId { get; set; } = "Scene1";
        public DateTime SaveDate { get; set; }
        public string SaveName { get; set; } = "";
        public string PreviewText { get; set; } = "";
        public GameState GameState { get; set; } = new GameState();
    }
}

