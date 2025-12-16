using System.Collections.Generic;

namespace VisualNovel.Models
{
    /// <summary>
    /// Manages game flags and state for branching storylines
    /// </summary>
    public class GameState
    {
        public Dictionary<string, bool> Flags { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, int> Variables { get; set; } = new Dictionary<string, int>();
        public string CurrentSceneId { get; set; } = "Scene1";
        public int CurrentDialogueIndex { get; set; } = 0;

        public void SetFlag(string flagName, bool value)
        {
            Flags[flagName] = value;
        }

        public bool GetFlag(string flagName)
        {
            return Flags.TryGetValue(flagName, out bool value) && value;
        }

        public void SetVariable(string varName, int value)
        {
            Variables[varName] = value;
        }

        public int GetVariable(string varName, int defaultValue = 0)
        {
            return Variables.TryGetValue(varName, out int value) ? value : defaultValue;
        }
    }
}

