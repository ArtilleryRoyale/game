using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    #region Fields

    [Header("Style")]
    public MapController.MapStyle Style = MapController.MapStyle.Jungle;

    [Header("References")]
    [SerializeField] private GameObject clouds = default;

    [Header("Config")]
    [SerializeField] private float speed = 0f;

    #endregion

    public void UpdateWind(float wind)
    {
        speed = wind;
    }

    private void Update()
    {
        var position = clouds.transform.localPosition;
        position.x += speed * Time.deltaTime;
        if (position.x > 300) {
            position.x -= 300;
        } else if (position.x < -300) {
            position.x += 300;
        }
        clouds.transform.localPosition = position;
    }
}
