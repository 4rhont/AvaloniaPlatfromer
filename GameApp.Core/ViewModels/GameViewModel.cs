using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace GameApp.Core.ViewModels
{
    public class GameViewModel : ReactiveObject, IDisposable
    {
        private readonly Player _player = new();
        private readonly HashSet<GameAction> _activeActions = new();
        private readonly ObservableCollection<Platform> _platforms = new();
        private IDisposable _gameLoop;
        private DateTime _lastUpdateTime;

        public Player Player => _player;
        public double PlayerX => _player.X;
        public double PlayerY => _player.Y;
        public ObservableCollection<Platform> Platforms => _platforms;

        public GameViewModel()
        {
            _player.WhenAnyValue(p => p.X).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerX)));
            _player.WhenAnyValue(p => p.Y).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerY)));

            InitializePlatforms();

            _lastUpdateTime = DateTime.Now;
            _gameLoop = Observable.Interval(TimeSpan.FromSeconds(1.0 / 60.0))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateGame());
        }

        private void InitializePlatforms()
        {
            
            _platforms.Add(new Platform(0, 800, 1000, 100));

            _platforms.Add(new Platform(500, 300, 100, 20));

            // Еще одна тестовая платформа
            _platforms.Add(new Platform(200, 450, 150, 20));

            // Убрали все остальные платформы для тестирования
            // _platforms.Add(new Platform(0, 500, 2000, 20));
            // _platforms.Add(new Platform(300, 400, 200, 20));
            // _platforms.Add(new Platform(600, 350, 150, 20));
            // _platforms.Add(new Platform(900, 300, 100, 20));
            // _platforms.Add(new Platform(1200, 250, 200, 20));
            // _platforms.Add(new Platform(-50, 0, 50, 600));
            // _platforms.Add(new Platform(2000, 0, 50, 600));
        }

        // Методы для управления действиями (не зависят от Avalonia!)
        public void StartAction(GameAction action) => _activeActions.Add(action);
        public void StopAction(GameAction action) => _activeActions.Remove(action);

        private void UpdateGame()
        {
            var deltaTime = CalculateDeltaTime();
            UpdatePhysics(deltaTime);
        }

        private double CalculateDeltaTime()
        {
            var currentTime = DateTime.Now;
            var deltaTime = (currentTime - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = currentTime;
            return deltaTime;
        }

        private void UpdatePhysics(double deltaTime)
        {
            ApplyGravity(deltaTime);
            HandleMovement(deltaTime);
            ApplyFriction(deltaTime);
            UpdatePosition(deltaTime);
            CheckGroundCollision();
            CheckFallDeath();
        }

        private void CheckFallDeath()
        {
            // Если игрок упал ниже определенного уровня
            if (_player.Y > 2000) // ПОКА ЧТО КОНСТАНТА ПОТОМ ПОМЕНЯТЬ
            {
                // Респавн игрока - возвращаем в начальную позицию
                _player.X = 200;
                _player.Y = 700;
                _player.VelocityX = 0;
                _player.VelocityY = 0;

                // Потом добавим эффекты
                System.Diagnostics.Debug.WriteLine("Player fell to death! Respawning...");
            }
        }

        private void ApplyGravity(double deltaTime)
        {
            if (!_player.IsOnGround)
            {
                _player.VelocityY += PhysicsService.Gravity * deltaTime;
            }
        }

        private void HandleMovement(double deltaTime)
        {
            // Горизонтальное движение через GameAction
            if (_activeActions.Contains(GameAction.MoveLeft))
            {
                _player.VelocityX -= PhysicsService.MoveAcceleration * deltaTime;
                _player.IsFacingRight = false;
            }
            if (_activeActions.Contains(GameAction.MoveRight))
            {
                _player.VelocityX += PhysicsService.MoveAcceleration * deltaTime;
                _player.IsFacingRight = true;
            }

            // Ограничение максимальной скорости
            _player.VelocityX = Math.Clamp(_player.VelocityX,
                -PhysicsService.MaxMoveSpeed, PhysicsService.MaxMoveSpeed);

            // Прыжок
            if (_activeActions.Contains(GameAction.Jump) && _player.IsOnGround)
            {
                _player.VelocityY = PhysicsService.JumpVelocity;
                _player.IsOnGround = false;
            }
        }

        private void ApplyFriction(double deltaTime)
        {
            if (_player.IsOnGround)
            {
                bool isTryingToMove = _activeActions.Contains(GameAction.MoveLeft) ||
                                     _activeActions.Contains(GameAction.MoveRight);

                if (!isTryingToMove)
                {
                    if (_player.VelocityX > 0)
                    {
                        _player.VelocityX = Math.Max(0, _player.VelocityX - PhysicsService.GroundFriction * deltaTime);
                    }
                    else if (_player.VelocityX < 0)
                    {
                        _player.VelocityX = Math.Min(0, _player.VelocityX + PhysicsService.GroundFriction * deltaTime);
                    }
                }
            }
        }

        private void UpdatePosition(double deltaTime)
        {
            _player.X += _player.VelocityX * deltaTime;
            _player.Y += _player.VelocityY * deltaTime;
        }

        private void CheckGroundCollision()
        {
            foreach (var p in _platforms)
            {
                if (PhysicsService.CheckCollision(_player, p))
                {
                    var col = PhysicsService.GetCollisionType(_player, p);
                    PhysicsService.ResolveCollision(_player, p, col);
                }
            }

            double feetX = _player.X + _player.Width / 2;
            double feetY = _player.Y + _player.Height;

            bool grounded = false;

            foreach (var p in _platforms)
            {
                bool withinX = feetX >= p.X && feetX <= p.X + p.Width;

                if (!withinX) continue;

                if (feetY >= p.Y - 3 && feetY <= p.Y + 3 && _player.VelocityY >= 0)
                {
                    grounded = true;
                    break;
                }
            }

            _player.IsOnGround = grounded;
        }




        public void Dispose()
        {
            _gameLoop?.Dispose();
        }
    }
}