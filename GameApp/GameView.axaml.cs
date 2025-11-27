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
using GameApp.ViewModels; // ← добавь этот using!
using System;
using System.Collections.Generic;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        private readonly GameViewModel _gameVM;                    // ← храним ссылку на GameViewModel
        private readonly PlayerAnimationViewModel _animationVM;    // ← и на анимацию
        private readonly List<Control> _platformVisuals = new();
        private Bitmap? _platformTexture;

        // ← Новый конструктор с параметром
        public GameView(GameViewModel gameVM)
        {
            _gameVM = gameVM;
            _animationVM = new PlayerAnimationViewModel(gameVM.Player);

            InitializeComponent();

            // Устанавливаем DataContext на анимацию (для биндингов X, Y, CurrentFrameBitmap)
            DataContext = _animationVM;

            LoadPlatformTexture();
            CreatePlatforms(); // создаём платформы сразу

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            Activated += (_, __) => Focus();
        }

        private void LoadPlatformTexture()
        {
            try
            {
                var uri = new Uri("avares://GameApp/Assets/platform.png");
                _platformTexture = new Bitmap(AssetLoader.Open(uri));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Texture load error: {ex.Message}");
                _platformTexture = null;
            }
        }

        private void CreatePlatforms()
        {
            foreach (var visual in _platformVisuals)
                MainCanvas.Children.Remove(visual);
            _platformVisuals.Clear();

            foreach (var platform in _gameVM.Platforms)
            {
                Control visual = _platformTexture != null
                    ? new Image
                    {
                        Width = platform.Width,
                        Height = platform.Height,
                        Source = _platformTexture,
                        Stretch = Stretch.Fill
                    }
                    : new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = platform.Width,
                        Height = platform.Height,
                        Fill = new SolidColorBrush(Colors.Green),
                        Stroke = new SolidColorBrush(Colors.DarkGreen),
                        StrokeThickness = 2
                    };

                Canvas.SetLeft(visual, platform.X);
                Canvas.SetTop(visual, platform.Y);
                MainCanvas.Children.Add(visual);
                _platformVisuals.Add(visual);
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var action = ConvertKeyToAction(e.Key);
            if (action.HasValue)
            {
                _gameVM.StartAction(action.Value);
                e.Handled = true;
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            var action = ConvertKeyToAction(e.Key);
            if (action.HasValue)
            {
                _gameVM.StopAction(action.Value);
                e.Handled = true;
            }
        }

        private GameAction? ConvertKeyToAction(Key key)
        {
            return key switch
            {
                Key.Left or Key.A => GameAction.MoveLeft,
                Key.Right or Key.D => GameAction.MoveRight,
                Key.Up or Key.W or Key.Space => GameAction.Jump,
                _ => null
            };
        }
    }
}