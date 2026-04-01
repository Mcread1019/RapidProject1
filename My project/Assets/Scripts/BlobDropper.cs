// =========================================================
//  BlobDropper.cs  —  Blob Stack  (Unity 6 compatible)
//  Attach to: BlobDropper prefab
//  Requires: Rigidbody2D, CircleCollider2D
// =========================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BlobDropper : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("Horizontal force applied each frame when A/D held")]
    public float nudgeForce = 6f;

    [Tooltip("Maximum horizontal speed — prevents infinite acceleration")]
    public float maxHSpeed   = 4f;

    [Header("Visuals")]
    [Tooltip("Color of the player blob (distinct from stack blobs)")]
    public Color playerColor = Color.white;

    // ── Private ────────────────────────────────────────────
    Rigidbody2D rb;
    bool landed = false;

    // ── Unity lifecycle ───────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = playerColor;
    }

    void Update()
    {
        // Ignore input if landed or game isn't in drop phase
        if (landed || GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Dropping) return;

        float input = Input.GetAxisRaw("Horizontal");  // A/D or ← →

        if (Mathf.Abs(input) > 0.01f)
        {
            rb.AddForce(Vector2.right * input * nudgeForce);

            // Unity 6: use linearVelocity instead of velocity
            rb.linearVelocity = new Vector2(
                Mathf.Clamp(rb.linearVelocity.x, -maxHSpeed, maxHSpeed),
                rb.linearVelocity.y
            );
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (landed) return;

        bool hitGround    = col.gameObject.CompareTag("Ground");
        bool hitStackBlob = col.gameObject.CompareTag("StackBlob");

        if (hitGround || hitStackBlob)
        {
            landed = true;

            // Freeze in place — becomes a visual until GameManager handles it
            // Unity 6: use linearVelocity
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic    = true;

            GameManager.Instance.OnBlobLanded(this);

            // Destroy self after a short delay so the player can see where it landed
            Destroy(gameObject, 0.6f);
        }
    }
}
