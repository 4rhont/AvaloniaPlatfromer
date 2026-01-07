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
using GameApp.Core.Services;
using GameApp.Core.ViewModels;
using GameApp.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        private readonly GameViewModel _gameVM;
        private readonly PlayerAnimationViewModel _animationVM;
        private DispatcherTimer _gameTimer;
        private Stopwatch _stopwatch = new Stopwatch();

        private readonly Dictionary<Platform, List<Control>> _platformVisualMap = new();
        private readonly Dictionary<Platform, Control> _debugPlatformMap = new();
        private Avalonia.Controls.Shapes.Rectangle? _debugAttackRect;

        private const double VisibilityBuffer = 200;

        private Bitmap? _platformTexture;

        // Пока что неп юзается, на будущее:
        private readonly Dictionary<Enemy, Image> _enemyVisualMap = new();
        private readonly Dictionary<Enemy, Control> _debugEnemyMap = new();
        private Bitmap? _enemyTexture;

        private Avalonia.Controls.Shapes.Rectangle? _debugPlayerRect;


        public GameView(GameViewModel gameVM)
        {
            _gameVM = gameVM;
            _animationVM = new PlayerAnimationViewModel(_gameVM.Player, _gameVM);

            InitializeComponent();

            MainCanvas.RenderTransform = new TranslateTransform();

            // Подписка на камеру
            _gameVM.Camera.WhenAnyValue(c => c.X, c => c.Y)
                .Subscribe(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var transform = (TranslateTransform)MainCanvas.RenderTransform!;
                        transform.X = -_gameVM.Camera.X;
                        transform.Y = -_gameVM.Camera.Y;

                        UpdatePlatformVisibility();
                        UpdateEnemyVisibility();
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

                _gameVM.Player.WhenAnyValue(
                    p => p.IsAttacking,
                    p => p.X,
                    p => p.Y,
                    p => p.IsFacingRight
                ).Subscribe(_ => Dispatcher.UIThread.Post(UpdateDebugAttackRect));

                InitDebugAttackRect();
            }

            PlayerImage.DataContext = _animationVM;
            _gameVM.WhenAnyValue(vm => vm.InterpolationAlpha)
    .Subscribe(_ => _animationVM.NotifyRenderPositionChanged());
            DataContext = _gameVM;
            gameVM.OnAttackTriggered += _animationVM.TriggerAttack;

            // Weather VM
            WeatherLayer.DataContext = new WeatherViewModel();

            LoadPlatformTexture();
            CreatePlatforms();

            
            var bgFar = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/background_far.png"))),
                Width = 5000,        // игровые единицы
                Height = 1500,
                Stretch = Stretch.None, // не растягиваем, берём оригинальный размер
                IsHitTestVisible = false
            };
            Canvas.SetLeft(bgFar, 0);
            Canvas.SetTop(bgFar, 0);


            MainCanvas.Children.Insert(0, bgFar);  // самый нижний слой

            // Слой 2: средний
            var bgMid = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/background_mid.png"))),
                Width = 5000,
                Height = 1500,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(bgMid, 0);
            Canvas.SetTop(bgMid, 0);
            MainCanvas.Children.Insert(1, bgMid);  // выше дальнего

            // Слой 3: ближний (быстрее)
            var bgNear = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/background_near.png"))),
                Width = 5000,
                Height = 1500,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(bgNear, 0);
            Canvas.SetTop(bgNear, 0);
            MainCanvas.Children.Insert(2, bgNear);  // выше среднего

            var foregroundImage = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/foreground.png"))),
                Width = 5000,
                Height = 1500,
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(foregroundImage, 0);
            Canvas.SetTop(foregroundImage, 0);

                MainCanvas.Children.Add(foregroundImage);
            

            // === PARALLAX: двигаем слои с разной скоростью ===
            _gameVM.Camera.WhenAnyValue(c => c.X, c => c.Y)
                .Subscribe(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        // Основной мир (платформы, игрок)
                        var transform = (TranslateTransform)MainCanvas.RenderTransform!;
                        transform.X = -_gameVM.Camera.X;
                        transform.Y = -_gameVM.Camera.Y;

                        // Parallax для фонов
                        Canvas.SetLeft(bgFar, -_gameVM.Camera.X * 0.1);   // очень медленно
                        Canvas.SetLeft(bgMid, -_gameVM.Camera.X * 0.12);   // средне
                        Canvas.SetLeft(bgNear, -_gameVM.Camera.X * 0.16);  // быстро


                        UpdatePlatformVisibility();
                        UpdateEnemyVisibility();
                    });
                });



            if (_gameVM.IsDebugMode)
            {
                DrawDebugPlatforms();
                DrawDebugEnemies();
                DrawDebugPlayer();
            }

            UpdatePlatformVisibility();

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            Activated += (_, __) => Focus();

            _stopwatch = Stopwatch.StartNew();

            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4) };  // FPS
            _gameTimer.Tick += OnTick;
            _gameTimer.Start();

        }

        private void InitDebugAttackRect()
        {
            _debugAttackRect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = PhysicsService.AttackHitboxWidth,
                Height = PhysicsService.AttackHitboxHeight,
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 2,
                Fill = null,
                IsVisible = false
            };
            DebugCanvas.Children.Add(_debugAttackRect);
        }

        private void UpdateDebugAttackRect()
        {
            if (_debugAttackRect == null || !_gameVM.Player.IsAttacking)
            {
                if (_debugAttackRect != null) _debugAttackRect.IsVisible = false;
                return;
            }

            // Вычисляем позицию хитбокса (аналогично PhysicsService)
            double hitboxX = _gameVM.Player.IsFacingRight
                ? _gameVM.Player.Right + PhysicsService.AttackHitboxOffsetX
                : _gameVM.Player.X - PhysicsService.AttackHitboxWidth - PhysicsService.AttackHitboxOffsetX;
            double hitboxY = _gameVM.Player.Y + PhysicsService.AttackHitboxOffsetY;

            Canvas.SetLeft(_debugAttackRect, hitboxX);
            Canvas.SetTop(_debugAttackRect, hitboxY);
            _debugAttackRect.IsVisible = true;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var deltaTime = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            if (deltaTime <= 0) deltaTime = 1.0 /240.0;  // Предотвращаем нулевые delta

            // Обновляем физику
            _gameVM.UpdateGame(deltaTime);

            // Обновляем анимации и погоду
            _animationVM.UpdateAnimation(deltaTime);

            if (WeatherLayer.DataContext is WeatherViewModel weatherVM)
            {
                weatherVM.Update(deltaTime);
            }

            _animationVM.NotifyRenderPositionChanged();
        }

        private void UpdateEnemyVisibility()
        {
            if (!_gameVM.IsDebugMode) return;

            double left = _gameVM.Camera.X - VisibilityBuffer;
            double top = _gameVM.Camera.Y - VisibilityBuffer;
            double right = _gameVM.Camera.X + _gameVM.Camera.ViewportWidth + VisibilityBuffer;
            double bottom = _gameVM.Camera.Y + _gameVM.Camera.ViewportHeight + VisibilityBuffer;

            foreach (var kvp in _debugEnemyMap)
            {
                var enemy = kvp.Key;
                var rect = kvp.Value;
                bool isVisible = enemy.X < right && enemy.Right > left && enemy.Y < bottom && enemy.Bottom > top;
                rect.IsVisible = isVisible;
            }
        }

        private void UpdatePlatformVisibility()
        {
            double left = _gameVM.Camera.X - VisibilityBuffer;
            double top = _gameVM.Camera.Y - VisibilityBuffer;
            double right = _gameVM.Camera.X + _gameVM.Camera.ViewportWidth + VisibilityBuffer;
            double bottom = _gameVM.Camera.Y + _gameVM.Camera.ViewportHeight + VisibilityBuffer;

            foreach (var kvp in _platformVisualMap)
            {
                var platform = kvp.Key;
                var visuals = kvp.Value;

                bool isVisible = platform.X < right && platform.Right > left && platform.Y < bottom && platform.Bottom > top;

                foreach (var visual in visuals)
                {
                    visual.IsVisible = isVisible;
                }
            }

            if (_gameVM.IsDebugMode)
            {
                foreach (var kvp in _debugPlatformMap)
                {
                    var platform = kvp.Key;
                    var rect = kvp.Value;

                    bool isVisible = platform.X < right && platform.Right > left && platform.Y < bottom && platform.Bottom > top;

                    rect.IsVisible = isVisible;
                }
            }
        }

        private void DrawDebugPlatforms()
        {
            if (_debugPlatformMap.Count > 0) return;

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
                _debugPlatformMap[p] = rect;
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

        private void DrawDebugEnemies()
        {
            foreach (var enemy in _gameVM.Enemies)
            {
                var rect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = enemy.Width,
                    Height = enemy.Height,
                    Fill = new SolidColorBrush(Colors.Purple), 
                    Stroke = new SolidColorBrush(Colors.Violet),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(rect, enemy.X);
                Canvas.SetTop(rect, enemy.Y);
                DebugCanvas.Children.Add(rect);
                _debugEnemyMap[enemy] = rect;

                // Подписка на изменения позиции (если враги будут двигаться)
                enemy.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(Enemy.X))
                    {
                        Canvas.SetLeft(rect, enemy.X);
                    }
                    if (e.PropertyName == nameof(Enemy.Y))
                    {
                        Canvas.SetTop(rect, enemy.Y);
                    }
                };
            }
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
            //if (_platformVisuals.Count > 0) return;

            foreach (var platform in _gameVM.Platforms)
            {
                var visuals = new List<Control>();

                if (_platformTexture == null)
                {
                    var rect = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = platform.Width,
                        Height = platform.Height,
                        Fill = new SolidColorBrush(Colors.Green),
                        Stroke = new SolidColorBrush(Colors.DarkGreen),
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(rect, platform.X);
                    Canvas.SetTop(rect, platform.Y);
                    MainCanvas.Children.Add(rect);
                    visuals.Add(rect);
                    continue;
                }

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

                        var tile = new Image
                        {
                            Source = _platformTexture,
                            Width = w,
                            Height = h,
                            Stretch = Stretch.None
                        };
                        Canvas.SetLeft(tile, platform.X + x * tileW);
                        Canvas.SetTop(tile, platform.Y + y * tileH);
                        MainCanvas.Children.Add(tile);
                        visuals.Add(tile);
                    }
                }

                _platformVisualMap[platform] = visuals;
            }
        }

        private Image CreateParallaxBackground(string assetPath, double scaleFactor)
        {
            var image = new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri(assetPath))),
                Width = 10000,   // большая ширина для покрытия уровня
                Height = 3000,   // большая высота
                Stretch = Stretch.UniformToFill,
                IsHitTestVisible = false,
                RenderTransformOrigin = new RelativePoint(0, 1, RelativeUnit.Relative)  // ТОЧКА МАСШТАБА: левый нижний угол
            };

            // Масштабирование от низа
            var scale = new ScaleTransform(scaleFactor, scaleFactor);
            image.RenderTransform = scale;

            // Прижимаем к левому нижнему углу
            Canvas.SetLeft(image, 0);
            Canvas.SetBottom(image, 0);

            return image;
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

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this);
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                _gameVM.StartAction(GameAction.Attack);
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