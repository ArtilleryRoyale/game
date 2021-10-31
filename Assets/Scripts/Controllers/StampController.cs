using Jrmgx.Helpers;
using UnityEngine;

public class StampController : NetworkObject
{
    [Header("References")]
    // public for local scale access in MapController TODO prio 5: could be changed to some method
    [SerializeField] public SpriteMask frontMask;
    [SerializeField] public SpriteMask backMask;
    [SerializeField] private bool isDash;

    protected override void Start()
    {
        base.Start();
        // Not sync on purpose
        // if (!isDash) {
        //     frontMask.transform.localRotation = Quaternion.Euler(0, 0, RandomNum.Value * 360f);
        //     backMask.transform.localRotation = frontMask.transform.localRotation;
        // }
    }

    public void Sort(int currentSortingLayer)
    {
        frontMask.frontSortingOrder = currentSortingLayer;
        backMask.backSortingOrder = -(currentSortingLayer + 1);
    }
}
