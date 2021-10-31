using UnityEngine;

namespace CC
{
    public struct GraphLinkInfo
    {
        public Vector2 LeftPoint;
        public Vector2 RightPoint;

        public GraphLinkInfo(Vector2 leftPoint, Vector2 rightPoint)
        {
            LeftPoint = leftPoint;
            RightPoint = rightPoint;
        }

        public override string ToString()
        {
            return "Left: " + LeftPoint + " / Right: " + RightPoint;
        }
    }
}
