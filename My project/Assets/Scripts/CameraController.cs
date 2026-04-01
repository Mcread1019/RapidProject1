// =========================================================
//  CameraController.cs  —  Blob Stack  (Unity 6 compatible)
//  Attach to: Main Camera
// =========================================================
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────
    [Header("Scene References")]
    public StackManager stackManager;

    [Header("Base framing")]
    [Tooltip("Orthographic size when the stack is empty")]
    public float baseCamSize = 7f;

    [Tooltip("World Y the camera looks at when the stack is empty")]
    public float baseYTarget = 1f;

    [Header("Zoom-out")]
    [Tooltip("Extra orthographic size added per blob in the stack")]
    public float sizePerBlob = 0.18f;

    [Tooltip("Maximum orthographic size (prevents infinite zoom-out)")]
    public float maxCamSize = 22f;

    [Header("Pan")]
    [Tooltip("How far above the stack top the camera looks (world units)")]
    public float lookAheadAboveStack = 3f;

    [Header("Smoothing")]
    [Tooltip("Lower = snappier, higher = floatier (good range: 2–6)")]
    public float smoothSpeed = 3.5f;

    // ── Private ────────────────────────────────────────────
    Camera cam;
    float targetSize;
    float targetY;

    // ── Unity lifecycle ───────────────────────────────────
    void Awake()
    {
        cam = GetComponent<Camera>();
        targetSize = baseCamSize;
        targetY    = baseYTarget;
    }

    void LateUpdate()
    {
        ComputeTargets();

        // Smoothly lerp size and Y position
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            Time.deltaTime * smoothSpeed
        );

        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * smoothSpeed);
        transform.position = pos;
    }

    // ── Helpers ───────────────────────────────────────────
    void ComputeTargets()
    {
        if (stackManager == null) return;

        int height = stackManager.Height;

        // Size grows with the stack, clamped to max
        targetSize = Mathf.Min(
            baseCamSize + height * sizePerBlob,
            maxCamSize
        );

        if (height == 0)
        {
            // No stack — return to base position
            targetY = baseYTarget;
        }
        else
        {
            // Pan so the top of the stack sits in the upper-mid portion of the screen.
            // stackManager.GetTopY() returns the Y of the topmost blob surface.
            float stackTop  = stackManager.GetTopY();

            // The camera centre should sit so that the stack top is visible
            // with some look-ahead room above it.
            // At orthographic size S, the top of the visible area is camY + S.
            // We want: camY + S * 0.7 = stackTop + lookAheadAboveStack
            // → camY = stackTop + lookAheadAboveStack - S * 0.7
            float desiredY = stackTop + lookAheadAboveStack - targetSize * 0.7f;

            // Never go below the base target (camera shouldn't dip underground)
            targetY = Mathf.Max(desiredY, baseYTarget);
        }
    }

    // ── Public — call this when the stack is cleared ──────
    /// <summary>
    /// Smoothly return to the base framing after a tip/reset.
    /// Already handled automatically by Update since Height drops to 0,
    /// but you can call this explicitly for an instant reset if preferred.
    /// </summary>
    public void SnapToBase()
    {
        cam.orthographicSize = baseCamSize;
        Vector3 pos = transform.position;
        pos.y = baseYTarget;
        transform.position = pos;
    }
}
