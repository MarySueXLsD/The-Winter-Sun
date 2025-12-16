using System;
using System.Windows;
using System.Windows.Controls;
using VisualNovel.Models;
using VisualNovel.Services;

namespace VisualNovel
{
    public partial class SaveLoadMenu : Window
    {
        private readonly SaveLoadService _saveLoadService;
        private readonly int? _currentDialogueIndex;
        private readonly bool _isLoadMenu;
        private readonly StoryService? _storyService;
        private readonly SoundService _soundService;

        public SaveLoadMenu(int? currentDialogueIndex, SaveLoadService saveLoadService, bool isLoadMenu = false, StoryService? storyService = null)
        {
            InitializeComponent();
            _saveLoadService = saveLoadService;
            _currentDialogueIndex = currentDialogueIndex;
            _isLoadMenu = isLoadMenu;
            _storyService = storyService;
            _soundService = new SoundService();

            // Load Minecraft font
            var minecraftFont = Services.FontHelper.LoadMinecraftFont();
            if (minecraftFont != null)
            {
                Resources["MinecraftFont"] = minecraftFont;
                // Apply font directly to title
                TitleText.FontFamily = minecraftFont;
                // Apply font directly to Cancel button
                CancelButton.FontFamily = minecraftFont;
            }

            // Load custom cursor
            LoadCustomCursor();

            TitleText.Text = _isLoadMenu ? "Load Game" : "Save Game";
            LoadSaveSlots();
        }

        private void LoadCustomCursor()
        {
            try
            {
                string cursorPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "AOM_Titans Cursor.cur");
                if (System.IO.File.Exists(cursorPath))
                {
                    this.Cursor = new System.Windows.Input.Cursor(cursorPath);
                }
            }
            catch
            {
                // Silently fail if cursor can't be loaded
            }
        }

        private void LoadSaveSlots()
        {
            SaveSlotsContainer.Items.Clear();

            // Auto-save (slot 0) always comes first
            var autoSave = _saveLoadService.LoadGame(0);
            if (autoSave != null)
            {
                autoSave.SaveName = "Auto-Save";
                var slotButton = CreateSlotButton(0, autoSave, isAutoSave: true);
                SaveSlotsContainer.Items.Add(slotButton);
            }

            // Regular saves (slots 1-10)
            for (int i = 1; i <= 10; i++)
            {
                var saveData = _saveLoadService.LoadGame(i);
                var slotButton = CreateSlotButton(i, saveData, isAutoSave: false);
                SaveSlotsContainer.Items.Add(slotButton);
            }
        }

        private Button CreateSlotButton(int slotNumber, SaveData? saveData, bool isAutoSave = false)
        {
            var button = new Button
            {
                Height = 80,
                Margin = new Thickness(0, 5, 0, 5),
                Style = (Style)FindResource("SaveSlotButtonStyle"),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var grid = new Grid
            {
                Margin = new Thickness(0)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0)
            };

            if (saveData != null)
            {
                var slotText = new TextBlock
                {
                    Text = isAutoSave ? $"Auto-Save" : $"Slot {slotNumber}: {saveData.SaveName}",
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("MinecraftFont"),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = isAutoSave ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)) : System.Windows.Media.Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap
                };

                var dateText = new TextBlock
                {
                    Text = saveData.SaveDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("MinecraftFont"),
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var previewText = new TextBlock
                {
                    Text = saveData.PreviewText.Length > 60 
                        ? saveData.PreviewText.Substring(0, 60) + "..." 
                        : saveData.PreviewText,
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("MinecraftFont"),
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                textStack.Children.Add(slotText);
                textStack.Children.Add(dateText);
                textStack.Children.Add(previewText);
            }
            else
            {
                var emptyText = new TextBlock
                {
                    Text = $"Slot {slotNumber}: Empty",
                    FontFamily = (System.Windows.Media.FontFamily)FindResource("MinecraftFont"),
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                textStack.Children.Add(emptyText);
            }

            Grid.SetColumn(textStack, 0);
            grid.Children.Add(textStack);

            button.Content = grid;
            button.Click += (s, e) => OnSlotClicked(slotNumber, saveData);

            return button;
        }

        private void OnSlotClicked(int slotNumber, SaveData? existingSave)
        {
            _soundService.PlayClickSound();
            
            if (_isLoadMenu)
            {
                // Load game
                if (existingSave != null)
                {
                    try
                    {
                        // Load from MainWindow with chapter title transition
                        if (Application.Current.MainWindow != null && Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            // Close this dialog first, then start the transition
                            this.Close();
                            mainWindow.LoadGameWithChapterTitle(existingSave.CurrentDialogueIndex, existingSave.GameState);
                        }
                        else
                        {
                            // If MainWindow is not available, load from GameScene
                            // Store reference to old GameScene before creating new one
                            var oldGameScene = this.Owner as GameScene;
                            if (oldGameScene == null && Application.Current.MainWindow is GameScene gs)
                            {
                                oldGameScene = gs;
                            }
                            
                            // Close this dialog first (before closing owner)
                            this.Close();
                            
                            // Create new GameScene
                            var gameScene = new GameScene(existingSave.CurrentDialogueIndex, existingSave.GameState);
                            gameScene.Show();
                            
                            // Now close the old GameScene (after new one is shown and this dialog is closed)
                            oldGameScene?.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load game: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                            "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("This save slot is empty.", "Load Game", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // Save game
                if (existingSave != null)
                {
                    var result = MessageBox.Show(
                        $"Overwrite save slot {slotNumber}?",
                        "Save Game",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                if (_currentDialogueIndex.HasValue && _storyService != null)
                {
                    var dialogue = _storyService.GetDialogue(_currentDialogueIndex.Value);
                    var previewText = dialogue != null ? dialogue.Text : "";
                    var gameState = _storyService.GetGameState();

                    var saveData = new SaveData
                    {
                        CurrentDialogueIndex = _currentDialogueIndex.Value,
                        CurrentSceneId = gameState.CurrentSceneId,
                        SaveDate = DateTime.Now,
                        SaveName = $"Save {slotNumber}",
                        PreviewText = previewText.Length > 100 ? previewText.Substring(0, 100) : previewText,
                        GameState = gameState
                    };

                    _saveLoadService.SaveGame(saveData, slotNumber);
                    MessageBox.Show($"Game saved to slot {slotNumber}!", "Save Game", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _soundService.PlayClickSound();
            this.Close();
        }
    }
}

