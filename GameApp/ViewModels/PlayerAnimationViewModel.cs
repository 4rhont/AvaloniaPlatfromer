using System;
using Avalonia;
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
        private readonly Bitmap[] _idleFrames;
        private int _currentFrame = 0;
        private int _currentIdleFrame = 0;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private Bitmap _currentFrameBitmap;

        [ObservableProperty]
        private bool _isFacingRight = true; // для зеркалирования

        public double X => _player.X;
        public double Y => _player.Y;
        public bool IsOnGround => _player.IsOnGround;

        [ObservableProperty]
        private double _visualWidth = 250;  // Фиксированный размер спрайта

        [ObservableProperty]
        private double _visualHeight = 250;

        public double VisualX => _player.X + VisualOffsetX;  // Позиция спрайта относительно хитбокса
        public double VisualY => _player.Y + VisualOffsetY;

        private double HandOffsetX = 0;
        private double HandOffsetY = 5;

        private double VisualOffsetX => (_player.Width - VisualWidth) / 2 + HandOffsetX;  // Центрируем по X: отрицательный, если спрайт шире (спрайт начинается левее хитбокса)
        private double VisualOffsetY => _player.Height - VisualHeight + HandOffsetY;      // Align по bottom по Y: отрицательный, если спрайт выше (спрайт сдвигается вверх, ноги на уровне земли)


        public PlayerAnimationViewModel(Player player)
        {
            _player = player;

            _frames = new Bitmap[5];

            for (int i = 0; i < _frames.Length; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/player_walk_0{i + 1}.png"); // поправить имена файлов и смещение по i
                _frames[i] = new Bitmap(AssetLoader.Open(uri));
            }

            _idleFrames = new Bitmap[4];
            for (int i = 0; i < _idleFrames.Length; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/player_idle_0{i + 1}.png"); // поправить имена файлов и смещение по i
                _idleFrames[i] = new Bitmap(AssetLoader.Open(uri));
            }

            CurrentFrameBitmap = _frames[0];
            IsFacingRight = _player.IsFacingRight;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _timer.Tick += (_, __) => Animate();
            _timer.Start();

            // Автоматически обновляем позицию и направление при изменении в Player
            _player.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Player.X)) OnPropertyChanged(nameof(VisualX));
                if (e.PropertyName == nameof(Player.Y)) OnPropertyChanged(nameof(VisualY));
                if (e.PropertyName == nameof(Player.X)) OnPropertyChanged(nameof(X));
                if (e.PropertyName == nameof(Player.Y)) OnPropertyChanged(nameof(Y));
                if (e.PropertyName == nameof(Player.IsOnGround)) OnPropertyChanged(nameof(IsOnGround));
                if (e.PropertyName == nameof(Player.IsFacingRight)) IsFacingRight = _player.IsFacingRight;
            };
        }

        private void Animate()
        {
            if (_player.IsOnGround && Math.Abs(_player.VelocityX) > 0.1)
            {
                // анимация движения
                _currentFrame = (_currentFrame + 1) % _frames.Length;
                CurrentFrameBitmap = _frames[_currentFrame];
            }
            else if (_player.IsOnGround)
            {
                // анимация покоя
                _currentIdleFrame = (_currentIdleFrame + 1) % _idleFrames.Length;
                CurrentFrameBitmap = _idleFrames[_currentIdleFrame];
            }
            else
            {
                // прыжок или падение (в разработке)
                CurrentFrameBitmap = _frames[0];
            }

            // отзеркаливание
            IsFacingRight = _player.IsFacingRight;
        }
    }
}
