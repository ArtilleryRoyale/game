using System.Collections;
using Jrmgx.Helpers;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenshotCamera : MonoBehaviour
{
    public Camera thatCamera = default;
    public RenderTexture renderTexture = default;
    public Transform followTransform = default;

    [Header("Sequence (Y key to start)")]
    public int NumberOfFrame = 60;

    [Header("Work on Renders")]
    public GameObject disableAllRendererOnThis = default;
    public GameObject enableAllRendererOnThis = default;

    [ContextMenu("Disable All Renderer Action")]
    public void DisableAllRenderer()
    {
        foreach (var renderer in disableAllRendererOnThis.GetComponents<Renderer>()) {
            renderer.enabled = false;
        }
        foreach (var renderer in disableAllRendererOnThis.GetComponentsInChildren<Renderer>()) {
            renderer.enabled = false;
        }
    }

    [ContextMenu("Enable All Renderer Action")]
    public void EnableAllRenderer()
    {
        foreach (var renderer in enableAllRendererOnThis.GetComponents<Renderer>()) {
            renderer.enabled = true;
        }
        foreach (var renderer in enableAllRendererOnThis.GetComponentsInChildren<Renderer>()) {
            renderer.enabled = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current.yKey.wasPressedThisFrame) {
            StartCoroutine(CaptureSequence());
        }

        if (disableAllRendererOnThis != null) {
            DisableAllRenderer();
            disableAllRendererOnThis = null;
        }

        if (enableAllRendererOnThis != null) {
            EnableAllRenderer();
            enableAllRendererOnThis = null;
        }
    }

    private void LateUpdate()
    {
        if (followTransform == null) return;
        thatCamera.transform.position = new Vector3(followTransform.position.x, followTransform.position.y - 5, -9);
    }

    private IEnumerator CaptureSequence()
    {
        Debug.Log("Capture started for " + NumberOfFrame);
        string sequenceIdentifier = "" + Basics.RandomIdentifier();
        for (int i = 0; i < NumberOfFrame; i++) {
            CaptureRenderCamera(sequenceIdentifier);
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("Capture finished");
    }

    [ContextMenu("Capture Render Camera")]
    public void CaptureRenderCamera()
    {
        CaptureRenderCamera("" + Basics.Timestamp);
    }

    private void CaptureRenderCamera(string sequenceIdentifier)
    {
        Basics.RenderTexturePNG(renderTexture, Application.persistentDataPath + "/capture_" + sequenceIdentifier + "_" + (Time.time * 1000) + "_" + Time.frameCount + ".png");
    }
}
