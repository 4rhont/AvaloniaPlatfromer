using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using GameApp.Core.Models;
using GameApp.Core.ViewModels;
using System;
using System.Diagnostics;

namespace GameApp.ViewModels
{
    public partial class EnemyAnimationViewModel : ObservableObject
    {
        private readonly Enemy _enemy;
        private readonly GameViewModel _gameVM;
        private readonly Bitmap?[] _walkFrames;

        private int _currentFrame = 0;
        private double _accumulator = 0;
        private const double FrameInterval = 0.12;
        private const double IdleVelocityThreshold = 0.1; // можно настроить

        public int CurrentFrameIndex => _currentFrame;
        public double EnemyScaleX => IsFacingRight ? 1.0 : -1.0;

        [ObservableProperty]
        private Bitmap? _currentFrameBitmap;

        [ObservableProperty]
        private bool _isFacingRight = true;

        // Генерируется при изменении IsFacingRight автоматически
        partial void OnIsFacingRightChanged(bool value)
        {
            // уведомим UI, что зависимое свойство EnemyScaleX изменилось
            OnPropertyChanged(nameof(EnemyScaleX));
        }

        // Интерполированные позиции для плавности
        public double VisualX
        {
            get
            {
                double alpha = Math.Max(_gameVM.InterpolationAlpha, 0);
                double x = _enemy.PrevX + (_enemy.X - _enemy.PrevX) * alpha;
                return x + VisualOffsetX;
            }
        }

        public double VisualY
        {
            get
            {
                double alpha = Math.Max(_gameVM.InterpolationAlpha, 0);
                double y = _enemy.PrevY + (_enemy.Y - _enemy.PrevY) * alpha;
                return y + VisualOffsetY;
            }
        }

        public double VisualWidth => _enemy.Width;
        public double VisualHeight => _enemy.Height;
        private double VisualOffsetX => 0;
        private double VisualOffsetY => 0;

        public EnemyAnimationViewModel(Enemy enemy, GameViewModel gameVM)
        {
            _enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            _gameVM = gameVM ?? throw new ArgumentNullException(nameof(gameVM));

            // Загружаем кадры в nullable-массив и логируем размеры
            _walkFrames = new Bitmap?[4];
            for (int i = 0; i < _walkFrames.Length; i++)
            {
                try
                {
                    var uri = new Uri($"avares://GameApp/Assets/Enemy/hardbug_{i + 1}.png");
                    var bmp = new Bitmap(AssetLoader.Open(uri));
                    _walkFrames[i] = bmp;
                    Debug.WriteLine($"[EnemyAnim] Loaded frame {i} size: {bmp.PixelSize.Width}x{bmp.PixelSize.Height}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EnemyAnim] Ошибка загрузки кадра {i}: {ex.Message}");
                    _walkFrames[i] = null;
                }
            }

            // Найдём первый непустой кадр и установим
            _currentFrame = FindNextValidFrame(-1);
            if (_currentFrame >= 0)
                SetFrameBitmap(_walkFrames[_currentFrame]);
            else
                CurrentFrameBitmap = null;

            // Подписываемся на изменения модели врага
            _enemy.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Enemy.VelocityX))
                {
                    if (Math.Abs(_enemy.VelocityX) > 10) // если хочешь другой порог — отредактируй
                        IsFacingRight = _enemy.VelocityX > 0;
                }

                if (e.PropertyName == nameof(Enemy.X) || e.PropertyName == nameof(Enemy.Y))
                {
                    OnPropertyChanged(nameof(VisualX));
                    OnPropertyChanged(nameof(VisualY));
                }
            };
        }

        // Найти следующий индекс кадра, у которого кадр != null
        private int FindNextValidFrame(int startIndex)
        {
            int len = _walkFrames.Length;
            if (len == 0) return -1;
            for (int offset = 1; offset <= len; offset++)
            {
                int idx = (startIndex + offset) % len;
                if (_walkFrames[idx] != null) return idx;
            }
            return -1;
        }

        public void SetFrameBitmap(Bitmap? bmp)
        {
            CurrentFrameBitmap = bmp;
            Debug.WriteLine($"[EnemyAnim] Enemy={_enemy.GetHashCode()} FrameIndex={_currentFrame} CurrentFrameBitmap != null: {CurrentFrameBitmap != null}");
        }

        public void UpdateAnimation(double deltaTime)
        {
            // Защита от нулевого delta
            if (deltaTime <= 0) return;

            double absVx = Math.Abs(_enemy.VelocityX);

            if (absVx < IdleVelocityThreshold)
            {
                // сброс в первый валидный кадр
                _currentFrame = FindNextValidFrame(-1);
                if (_currentFrame >= 0)
                    SetFrameBitmap(_walkFrames[_currentFrame]);
                _accumulator = 0;
                return;
            }

            _accumulator += deltaTime;
            if (_accumulator >= FrameInterval)
            {
                // переход к следующему валидному кадру
                int next = FindNextValidFrame(_currentFrame);
                if (next >= 0)
                {
                    _currentFrame = next;
                    SetFrameBitmap(_walkFrames[_currentFrame]);
                }
                _accumulator -= FrameInterval;
            }
        }
    }
}
