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
            // очистка старых платформ
            foreach (var visual in _platformVisuals)
                MainCanvas.Children.Remove(visual);
            _platformVisuals.Clear();

            foreach (var platform in _gameVM.Platforms)
            {
                if (_platformTexture == null)
                {
                    AddRectanglePlatform(platform);
                    continue;
                }

                // заполнение через дублирование и обрезание текстуры
                double tileW = _platformTexture.PixelSize.Width;
                double tileH = _platformTexture.PixelSize.Height;

                int tilesX = (int)Math.Ceiling(platform.Width / tileW);
                int tilesY = (int)Math.Ceiling(platform.Height / tileH);

                for (int x = 0; x < tilesX; x++)
                {
                    for (int y = 0; y < tilesY; y++)
                    {
                        double w = Math.Min(tileW, platform.Width - x * tileW);
                        double h = Math.Min(tileH, platform.Height - y * tileH);

                        AddTile(platform.X + x * tileW, platform.Y + y * tileH, w, h);
                    }
                }
            }
        }

        //создание "обрезков" текстуры
        private void AddTile(double left, double top, double width, double height)
        {
            var tile = new Image
            {
                Source = _platformTexture,
                Width = width,
                Height = height,
                Stretch = Stretch.None
            };
            Canvas.SetLeft(tile, left);
            Canvas.SetTop(tile, top);
            MainCanvas.Children.Add(tile);
            _platformVisuals.Add(tile);
        }

        private void AddRectanglePlatform(Platform platform)
        {
            var rect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = platform.Width,
                Height = platform.Height,
                Fill = new SolidColorBrush(Colors.Green), //зеленая заглушка для платформы, если нет текстуру
                Stroke = new SolidColorBrush(Colors.DarkGreen),
                StrokeThickness = 2
            };
            Canvas.SetLeft(rect, platform.X);
            Canvas.SetTop(rect, platform.Y);
            MainCanvas.Children.Add(rect);
            _platformVisuals.Add(rect);
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