using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameApp.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private Canvas? _snowCanvas;
        private List<SnowParticle> _particles = new();
        private Random _rand = new Random();
        private DispatcherTimer? _snowTimer;
        private Stopwatch _snowStopwatch = new();

        public void SetSnowCanvas(Canvas canvas)
        {
            _snowCanvas = canvas;
            InitSnow();
            StartSnowAnimation();
        }

        private void InitSnow()
        {
            if (_snowCanvas == null)
                return;

            _particles.Clear();
            _snowCanvas.Children.Clear();

            var width = _snowCanvas.Bounds.Width > 0 ? _snowCanvas.Bounds.Width : 400;
            var height = _snowCanvas.Bounds.Height > 0 ? _snowCanvas.Bounds.Height : 500;

            for (int i = 0; i < 50; i++)
            {
                var ellipse = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = Brushes.White,
                    Opacity = 0.7
                };

                var particle = new SnowParticle
                {
                    X = _rand.NextDouble() * width,
                    Y = _rand.NextDouble() * height,
                    SpeedY = 20 + _rand.NextDouble() * 40,
                    Visual = ellipse
                };

                _particles.Add(particle);
                _snowCanvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, particle.X);
                Canvas.SetTop(ellipse, particle.Y);
            }
        }

        private void StartSnowAnimation()
        {
            if (_snowTimer != null)
                return;

            _snowTimer = new DispatcherTimer();
            _snowTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _snowTimer.Tick += (s, e) => UpdateSnow();
            _snowStopwatch.Restart();
            _snowTimer.Start();
        }

        private void UpdateSnow()
        {
            if (_snowCanvas == null)
                return;

            var height = _snowCanvas.Bounds.Height > 0 ? _snowCanvas.Bounds.Height : 500;
            var width = _snowCanvas.Bounds.Width > 0 ? _snowCanvas.Bounds.Width : 400;
            var deltaTime = 0.016; // 16ms ≈ 60 FPS

            foreach (var particle in _particles)
            {
                particle.Y += particle.SpeedY * deltaTime;

                // Перезапускаем частицу если она упала вниз
                if (particle.Y > height + 10)
                {
                    particle.Y = -10;
                    particle.X = _rand.NextDouble() * width;
                }

                Canvas.SetTop(particle.Visual, particle.Y);
                Canvas.SetLeft(particle.Visual, particle.X);
            }
        }

        public void StopSnowAnimation()
        {
            _snowTimer?.Stop();
            _snowTimer = null;
        }

        private class SnowParticle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedY { get; set; }
            public Ellipse Visual { get; set; } = null!;
        }
    }
}
