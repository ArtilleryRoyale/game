using UnityEngine;
using UnityEngine.InputSystem;

public class ClickExplode : MonoBehaviour
{
    [SerializeField] private Explosion explosion = default;
    private ExplosionManager explosionManager = default;

    private void Start()
    {
        explosionManager = FindObjectOfType<ExplosionManager>();
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Boom(Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
        }
    }

    private void Boom(Vector2 where)
    {
        explosionManager.GetExplosion(where).InitSimulable(explosion, 0, null, 0, false, 0);
    }
}
