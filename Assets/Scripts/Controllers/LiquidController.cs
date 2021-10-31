using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class LiquidController : MonoBehaviour
{
    #region Fields

    [Header("Style")]
    public MapController.MapStyle Style = MapController.MapStyle.Jungle;

    [Header("References")]
    [SerializeField] private Transform objectTransform = default;
    [SerializeField] private Transform frontTransform = default;
    [SerializeField] private Transform backTransform = default;
    [SerializeField] private Collider2D mainCollider = default;

    [Header("Config")]
    public float Speed = 1.2f;
    public const float LiquidRiseLevel = 2f;
    public const float LiquidRiseTime = 2f;

    // State
    private float currentDirection = 1f;

    #endregion

    private void Update()
    {
        if (frontTransform.position.x > 30 || frontTransform.position.x < 0) {
            currentDirection *= -1f;
        }

        frontTransform.position += (Vector3)Vector2.left  * Speed * Time.deltaTime * currentDirection;
        backTransform.position  += (Vector3)Vector2.right * Speed * Time.deltaTime * currentDirection;
    }

    public async UniTask Rise(float level, float time)
    {
#if CC_EXTRA_CARE
try {
#endif
        await objectTransform.DOLocalMoveY(objectTransform.position.y + level, time).WithCancellation(this.GetCancellationTokenOnDestroy());
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    public bool OverlapPosition(Vector2 position)
    {
        return mainCollider.OverlapPoint(position);
    }
}
