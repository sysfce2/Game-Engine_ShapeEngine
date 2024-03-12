using ShapeEngine.Core.Collision;

namespace ShapeEngine.Pathfinding;

public interface IPathfinderObstacle : IShape
{
    /// <summary>
    /// Invoke this event the shape of the obstacle has changed
    /// </summary>
    public event Action<IPathfinderObstacle>? OnShapeChanged;
    
    /// <summary>
    /// Those are the values that will change the pathfinder grid. Do not change them once the obstacle was added.
    /// If the cell values have to be changed the obstacle has to be removed with the orginial values, and can then be
    /// added again with the new values
    /// </summary>
    public NodeValue[] GetNodeValues();
}