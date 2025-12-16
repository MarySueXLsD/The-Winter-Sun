# The Winter Sun: Silent Mother, Starving God

<div align="center">

**A haunting visual novel experience about a cursed traveler, forgotten memories, and the price of healing**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp)](https://docs.microsoft.com/dotnet/csharp/)

</div>

---

## 🌟 Overview

**The Winter Sun** is an immersive visual novel that tells the story of a cursed traveler burdened with a mysterious black furnace on their back—a device that glows with an unnatural red light and holds the power to burn away memories. As you arrive at a remote village during a harsh winter, you'll discover that the villagers view you with both fear and desperate hope. They come seeking "healing," but what you offer is far more complex: the ability to erase their painful memories, leaving them forever changed.

Every choice matters. Every memory burned has consequences. In a world where forgetting might be the only escape from suffering, what price are you willing to pay?

---

## 📖 Story

You are a traveler marked by an ancient curse. Chains clink on your shoulders, holding a black furnace that burns with an eerie red glow. The origin of your curse is lost to you—memories that slip away like smoke, fragments of a past you can barely recall.

When you arrive at the village, you know your presence will only bring suspicion. The best you can hope for is that they will look at you imploringly, desperate enough to seek your help despite their fear.

The villagers come to you not for physical healing, but for something deeper: relief from the weight of their memories. A young girl carrying a cat, her eyes distant and tired. A mother watching warily, torn between protecting her child and seeking solace. A village chief who sees you as both a threat and a necessary evil.

You have the power to burn their memories, to make them forget their pain. But is forgetting truly healing? And what happens to those who lose pieces of themselves in the process?

---

## ✨ Key Features

### 🎭 Immersive Narrative Experience

- **Choice-Driven Storytelling**: Your decisions shape the narrative and determine the fates of those you encounter
- **Memory Burning Mechanic**: A unique gameplay system where you can erase painful memories from characters, with lasting consequences
- **Multiple Endings**: Your choices lead to different outcomes, encouraging multiple playthroughs
- **Rich Character Development**: Deep, nuanced characters with their own motivations, fears, and desires

### 🎨 Visual Excellence

- **Dynamic Parallax Effects**: Immersive depth-of-field effects that respond to mouse movement, creating a living, breathing world
- **Advanced Camera System**: Smooth zoom and pan controls that enhance dramatic moments and focus on important details
- **Multi-Character Positioning**: Up to 6 character slots with dynamic positioning, facing directions, and mood expressions
- **Beautiful Art Assets**: Handcrafted character sprites, backgrounds, and atmospheric visuals that bring the story to life
- **Full-Screen Immersion**: Optional full-screen mode for an uninterrupted narrative experience

### 🌍 Multi-Language Support

Experience the story in your native language:
- 🇬🇧 **English**
- 🇷🇺 **Russian** (Русский)
- 🇺🇦 **Ukrainian** (Українська)
- 🇵🇱 **Polish** (Polski)

All dialogue, menus, and interface elements are fully localized.

### ⚙️ Comprehensive Customization

- **Text Speed Control**: Adjust typing animation speed to your preference
- **Auto-Advance Options**: Set automatic text progression with customizable delays
- **Skip Functionality**: Skip unread or previously read text at your own pace
- **Graphics Quality Settings**: Optimize performance with low, medium, or high quality presets
- **Audio Controls**: Independent volume sliders for master, music, and sound effects
- **Accessibility Features**: High contrast mode, large text mode, and animation reduction options

### 💾 Save & Load System

- **10 Save Slots**: Multiple save files for different playthroughs or branching paths
- **Quick Save**: Press `Ctrl+S` to save your progress instantly
- **Auto-Save**: Optional automatic saving at key story moments
- **Save Anytime**: No restrictions—save whenever you want to preserve your choices

### 🎵 Audio Experience

- **Atmospheric Soundtrack**: Original music that enhances the emotional impact of each scene
- **Environmental Sound Effects**: Immersive audio that brings the world to life
- **Dynamic Audio Mixing**: Separate controls for music and sound effects

### 🎮 Technical Features

- **Smooth Typing Animation**: Character-by-character text display with customizable speed
- **FPS Counter**: Monitor performance with optional frame rate display
- **VSync Support**: Eliminate screen tearing for smooth visuals
- **Optimized Performance**: Efficient rendering system that runs smoothly on a wide range of hardware

---

## 🎮 Controls

### In-Game Controls

| Action | Input |
|--------|-------|
| **Advance Dialogue** | `Enter`, `Space`, or `Left Mouse Click` |
| **Quick Save** | `Ctrl+S` |
| **Open Save Menu** | Click **Save** button (top-right) |
| **Open Load Menu** | Click **Load** button (top-right) |
| **Return to Main Menu** | `Escape` or click **Menu** button |

### Main Menu

- **New Game**: Start a fresh playthrough
- **Load Game**: Continue from a saved game
- **Options**: Customize settings, language, and preferences
- **Credits**: View game credits
- **Quit**: Exit the application

---

## 🚀 Getting Started

### System Requirements

- **OS**: Windows 10 or later
- **.NET Runtime**: .NET 8.0 or later ([Download](https://dotnet.microsoft.com/download))
- **RAM**: 2 GB minimum (4 GB recommended)
- **Storage**: 500 MB available space
- **Graphics**: DirectX 9 compatible graphics card

### Installation

1. **Download the latest release** from the releases page
2. **Extract** the game files to your desired location
3. **Run** `VisualNovel.exe`
4. **Enjoy** your journey through The Winter Sun

### Building from Source

If you want to build the game yourself:

1. **Install Prerequisites**:
   - [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
   - [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or [VS Code](https://code.visualstudio.com/) with C# extension

2. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/Visual_Novel.git
   cd Visual_Novel
   ```

3. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the Project**:
   ```bash
   dotnet build
   ```

5. **Run the Game**:
   ```bash
   dotnet run
   ```

Or open the project in Visual Studio and press `F5` to build and run.

---

## 📁 Project Structure

```
VisualNovel/
├── Assets/                    # Game assets (images, music, fonts)
│   ├── Images/
│   │   ├── Characters/       # Character sprites
│   │   └── Scenes/          # Background images
│   ├── Music/                # Background music tracks
│   ├── Sounds/               # Sound effects
│   └── Fonts/                # Custom fonts
├── Models/                    # Data models
│   ├── Character.cs          # Character data structure
│   ├── GameState.cs          # Game state and flags
│   ├── GameConfig.cs         # Configuration settings
│   └── SaveData.cs           # Save file structure
├── Scenes/                    # Story scenes
│   ├── BaseScene.cs          # Base scene implementation
│   ├── IScene.cs             # Scene interface
│   └── Scene1.cs             # First scene implementation
├── Services/                  # Core services
│   ├── StoryService.cs       # Story management
│   ├── SaveLoadService.cs    # Save/load functionality
│   ├── TranslationService.cs # Multi-language support
│   ├── SoundService.cs       # Audio management
│   └── ConfigService.cs      # Configuration management
├── Dialogs/                   # Dialog windows
│   ├── ChoiceMenu.xaml       # Choice selection UI
│   └── GameDialog.xaml       # Game dialog windows
├── GameScene.xaml             # Main game window UI
├── GameScene.xaml.cs          # Main game logic
├── MainWindow.xaml            # Main menu UI
├── MainWindow.xaml.cs         # Main menu logic
└── VisualNovel.csproj         # Project configuration
```

---

## 🎯 Gameplay Mechanics

### Memory Burning System

The core mechanic of **The Winter Sun** is the ability to "burn" memories from characters. When you use this power:

- **Characters forget painful experiences**, allowing them to move forward
- **The cost is permanent**—memories cannot be restored
- **Choices matter**—deciding when and how to use this power affects the story
- **Consequences unfold**—the narrative adapts based on what memories you've erased

### Choice System

Throughout your journey, you'll encounter branching dialogue and meaningful choices:

- **Dialogue Choices**: Shape conversations and relationships with characters
- **Action Choices**: Decide how to interact with the world and its inhabitants
- **Moral Dilemmas**: Face difficult decisions with no clear "right" answer
- **Multiple Paths**: Different choices unlock different story branches

### Save System

- **Save Slots**: 10 independent save slots for multiple playthroughs
- **Save Location**: `%AppData%\VisualNovel\Saves\`
- **Save Anytime**: No restrictions on when you can save
- **Auto-Save**: Optional automatic saving at key moments

---

## ⚙️ Configuration

### Settings Menu

Access the settings menu from the main menu or in-game to customize:

#### Audio Settings
- Master Volume
- Music Volume
- Sound Effects Volume
- Mute options for music and sound effects

#### Text Settings
- Text Speed (typing animation speed)
- Font Size
- Auto-Advance Text (with delay control)
- Skip options for read/unread text

#### Display Settings
- Fullscreen mode
- Window resolution
- VSync
- FPS counter display
- Target FPS

#### Graphics Settings
- Graphics Quality (Low/Medium/High)
- Visual Effects toggle
- Shadows toggle
- Background Opacity

#### Gameplay Settings
- Auto-Save enable/disable
- Auto-Save interval
- Exit confirmation
- Skip indicator display
- Location & time display
- Chapter title display

#### Accessibility
- High Contrast Mode
- Large Text Mode
- Reduce Animations

---

## 🌐 Localization

The game supports multiple languages with full localization of:
- All dialogue and story text
- User interface elements
- Menu options and settings
- Character names and locations

To change the language:
1. Open the **Settings** menu
2. Navigate to **Language** section
3. Select your preferred language
4. The game will immediately switch to the new language

---

## 🐛 Troubleshooting

### Common Issues

**Game won't start**
- Ensure you have .NET 8.0 Runtime installed
- Check that all game files are present in the installation directory
- Try running as administrator

**Performance issues**
- Lower graphics quality in Settings
- Disable visual effects
- Reduce parallax intensity
- Close other applications

**Save files not working**
- Check that you have write permissions in `%AppData%\VisualNovel\Saves\`
- Ensure sufficient disk space
- Try running as administrator

**Audio not playing**
- Check Windows volume settings
- Verify audio settings in-game
- Ensure audio drivers are up to date

**Text not displaying correctly**
- Try changing the font size in Settings
- Switch to a different language and back
- Check that font files are present in `Assets/Fonts/`

---

## 📝 Development

### Adding New Scenes

To add a new scene to the game:

1. Create a new class inheriting from `BaseScene`:
   ```csharp
   public class Scene2 : BaseScene
   {
       public override string SceneId => "Scene2";
       public override string SceneName => "Your Scene Name";
       
       public override List<DialogueLine> GetDialogues(GameState gameState)
       {
           // Your dialogue implementation
       }
   }
   ```

2. Register the scene in `StoryService.cs`:
   ```csharp
   private Dictionary<string, IScene> RegisterScenes()
   {
       return new Dictionary<string, IScene>
       {
           { "Scene1", new Scene1() },
           { "Scene2", new Scene2() }  // Add your scene here
       };
   }
   ```

3. Add translations for your scene in `TranslationService.cs`

### Customizing Typing Speed

Edit the text speed in `GameScene.xaml.cs`:
```csharp
_typingTimer = new DispatcherTimer
{
    Interval = TimeSpan.FromMilliseconds(_configService.GetConfig().TextSpeed)
};
```

Or adjust it through the Settings menu in-game.

---

## 📄 License

This project is provided as-is for educational and personal use.

---

## 🙏 Credits

**The Winter Sun: Silent Mother, Starving God**

Developed with passion for storytelling and immersive narrative experiences.

Built with:
- **.NET 8.0** - Modern C# development platform
- **WPF (Windows Presentation Foundation)** - Rich desktop UI framework
- **Newtonsoft.Json** - JSON serialization

---

## 🔮 Future Updates

Planned features and improvements:
- Additional scenes and story content
- More character interactions and branching paths
- Enhanced visual effects and animations
- Additional language support
- Steam integration (planned)
- Achievement system
- Gallery mode for viewing unlocked content

---

## 📧 Contact & Support

For questions, bug reports, or feedback:
- Open an issue on the GitHub repository
- Check the documentation for common questions
- Review the troubleshooting section above

---

<div align="center">

**Step into a world where memories are currency, and forgetting might be the only escape.**

**The Winter Sun awaits.**

</div>
