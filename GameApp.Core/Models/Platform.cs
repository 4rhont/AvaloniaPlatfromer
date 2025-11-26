using ReactiveUI;

namespace GameApp.Core.Models
{
    public class Platform : ReactiveObject
    {
        private double _x;
        private double _y;
        private double _width;
        private double _height;

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

        public double Right => X + Width;
        public double Bottom => Y + Height;
        public double CenterX => X + Width / 2;
        public double CenterY => Y + Height / 2;

        public Platform(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            // Подписываемся на изменения размеров
            this.WhenAnyValue(p => p.Width).Subscribe(_ => UpdateSize());
            this.WhenAnyValue(p => p.Height).Subscribe(_ => UpdateSize());
        }

        private void UpdateSize()
        {
            this.RaisePropertyChanged(nameof(Right));
            this.RaisePropertyChanged(nameof(Bottom));
            this.RaisePropertyChanged(nameof(CenterX));
            this.RaisePropertyChanged(nameof(CenterY));
        }
    }
}