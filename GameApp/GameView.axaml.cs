using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GameApp.Converters;
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
using System.Threading.Tasks;

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
        private readonly Dictionary<Enemy, EnemyAnimationViewModel> _enemyAnimationMap = new();
        private Avalonia.Controls.Shapes.Rectangle? _debugAttackRect;

        private readonly ObservableCollection<EnemyAnimationViewModel> _enemyViewModels = new();
        public ObservableCollection<EnemyAnimationViewModel> EnemyViewModels => _enemyViewModels;


        private readonly Dictionary<string, Avalonia.Controls.Shapes.Rectangle> debugEndZoneMap = new();
        private const double VisibilityBuffer = 200;

        private Bitmap? _platformTexture;
        private Bitmap? _damagingPlatformTexture;
        private Bitmap? _hp5Texture, _hp4Texture, _hp3Texture, _hp2Texture, _hp1Texture;

        
        private readonly Dictionary<Enemy, Image> _enemyVisualMap = new();
        private readonly Dictionary<Enemy, Control> _debugEnemyMap = new();
        private Bitmap? _enemyTexture;

        private Avalonia.Controls.Shapes.Rectangle? _debugPlayerRect;


        public GameView(GameViewModel gameVM)
        {
            _gameVM = gameVM;
            _animationVM = new PlayerAnimationViewModel(_gameVM.Player, _gameVM);
            InitializeComponent();

            this.Focus();

            LoadHpTextures();
            UpdateHpBar();
            // Подписка на изменение здоровья игрока
            _gameVM.Player.PropertyChanged += (s, e) =>
            {
                Debug.WriteLine($"Player PropertyChanged: {e.PropertyName} = {_gameVM.Player.CurrentHealth}");

                if (e.PropertyName == nameof(Player.CurrentHealth))
                {
                    Debug.WriteLine($"HP изменилось: {_gameVM.Player.CurrentHealth}/{_gameVM.Player.MaxHealth}");
                    Dispatcher.UIThread.Post(UpdateHpBar);
                }
            };

            MainCanvas.RenderTransform = new TranslateTransform();

            PlayerImage.DataContext = _animationVM;
            DataContext = _gameVM;

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

            _gameVM.Enemies.CollectionChanged += (sender, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (_gameVM.IsDebugMode)
                        foreach (Enemy removedEnemy in e.OldItems!)
                        {
                            if (_gameVM.IsDebugMode)
                                if (_debugEnemyMap.TryGetValue(removedEnemy, out var rect))
                                {
                                    DebugCanvas.Children.Remove(rect);
                                    _debugEnemyMap.Remove(removedEnemy);
                                }
                        }

                    foreach (Enemy removedEnemy in e.OldItems!)
                    {
                        if (_enemyVisualMap.TryGetValue(removedEnemy, out var image))
                        {
                            MainCanvas.Children.Remove(image);
                            _enemyVisualMap.Remove(removedEnemy);
                        }
                    }
                }
            };

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
                    p => p.IsFacingRight,
                    p => p.AttackProgress
                ).Subscribe(_ => Dispatcher.UIThread.Post(UpdateDebugAttackRect));

                InitDebugAttackRect();
            }


            _gameVM.WhenAnyValue(vm => vm.InterpolationAlpha).Subscribe(_ => _animationVM.NotifyRenderPositionChanged());


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

            foreach (var enemy in _gameVM.Enemies)
            {
                var animVM = new EnemyAnimationViewModel(enemy, _gameVM);
                EnemyViewModels.Add(animVM);

                // Создаем Image для врага
                var enemyImage = new Image
                {
                    Width = 150,
                    Height = 150,
                    Source = animVM.CurrentFrameBitmap,
                    Stretch = Stretch.UniformToFill,
                    Opacity = 1.0,
                    IsHitTestVisible = false
                };

                // Создаем ScaleTransform для зеркалирования
                var scaleTransform = new ScaleTransform
                {
                    ScaleX = 1.0, // Начальное значение - смотрит вправо
                    ScaleY = 1.0
                };
                enemyImage.RenderTransform = scaleTransform;
                enemyImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

                // Устанавливаем начальную позицию
                Canvas.SetLeft(enemyImage, enemy.X);
                Canvas.SetTop(enemyImage, enemy.Y);

                MainCanvas.Children.Insert(3, enemyImage);

                Debug.WriteLine($"Враг создан в ({enemy.X}, {enemy.Y})");

                // ПОДПИСКА 1: на изменения позиции врага
                enemy.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Enemy.X))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Canvas.SetLeft(enemyImage, enemy.X);
                        });
                    }
                    if (e.PropertyName == nameof(Enemy.Y))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Canvas.SetTop(enemyImage, enemy.Y);
                        });
                    }

                    // ЗЕРКАЛИРОВАНИЕ по VelocityX
                    if (e.PropertyName == nameof(Enemy.VelocityX))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Если VelocityX > 0 - смотрит вправо (ScaleX = 1)
                            // Если VelocityX < 0 - смотрит влево (ScaleX = -1)
                            // Если VelocityX = 0 - оставляем как есть
                            if (Math.Abs(enemy.VelocityX) > 0.1) // Небольшой порог для устранения дребезга
                            {
                                double scaleX = enemy.VelocityX > 0 ? -1.0 : 1.0;
                                scaleTransform.ScaleX = scaleX;

                                Debug.WriteLine($"Враг направление: VelocityX={enemy.VelocityX}, ScaleX={scaleX}");
                            }
                        });
                    }
                };

                // ПОДПИСКА 2: на изменение анимации
                animVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(EnemyAnimationViewModel.CurrentFrameBitmap))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            enemyImage.Source = animVM.CurrentFrameBitmap;
                        });
                    }
                };
            }

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
            //MainCanvas.Children.Insert(3, foregroundImage);


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
                DrawDebugEndZone();
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

            gameVM.OnLevelLoaded += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RecreatePlatforms();
                    RecreateEnemyViewModels();

                    // Пересоздать дебаг-элементы
                    if (gameVM.IsDebugMode)
                    {
                        DrawDebugPlatforms();
                        DrawDebugEnemies();
                        DrawDebugEndZone();
                        
                    }
                });
            };
        }

        private void RecreateEnemyViewModels()
        {
            // Очистить старые
            _enemyViewModels.Clear();

            // Удалить старые визуальные элементы врагов из Canvas
            foreach (var image in _enemyVisualMap.Values)
            {
                MainCanvas.Children.Remove(image);
            }
            _enemyVisualMap.Clear();

            // Удалить дебаг-прямоугольники
            if (_gameVM.IsDebugMode)
            {
                foreach (var rect in _debugEnemyMap.Values)
                {
                    DebugCanvas.Children.Remove(rect);
                }
                _debugEnemyMap.Clear();
            }

            // Создать новые ViewModels для всех врагов в текущем уровне
            foreach (var enemy in _gameVM.Enemies)
            {
                var animVM = new EnemyAnimationViewModel(enemy, _gameVM);
                _enemyViewModels.Add(animVM);

                // Создать визуальный элемент врага
                var enemyImage = new Image
                {
                    Width = 150,
                    Height = 150,
                    Source = animVM.CurrentFrameBitmap,
                    Stretch = Stretch.UniformToFill,
                    Opacity = 1.0,
                    IsHitTestVisible = false
                };

                var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
                enemyImage.RenderTransform = scaleTransform;
                enemyImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

                Canvas.SetLeft(enemyImage, enemy.X);
                Canvas.SetTop(enemyImage, enemy.Y);

                MainCanvas.Children.Insert(3, enemyImage);
                _enemyVisualMap[enemy] = enemyImage;

                // Подписаться на изменения позиции врага
                enemy.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Enemy.X))
                    {
                        Dispatcher.UIThread.Post(() => Canvas.SetLeft(enemyImage, enemy.X));
                    }
                    if (e.PropertyName == nameof(Enemy.Y))
                    {
                        Dispatcher.UIThread.Post(() => Canvas.SetTop(enemyImage, enemy.Y));
                    }
                    if (e.PropertyName == nameof(Enemy.VelocityX))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (Math.Abs(enemy.VelocityX) > 0.1)
                            {
                                double scaleX = enemy.VelocityX > 0 ? -1.0 : 1.0;
                                scaleTransform.ScaleX = scaleX;
                            }
                        });
                    }
                };

                // Подписаться на изменения фрейма анимации
                animVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(EnemyAnimationViewModel.CurrentFrameBitmap))
                    {
                        Dispatcher.UIThread.Post(() => enemyImage.Source = animVM.CurrentFrameBitmap);
                    }
                };

                // Добавить дебаг-прямоугольник если нужно
                if (_gameVM.IsDebugMode)
                {
                    var rect = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = enemy.Width,
                        Height = enemy.Height,
                        Fill = null,
                        Stroke = new SolidColorBrush(Colors.Violet),
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(rect, enemy.X);
                    Canvas.SetTop(rect, enemy.Y);
                    DebugCanvas.Children.Add(rect);
                    _debugEnemyMap[enemy] = rect;
                }
            }
        }

        private void RecreatePlatforms()
        {
            // Очистить визуальные элементы платформ
            foreach (var visuals in _platformVisualMap.Values)
            {
                foreach (var visual in visuals)
                {
                    MainCanvas.Children.Remove(visual);
                }
            }
            _platformVisualMap.Clear();

            // Очистить дебаг-элементы
            if (_gameVM.IsDebugMode)
            {
                foreach (var rect in _debugPlatformMap.Values)
                {
                    DebugCanvas.Children.Remove(rect);
                }
                _debugPlatformMap.Clear();
            }

            // Пересоздать все платформы
            CreatePlatforms();
        }

        private void InitDebugAttackRect()
        {
            _debugAttackRect = new Avalonia.Controls.Shapes.Rectangle
            {
                Width = 0,
                Height = Player.AttackHitboxHeight,
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
            double progress = _gameVM.Player.AttackProgress;
            double currentWidth = Player.AttackHitboxWidth * progress;


            double hitboxX = _gameVM.Player.IsFacingRight
                ? _gameVM.Player.Right + Player.AttackHitboxOffsetX
                : _gameVM.Player.X - currentWidth - Player.AttackHitboxOffsetX;
            double hitboxY = _gameVM.Player.Y + Player.AttackHitboxOffsetY;

            _debugAttackRect.Width = currentWidth;

            Canvas.SetLeft(_debugAttackRect, hitboxX);
            Canvas.SetTop(_debugAttackRect, hitboxY);
            _debugAttackRect.IsVisible = true;
        }


        private void OnTick(object? sender, EventArgs e)
        {
            var deltaTime = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            if (deltaTime <= 0) deltaTime = 1.0 / 240.0;  // Предотвращаем нулевые delta

            // Обновляем физику
            _gameVM.UpdateGame(deltaTime);

            // Обновляем анимации и погоду
            _animationVM.UpdateAnimation(deltaTime);

            foreach (var vm in EnemyViewModels)
            {
                vm.UpdateAnimation(deltaTime);
            }

            if (WeatherLayer.DataContext is WeatherViewModel weatherVM)
            {
                weatherVM.Update(deltaTime);
            }

            _animationVM.NotifyRenderPositionChanged();
        }

        private void UpdateEnemyVisibility()
        {

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
            foreach (var vm in EnemyViewModels)
            {
                bool isVisible = vm.VisualX < right && (vm.VisualX + vm.VisualWidth) > left && vm.VisualY < bottom && (vm.VisualY + vm.VisualHeight) > top;
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

        private void DrawDebugEndZone()
        {
            if (!debugEndZoneMap.ContainsKey("end") && _gameVM.HasEndZone)
            {
                var rect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = _gameVM.EndZoneWidth,
                    Height = _gameVM.EndZoneHeight,
                    Stroke = new SolidColorBrush(Colors.Blue),
                    StrokeThickness = 3,
                    Fill = null
                };
                Canvas.SetLeft(rect, _gameVM.EndZoneX);
                Canvas.SetTop(rect, _gameVM.EndZoneY);
                DebugCanvas.Children.Add(rect);
                debugEndZoneMap["end"] = rect;
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
                    Fill = null, 
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



        private void LoadHpTextures()
        {
            try
            {
                _hp5Texture = LoadTexture("5hp.png");
                _hp4Texture = LoadTexture("4hp.png");
                _hp3Texture = LoadTexture("3hp.png");
                _hp2Texture = LoadTexture("2hp.png");
                _hp1Texture = LoadTexture("1hp.png");

                Debug.WriteLine("HP текстуры загружены");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки HP текстур: {ex.Message}");
            }
        }


        private Bitmap? LoadTexture(string filename)
        {
            try
            {
                var uri = new Uri($"avares://GameApp/Assets/{filename}");
                return new Bitmap(AssetLoader.Open(uri));
            }
            catch
            {
                Debug.WriteLine($"Файл {filename} не найден");
                return null;
            }
        }

        private void UpdateHpBar()
        {
            if (_gameVM?.Player == null || HpBarImage == null) return;

            int currentHp = _gameVM.Player.CurrentHealth;

            // Выбираем нужную текстуру
            Bitmap? texture = currentHp switch
            {
                5 => _hp5Texture,
                4 => _hp4Texture,
                3 => _hp3Texture,
                2 => _hp2Texture,
                1 => _hp1Texture,
                _ => _hp1Texture // Если HP < 1
            };

            if (texture != null)
            {
                HpBarImage.Source = texture;
            }

            // Анимация при получении урона
            if (currentHp < 5)
            {
                Dispatcher.UIThread.Post(() => PulseHpBar());
            }
        }

        private async void PulseHpBar()
        {
            if (HpBarImage == null) return;

            // Можно добавить проверку, что здоровье действительно уменьшилось
            if (_gameVM?.Player?.CurrentHealth < 5)
            {
                // Простая анимация пульсации
                for (int i = 0; i < 3; i++)
                {
                    HpBarImage.Opacity = 0.5;
                    await Task.Delay(100);
                    HpBarImage.Opacity = 1.0;
                    await Task.Delay(100);
                }
            }
        }

        private void LoadPlatformTexture()
        {
            try
            {
                // Обычная текстура платформы
                var uri = new Uri("avares://GameApp/Assets/platform.png");
                _platformTexture = new Bitmap(AssetLoader.Open(uri));
                Debug.WriteLine("Загружена обычная текстура платформы");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Texture load error (platform): {ex.Message}");
                _platformTexture = null;
            }

            try
            {
                // Текстура платформы с уроном
                var damagingUri = new Uri("avares://GameApp/Assets/spike.png");
                _damagingPlatformTexture = new Bitmap(AssetLoader.Open(damagingUri));
                Debug.WriteLine("Загружена текстура платформы с уроном");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Texture load error (damaging): {ex.Message}");
                _damagingPlatformTexture = null;
            }
        }

        private void CreatePlatforms()
        {
            //if (_platformVisuals.Count > 0) return;

            foreach (var platform in _gameVM.Platforms)
            {
                var visuals = new List<Control>();

                // ВЫБИРАЕМ ТЕКСТУРУ В ЗАВИСИМОСТИ ОТ ТИПА ПЛАТФОРМЫ
                Bitmap? currentTexture = platform.IsDamaging
                    ? _damagingPlatformTexture
                    : _platformTexture;

                if (currentTexture == null)
                {
                    // Если текстуры нет - цветные прямоугольники
                    IBrush fillBrush;
                    IBrush strokeBrush;

                    if (platform.IsDamaging)
                    {
                        fillBrush = new SolidColorBrush(Colors.Red);
                        strokeBrush = new SolidColorBrush(Colors.DarkRed);
                        Debug.WriteLine($"Платформа с уроном (цветная): ({platform.X}, {platform.Y})");
                    }
                    else
                    {
                        fillBrush = new SolidColorBrush(Colors.Green);
                        strokeBrush = new SolidColorBrush(Colors.DarkGreen);
                    }

                    var rect = new Avalonia.Controls.Shapes.Rectangle
                    {
                        Width = platform.Width,
                        Height = platform.Height,
                        Fill = fillBrush,
                        Stroke = strokeBrush,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(rect, platform.X);
                    Canvas.SetTop(rect, platform.Y);
                    MainCanvas.Children.Add(rect);
                    visuals.Add(rect);
                    continue;
                }

                // Текстурированные платформы
                double tileW = currentTexture.PixelSize.Width;
                double tileH = currentTexture.PixelSize.Height;

                int tilesX = (int)Math.Ceiling(platform.Width / tileW);
                int tilesY = (int)Math.Ceiling(platform.Height / tileH);

                Debug.WriteLine($"Создание платформы: X={platform.X}, Y={platform.Y}, " +
                               $"W={platform.Width}, H={platform.Height}, " +
                               $"Damaging={platform.IsDamaging}, Tiles={tilesX}x{tilesY}");

                for (int x = 0; x < tilesX; x++)
                {
                    for (int y = 0; y < tilesY; y++)
                    {
                        double w = Math.Min(tileW, platform.Width - x * tileW);
                        double h = Math.Min(tileH, platform.Height - y * tileH);

                        var tile = new Image
                        {
                            Source = currentTexture,
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

            if (e.Key == Key.F)
            {
                _gameVM.ShowFps = !_gameVM.ShowFps;
                e.Handled = true;
            }

            if (e.Key == Key.Escape)
            {
                // Возврат в меню
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
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