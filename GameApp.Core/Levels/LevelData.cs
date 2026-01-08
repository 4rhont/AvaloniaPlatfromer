using System.Collections.Generic;

namespace GameApp.Core.Levels
{
    public class LevelData
    {
        public string Id { get; set; } = "";
        public double PlayerStartX { get; set; }
        public double PlayerStartY { get; set; }
        public List<PlatformData> Platforms { get; set; } = new();
        public List<EnemyData> Enemies { get; set; } = new();
        public double Width { get; set; } = 1920;  // Default, если не указано
        public double Height { get; set; } = 1080;

        public double? EndX { get; set; }  // Nullable, если уровень без конца
        public double? EndY { get; set; }
        public double? EndWidth { get; set; } = 100; 
        public double? EndHeight { get; set; } = 200;
        public string? NextLevelId { get; set; }  
    }
}
