using GameApp.Core.Levels;
using GameApp.Core.Services;
using ReactiveUI;

namespace GameApp.Core.Models
{
    public class Enemy : ReactiveObject
    {
        private double _x;
        private double _y;
        private double _velocityX;
        private double _velocityY;
        private double _width = 100;
        private double _height = 100;
        private int _damage = 1;
        private int _health = 5;
        private int _maxHealth = 5;

        private double _knockbackTimer = 0;

        private double _startX;
        private double _patrolRange = 800;
        private double _speed = 100;
        private int _direction = 1;
        private const double JumpVelocity = -600;

        
        public double GetSpeedX => _speed;

        private double _prevX;
        public double PrevX { get => _prevX; set => this.RaiseAndSetIfChanged(ref _prevX, value); }
        public int GetStuckCounter => _stuckCounter;
        public int StuckCounter { get => _stuckCounter; set => this.RaiseAndSetIfChanged(ref _stuckCounter, value); }
        private int _stuckCounter = 0;
        public const int StuckThreshold = 3;  // Кол-во кадров без движения для разворота
        public const double StuckEpsilon = 1.0;  // Минимальное изменение X для "движения" (пиксели за кадр)

        public const double EnemyKnockbackDuration = 0.5;  // Длительность отскока (сек)
        public const double EnemyKnockbackFriction = 3000;  // Трение во время отскока 
        //public Platform? JumpStartPlatform { get; set; }
        public bool IsJumping { get; set; } = false;
        public double JumpStartY { get; set; }

        public int Direction { get => _direction; set => this.RaiseAndSetIfChanged(ref _direction, value); }

        public const double JumpHeightThreshold = 20; // Порог для проверки "того же места" по Y

        public int JumpStartDirection { get; set; }  // Направление на момент старта прыжка
        public double X { get => _x; set => this.RaiseAndSetIfChanged(ref _x, value); }
        public double Y { get => _y; set => this.RaiseAndSetIfChanged(ref _y, value); }
        public double Width { get => _width; set => this.RaiseAndSetIfChanged(ref _width, value); }
        public double Height { get => _height; set => this.RaiseAndSetIfChanged(ref _height, value); }
        public int Damage { get => _damage; set => this.RaiseAndSetIfChanged(ref _damage, value); }
        public int Health { get => _health; set => this.RaiseAndSetIfChanged(ref _health, value); }
        public int MaxHealth { get => _maxHealth; set => this.RaiseAndSetIfChanged(ref _maxHealth, value); }

        public bool IsOnGround { get; set; }

        public double Right => X + Width;
        public double Bottom => Y + Height;
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;

        public double VelocityX
        {
            get => _velocityX;
            set => this.RaiseAndSetIfChanged(ref _velocityX, value);
        }

        public double VelocityY
        {
            get => _velocityY;
            set => this.RaiseAndSetIfChanged(ref _velocityY, value);
        }

        public Enemy(double x, double y, double width, double height, int damage, int health)
        {
            JumpStartDirection = 0;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Damage = damage;
            Health = health;
            MaxHealth = health;

            _prevX = x;
            _startX = x;
            IsJumping = false;
        }

        public Enemy(EnemyData data) : this(data.X, data.Y, data.Width, data.Height, data.Damage, data.Health)
        {
            if (data.Direction.HasValue)
            {
                _direction = data.Direction.Value;
            }

            if (data.PatrolRange.HasValue)
            {
                _patrolRange = data.PatrolRange.Value;
            }
            _prevX = data.X;
        }
        public void TakeDamage(int amount, double knockbackX = 0, double knockbackY = 0)
        {
            if (amount <= 0) return;
            Health = Math.Max(0, Health - amount);
            VelocityX += knockbackX;
            VelocityY += knockbackY;
            _knockbackTimer = EnemyKnockbackDuration;
        }

        public void Update(double deltaTime)
        {
            if (_knockbackTimer > 0)
            {
                _knockbackTimer -= deltaTime;
                if (_knockbackTimer < 0) _knockbackTimer = 0;

                // Применяем фрикцию во время отскока (как у игрока)
                if (IsOnGround)
                {
                    if (VelocityX > 0)
                    {
                        VelocityX = Math.Max(0, VelocityX - PhysicsService.GroundFriction * deltaTime);
                    }
                    else if (VelocityX < 0)
                    {
                        VelocityX = Math.Min(0, VelocityX + PhysicsService.GroundFriction * deltaTime);
                    }
                }
                return;  // Пропускаем патруль логику
            }

            if (X > _startX + _patrolRange)
                _direction = -1;
            else if (X < _startX - _patrolRange)
                _direction = 1;

            VelocityX = _direction * _speed;
        }
    }
}