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

        private double _startX;
        private double _patrolRange = 800;
        private double _speed = 200;
        public int _direction = 1;
        private const double JumpVelocity = -600;

        public bool IsJumping { get; set; } = false;
        public double JumpStartY { get; set; }

        public int Direction => _direction;

        public const double JumpHeightThreshold = 10;  // Порог для проверки "того же места" по Y
        public double JumpStartDirection { get; set; }  // Направление на момент старта прыжка
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

            _startX = x;
            IsJumping = false;
        }

        public void Update(double deltaTime)
        {
            if (X > _startX + _patrolRange)
                _direction = -1;
            else if (X < _startX - _patrolRange)
                _direction = 1;

            VelocityX = _direction * _speed;
        }
    }
}