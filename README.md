# Visual Novel System in C#

A complete Visual Novel game system built with WPF (Windows Presentation Foundation) in C#.

## Features

- **Main Menu**: Start new games, load saved games, or exit
- **Character-by-Character Text Display**: Smooth typing animation effect
- **Skip Functionality**: Press Enter, Space, or Left Mouse Click to skip typing animation or advance dialogue
- **Save System**: Save your progress at any point (Ctrl+S or Save button)
- **Load System**: Load from up to 10 save slots
- **Full-Screen Gameplay**: Immersive full-screen experience

## Controls

### In-Game
- **Enter/Space/Left Mouse Click**: Skip typing animation or advance to next dialogue
- **Ctrl+S**: Open save menu
- **Escape**: Return to main menu
- **Save Button**: Open save menu (top-right)
- **Menu Button**: Return to main menu (top-right)

### Main Menu
- **New Game**: Start a fresh playthrough
- **Load Game**: Load from a saved game slot
- **Exit**: Quit the application

## Project Structure

```
VisualNovel/
├── Models/
│   ├── StoryData.cs          # Story and dialogue data models
│   └── SaveData.cs            # Save game data model
├── Services/
│   ├── StoryService.cs        # Manages story content
│   └── SaveLoadService.cs     # Handles save/load operations
├── App.xaml                   # Application entry point
├── App.xaml.cs
├── MainWindow.xaml            # Main menu window
├── MainWindow.xaml.cs
├── GameScene.xaml             # Gameplay window
├── GameScene.xaml.cs
├── SaveLoadMenu.xaml          # Save/Load menu window
├── SaveLoadMenu.xaml.cs
└── VisualNovel.csproj         # Project file
```

## Building and Running

1. Make sure you have .NET 8.0 SDK installed
2. Open the project in Visual Studio or use the command line:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

## Customization

### Adding Your Own Story

Edit `Services/StoryService.cs` and modify the `CreateDefaultStory()` method to add your own dialogue lines:

```csharp
new DialogueLine
{
    CharacterName = "Your Character",
    Text = "Your dialogue text here...",
    BackgroundImage = "path/to/background.png",  // Optional
    CharacterImage = "path/to/character.png"      // Optional
}
```

### Adjusting Typing Speed

In `GameScene.xaml.cs`, modify the `Interval` property in `SetupTypingTimer()`:

```csharp
Interval = TimeSpan.FromMilliseconds(30) // Lower = faster typing
```

### Save Location

Saves are stored in: `%AppData%\VisualNovel\Saves\`

## Requirements

- .NET 8.0 or later
- Windows OS (WPF requirement)
- Visual Studio 2022 or VS Code with C# extension (recommended)

## License

This project is provided as-is for educational and personal use.

