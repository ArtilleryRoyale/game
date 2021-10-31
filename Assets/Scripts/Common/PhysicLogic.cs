using UnityEngine;
using Jrmgx.Helpers;

namespace CC
{
    public static class PhysicLogic
    {
        public static bool CheckForBounce(
            Vector2 currentPosition,
            Vector2 wannabePosition,
            float radius,
            ref RaycastHit2D bounce,
            int layerMask = -1)
        {
            if (layerMask == -1) {
                layerMask = (Layer.Terrain | Layer.Bounds).Mask;
            }

            Vector2 fromCurrentToWanna = Basics.VectorBetween(currentPosition, wannabePosition);
            bounce = Physics2D.CircleCast(
                currentPosition,
                radius,
                direction: fromCurrentToWanna.normalized,
                distance: fromCurrentToWanna.magnitude,
                layerMask
            );

            return bounce.collider != null;
        }

        public static bool CheckOutOfBounds(Collider2D collider, out string tag)
        {
            tag = collider.tag;
            return collider.CompareTag(Config.TagBounds) || collider.CompareTag(Config.TagBoundLava);
        }
    }
}
