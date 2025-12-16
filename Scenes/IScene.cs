using System.Collections.Generic;
using VisualNovel.Models;

namespace VisualNovel.Scenes
{
    /// <summary>
    /// Interface for all story scenes
    /// </summary>
    public interface IScene
    {
        string SceneId { get; }
        string SceneName { get; }
        List<DialogueLine> GetDialogues(GameState gameState);
        string? GetNextSceneId(GameState gameState);
    }
}

