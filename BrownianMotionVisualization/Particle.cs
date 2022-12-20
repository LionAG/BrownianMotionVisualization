namespace BrownianMotionVisualization
{
    public class Particle
    {
        public Brush Brush { get; set; } = Brushes.White;
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; }
        public List<Point> MovementStory { get; set; } = new();
    }
}
