using ReactiveUI;

namespace GameApp.Core.Models
{
    public class Player : ReactiveObject
    {
        private double _x;
        private double _y;
        private double _velocityX;
        private double _velocityY;
        private bool _isOnGround;
        private bool _isFacingRight = true;
        private double _width = 120; 
        private double _height = 210;

        private int _maxHealth = 5;
        private int _currentHealth = 5;

        public bool IsInvincible => _invincibilityRemaining > 0;
        private double _invincibilityRemaining = 0;  // Время неуязвимости после дамага
        private const double InvincibilityDuration = 0.5;  // 0.5 сек

        public int MaxHealth
        {
            get => _maxHealth;
            set => this.RaiseAndSetIfChanged(ref _maxHealth, value);
        }

        public int CurrentHealth
        {
            get => _currentHealth;
            set => this.RaiseAndSetIfChanged(ref _currentHealth, value);
        }

        private double _prevX;
        private double _prevY;

        public double PrevX => _prevX;
        public double PrevY => _prevY;

        private double _spawnX;
        private double _spawnY;

        public void SetSpawnPoint(double x, double y)
        {
            _spawnX = x;
            _spawnY = y;
        }

        public void TakeDamage(int amount, double knockbackX = 0, double knockbackY = 0)
        {
            if (amount <= 0) return;

            if (_invincibilityRemaining > 0)
                return;

            _currentHealth = Math.Max(0, _currentHealth - amount);
            VelocityX += knockbackX;  // Отбрасывание по X (например, в сторону от врага)
            VelocityY += knockbackY;  // Отбрасывание по Y (отрицательное для прыжка вверх)
            _invincibilityRemaining = InvincibilityDuration;

            if (_currentHealth <= 0)
            {
                // Логика смерти: респаун на спавн-точку
                X = _spawnX;  
                Y = _spawnY;
                VelocityX = 0;
                VelocityY = 0;
                _currentHealth = _maxHealth;  // Восстановить здоровье после респауна
                                              // Можно добавить событие OnDeath для анимации или UI
                _invincibilityRemaining = 0;
            }

            
        }

        public void Update(double deltaTime)
        {
            if (_invincibilityRemaining > 0)
            {
                _invincibilityRemaining -= deltaTime;
                if (_invincibilityRemaining < 0) _invincibilityRemaining = 0;
            }
        }

        public void SavePreviousPosition()
        {
            _prevX = X;
            _prevY = Y;
        }

        public double X
        {
            get => _x;
            set => this.RaiseAndSetIfChanged(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => this.RaiseAndSetIfChanged(ref _y, value);
        }

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

        public bool IsOnGround
        {
            get => _isOnGround;
            set => this.RaiseAndSetIfChanged(ref _isOnGround, value);
        }

        public bool IsFacingRight
        {
            get => _isFacingRight;
            set => this.RaiseAndSetIfChanged(ref _isFacingRight, value);
        }

        public double Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        // Вспомогательные свойства для коллизий
        public double Right => X + Width;
        public double Bottom => Y + Height;
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;
    }
}