using GameApp.Core.Levels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameApp.Core.Models
{
    public class FlyingEnemy : Enemy
    {
        private const double PatrolSpeed = 120;
        private const double ChaseSpeed = 220;
        private const double ChaseDistance = 600;

        public static List<FlightStep> CreateDefaultPattern()
        {
            return new()
            {
                new(new Vector2(1f, 0f), 3.0),   // Вверх 3s
                new(new Vector2(0f, -0.5f), 2.0), // Вверх вправо 2s
                new(new Vector2(-1f, 0f), 3.0),  // Вниз 3s
                new(new Vector2(-0.5f, 0f), 2.0) // Вверх вправо 2s
            };
        }

        public enum FlyingEnemyMode
        {
            Patrol,
            Chase
        }

        public class FlightStep
        {
            public Vector2 Direction { get; }
            public double Duration { get; }

            public FlightStep(Vector2 direction, double duration)
            {
                Direction = direction;
                Duration = duration;
            }
        }

        private FlyingEnemyMode _mode = FlyingEnemyMode.Patrol;

        private readonly List<FlightStep> _pattern;
        private int _currentStepIndex = 0;
        private double _stepTimer = 0;

        public FlyingEnemyMode Mode
        {
            get => _mode;
            private set => this.RaiseAndSetIfChanged(ref _mode, value);
        }

        public FlyingEnemy(
            EnemyData data,
            List<FlightStep> flightPattern
        ) : base(data)
        {
            _pattern = flightPattern;
            IsOnGround = false;
        }

        public void UpdateFlying(
            double deltaTime,
            Player player,
            IReadOnlyList<Platform> platforms
        )
        {
            switch (Mode)
            {
                case FlyingEnemyMode.Patrol:
                    UpdatePatrol(deltaTime);
                    TrySwitchToChase(player, platforms);
                    break;

                case FlyingEnemyMode.Chase:
                    UpdateChase(deltaTime, player);
                    break;
            }
        }

        private void UpdatePatrol(double deltaTime)
        {
            var step = _pattern[_currentStepIndex];

            VelocityX = step.Direction.X * PatrolSpeed;
            VelocityY = step.Direction.Y * PatrolSpeed;

            _stepTimer += deltaTime;
            if (_stepTimer >= step.Duration)
            {
                _stepTimer = 0;
                _currentStepIndex = (_currentStepIndex + 1) % _pattern.Count;
            }
        }

        private void UpdateChase(double deltaTime, Player player)
        {
            var toPlayer = new Vector2(
                (float)(player.CenterX - CenterX),
                (float)(player.CenterY - CenterY)
            );

            if (toPlayer.Length() > ChaseDistance)
            {
                Mode = FlyingEnemyMode.Patrol;
                return;
            }

            var dir = Vector2.Normalize(toPlayer);

            VelocityX = dir.X * ChaseSpeed;
            VelocityY = dir.Y * ChaseSpeed;
        }

        private void TrySwitchToChase(Player player, IReadOnlyList<Platform> platforms)
        {
            var dx = player.CenterX - CenterX;
            var dy = player.CenterY - CenterY;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist > ChaseDistance)
                return;

            if (!HasPlatformBetween(player, platforms))
            {
                Mode = FlyingEnemyMode.Chase;
            }
        }

        private bool HasPlatformBetween(Player player, IReadOnlyList<Platform> platforms)
        {
            var enemyCenter = new Vector2((float)CenterX, (float)CenterY);
            var playerCenter = new Vector2((float)player.CenterX, (float)player.CenterY);
            var dir = playerCenter - enemyCenter;
            var dist = dir.Length();
            if (dist == 0) return false;
            dir /= dist;
            const float step = 50f;
            for (float t = 0.1f; t < dist; t += step)
            {
                var point = enemyCenter + dir * t;
                foreach (var p in platforms)
                {
                    if (point.X >= p.X && point.X <= p.X + p.Width &&
                        point.Y >= p.Y && point.Y <= p.Y + p.Height)
                        return true;
                }
            }
            return false;
        }
    }
}
