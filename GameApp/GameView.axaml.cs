using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.ViewModels;
using GameApp.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        private readonly GameViewModel _gameVM;
        private readonly PlayerAnimationViewModel _animationVM;


        private readonly List<Control> _platformVisuals = new();
        private Bitmap? _platformTexture;

        private readonly List<Control> _debugPlatformRects = new();
        private Avalonia.Controls.Shapes.Rectangle? _debugPlayerRect;

        private DispatcherTimer _gameTimer;

        private void DrawDebugPlatforms()
        {
            if (_debugPlatformRects.Count == 0)
            {
                foreach (var p in _gameVM.Platforms)
                {
                    var rect = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = p.Width,
                        Height = p.Height,
                        Stroke = new SolidColorBrush(Colors.Red),
                        StrokeThickness = 2,
                        Fill = null
                    };

                    Canvas.SetLeft(rect, p.X);
                    Canvas.SetTop(rect, p.Y);

                    DebugCanvas.Children.Add(rect);
                    _debugPlatformRects.Add(rect);
                }
            }
        }

        private void DrawDebugPlayer()
        {
            if (_debugPlayerRect == null)
            {
                _debugPlayerRect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Stroke = new SolidColorBrush(Colors.Lime),
                    StrokeThickness = 2,
                    Fill = null
                };
                DebugCanvas.Children.Add(_debugPlayerRect);
            }

            _debugPlayerRect.Width = _gameVM.Player.Width;
            _debugPlayerRect.Height = _gameVM.Player.Height;

            Canvas.SetLeft(_debugPlayerRect, _gameVM.Player.X);
            Canvas.SetTop(_debugPlayerRect, _gameVM.Player.Y);
        }


        public GameView(GameViewModel gameVM)
        {
            _gameVM = gameVM;
            _animationVM = new PlayerAnimationViewModel(gameVM.Player);

            InitializeComponent();

            MainCanvas.RenderTransform = new TranslateTransform();

            // Подписка на камеру (без изменений)
            _gameVM.Camera.WhenAnyValue(c => c.X, c => c.Y)
                .Subscribe(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var transform = (TranslateTransform)MainCanvas.RenderTransform!;
                        transform.X = -_gameVM.Camera.X;
                        transform.Y = -_gameVM.Camera.Y;
                    });
                });

            if (_gameVM.IsDebugMode)
            {
                _gameVM.WhenAnyValue(vm => vm.PlayerX, vm => vm.PlayerY)
                    .Subscribe(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            DrawDebugPlayer();
                        });
                    });
            }

            PlayerImage.DataContext = _animationVM;
            DataContext = _gameVM;

            LoadPlatformTexture();
            CreatePlatforms();

            if (_gameVM.IsDebugMode)
            {
                DrawDebugPlatforms();
                DrawDebugPlayer();
            }

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            Activated += (_, __) => Focus();

            // Новый game loop с DispatcherTimer
            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / 120.0),  // TIMER
            };
            _gameTimer.Tick += (sender, e) => _gameVM.UpdateGame();
            _gameTimer.Start();
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
            if (_platformVisuals.Count > 0) return;  // Защита от повторного вызова

            foreach (var platform in _gameVM.Platforms)
            {
                if (_platformTexture == null)
                {
                    AddRectanglePlatform(platform);
                    continue;
                }

                // Тайлинг — без изменений, но один раз
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