using UnityEngine;

public class MortarWeaponController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private Transform rotateTransform = default;

    #endregion

    #region Display Logic

    public void SetAngle(float angle, bool isFacingRight)
    {
        rotateTransform.rotation = Quaternion.Euler(0, 0, angle * (isFacingRight ? 1f : -1f));
    }

    #endregion
}
