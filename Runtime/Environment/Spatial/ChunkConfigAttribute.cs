namespace Rayforge.Core.Environment.Spatial
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
    public class ChunkConfigAttribute : System.Attribute
    {
        public SpatialAxes Axes { get; }
        public ChunkConfigAttribute(SpatialAxes axes) => Axes = axes;
    }
}
