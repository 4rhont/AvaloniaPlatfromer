using System.Collections.Generic;

namespace GameApp.Core.Levels
{
    public class LevelData
    {
        public string Id { get; set; } = "";
        public double PlayerStartX { get; set; }
        public double PlayerStartY { get; set; }
        public List<PlatformData> Platforms { get; set; } = new();
    }
}
