using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace VisualNovel
{
    /// <summary>
    /// Chroma key effect to make a specific color transparent
    /// Attempts to load compiled pixel shader, falls back to no-op if not available
    /// </summary>
    public class ChromaKeyEffect : ShaderEffect
    {
        private static readonly PixelShader _pixelShader;

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty("Input", typeof(ChromaKeyEffect), 0);

        public static readonly DependencyProperty ColorKeyProperty =
            DependencyProperty.Register("ColorKey", typeof(Color), typeof(ChromaKeyEffect),
                new UIPropertyMetadata(Colors.Black, PixelShaderConstantCallback(0)));

        public static readonly DependencyProperty ToleranceProperty =
            DependencyProperty.Register("Tolerance", typeof(double), typeof(ChromaKeyEffect),
                new UIPropertyMetadata(0.3, PixelShaderConstantCallback(1)));

        static ChromaKeyEffect()
        {
            _pixelShader = new PixelShader();
            try
            {
                // Try to load compiled shader from resources
                // For now, we'll create a placeholder - shader needs to be compiled separately
                // using fxc.exe: fxc /T ps_2_0 /E main /Fo ChromaKey.ps.cs ChromaKey.ps
                string shaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChromaKey.ps.cs");
                if (File.Exists(shaderPath))
                {
                    _pixelShader.UriSource = new Uri(shaderPath, UriKind.Absolute);
                }
                else
                {
                    // Try embedded resource
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "VisualNovel.ChromaKey.ps.cs";
                    var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        _pixelShader.UriSource = new Uri("pack://application:,,,/ChromaKey.ps.cs");
                    }
                }
            }
            catch
            {
                // Shader not available - effect will be a no-op
            }
        }

        public ChromaKeyEffect()
        {
            PixelShader = _pixelShader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ColorKeyProperty);
            UpdateShaderValue(ToleranceProperty);
        }

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public Color ColorKey
        {
            get => (Color)GetValue(ColorKeyProperty);
            set => SetValue(ColorKeyProperty, value);
        }

        public double Tolerance
        {
            get => (double)GetValue(ToleranceProperty);
            set => SetValue(ToleranceProperty, value);
        }
    }
}

