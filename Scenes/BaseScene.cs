using System.Collections.Generic;
using System.Linq;
using VisualNovel.Models;
using VisualNovel.Services;

namespace VisualNovel.Scenes
{
    /// <summary>
    /// Base class for scenes with common functionality
    /// </summary>
    public abstract class BaseScene : IScene
    {
        protected Dictionary<string, Character> _characters = new Dictionary<string, Character>();
        protected TranslationService? _translationService;

        public abstract string SceneId { get; }
        public abstract string SceneName { get; }

        public abstract List<DialogueLine> GetDialogues(GameState gameState);

        public virtual string? GetNextSceneId(GameState gameState)
        {
            // Default: return null to end story, or override in derived classes
            return null;
        }

        protected bool CheckFlag(GameState gameState, string flagName)
        {
            return gameState.GetFlag(flagName);
        }

        protected int GetVariable(GameState gameState, string varName, int defaultValue = 0)
        {
            return gameState.GetVariable(varName, defaultValue);
        }

        /// <summary>
        /// Register a character for use in dialogues
        /// </summary>
        protected void RegisterCharacter(string id, string nameKey, string normalImage, 
            string angryImage = "", string sadImage = "", string happyImage = "", string surprisedImage = "")
        {
            _characters[id] = new Character
            {
                Id = id,
                NameKey = nameKey,
                NormalImage = normalImage,
                AngryImage = angryImage,
                SadImage = sadImage,
                HappyImage = happyImage,
                SurprisedImage = surprisedImage
            };
        }

