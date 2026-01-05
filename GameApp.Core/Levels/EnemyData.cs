namespace GameApp.Core.Levels
{
    public class EnemyData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 100; 
        public double Height { get; set; } = 100;
        public int Damage { get; set; } = 1;
        public int Health { get; set; } = 3;
    }
}