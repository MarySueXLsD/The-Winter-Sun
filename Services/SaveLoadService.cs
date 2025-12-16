using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using VisualNovel.Models;

namespace VisualNovel.Services
{
    public class SaveLoadService
    {
        private readonly string _saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VisualNovel", "Saves");

        public SaveLoadService()
        {
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        public void SaveGame(SaveData saveData, int slotNumber)
        {
            string filePath = Path.Combine(_saveDirectory, $"save_{slotNumber}.json");
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public SaveData? LoadGame(int slotNumber)
        {
            string filePath = Path.Combine(_saveDirectory, $"save_{slotNumber}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<SaveData>(json);
            }
            return null;
        }

        public List<SaveData> GetAllSaves()
        {
            var saves = new List<SaveData>();
            // Auto-save is always slot 0 and should be first
            var autoSave = LoadGame(0);
            if (autoSave != null)
            {
                autoSave.SaveName = "Auto-Save";
                saves.Add(autoSave);
            }
            // Regular saves are slots 1-10
            for (int i = 1; i <= 10; i++)
            {
                var save = LoadGame(i);
                if (save != null)
                {
                    saves.Add(save);
                }
            }
            return saves;
        }

        public void AutoSave(int dialogueIndex, StoryService storyService)
        {
            var dialogue = storyService.GetDialogue(dialogueIndex);
            var previewText = dialogue != null ? dialogue.Text : "";
            var gameState = storyService.GetGameState();

            var saveData = new SaveData
            {
                CurrentDialogueIndex = dialogueIndex,
                CurrentSceneId = gameState.CurrentSceneId,
                SaveDate = DateTime.Now,
                SaveName = "Auto-Save",
                PreviewText = previewText.Length > 100 ? previewText.Substring(0, 100) : previewText,
                GameState = gameState
            };

            SaveGame(saveData, 0); // Slot 0 is always auto-save
        }

        public bool HasSave(int slotNumber)
        {
            string filePath = Path.Combine(_saveDirectory, $"save_{slotNumber}.json");
            return File.Exists(filePath);
        }

        public void DeleteSave(int slotNumber)
        {
            string filePath = Path.Combine(_saveDirectory, $"save_{slotNumber}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}

