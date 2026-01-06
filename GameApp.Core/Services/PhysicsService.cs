using GameApp.Core.Models;

namespace GameApp.Core.Services
{
    public static class PhysicsService
    {
        public const double Gravity = 900;
        public const double MoveAcceleration = 1200;
        public const double MaxMoveSpeed = 300;
        public const double JumpVelocity = -800;
        public const double GroundFriction = 3000;

        private const double VelocityEpsilon = 20.0;

        public static bool CheckCollision(Player player, Platform platform)
        {
            return player.X < platform.X + platform.Width &&
                   player.Right > platform.X &&
                   player.Y < platform.Y + platform.Height &&
                   player.Bottom > platform.Y;
        }
        public static bool CheckCollision(Player player, Enemy enemy)
        {
            return player.X < enemy.X + enemy.Width &&
                   player.Right > enemy.X &&
                   player.Y < enemy.Y + enemy.Height &&
                   player.Bottom > enemy.Y;
        }

        public static bool CheckCollision(Enemy enemy, Platform platform)
        {
            return enemy.X <= platform.Right &&
                   enemy.Right >= platform.X &&
                   enemy.Y <= platform.Bottom &&
                   enemy.Bottom >= platform.Y;
        }

        public static CollisionType GetCollisionType(Player player, Platform platform)
        {
            var playerBottom = player.Bottom;
            var playerRight = player.Right;
            var platformBottom = platform.Y + platform.Height;
            var platformRight = platform.X + platform.Width;

            // Определяем глубину проникновения с каждой стороны
            var overlapLeft = playerRight - platform.X;
            var overlapRight = platformRight - player.X;
            var overlapTop = playerBottom - platform.Y;
            var overlapBottom = platformBottom - player.Y;

            // Находим минимальное перекрытие
            var minOverlap = Math.Min(Math.Min(overlapLeft, overlapRight),
                                    Math.Min(overlapTop, overlapBottom));

            // Определяем тип коллизии по минимальному перекрытию
            if (minOverlap == overlapTop)
            {
                return CollisionType.Top;
            }
            else if (minOverlap == overlapBottom)
            {
                return CollisionType.Bottom;
            }
            else if (minOverlap == overlapLeft || minOverlap == overlapRight)
            {
                return CollisionType.Side;
            }

            return CollisionType.None;
        }

        public static CollisionType GetCollisionType(Player player, Enemy enemy)
        {
            var playerBottom = player.Bottom;
            var playerRight = player.Right;
            var enemyBottom = enemy.Y + enemy.Height;
            var enemyRight = enemy.X + enemy.Width;

            var overlapLeft = playerRight - enemy.X;
            var overlapRight = enemyRight - player.X;
            var overlapTop = playerBottom - enemy.Y;
            var overlapBottom = enemyBottom - player.Y;

            var minOverlap = Math.Min(Math.Min(overlapLeft, overlapRight),
                                      Math.Min(overlapTop, overlapBottom));

            if (minOverlap == overlapTop)
            {
                return CollisionType.Top;
            }
            else if (minOverlap == overlapBottom)
            {
                return CollisionType.Bottom;
            }
            else if (minOverlap == overlapLeft || minOverlap == overlapRight)
            {
                return CollisionType.Side;
            }

            return CollisionType.None;
        }

        public static CollisionType GetCollisionType(Enemy enemy, Platform platform)
        {
            var enemyBottom = enemy.Bottom;
            var enemyRight = enemy.Right;
            var platformBottom = platform.Y + platform.Height;
            var platformRight = platform.X + platform.Width;

            var overlapLeft = enemyRight - platform.X;
            var overlapRight = platformRight - enemy.X;
            var overlapTop = enemyBottom - platform.Y;
            var overlapBottom = platformBottom - enemy.Y;

            var minOverlap = Math.Min(Math.Min(overlapLeft, overlapRight),
                                      Math.Min(overlapTop, overlapBottom));

            if (minOverlap == overlapTop)
            {
                return CollisionType.Top;
            }
            else if (minOverlap == overlapBottom)
            {
                return CollisionType.Bottom;
            }
            else if (minOverlap == overlapLeft || minOverlap == overlapRight)
            {
                return CollisionType.Side;
            }

            return CollisionType.None;
        }

        public static void ResolveCollision(Enemy enemy, Platform platform, CollisionType type)
        {
            switch (type)
            {
                case CollisionType.Top:
                    // Враг "приземляется" на платформу
                    enemy.Y = platform.Y - enemy.Height;
                    enemy.VelocityY = 0;
                    enemy.IsOnGround = true;
                    break;

                case CollisionType.Bottom:
                    // Враг ударяется "головой"
                    enemy.Y = platform.Y + platform.Height;
                    enemy.VelocityY = 0;
                    break;

                case CollisionType.Side:
                    // Отодвигаем всегда, чтобы не застревать
                    if (enemy.CenterX < platform.CenterX) // Слева от платформы
                    {
                        enemy.X = platform.X - enemy.Width - 1; // Отодвинуть на 1px
                    }
                    else // Справа
                    {
                        enemy.X = platform.X + platform.Width + 1;
                    }

                    // Прыжок только если не в прыжке уже
                    if (!enemy.IsJumping && Math.Abs(enemy.VelocityY) < VelocityEpsilon)
                    {
                        // Попытка запрыгнуть
                        enemy.VelocityY = JumpVelocity;  // -600
                        enemy.IsOnGround = false;
                        enemy.IsJumping = true;
                        enemy.JumpStartPlatform = platform;
                        enemy.JumpStartY = enemy.Y;
                        enemy.JumpStartDirection = enemy.Direction;  // Запоминаем направление старта прыжка
                        // System.Diagnostics.Debug.WriteLine($"Enemy starting jump at X={enemy.X:F1}, Y={enemy.Y:F1}, Dir={enemy.JumpStartDirection}");
                    }

                    break;
            }
        }

        public static void ResolveCollision(Player player, Platform platform, CollisionType type)
        {
            switch (type)
            {
                case CollisionType.Top:
                    // Игрок приземляется на платформу
                    player.Y = platform.Y - player.Height;
                    player.VelocityY = 0;
                    player.IsOnGround = true;
                    if (platform.IsDamaging)
                    {
                        player.TakeDamage(platform.Damage, 0, -400);  // Дамаг с отскоком вверх
                    }
                    break;

                case CollisionType.Bottom:
                    // Игрок ударяется головой
                    player.Y = platform.Y + platform.Height;
                    player.VelocityY = 0;
                    if (platform.IsDamaging)
                    {
                        player.TakeDamage(platform.Damage, 0, 300);  // Дамаг с отскоком вниз
                    }
                    break;

                case CollisionType.Side:
                    // Игрок ударяется сбоку
                    if (player.CenterX < platform.CenterX) // Игрок слева от платформы
                    {
                        player.X = platform.X - player.Width;
                    }
                    else // Игрок справа от платформы
                    {
                        player.X = platform.X + platform.Width;
                    }

                    if (platform.IsDamaging)
                    {
                        //player.TakeDamage(platform.Damage, -player.VelocityX * 0.4, -50);  // Дамаг с отскоком в сторону и вверх
                        player.TakeDamage(platform.Damage, -3.4*player.VelocityX, -100);  // Дамаг с отскоком в сторону и вверх
                    }
                    else
                    {
                        player.VelocityX = 0;
                    }


                    break;
            }
        }
    }

    public enum CollisionType
    {
        None,
        Top,
        Bottom,
        Side
    }
}