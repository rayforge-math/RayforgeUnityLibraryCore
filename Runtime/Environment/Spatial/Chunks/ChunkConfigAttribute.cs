namespace Rayforge.Core.Environment.Spatial.Chunks
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
    public class ChunkConfigAttribute : System.Attribute
    {
        public SpatialAxes Axes { get; }
        public ChunkConfigAttribute(SpatialAxes axes) => Axes = axes;
    }
}
