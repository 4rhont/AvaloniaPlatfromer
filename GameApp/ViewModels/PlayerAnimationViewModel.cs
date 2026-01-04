using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using GameApp.Core.Models;
using GameApp.Core.ViewModels;
using System;

namespace GameApp.ViewModels
{
    public partial class PlayerAnimationViewModel : ObservableObject
    {
        private readonly Player _player;
        private readonly Bitmap[] _frames;
        private readonly Bitmap[] _idleFrames;
        private readonly Bitmap[] _attackFrames;
        private int _currentFrame = 0;
        private int _currentIdleFrame = 0;

        // состояние атаки
        private bool _isAttacking = false;
        private double _attackTimeElapsed = 0.0;
        private const double AttackDuration = 0.5;         // время на всю анимацию атаки
        private const double AttackCooldown = 0.2;         // кулдаун между атаками
        private double _attackCooldownRemaining = 0.0;


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

        //public double VisualX => _player.X + VisualOffsetX;  // Позиция спрайта относительно хитбокса
        //public double VisualY => _player.Y + VisualOffsetY;

        public double VisualX
        {
            get
            {
                double alpha = _gameVM.InterpolationAlpha;
                double x =
                    _player.PrevX +
                    (_player.X - _player.PrevX) * alpha;

                return x + VisualOffsetX;
            }
        }

        public double VisualY
        {
            get
            {
                double alpha = _gameVM.InterpolationAlpha;
                double y =
                    _player.PrevY +
                    (_player.Y - _player.PrevY) * alpha;

                return y + VisualOffsetY;
            }
        }


        private double HandOffsetX = -5;
        private double HandOffsetY = 5;

        private double MirrorCorrectionX = 0;

        private double VisualOffsetX => IsFacingRight
        ? (_player.Width - VisualWidth) / 2 + HandOffsetX
        : (_player.Width - VisualWidth) / 2 + MirrorCorrectionX + HandOffsetY;  // MirrorCorrectionX - корректировка если сдвигается спрайт при повороте

        private double VisualOffsetY => _player.Height - VisualHeight + HandOffsetY;

        private double _animationAccumulator = 0;
        private const double FrameInterval = 0.15;  // 150ms

        private readonly GameViewModel _gameVM;

        public void NotifyRenderPositionChanged()
        {
            OnPropertyChanged(nameof(VisualX));
            OnPropertyChanged(nameof(VisualY));
        }

        public PlayerAnimationViewModel(Player player, GameViewModel gameVM)
        {
            _player = player;
            _gameVM = gameVM;

            _frames = new Bitmap[5];

            for (int i = 0; i < _frames.Length; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/player_walk_0{i + 1}.png");
                _frames[i] = new Bitmap(AssetLoader.Open(uri));
            }

            _idleFrames = new Bitmap[4];
            for (int i = 0; i < _idleFrames.Length; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/player_idle_0{i + 1}.png");
                _idleFrames[i] = new Bitmap(AssetLoader.Open(uri));
            }

            _attackFrames = new Bitmap[8];
            for (int i = 0; i < 8; i++)
            {
                var uri = new Uri($"avares://GameApp/Assets/Player/player_attack_0{i + 1}.png");
                _attackFrames[i] = new Bitmap(AssetLoader.Open(uri));
            }


            CurrentFrameBitmap = _frames[0];
            IsFacingRight = _player.IsFacingRight;

            // Автоматически обновляем позицию и направление при изменении в Player
            _player.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Player.X))
                {
                    OnPropertyChanged(nameof(VisualX));
                    OnPropertyChanged(nameof(X));
                }
                if (e.PropertyName == nameof(Player.Y))
                {
                    OnPropertyChanged(nameof(VisualY));
                    OnPropertyChanged(nameof(Y));
                }
                if (e.PropertyName == nameof(Player.IsOnGround)) OnPropertyChanged(nameof(IsOnGround));
                if (e.PropertyName == nameof(Player.IsFacingRight)) IsFacingRight = _player.IsFacingRight;
            };
        }

        public void TriggerAttack()
        {
            if (_isAttacking || _attackCooldownRemaining > 0)
            {
                //System.Diagnostics.Debug.WriteLine("Атака заблокирована: уже идёт или на кулдауне");
                return;
            }

            //System.Diagnostics.Debug.WriteLine("началась атака");
            _isAttacking = true;
            _attackTimeElapsed = 0.0;
        }

        public void UpdateAnimation(double deltaTime)
        {
            // обновляем кулдаун
            if (_attackCooldownRemaining > 0)
            {
                _attackCooldownRemaining -= deltaTime;
                if (_attackCooldownRemaining < 0) _attackCooldownRemaining = 0;
            }

            // атака (в приоритете)
            if (_isAttacking)
            {
                _attackTimeElapsed += deltaTime;

                // выычисляем кадр
                int frameIndex = (int)(_attackTimeElapsed / AttackDuration * 8);
                frameIndex = Math.Clamp(frameIndex, 0, 7); // до 7

                CurrentFrameBitmap = _attackFrames[frameIndex];
                //System.Diagnostics.Debug.WriteLine($"Атака: кадр {frameIndex}, время { _attackTimeElapsed:F2}/{AttackDuration}");
                // если анимация закончилась
                if (_attackTimeElapsed >= AttackDuration)
                {
                    //System.Diagnostics.Debug.WriteLine("атака завершена");
                    _isAttacking = false;
                    _attackCooldownRemaining = AttackCooldown;
                }
                else
                {
                    // во время атаки не запускаем обычную анимацию
                    IsFacingRight = _player.IsFacingRight;
                    return;
                }
            }

            // анимация покоя
            _animationAccumulator += deltaTime;
            if (_animationAccumulator >= FrameInterval)
            {
                if (_player.IsOnGround && Math.Abs(_player.VelocityX) > 0.1)
                {
                    _currentFrame = (_currentFrame + 1) % _frames.Length;
                    CurrentFrameBitmap = _frames[_currentFrame];
                }
                else if (_player.IsOnGround)
                {
                    _currentIdleFrame = (_currentIdleFrame + 1) % _idleFrames.Length;
                    CurrentFrameBitmap = _idleFrames[_currentIdleFrame];
                }
                else
                {
                    CurrentFrameBitmap = _frames[0];
                }
                _animationAccumulator -= FrameInterval;
            }

            IsFacingRight = _player.IsFacingRight;
        }

    }
}