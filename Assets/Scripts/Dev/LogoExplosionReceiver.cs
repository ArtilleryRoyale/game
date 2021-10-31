using Cysharp.Threading.Tasks;
using UnityEngine;

public class LogoExplosionReceiver : MonoBehaviour, ExplosionReceiver
{
    [SerializeField] private StampController explosionStampPrefab = default;

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
#if CC_EXTRA_CARE
try {
#endif
        Stamp(explosion, force, damages).Forget();
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    private async UniTask Stamp(ExplosionController explosion, Vector2 force, int damages)
    {
        await UniTask.Delay(300 /*ms*/, cancellationToken: this.GetCancellationTokenOnDestroy());

        Vector2 origin = explosion.transform.position;
        float radius = explosion.RadiusMax;

        StampController explosionStamp = Instantiate(explosionStampPrefab);
        explosionStamp.Sort(MapController.CurrentSortingLayer);
        explosionStamp.transform.SetParent(transform);
        explosionStamp.transform.position = origin;

        float frontRadius = radius;
        float backRadius = frontRadius * Config.MapOutlineInPercent;

        explosionStamp.frontMask.transform.localScale = Vector3.one * frontRadius;
        explosionStamp.backMask.transform.localScale = Vector3.one * backRadius;
    }
}
