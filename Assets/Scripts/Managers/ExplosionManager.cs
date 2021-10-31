using UnityEngine;
using CC.StreamPlay;

public class ExplosionManager : NetworkObject
{
    #region Fields

    public static ExplosionManager Instance;

    [Header("References")]
    [SerializeField] protected StampMakerController stampMakerController = default;
    public StampMakerController StampMakerController => stampMakerController;

    [Header("Prefabs")]
    [SerializeField] protected ExplosionController ExplosionPrefab = default;
    [SerializeField] protected ExplosionController ExplosionPrefabAI = default;

    #endregion

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        NetworkIdentifier = NETWORK_IDENTIFIER_EXPLOSION_MANAGER;
    }

    public ExplosionController GetExplosion(Vector2 origin, bool isAISimulationActive = false)
    {
        ExplosionController explosionController = Object.Instantiate(
            isAISimulationActive ? ExplosionPrefabAI : ExplosionPrefab,
            origin,
            Quaternion.identity
        );
        explosionController.NetworkIdentifier = NetworkObject.NetworkIdentifierNew();
        explosionController.IsAISimulationActive = isAISimulationActive;
        if (isAISimulationActive) {
            GameManager.Instance.MoveGameObjectToAIScene(explosionController.gameObject);
        } else {
            NetworkRecordSnapshot(Method_GetExplosion_Guest, origin, explosionController.NetworkIdentifier);
        }
        return explosionController;
    }

    [StreamPlay(Method_GetExplosion_Guest)]
    protected void GetExplosion_Guest(Vector2 origin, int ownerNetworkIdentifier)
    {
        ExplosionController explosionController = Object.Instantiate(ExplosionPrefab, origin, Quaternion.identity);
        explosionController.NetworkIdentifier = ownerNetworkIdentifier;
        StreamPlayPlayer.Refresh();
    }
}
