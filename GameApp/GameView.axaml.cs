using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.ViewModels;
using System;
using System.Collections.Generic;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        private List<Control> _platformVisuals = new List<Control>(); // Изменили тип на Control
        private GameViewModel _viewModel;
        private Bitmap _platformTexture;

        public GameView()
        {
            InitializeComponent();
            LoadPlatformTexture();

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            Activated += OnActivated;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            _viewModel = DataContext as GameViewModel;
            if (_viewModel != null)
            {
                CreatePlatforms();
            }
        }

        private void LoadPlatformTexture()
        {
            try
            {
                var uri = new Uri("avares://GameApp/Assets/platform.png");
                _platformTexture = new Bitmap(AssetLoader.Open(uri));
                System.Diagnostics.Debug.WriteLine("Platform texture loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load platform texture: {ex.Message}");
                _platformTexture = null;
            }
        }

        private void CreatePlatforms()
        {
            // Очищаем старые платформы
            foreach (var visual in _platformVisuals)
            {
                MainCanvas.Children.Remove(visual);
            }
            _platformVisuals.Clear();

            // Создаем новые платформы
            if (_viewModel != null)
            {
                foreach (var platform in _viewModel.Platforms)
                {
                    if (_platformTexture != null)
                    {
                        // Используем кэшированную текстуру
                        var platformImage = new Image
                        {
                            Width = platform.Width,
                            Height = platform.Height,
                            Source = _platformTexture,
                            Stretch = Stretch.Fill
                        };

                        Canvas.SetLeft(platformImage, platform.X);
                        Canvas.SetTop(platformImage, platform.Y);

                        MainCanvas.Children.Add(platformImage);
                        _platformVisuals.Add(platformImage);
                    }
                    else
                    {
                        // Fallback - создаем цветной прямоугольник
                        CreateFallbackPlatform(platform);
                    }
                }
            }
        }

        private void CreateFallbackPlatform(Platform platform)
        {
            try
            {
                var rectangle = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = platform.Width,
                    Height = platform.Height,
                    Fill = new SolidColorBrush(Colors.Green),
                    Stroke = new SolidColorBrush(Colors.DarkGreen),
                    StrokeThickness = 2
                };

                Canvas.SetLeft(rectangle, platform.X);
                Canvas.SetTop(rectangle, platform.Y);
                MainCanvas.Children.Add(rectangle);
                _platformVisuals.Add(rectangle);

                System.Diagnostics.Debug.WriteLine($"Created fallback platform at ({platform.X}, {platform.Y})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create fallback platform: {ex.Message}");
            }
        }

        private void OnActivated(object sender, EventArgs e)
        {
            Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = DataContext as GameViewModel;
            if (viewModel == null) return;

            var action = ConvertKeyToAction(e.Key, true);
            if (action.HasValue)
            {
                viewModel.StartAction(action.Value);
                e.Handled = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            var viewModel = DataContext as GameViewModel;
            if (viewModel == null) return;

            var action = ConvertKeyToAction(e.Key, false);
            if (action.HasValue)
            {
                viewModel.StopAction(action.Value);
                e.Handled = true;
            }
        }

        private GameAction? ConvertKeyToAction(Key key, bool isKeyDown)
        {
            switch (key)
            {
                case Key.Left:
                case Key.A:
                    return GameAction.MoveLeft;
                case Key.Right:
                case Key.D:
                    return GameAction.MoveRight;
                case Key.Up:
                case Key.W:
                case Key.Space:
                    return GameAction.Jump;
                default:
                    return null;
            }
        }
    }
}