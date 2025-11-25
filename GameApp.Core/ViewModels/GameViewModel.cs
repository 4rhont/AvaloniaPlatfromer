using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive.Linq;

namespace GameApp.Core.ViewModels
{
    public class GameViewModel : ReactiveObject, IDisposable
    {
        private readonly Player _player = new();
        private readonly HashSet<GameAction> _activeActions = new();
        private IDisposable _gameLoop;
        private DateTime _lastUpdateTime;

        public double PlayerX => _player.X;
        public double PlayerY => _player.Y;

        public GameViewModel()
        {
            _player.WhenAnyValue(p => p.X).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerX)));
            _player.WhenAnyValue(p => p.Y).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerY)));

            _lastUpdateTime = DateTime.Now;
            _gameLoop = Observable.Interval(TimeSpan.FromSeconds(1.0 / 60.0))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => UpdateGame());
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

        // ... остальные методы (ApplyFriction, UpdatePosition, CheckGroundCollision) остаются без изменений
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
            if (_player.Y >= 350)
            {
                _player.Y = 350;
                _player.VelocityY = 0;
                _player.IsOnGround = true;
            }
            else
            {
                _player.IsOnGround = false;
            }

            if (_player.X < 0)
            {
                _player.X = 0;
                _player.VelocityX = 0;
            }
            else if (_player.X > 350)
            {
                _player.X = 350;
                _player.VelocityX = 0;
            }
        }

        public void Dispose()
        {
            _gameLoop?.Dispose();
        }
    }
}