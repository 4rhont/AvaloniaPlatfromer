using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameApp.Views
{
    public partial class GameView : Window
    {
        private Dictionary<Platform, Image> _platformImages = new Dictionary<Platform, Image>();
        private GameViewModel _viewModel;
        private Bitmap _platformTexture;

        public GameView()
        {
            InitializeComponent();
            LoadTextures();

            Focusable = true;
            AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            Activated += OnActivated;
            DataContextChanged += OnDataContextChanged;
        }

        private void LoadTextures()
        {
            try
            {
                // Загрузка текстуры платформы
                _platformTexture = new Bitmap("Assets/platform.png");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load texture: {ex.Message}");
                // Создаем fallback - можно использовать цветные прямоугольники
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            _viewModel = DataContext as GameViewModel;
            if (_viewModel != null)
            {
                _viewModel.Platforms.CollectionChanged += Platforms_CollectionChanged;
                UpdateAllPlatforms();
            }
        }

        private void Platforms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Platform platform in e.OldItems)
                {
                    if (_platformImages.TryGetValue(platform, out Image image))
                    {
                        MainCanvas.Children.Remove(image);
                        _platformImages.Remove(platform);
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (Platform platform in e.NewItems)
                {
                    CreatePlatformImage(platform);
                }
            }
        }

        private void UpdateAllPlatforms()
        {
            // Очищаем старые платформы
            foreach (var image in _platformImages.Values)
            {
                MainCanvas.Children.Remove(image);
            }
            _platformImages.Clear();

            // Создаем новые платформы
            foreach (var platform in _viewModel.Platforms)
            {
                CreatePlatformImage(platform);
            }
        }

        private void CreatePlatformImage(Platform platform)
        {
            var image = new Image
            {
                Width = platform.Width,
                Height = platform.Height,
                Source = _platformTexture
            };

            Canvas.SetLeft(image, platform.X);
            Canvas.SetTop(image, platform.Y);

            MainCanvas.Children.Add(image);
            _platformImages[platform] = image;

            // Подписываемся на изменения позиции платформы
            platform.WhenAnyValue(p => p.X).Subscribe(x => Canvas.SetLeft(image, x));
            platform.WhenAnyValue(p => p.Y).Subscribe(y => Canvas.SetTop(image, y));
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