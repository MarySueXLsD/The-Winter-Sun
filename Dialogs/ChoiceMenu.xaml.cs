using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VisualNovel.Models;

namespace VisualNovel
{
    public partial class ChoiceMenu : UserControl
    {
        public event Action<Choice>? ChoiceSelected;

        public ChoiceMenu()
        {
            InitializeComponent();
            
            // Load Minecraft font
            var minecraftFont = Services.FontHelper.LoadMinecraftFont();
            if (minecraftFont != null)
            {
                Resources["MinecraftFont"] = minecraftFont;
            }
        }

        public void ShowChoices(List<Choice> choices)
        {
            ChoicesContainer.Children.Clear();

            if (choices == null || choices.Count == 0)
            {
                this.Visibility = Visibility.Collapsed;
                return;
            }

            this.Visibility = Visibility.Visible;

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var button = CreateChoiceButton(choice, i + 1);
                ChoicesContainer.Children.Add(button);
            }
        }

        private Button CreateChoiceButton(Choice choice, int choiceNumber)
        {
            // Create a TextBlock that supports text wrapping for multi-line choices
            var textBlock = new TextBlock
            {
                Text = $"{choiceNumber}. {choice.Text}",
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                FontFamily = (FontFamily)FindResource("MinecraftFont"),
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };

            // Bind TextBlock foreground to Button foreground so it changes with button state
            textBlock.SetBinding(TextBlock.ForegroundProperty, 
                new System.Windows.Data.Binding("Foreground") 
                { 
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1) 
                });

            var button = new Button
            {
                Content = textBlock,
                Style = (Style)FindResource("ChoiceButtonStyle")
            };

            button.Click += (s, e) =>
            {
                ChoiceSelected?.Invoke(choice);
            };

            return button;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
            ChoicesContainer.Children.Clear();
        }
    }
}

