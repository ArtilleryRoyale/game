using UnityEngine;
using Jrmgx.Helpers;
using Cinemachine;
using DG.Tweening;
using System.Collections;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;

public class CameraManager : MonoBehaviour
{
    #region Fields

    public static CameraManager Instance;

    private const int CameraZoomLevelFullMap = 70;
    private const int CameraZoomLevelMin = 70;
    private const int CameraZoomLevelMedium = 50;
    private const int CameraZoomLevelMax = 30;

    private static readonly Vector2 FrameMoveViewLarge = new Vector2(.5f, .5f);
    private static readonly Vector2 FrameDefaultMedium = new Vector2(.2f, .2f);
    private static readonly Vector2 FrameTrackPointSmall = new Vector2(.01f, .01f);

    [Header("Extern References")]
    public Camera MainCamera;
    [SerializeField] private CinemachineVirtualCamera cameraWorld = default;
    [SerializeField] private GameObject cameraBounds = default;
    [SerializeField] private GameObject pointerPositionHolder = default;
    private Vector2 pointerPositionHolderOriginal;

    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = default;

    // State
    public enum ZoomLevel : int { Min = 0, Medium = 1, Max = 2, NoChange = 100 }
    private ZoomLevel currentCameraZoom = ZoomLevel.Medium;
    private const float stopFollowPointerTime = 3f;
    private bool followingPointer;

    // Follow queue mechanism
    private readonly object objectToFollowLock = new object();
    private readonly List<Transform> objectToFollow = new List<Transform>();

    #endregion

    #region Init

    private void Awake()
    {
        Instance = this;

        if (MainCamera == null) {
            MainCamera = Camera.main;
        }

        pointerPositionHolderOriginal = pointerPositionHolder.transform.position;
        cinemachineBasicMultiChannelPerlin = cameraWorld.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        Cursor.visible = true;
    }

    private void Start()
    {
        // Log.Message("CameraManager", "Started");
        ZoomFullMap();
    }

    #endregion

    #region Pointer

    public void FollowPointer(Vector2 screenPosition)
    {
        // Log.Message("CameraManager", "Will follow pointer");
        followingPointer = true;
        pointerPositionHolder.transform.position = ScreenToWorldPoint(screenPosition).WithZ(0);
        Follow(pointerPositionHolder.transform);
        Frame(FrameTrackPointSmall);
    }

    public void FollowPointerRelease()
    {
        // Log.Message("CameraManager", "Stop follow pointer");
        followingPointer = false;
        StartCoroutine(FollowPointerStop());
    }

    /// From axis position to relative position to screen position to world position
    private Vector3 AxisToWorldPosition(Vector2 axisPosition)
    {
        Vector2 relativePosition = new Vector2(axisPosition.x + 1, axisPosition.y + 1) / 2f; // [-1, 1] => [0, 1]
        Vector2 screenPosition = relativePosition * new Vector2(Screen.width, Screen.height);
        return ScreenToWorldPoint(screenPosition).WithZ(0);
    }

    private IEnumerator FollowPointerStop()
    {
        // Lerp pointer position to center
        float time = 0;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 startPosition = pointerPositionHolder.transform.position;
        Vector2 targetPosition = ScreenToWorldPoint(new Vector2(0.5f, 0.5f) * screenSize);

        while (time < stopFollowPointerTime) {
            try {
                targetPosition = ScreenToWorldPoint(new Vector2(0.5f, 0.5f) * screenSize);
                pointerPositionHolder.transform.position = Vector2.Lerp(startPosition, targetPosition, time / stopFollowPointerTime).WithZ(0);
                time += Time.deltaTime;
            } catch (Exception e) {
                Log.Critical("CameraManager", "Stop follow pointer crash (1): " + e);
            }
            yield return new WaitForEndOfFrame();
        }

        try {
            pointerPositionHolder.transform.position = targetPosition.WithZ(0);

            FollowPrevious();
            Frame(FrameDefaultMedium);
        } catch (Exception e) {
            Log.Critical("CameraManager", "Stop follow pointer crash (2): " + e);
        }
    }

    #endregion

    #region Shake

    /// <summary>
    /// Shake the camera, intensity 2 is low, 5 is quite high
    /// </summary>
    public void ShakeCamera(float intensity, float time)
    {
        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;
        this.ExecuteInSecond(time, () => {
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0;
        });
    }

    #endregion

    #region Sudden Death

