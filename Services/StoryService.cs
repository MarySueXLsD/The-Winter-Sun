using System.Collections.Generic;
using System.Linq;
using VisualNovel.Models;
using VisualNovel.Scenes;

namespace VisualNovel.Services
{
    public class StoryService
    {
        private GameState _gameState;
        private Dictionary<string, IScene> _scenes;
        private List<DialogueLine> _currentDialogues = new List<DialogueLine>();

        public StoryService()
        {
            _gameState = new GameState();
            _scenes = RegisterScenes();
            LoadScene(_gameState.CurrentSceneId);
        }

        public StoryService(GameState gameState)
        {
            _gameState = gameState;
            _scenes = RegisterScenes();
            LoadScene(_gameState.CurrentSceneId);
        }

        /// <summary>
        /// Register all scenes here. Add new scenes to this dictionary.
        /// </summary>
        private Dictionary<string, IScene> RegisterScenes()
        {
            return new Dictionary<string, IScene>
            {
                { "Scene1", new Scene1() }
                // Add more scenes here as you create them
                // { "Scene2", new Scene2() },
                // { "Scene3", new Scene3() }
            };
        }

        /// <summary>
        /// Load a scene and get its dialogues based on current game state
        /// </summary>
        public void LoadScene(string sceneId)
        {
            if (_scenes.TryGetValue(sceneId, out IScene? scene) && scene != null)
            {
                _gameState.CurrentSceneId = sceneId;
                _gameState.CurrentDialogueIndex = 0;
                _currentDialogues = scene.GetDialogues(_gameState);
            }
            else
            {
                _currentDialogues = new List<DialogueLine>();
            }
        }

        public DialogueLine? GetDialogue(int index)
        {
            if (index >= 0 && index < _currentDialogues.Count)
            {
                return _currentDialogues[index];
            }
            return null;
        }

        public int GetTotalDialogues()
        {
            return _currentDialogues?.Count ?? 0;
        }

        public GameState GetGameState()
        {
            return _gameState;
        }

        /// <summary>
        /// Advance to the next scene if we've reached the end of current scene
        /// </summary>
        public bool TryAdvanceToNextScene()
        {
            if (_scenes.TryGetValue(_gameState.CurrentSceneId, out IScene? currentScene) && currentScene != null)
            {
                string? nextSceneId = currentScene.GetNextSceneId(_gameState);
                if (!string.IsNullOrEmpty(nextSceneId))
                {
                    LoadScene(nextSceneId);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the current scene
        /// </summary>
        public IScene? GetCurrentScene()
        {
            _scenes.TryGetValue(_gameState.CurrentSceneId, out IScene? scene);
            return scene;
        }
    }
}

