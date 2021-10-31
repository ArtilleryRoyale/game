using UnityEngine;

namespace Jrmgx.Helpers
{
    public static class PhysicForces
    {
        public static Vector2 AddForceImpulse(Vector2 currentVelocity, Vector2 impulse)
        {
            return currentVelocity + impulse;
        }

        public static Vector2 AddForce(Vector2 currentVelocity, Vector2 force)
        {
            currentVelocity += force * Time.fixedDeltaTime;
            return currentVelocity;
        }

        public static Vector2 AddForceGravity(Vector2 currentVelocity, float gravity)
        {
            currentVelocity.y -= gravity * Time.fixedDeltaTime;
            return currentVelocity;
        }

        public static Vector2 AddDrag(Vector2 currentVelocity, Vector2 drag)
        {
            currentVelocity.x *= Mathf.Max(0, 1.0f - drag.x);
            currentVelocity.y *= Mathf.Max(0, 1.0f - drag.y);
            return currentVelocity;
        }

        public static Vector2 ApplyBasicBounce(Vector2 currentVelocity, RaycastHit2D hit)
        {
            return Vector2.Reflect(currentVelocity, hit.normal);
        }
    }
}
