using System.Collections.Generic;
using VisualNovel.Models;
using VisualNovel.Services;

namespace VisualNovel.Scenes
{
    /// <summary>
    /// Scene 1 - Winter Sun: The cursed traveler arrives at the village
    /// </summary>
    public class Scene1 : BaseScene
    {
        public Scene1()
        {
            _translationService = TranslationService.Instance;
            
            // Register characters - using available assets or placeholders
            // Player character (Main Character)
            RegisterCharacter(
                id: "Player",
                nameKey: "Scene1_Character_Player",
                normalImage: "Assets/Images/Characters/MC.png",
                sadImage: "Assets/Images/Characters/MC.png"
            );
            
            // Girl with cat
            RegisterCharacter(
                id: "GirlWithCat",
                nameKey: "Scene1_Character_GirlWithCat",
                normalImage: "Assets/Images/Characters/girl.png",
                sadImage: "Assets/Images/Characters/girl.png",
                happyImage: "Assets/Images/Characters/girl.png"
            );
            
            // Mother of the girl
            RegisterCharacter(
                id: "Mother",
                nameKey: "Scene1_Character_Mother",
                normalImage: "Assets/Images/Characters/mother.png", // Placeholder - using girl.png
                sadImage: "Assets/Images/Characters/mother.png"
            );
            
            // Village Chief
            RegisterCharacter(
                id: "VillageChief",
                nameKey: "Scene1_Character_VillageChief",
                normalImage: "Assets/Images/Characters/doc.png"
            );
            
            // Narrator (no image - for narration text)
            RegisterCharacter(
                id: "Narrator",
                nameKey: "Scene1_Character_Narrator",
                normalImage: ""
            );
        }

        public override string SceneId => "Scene1";
        public override string SceneName => _translationService!.GetTranslation("Scene1_Name");

        public override List<DialogueLine> GetDialogues(GameState gameState)
        {
            var dialogues = new List<DialogueLine>();
            
            // Introduction - winter sun description
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_Introduction_WinterSun"
            ));
            
            // Background changes to distant view of village
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_VillageAppears",
                backgroundImage: "Assets/Images/Scenes/village.png"
            ));
            
            // Text about journey and chains
            // Main character appears from left on spot 1
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_JourneyText_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_ChainsText",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_CurseText",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            // Screen darkens
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_LastSteps",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_VillageNoise",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            // Background changes to main street
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_MainStreet",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_JourneyText_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_1",
                mood: CharacterMood.Sad,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                cameraZoom: 1.0
            ));
            
            // Mother appears from right on spot 6
            dialogues.Add(CreateDialogue(
                characterId: "Mother",
                textKey: "Scene1_Mother_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            // Mother exits to right, Village Chief appears on spot 4
            dialogues.Add(CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_1_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            dialogues.Add(CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_1_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            // Village Chief dialogue with choices
            var villageChiefDialogue = CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            );
            villageChiefDialogue.Choices.Add(CreateChoice(
                _translationService!.GetTranslation("Scene1_Choice_WhoNeedsHelp"),
                null, null, null, null
            ));
            villageChiefDialogue.Choices.Add(CreateChoice(
                _translationService!.GetTranslation("Scene1_Choice_LastBurner"),
                null, null, null, null
            ));
            villageChiefDialogue.Choices.Add(CreateChoice(
                _translationService!.GetTranslation("Scene1_Choice_RestockSupplies"),
                null, null, null, null
            ));
            villageChiefDialogue.Choices.Add(CreateChoice(
                _translationService!.GetTranslation("Scene1_Choice_ExitDialogue"),
                null, null, null, null
            ));
            dialogues.Add(villageChiefDialogue);
            
            // Response dialogues based on choices (we'll use flags to determine which to show)
            // For now, we'll show all responses sequentially
            dialogues.Add(CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_Response_WhoNeedsHelp",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_Response_LastBurner",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "VillageChief",
                textKey: "Scene1_VillageChief_Response_RestockSupplies",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "VillageChief",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "GirlWithCat",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.0
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_GirlApproaches",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.3
            ));
            
            // Girl with cat dialogue
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_2",
                mood: CharacterMood.Sad,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot6: "Mother",
                spot6Facing: "Right",
                spot6Mood: CharacterMood.Normal,
                cameraZoom: 1.3
            ));
            
            // Mother dialogue
            dialogues.Add(CreateDialogue(
                characterId: "Mother",
                textKey: "Scene1_Mother_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Player dialogue - always on spot 1
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_Player_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_Player_Context_1_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Player reassures the mother
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_Player_1_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_Player_Context_1_3",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Player asks about cat - always on spot 1
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_Player_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_GirlWithCat_Response_Cat_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_GirlWithCat_Response_Cat_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Sad,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Girl becomes happy as conversation progresses
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_3_1",
                mood: CharacterMood.Happy,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_3_2",
                mood: CharacterMood.Happy,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Player asks about mom - player on spot 1, girl on spot4 (closer)
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_Player_3",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_4",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Burn Memory button appears
            var burnMemoryDialogue = CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_BurnMemory_Prompt",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            );
            burnMemoryDialogue.Choices.Add(CreateChoice(
                _translationService!.GetTranslation("Scene1_Choice_BurnMemory"),
                new Dictionary<string, bool> { { "memoryBurned", true } },
                null, null, null
            ));
            dialogues.Add(burnMemoryDialogue);
            
            // Memory burning sequence
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_BurnMemory_Effect_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_BurnMemory_Effect_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_BurnMemory_Effect_3",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Normal,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Girl's reaction after memory burn
            dialogues.Add(CreateDialogue(
                characterId: "GirlWithCat",
                textKey: "Scene1_GirlWithCat_AfterBurn",
                mood: CharacterMood.Happy,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Mother dialogue
            dialogues.Add(CreateDialogue(
                characterId: "Mother",
                textKey: "Scene1_Mother_3",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Narrator",
                textKey: "Scene1_Mother_Context",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            dialogues.Add(CreateDialogue(
                characterId: "Mother",
                textKey: "Scene1_Mother_4_1",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            dialogues.Add(CreateDialogue(
                characterId: "Mother",
                textKey: "Scene1_Mother_4_2",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // Player response
            dialogues.Add(CreateDialogue(
                characterId: "Player",
                textKey: "Scene1_Player_4",
                mood: CharacterMood.Normal,
                spot1: "Player",
                spot1Facing: "Right",
                spot1Mood: CharacterMood.Normal,
                spot4: "GirlWithCat",
                spot4Facing: "Right",
                spot4Mood: CharacterMood.Happy,
                spot5: "Mother",
                spot5Facing: "Right",
                spot5Mood: CharacterMood.Normal,
                cameraZoom: 1.2
            ));
            
            // End of dialogue - player enters hub
            dialogues.Add(CreateTextOnlyDialogue("Scene1_End_Hub"));

            return dialogues;
        }
        
        /// <summary>
        /// Helper method to create a text-only dialogue (narration) with optional background
        /// </summary>
        private DialogueLine CreateTextOnlyDialogue(string textKey, string? backgroundImage = null)
        {
            if (_translationService == null)
                _translationService = TranslationService.Instance;
                
            return new DialogueLine
            {
                CharacterName = "",
                Text = _translationService.GetTranslation(textKey),
                BackgroundImage = backgroundImage ?? "",
                CharacterSlots = new List<CharacterSlot>()
            };
        }

        public override string? GetNextSceneId(GameState gameState)
        {
            // Return null to end the story, or return a scene ID when Scene2 is created
            return null;
        }
    }
}
