using UnityEngine;

public interface ExplosionReceiver
{
    void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages);

    // Trick
    GameObject gameObject { get; }
}
