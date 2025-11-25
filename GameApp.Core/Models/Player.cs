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
    }
}