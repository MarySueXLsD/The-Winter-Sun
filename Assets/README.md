# Assets Folder

This folder contains all game assets including videos, images, sounds, etc.

## Structure

```
Assets/
├── Videos/          # Video files (backgrounds, cutscenes, etc.)
├── Images/          # Image files (sprites, backgrounds, UI elements)
├── Audio/           # Audio files (music, sound effects)
└── Fonts/           # Custom fonts
```

## Usage

Assets are automatically copied to the output directory during build. Reference them using:

```csharp
// Example: Load a video file
string assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Videos", "your_video.mp4");
```

## Note on content.mgcb

The `content.mgcb` file is specific to MonoGame's content pipeline. Since this project uses WPF, assets are referenced directly as files. If you plan to migrate to MonoGame in the future, you would need to create a content.mgcb file and use the MonoGame Content Pipeline to process assets.