    public async UniTask RiseBounds(float level, float time)
    {
#if CC_EXTRA_CARE
try {
#endif
        await cameraBounds.transform.DOLocalMoveY(cameraBounds.transform.position.y + level, time).WithCancellation(this.GetCancellationTokenOnDestroy());
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    #endregion

    #region Follow Transform

#if CC_DEBUG

    private bool isLocked; // Used in trailer scenes
    public void RequestFollow(Transform followMe, float releaseInSeconds = 0, bool locked = false)
    {
        if (isLocked && !locked) return;
        isLocked = locked;
        // Log.Message("CameraManager", "Request to follow: " + followMe.name);
        if (followingPointer) return;
        lock (objectToFollowLock) {
            if (objectToFollow.Count > 0 && objectToFollow[objectToFollow.Count - 1] == followMe) return;
            objectToFollow.Add(followMe);
        }
        // Log.Message("CameraManager", "Will follow: " + followMe.name);
        Follow(followMe);
        Frame(FrameDefaultMedium);
        if (releaseInSeconds > 0) {
            this.ExecuteInSecond(releaseInSeconds, () => ReleaseFollow(followMe));
        }
    }

#else

    public void RequestFollow(Transform followMe, float releaseInSeconds = 0)
    {
        // Log.Message("CameraManager", "Request to follow: " + followMe.name);
        if (followingPointer) return;
        lock (objectToFollowLock) {
            if (objectToFollow.Count > 0 && objectToFollow[objectToFollow.Count - 1] == followMe) return;
            objectToFollow.Add(followMe);
        }
        // Log.Message("CameraManager", "Will follow: " + followMe.name);
        Follow(followMe);
        Frame(FrameDefaultMedium);
        if (releaseInSeconds > 0) {
            this.ExecuteInSecond(releaseInSeconds, () => ReleaseFollow(followMe));
        }
    }

#endif

    /// <summary>
    /// Stop following this object and remove it from the queue
    /// </summary>
    public void ReleaseFollow(Transform followMe)
    {
        if (followMe == null) return;

        // Log.Message("CameraManager", "Release follow of: " + followMe.name);
        if (followingPointer) return;
        lock (objectToFollowLock) {
            // Remove the last one from the list
            int index = -1;
            for (int i = 0; i < objectToFollow.Count; i++) {
                Transform candidate = objectToFollow[i];
                if (candidate == followMe) {
                    index = i;
                }
            }
            if (index >= 0 && index < objectToFollow.Count) {
                // Log.Message("CameraManager", "Release follow of: " + followMe.name + " success");
                objectToFollow.RemoveAt(index);
            } else {
                // Log.Message("CameraManager", "Release follow of: " + followMe.name + " gave unvalid index: " + index);
            }
            // Cleanup
            objectToFollow.RemoveAll(i => i == null);
        }

        FollowPrevious();
    }

    private void Follow(Transform transform)
    {
        if (transform == null) return;
        cameraWorld.Follow = transform;
    }

    private void FollowPrevious()
    {
        if (objectToFollow.Count == 0) {
            // Log.Message("CameraManager", "Ask follow previous, but no previous object are available (it's ok when first start)");
            return;
        }
        try {
            // Log.Message("CameraManager", "Will follow previous: " + objectToFollow[objectToFollow.Count - 1].name);
            Follow(objectToFollow[objectToFollow.Count - 1]);
        } catch {
            // Object have been destroyed
        }
    }

    #endregion

    #region Camera Zoom

    public void ResetCamera()
    {
        ZoomAt(ZoomLevel.Medium);
        Frame(FrameDefaultMedium);
    }

    public void ZoomIn()
    {
        // Log.Message("CameraManager", "Zoom in");
        int currentLevel = (int)currentCameraZoom;
        currentLevel++;
        if (currentLevel > (int)ZoomLevel.Max) {
            currentLevel = (int)ZoomLevel.Max;
        } else {
            ZoomAt((ZoomLevel)currentLevel);
        }
    }

    public void ZoomOut()
    {
        // Log.Message("CameraManager", "Zoom out");
        int currentLevel = (int)currentCameraZoom;
        currentLevel--;
        if (currentLevel < (int)ZoomLevel.Min) {
            currentLevel = (int)ZoomLevel.Min;
        } else {
            ZoomAt((ZoomLevel)currentLevel);
        }
    }

    public void ZoomAt(ZoomLevel level)
    {
        // Log.Message("CameraManager", "Zoom At: " + level.ToString());
        if (level == ZoomLevel.NoChange) return;
        currentCameraZoom = level;
        switch (currentCameraZoom) {
            case ZoomLevel.Min:
                Size(CameraZoomLevelMin);
                break;
            case ZoomLevel.Medium:
                Size(CameraZoomLevelMedium);
                break;
            case ZoomLevel.Max:
                Size(CameraZoomLevelMax);
                break;
        }
    }

    public void ZoomFullMap()
    {
        Size(CameraZoomLevelFullMap);
        Frame(FrameTrackPointSmall);
        pointerPositionHolder.transform.position = pointerPositionHolderOriginal;
        Follow(pointerPositionHolder.transform);
    }

#if CC_DEBUG // For trailer
    public void Frame(Vector2 frameSize)
#else
    private void Frame(Vector2 frameSize)
#endif
    {
        CinemachineFramingTransposer framingTransposer = cameraWorld.GetCinemachineComponent<CinemachineFramingTransposer>();
        framingTransposer.m_DeadZoneHeight = frameSize.x;
        framingTransposer.m_DeadZoneWidth = frameSize.y;
        framingTransposer.m_ScreenY = frameSize.y < 0.1f ? 0.5f : 0.65f;
    }

#if CC_DEBUG // For trailer
    public void Size(float size)
#else
    private void Size(float size)
#endif
    {
        DOTween
            .To(() => cameraWorld.m_Lens.OrthographicSize, v => cameraWorld.m_Lens.OrthographicSize = v, size, .3f)
        ;
    }

    #endregion

    #region Calculus

    public Vector3 ScreenToWorldPoint(Vector3 v)
    {
        // This may happen when the game stop, the camera is destructed before the Input mechanism that use it
        if (MainCamera == null) return Vector3.zero;
        return MainCamera.ScreenToWorldPoint(v);
    }

    #endregion
}
