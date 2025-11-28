using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace GameApp.Views
{
    public partial class WeatherLayer : UserControl
    {
        private readonly DispatcherTimer _timer;
        private readonly List<Particle> _particles = new();
        private readonly Random _rand = new();

        public WeatherLayer()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += OnTick;
            _timer.Start();

            this.AttachedToVisualTree += (_, __) => InitParticles();
        }

        private void InitParticles()
        {
            _particles.Clear();
            WeatherCanvas.Children.Clear();

            var width = Bounds.Width > 0 ? Bounds.Width : 1920;
            var height = Bounds.Height > 0 ? Bounds.Height : 1080;

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
                WeatherCanvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, p.X);
                Canvas.SetTop(ellipse, p.Y);
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var height = Bounds.Height > 0 ? Bounds.Height : 1080;
            var width = Bounds.Width > 0 ? Bounds.Width : 1920;

            foreach (var p in _particles)
            {
                p.Y += p.SpeedY;

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
