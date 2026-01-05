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

        
    }
}
