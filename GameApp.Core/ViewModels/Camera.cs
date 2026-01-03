using ReactiveUI;
using System;

namespace GameApp.Core.ViewModels
{
    public class Camera : ReactiveObject
    {
        private double _x;  // OffsetX камеры т.е. мир.X - Camera.X = экран.X
        private double _y;
        private double _viewportWidth = 1920;  // Размер видимой области (Window.Width)
        private double _viewportHeight = 1080;
        private double _levelWidth = 10000;   // Размер уровня (из LevelData)
        private double _levelHeight = 3000;

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

        // Обновление камеры: центрируем на игроке
        public void Follow(double targetX, double targetY, double targetWidth, double targetHeight)
        {
            X = targetX + targetWidth / 2 - ViewportWidth / 2;
            Y = targetY + targetHeight / 2 - ViewportHeight / 2;

            // Clamp: не выходим за уровень
            X = Math.Clamp(X, 0, LevelWidth - ViewportWidth);
            Y = Math.Clamp(Y, 0, LevelHeight - ViewportHeight);
        }

        // Свойства для binding и настроек
        public double ViewportWidth { get => _viewportWidth; set => _viewportWidth = value; }
        public double ViewportHeight { get => _viewportHeight; set => _viewportHeight = value; }
        public double LevelWidth { get => _levelWidth; set => _levelWidth = value; }
        public double LevelHeight { get => _levelHeight; set => _levelHeight = value; }
    }
}