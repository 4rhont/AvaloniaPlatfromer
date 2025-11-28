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
        private int _currentFrame = 0;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private Bitmap _currentFrameBitmap;

        [ObservableProperty]
        private bool _isFacingRight = true; // для зеркалирования

        public double X => _player.X;
        public double Y => _player.Y;
        public bool IsOnGround => _player.IsOnGround;

        public PlayerAnimationViewModel(Player player)
        {
            _player = player;

            _frames = new Bitmap[7];

            for (int i = 0; i < _frames.Length; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/hero_spritesheet_0{i + 2}.png");
                _frames[i] = new Bitmap(AssetLoader.Open(uri));
            }


            CurrentFrameBitmap = _frames[0];
            IsFacingRight = _player.IsFacingRight;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _timer.Tick += (_, __) => Animate();
            _timer.Start();

            // Автоматически обновляем позицию и направление при изменении в Player
            _player.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Player.X)) OnPropertyChanged(nameof(X));
                if (e.PropertyName == nameof(Player.Y)) OnPropertyChanged(nameof(Y));
                if (e.PropertyName == nameof(Player.IsOnGround)) OnPropertyChanged(nameof(IsOnGround));
                if (e.PropertyName == nameof(Player.IsFacingRight)) IsFacingRight = _player.IsFacingRight;
            };
        }

        private void Animate()
        {
            // Анимация только если игрок движется по горизонтали и стоит на земле
            if (_player.IsOnGround && Math.Abs(_player.VelocityX) > 0.1)
            {
                _currentFrame = (_currentFrame + 1) % _frames.Length;
                CurrentFrameBitmap = _frames[_currentFrame];
            }
            else
            {
                // Стоячий кадр (можно сделать первый кадр)
                CurrentFrameBitmap = _frames[0];
            }

            // Отзеркаливание
            IsFacingRight = _player.IsFacingRight;
        }
    }
}
