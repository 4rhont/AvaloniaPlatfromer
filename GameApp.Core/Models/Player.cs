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
        private double _width = 100; 
        private double _height = 100;

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