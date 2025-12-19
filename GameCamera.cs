using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VisualNovel
{
    public class GameCamera
    {
        private readonly ScaleTransform _zoomTransform;
        private readonly TranslateTransform _panTransform;
        private readonly Border _cameraContainer;
        private readonly Canvas _characterContainer;
        private readonly Window _window;
        
        private double _currentZoom = 1.0;
        private List<int> _activeCharacterSpots = new List<int>();
        private double _defaultPanX = 0.0; // Default pan offset for wider backgrounds
        
        // Character spot positions (1-6, as percentage of screen width from left)
        // Evenly distributed across 6 columns (characters are 550px wide)
        private static readonly double[] SpotPositions = { 0.083, 0.25, 0.417, 0.583, 0.75, 0.917 }; // Spot 1-6 (6-column grid)
        
        // Extended spot positions for ranges beyond 1-6 (0-7 for edge cases)
        // Spot 0 is at -0.083 (left of spot 1), Spot 7 is at 1.083 (right of spot 6)
        private static readonly double[] ExtendedSpotPositions = { -0.083, 0.083, 0.25, 0.417, 0.583, 0.75, 0.917, 1.083 };

        public GameCamera(
            ScaleTransform zoomTransform,
            TranslateTransform panTransform,
            Border cameraContainer,
            Canvas characterContainer,
            Window window)
        {
            _zoomTransform = zoomTransform ?? throw new ArgumentNullException(nameof(zoomTransform));
            _panTransform = panTransform ?? throw new ArgumentNullException(nameof(panTransform));
            _cameraContainer = cameraContainer ?? throw new ArgumentNullException(nameof(cameraContainer));
            _characterContainer = characterContainer ?? throw new ArgumentNullException(nameof(characterContainer));
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public double CurrentZoom => _currentZoom;

        /// <summary>
        /// Updates the list of active character spots and automatically adjusts camera
        /// </summary>
        public void UpdateCharacterSpots(List<int> activeSpots)
        {
            if (activeSpots == null || activeSpots.Count == 0)
            {
                _activeCharacterSpots.Clear();
                // Reset to default view when no characters
                AdjustCameraToCharacters();
                return;
            }

            // Check if spots have actually changed
            var sortedSpots = activeSpots.OrderBy(s => s).ToList();
            if (_activeCharacterSpots.SequenceEqual(sortedSpots))
            {
                return; // No change, skip adjustment
            }

            _activeCharacterSpots = sortedSpots;
            AdjustCameraToCharacters();
        }

        /// <summary>
        /// Sets the character spots without triggering automatic camera adjustment
        /// Used when manual zoom will be applied
        /// </summary>
        public void SetCharacterSpotsWithoutAdjustment(List<int> activeSpots)
        {
            if (activeSpots == null || activeSpots.Count == 0)
            {
                _activeCharacterSpots.Clear();
                return;
            }

            _activeCharacterSpots = activeSpots.OrderBy(s => s).ToList();
        }

        /// <summary>
        /// Sets the default pan offset for when no characters are visible (e.g., for wider backgrounds)
        /// </summary>
        public void SetDefaultPanOffset(double panX)
        {
            _defaultPanX = panX;
        }

        /// <summary>
        /// Automatically adjusts camera zoom and pan to keep all characters visible
        /// </summary>
        private void AdjustCameraToCharacters()
        {
            if (_activeCharacterSpots.Count == 0)
            {
                // No characters, reset to default (which may include centering on wider background)
                AnimateZoomAndPan(1.0, _defaultPanX);
                return;
            }

            // Find min and max character spots
            int minSpot = _activeCharacterSpots.Min();
            int maxSpot = _activeCharacterSpots.Max();

            // Calculate the desired visible range with balanced padding
            // Goal: center the characters with equal padding on both sides when possible
            // If characters at 2 and 5, show 1-6 (balanced)
            // If characters at 2 and 4, show 1-5
            // If characters at 3 and 6, show 2-7
            // If characters at 1, 2, 4, 6, show 0-7
            
            int visibleMinSpot;
            int visibleMaxSpot;
            
            // Special case: if characters are at 2 and 5, show 1-6 for better centering
            if (minSpot == 2 && maxSpot == 5)
            {
                visibleMinSpot = 1;
                visibleMaxSpot = 6;
            }
            // Edge case: if min is 1, extend to 0
            else if (minSpot == 1)
            {
                visibleMinSpot = 0;
                visibleMaxSpot = Math.Min(7, maxSpot + 1);
            }
            // Edge case: if max is 6, extend to 7
            else if (maxSpot == 6)
            {
                visibleMinSpot = Math.Max(0, minSpot - 1);
                visibleMaxSpot = 7;
            }
            // Default: add one spot padding on each side
            else
            {
                visibleMinSpot = Math.Max(0, minSpot - 1);
                visibleMaxSpot = Math.Min(7, maxSpot + 1);
            }

            // Calculate the range we need to show (as percentage of screen width)
            double minPosition = ExtendedSpotPositions[visibleMinSpot];
            double maxPosition = ExtendedSpotPositions[visibleMaxSpot];
            double rangeWidth = maxPosition - minPosition;

            // Calculate zoom needed to show this range
            // rangeWidth is the percentage of screen we want to show (e.g., 0.667 for spots 1-5)
            // To fit rangeWidth into 1.0 (full screen), we need: zoom = 1.0 / rangeWidth
            // Clamp zoom between 0.5 (zoom out to show 200%) and 2.0 (zoom in to show 50%)
            double targetZoom = Math.Max(0.5, Math.Min(2.0, 1.0 / rangeWidth));

            // Calculate pan to center the visible range
            double windowWidth = GetWindowWidth();
            double centerPosition = (minPosition + maxPosition) / 2.0;
            
            // The center position in screen coordinates (0.0 to 1.0 range)
            // We want this center position to align with screen center (0.5)
            double screenCenterPosition = 0.5;
            
            // Pan calculation: when zoomed, we need to shift the view
            // The pan is the difference between where we want the center to be (screenCenterPosition)
            // and where it currently is (centerPosition), adjusted for zoom
            // Since zoom center is at 0.5 (50% of screen), we calculate pan relative to that
            double positionOffset = (screenCenterPosition - centerPosition) * windowWidth;
            
            // When zoomed in (zoom > 1), the pan needs to be scaled by the zoom factor
            // Formula: pan = offset * (zoom - 1) to move the view
            double panX = positionOffset * (targetZoom - 1.0);

            // Clamp pan to prevent going too far off-screen
            // Maximum pan is limited by how much we can zoom and the window size
            double maxPan = windowWidth * (targetZoom - 1.0) / 2.0;
            panX = Math.Max(-maxPan, Math.Min(maxPan, panX));

            AnimateZoomAndPan(targetZoom, panX);
        }

        /// <summary>
        /// Animates both zoom and pan together
        /// </summary>
        private void AnimateZoomAndPan(double targetZoom, double targetPanX)
        {
            // Get current animated values BEFORE stopping animations
            // In WPF, when you stop an animation with null, the property returns to its base value
            // So we need to get the current animated value first
            double currentZoomX = (double)_zoomTransform.GetValue(ScaleTransform.ScaleXProperty);
            double currentPanX = (double)_panTransform.GetValue(TranslateTransform.XProperty);
            
            // Only animate if there's a significant change
            if (Math.Abs(currentZoomX - targetZoom) < 0.01 && Math.Abs(currentPanX - targetPanX) < 1.0)
            {
                return;
            }

            // Stop any ongoing animations and set the current values as base values
            // This preserves the current visual state
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _panTransform.BeginAnimation(TranslateTransform.XProperty, null);
            
            // Set the current animated values as the new base values
            _zoomTransform.ScaleX = currentZoomX;
            _zoomTransform.ScaleY = currentZoomX; // Keep Y in sync with X
            _panTransform.X = currentPanX;

            var zoomAnimation = new DoubleAnimation
            {
                From = currentZoomX,
                To = targetZoom,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var panAnimation = new DoubleAnimation
            {
                From = currentPanX,
                To = targetPanX,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            _zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, zoomAnimation);
            _zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, zoomAnimation);
            _panTransform.BeginAnimation(TranslateTransform.XProperty, panAnimation);
            
            _currentZoom = targetZoom;
        }

        /// <summary>
        /// Legacy method for manual zoom control (kept for backward compatibility)
        /// </summary>
        public void ApplyZoom(double? targetZoom)
        {
            // If manual zoom is specified, use it; otherwise use automatic adjustment
            if (targetZoom.HasValue && targetZoom.Value > 0)
            {
                // Manual zoom mode - calculate pan based on current character positions
                double panX = 0;
                if (targetZoom.Value > 1.0 && _activeCharacterSpots.Count >= 2)
                {
                    // Calculate center of characters for pan
                    double windowWidth = GetWindowWidth();
                    var positions = _activeCharacterSpots.Select(s => SpotPositions[s - 1] * windowWidth).ToList();
                    double centerX = (positions.Min() + positions.Max()) / 2.0;
                    panX = (windowWidth / 2 - centerX) * (targetZoom.Value - 1.0) * 0.3;
                    double maxPan = (windowWidth * (targetZoom.Value - 1.0)) / 3;
                    panX = Math.Max(-maxPan, Math.Min(maxPan, panX));
                }
                
                AnimateZoomAndPan(targetZoom.Value, panX);
            }
            else
            {
                // No manual zoom specified, use automatic adjustment
                AdjustCameraToCharacters();
            }
        }

        public double GetWindowWidth()
        {
            return _characterContainer.ActualWidth > 0 ? _characterContainer.ActualWidth : 
                   (_cameraContainer.ActualWidth > 0 ? _cameraContainer.ActualWidth : 
                   (_window.ActualWidth > 0 ? _window.ActualWidth : System.Windows.SystemParameters.PrimaryScreenWidth));
        }

        /// <summary>
        /// Centers the camera on a wider background image
        /// </summary>
        public void CenterOnBackgroundImage(double imagePixelWidth, double imagePixelHeight, double viewportWidth, double viewportHeight)
        {
            // Calculate the aspect ratios
            double imageAspectRatio = imagePixelWidth / imagePixelHeight;
            double viewportAspectRatio = viewportWidth / viewportHeight;

            // With UniformToFill, the image is scaled to fill the viewport while maintaining aspect ratio
            // If image is wider than viewport aspect ratio, it will be cropped on the sides
            if (imageAspectRatio > viewportAspectRatio)
            {
                // Image is wider - calculate how much wider the rendered image is than the viewport
                // The image is scaled to fill the height, so: renderedWidth = viewportHeight * imageAspectRatio
                double renderedWidth = viewportHeight * imageAspectRatio;
                double excessWidth = renderedWidth - viewportWidth;
                
                // Pan to center: move left by half the excess width
                // Negative pan moves left, positive moves right
                double panX = -excessWidth / 2.0;
                
                // Animate to center position with zoom at 1.0
                AnimateZoomAndPan(1.0, panX);
            }
            else
            {
                // Image is not wider, reset to default position
                AnimateZoomAndPan(1.0, 0);
            }
        }
    }
}

