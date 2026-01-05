using GameApp.Core.Input;
using GameApp.Core.Models;
using GameApp.Core.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

using GameApp.Core.Levels;

namespace GameApp.Core.ViewModels
{
    public class GameViewModel : ReactiveObject, IDisposable
    {
        private string _currentLevelId = "level1";
        private const bool DebugMode = true;
        public bool IsDebugMode => DebugMode;
        private string _debugInfo = "";

        private readonly Camera _camera = new();
        public Camera Camera => _camera;

        private double _spawnX;
        private double _spawnY;

        public event Action? OnAttackTriggered; //атака

        private const double FallDamageThreshold = 1000; // Порог скорости Y для урона от падения
        private double _lastVelocityY; // Для отслеживания скорости перед приземлением

        // FPS
        private int _frameCounter = 0;
        private double _fps = 0;
        private double _fpsTimer = 0;

        // Настройки FPS
        private const double FpsUpdateInterval = 0.5; // секунд

        private void UpdateFps(double deltaTime)
        {
            _frameCounter++;
            _fpsTimer += deltaTime;

            if (_fpsTimer >= FpsUpdateInterval)
            {
                _fps = _frameCounter / _fpsTimer;
                _frameCounter = 0;
                _fpsTimer = 0;
            }
        }

        public string DebugInfo
        {
            get => _debugInfo;
            private set => this.RaiseAndSetIfChanged(ref _debugInfo, value);
        }

        private void UpdateDebugInfo()
        {
            if (!DebugMode)
                return;

            var lines = new List<string>();

            lines.Add($"LEVEL: {_currentLevelId}");
            lines.Add($"PLATFORMS COUNT: {_platforms.Count}");
            lines.Add("");

            int index = 0;
            foreach (var p in _platforms)
            {
                lines.Add(
                    $"[{index}] X:{p.X} Y:{p.Y} W:{p.Width} H:{p.Height}"
                );
                index++;
            }

            lines.Add("");
            lines.Add($"PLAYER X:{_player.X:F1} Y:{_player.Y:F1}");
            lines.Add($"HEALTH: {_player.CurrentHealth}/{_player.MaxHealth}");
            lines.Add($"ON GROUND: {_player.IsOnGround}");
            lines.Add($"VEL X:{_player.VelocityX:F1} Y:{_player.VelocityY:F1}");
            lines.Add($"FPS: {_fps:F1}");

            lines.Add("");
            lines.Add($"CAMERA X:{_camera.X:F1} Y:{_camera.Y:F1}");
            lines.Add($"LEVEL SIZE: W:{_camera.LevelWidth} H:{_camera.LevelHeight}");

            DebugInfo = string.Join(Environment.NewLine, lines);
        }

        public string CurrentLevelId => _currentLevelId;

        private readonly Player _player = new();
        private readonly HashSet<GameAction> _activeActions = new();
        private readonly ObservableCollection<Platform> _platforms = new();

        public Player Player => _player;
        public double PlayerX => _player.X;
        public double PlayerY => _player.Y;
        public ObservableCollection<Platform> Platforms => _platforms;

        public GameViewModel()
        {
            _player.WhenAnyValue(p => p.X).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerX)));
            _player.WhenAnyValue(p => p.Y).Subscribe(_ => this.RaisePropertyChanged(nameof(PlayerY)));

            LoadLevel(_currentLevelId);

            // Initial setup
            _camera.Follow(_player.X, _player.Y, _player.Width, _player.Height);
            if (DebugMode)
            {
                UpdateDebugInfo();  // Force initial debug
            }
        }

        private void LoadLevel(string levelId)
        {
            _platforms.Clear();

            var level = LevelLoader.Load(levelId);

            _player.X = level.PlayerStartX;
            _player.Y = level.PlayerStartY;
            _spawnX = level.PlayerStartX;
            _spawnY = level.PlayerStartY;
            _player.VelocityX = 0;
            _player.VelocityY = 0;
            _player.SetSpawnPoint(level.PlayerStartX, level.PlayerStartY);
            _player.IsOnGround = false;

            foreach (var p in level.Platforms)
            {
                _platforms.Add(new Platform(
                    p.X,
                    p.Y,
                    p.Width,
                    p.Height,
                    p.IsDamaging ?? false,  // Из json или false
                    p.Damage ?? 0           // Из json или 0
                ));
            }

            _currentLevelId = level.Id;

            _camera.LevelWidth = level.Width;
            _camera.LevelHeight = level.Height;
        }

        public void StartAction(GameAction action)
        {
            _activeActions.Add(action);

            if (action == GameAction.Attack)
            {
                OnAttackTriggered?.Invoke();               //анимация атаки
                _activeActions.Remove(GameAction.Attack);  // одноразовое использование
            }
        }
        public void StopAction(GameAction action) => _activeActions.Remove(action);

        private double _debugTimer;

        private void UpdateDebugInfoThrottled(double deltaTime)
        {
            _debugTimer += deltaTime;
            if (_debugTimer < 0.2)
                return;

            _debugTimer = 0;
            UpdateDebugInfo();
        }

        private double _accumulator = 0;
        private const double FixedDelta = 1.0 / 60.0;

        private int MaxSteps = 5;

        public void UpdateGame(double deltaTime)
        {
            _accumulator += deltaTime;

            int steps = 0;
            while (_accumulator >= FixedDelta && steps < MaxSteps)
            {
                _player.SavePreviousPosition(); 
                UpdatePhysics(FixedDelta);
                _accumulator -= FixedDelta;
                steps++;
            }

            _camera.Follow(_player.X, _player.Y, _player.Width, _player.Height);

            if (DebugMode)
            {
                UpdateFps(deltaTime);
                UpdateDebugInfoThrottled(deltaTime);
            }
        }

        public double InterpolationAlpha
        {
            get
            {
                if (FixedDelta <= 0)
                    return 0;

                return Math.Clamp(_accumulator / FixedDelta, 0.0, 1.0);
            }
        }


        private void UpdatePhysics(double deltaTime)
        {
            ApplyGravity(deltaTime);
            HandleMovement(deltaTime);
            ApplyFriction(deltaTime);

            _player.Update(deltaTime);

            _lastVelocityY = _player.VelocityY;

            UpdatePosition(deltaTime);
            CheckGroundCollision();
            CheckFallDeath();
        }

        private void CheckFallDeath()
        {
            if (_player.Y > Camera.LevelHeight)
            {
                _player.X = _spawnX;
                _player.Y = _spawnY;
                _player.VelocityX = 0;
                _player.VelocityY = 0;

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

            _player.VelocityX = Math.Clamp(_player.VelocityX,
                -PhysicsService.MaxMoveSpeed, PhysicsService.MaxMoveSpeed);

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
            bool wasOnGround = _player.IsOnGround;

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

            if (grounded && !wasOnGround && _lastVelocityY > FallDamageThreshold)
            {
                int damage = 1;
                _player.TakeDamage(damage, 0, -200);  // Отскок
            }
        }
        public void Dispose()
        {
            // Здесь можно добавить очистку
        }
    }
}