        /// <summary>
        /// Create a dialogue with explicit character positioning (Spot_1 through Spot_6 style)
        /// Each spot can have a character with a specific mood
        /// </summary>
        protected DialogueLine CreateDialogue(string characterId, string textKey, CharacterMood mood = CharacterMood.Normal,
            string? spot1 = null, string? spot2 = null, string? spot3 = null, 
            string? spot4 = null, string? spot5 = null, string? spot6 = null,
            string? spot1Facing = null, string? spot2Facing = null, string? spot3Facing = null,
            string? spot4Facing = null, string? spot5Facing = null, string? spot6Facing = null,
            CharacterMood? spot1Mood = null, CharacterMood? spot2Mood = null, CharacterMood? spot3Mood = null,
            CharacterMood? spot4Mood = null, CharacterMood? spot5Mood = null, CharacterMood? spot6Mood = null,
            double? cameraZoom = null, string? backgroundImage = null)
        {
            if (_translationService == null)
                _translationService = TranslationService.Instance;

            var character = _characters.ContainsKey(characterId) ? _characters[characterId] : null;
            
            // Build a dictionary of spot assignments with character IDs, moods, and facing directions
            var spotAssignments = new Dictionary<int, (string charId, CharacterMood charMood, string facing)>();
            
            if (spot1 != null)
            {
                var charMood = spot1Mood ?? (spot1 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[1] = (spot1, charMood, spot1Facing ?? "Right");
            }
            if (spot2 != null)
            {
                var charMood = spot2Mood ?? (spot2 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[2] = (spot2, charMood, spot2Facing ?? "Right");
            }
            if (spot3 != null)
            {
                var charMood = spot3Mood ?? (spot3 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[3] = (spot3, charMood, spot3Facing ?? "Right");
            }
            if (spot4 != null)
            {
                var charMood = spot4Mood ?? (spot4 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[4] = (spot4, charMood, spot4Facing ?? "Left");
            }
            if (spot5 != null)
            {
                var charMood = spot5Mood ?? (spot5 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[5] = (spot5, charMood, spot5Facing ?? "Left");
            }
            if (spot6 != null)
            {
                var charMood = spot6Mood ?? (spot6 == characterId ? mood : CharacterMood.Normal);
                spotAssignments[6] = (spot6, charMood, spot6Facing ?? "Left");
            }

            // Find leftmost and rightmost spots for camera positioning
            int? leftmostSpot = null;
            int? rightmostSpot = null;
            string leftFacing = "Right";
            string rightFacing = "Left";
            string? leftmostCharId = null;
            string? rightmostCharId = null;
            CharacterMood leftmostMood = CharacterMood.Normal;
            CharacterMood rightmostMood = CharacterMood.Normal;

            if (spotAssignments.Count > 0)
            {
                leftmostSpot = spotAssignments.Keys.Min();
                rightmostSpot = spotAssignments.Keys.Max();
                
                var leftmost = spotAssignments[leftmostSpot.Value];
                var rightmost = spotAssignments[rightmostSpot.Value];
                
                leftmostCharId = leftmost.charId;
                rightmostCharId = rightmost.charId;
                leftmostMood = leftmost.charMood;
                rightmostMood = rightmost.charMood;
                leftFacing = leftmost.facing;
                rightFacing = rightmost.facing;
            }

            var dialogue = new DialogueLine
            {
                CharacterName = character != null 
                    ? _translationService.GetTranslation(character.NameKey) 
                    : "",
                Text = _translationService.GetTranslation(textKey),
                BackgroundImage = backgroundImage ?? "",
                CharacterSpot = leftmostSpot,
                CharacterFacing = leftFacing,
                CharacterSpotRight = rightmostSpot,
                CharacterFacingRight = rightFacing,
                CameraZoom = cameraZoom,
                CharacterSlots = new List<CharacterSlot>()
            };

            // Populate all character slots
            foreach (var kvp in spotAssignments.OrderBy(x => x.Key))
            {
                var charId = kvp.Value.charId;
                var charMood = kvp.Value.charMood;
                var facing = kvp.Value.facing;
                
                if (_characters.ContainsKey(charId))
                {
                    dialogue.CharacterSlots.Add(new CharacterSlot
                    {
                        CharacterImage = _characters[charId].GetImagePath(charMood),
                        Spot = kvp.Key,
                        Facing = facing
                    });
                }
            }

            // Set character images for leftmost and rightmost characters with their specific moods (backward compatibility)
            if (leftmostCharId != null && _characters.ContainsKey(leftmostCharId))
            {
                dialogue.CharacterImage = _characters[leftmostCharId].GetImagePath(leftmostMood);
            }
            if (rightmostCharId != null && _characters.ContainsKey(rightmostCharId))
            {
                dialogue.CharacterImageRight = _characters[rightmostCharId].GetImagePath(rightmostMood);
            }

            return dialogue;
        }

        /// <summary>
        /// Helper method to create a choice with flags and variables
        /// </summary>
        protected Choice CreateChoice(string text, Dictionary<string, bool>? setFlags = null, 
            Dictionary<string, int>? modifyVariables = null, string? nextSceneId = null, 
            int? jumpToDialogueIndex = null)
        {
            return new Choice
            {
                Text = text,
                SetFlags = setFlags ?? new Dictionary<string, bool>(),
                ModifyVariables = modifyVariables ?? new Dictionary<string, int>(),
                NextSceneId = nextSceneId,
                JumpToDialogueIndex = jumpToDialogueIndex
            };
        }

        /// <summary>
        /// Helper method to create a simple choice that sets a flag
        /// </summary>
        protected Choice CreateFlagChoice(string text, string flagName, bool flagValue = true)
        {
            return CreateChoice(text, new Dictionary<string, bool> { { flagName, flagValue } });
        }

        /// <summary>
        /// Helper method to create a choice that modifies a variable
        /// </summary>
        protected Choice CreateVariableChoice(string text, string varName, int valueChange)
        {
            return CreateChoice(text, null, new Dictionary<string, int> { { varName, valueChange } });
        }

        /// <summary>
        /// Helper method to create a choice that jumps to a scene
        /// </summary>
        protected Choice CreateSceneChoice(string text, string nextSceneId, Dictionary<string, bool>? setFlags = null)
        {
            return CreateChoice(text, setFlags, null, nextSceneId);
        }
    }
}

