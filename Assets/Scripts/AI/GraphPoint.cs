using UnityEngine;

namespace CC
{
    public struct GraphPoint : WithHeuristic<GraphPoint>
    {
        public readonly Vector2 Point;

        public GraphPoint(Vector2 point)
        {
            Point = point;
        }

        public float Heuristic(GraphPoint goal)
        {
            return (Point - goal.Point).magnitude;
        }

        public override string ToString()
        {
            return "GraphPoint (" + Point.x + ", " + Point.y + ")";
        }
    }

    public static class GraphPointExtension
    {
        public static GraphPoint GetGraphPoint(this Vector2 v)
        {
            return new GraphPoint(v);
        }
    }
}
