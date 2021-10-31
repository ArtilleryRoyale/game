using UnityEngine;

public class DummyExplosionReceiver : MonoBehaviour, ExplosionReceiver
{
    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        // Do nothing
        // Log.Message("AI Simulation", "OnReceiveExplosion Dummy " + name);
    }
}
