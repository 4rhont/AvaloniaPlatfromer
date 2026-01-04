using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace GameApp.ViewModels
{
    public class WeatherViewModel : ViewModelBase
    {
        private readonly List<Particle> _particles = new();
        private readonly Random _rand = new Random();
        private Canvas? _weatherCanvas;

        public void SetCanvas(Canvas canvas)
        {
            _weatherCanvas = canvas;
            InitParticles();
        }

        private void InitParticles()
        {
            _particles.Clear();
            if (_weatherCanvas == null) return;
            _weatherCanvas.Children.Clear();

            var width = _weatherCanvas.Bounds.Width > 0 ? _weatherCanvas.Bounds.Width : 1920;
            var height = _weatherCanvas.Bounds.Height > 0 ? _weatherCanvas.Bounds.Height : 1080;

            for (int i = 0; i < 80; i++)
            {
                var ellipse = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.White,
                    Opacity = 0.8
                };

                var p = new Particle
                {
                    X = _rand.NextDouble() * width,
                    Y = _rand.NextDouble() * height,
                    SpeedY = 1 + _rand.NextDouble() * 2,
                    Visual = ellipse
                };

                _particles.Add(p);
                _weatherCanvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, p.X);
                Canvas.SetTop(ellipse, p.Y);
            }
        }

        public void Update(double deltaTime)
        {
            if (_weatherCanvas == null) return;

            var height = _weatherCanvas.Bounds.Height > 0 ? _weatherCanvas.Bounds.Height : 1080;
            var width = _weatherCanvas.Bounds.Width > 0 ? _weatherCanvas.Bounds.Width : 1920;

            foreach (var p in _particles)
            {
                p.Y += p.SpeedY * (deltaTime * 60);  // Масштабируем под ~60 FPS

                if (p.Y > height + 5)
                {
                    p.Y = -5;
                    p.X = _rand.NextDouble() * width;
                }

                Canvas.SetLeft(p.Visual, p.X);
                Canvas.SetTop(p.Visual, p.Y);
            }
        }

        private class Particle
        {
            public double X;
            public double Y;
            public double SpeedY;
            public Avalonia.Controls.Shapes.Ellipse Visual = null!;
        }
    }
}