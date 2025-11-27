using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using GameApp.Core.Models;

namespace GameApp.ViewModels
{
    public partial class PlayerAnimationViewModel : ObservableObject
    {
        private readonly Player _player;
        private readonly Bitmap[] _frames;
        private int _currentFrame = 0;
        private readonly DispatcherTimer _timer;

        public double X => _player.X;
        public double Y => _player.Y;
        public bool IsOnGround => _player.IsOnGround;

        [ObservableProperty]
        private Bitmap _currentFrameBitmap;

        public PlayerAnimationViewModel(Player player)
        {
            _player = player;

            _frames = new[]
            {
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_02.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_03.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_04.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_05.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_06.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_07.png"))),
                new Bitmap(AssetLoader.Open(new Uri("avares://GameApp/Assets/Player/hero_spritesheet_08.png")))
            };

            CurrentFrameBitmap = _frames[0];

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _timer.Tick += (_, __) =>
            {
                _currentFrame = (_currentFrame + 1) % _frames.Length;
                CurrentFrameBitmap = _frames[_currentFrame];
            };
            _timer.Start();

            // Автоматически обновляем позицию при изменении в Player
            _player.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(Player.X) or nameof(Player.Y) or nameof(Player.IsOnGround))
                    OnPropertyChanged(e.PropertyName == nameof(Player.X) ? nameof(X) :
                                         e.PropertyName == nameof(Player.Y) ? nameof(Y) :
                                         nameof(IsOnGround));
            };
        }
    }
}