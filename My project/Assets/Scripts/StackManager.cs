// =========================================================
//  StackManager.cs  —  Blob Stack
//  Attach to: empty GameObject "StackManager" in scene
//
//  Handles:
//    • Saving/loading stack data via PlayerPrefs + JSON
//    • Spawning kinematic stack blobs as physics colliders
//    • Tip detection
//    • Color-coding blobs by age
// =========================================================
using System;
using System.Collections.Generic;
using UnityEngine;

// ── Serialisable data model ────────────────────────────────
[Serializable]
public class BlobRecord
{
    public float x;   // world X of landing
    public float y;   // world Y (computed from stack order, not physics Y)
}

[Serializable]
public class StackSaveData
{
    public List<BlobRecord> blobs = new List<BlobRecord>();
}

// ── StackManager ──────────────────────────────────────────
public class StackManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────
    [Header("Prefab")]
    [Tooltip("Circle sprite, kinematic Rigidbody2D, CircleCollider2D, tag='StackBlob'")]
    public GameObject stackBlobPrefab;

    [Header("Stack Physics")]
    [Tooltip("Radius of each blob (match CircleCollider2D radius)")]
    public float blobRadius = 0.4f;

    [Tooltip("World Y of the ground surface (match your Ground collider top edge)")]
    public float groundY = -3.8f;

    [Tooltip("|x| beyond this = stack considered tipped (checked on last 3 blobs)")]
    public float tipThreshold = 3.2f;

    [Header("Visuals")]
    [Tooltip("Oldest blob = left end, newest = right end")]
    public Gradient ageGradient;

    // ── Private ────────────────────────────────────────────
    StackSaveData data = new StackSaveData();
    List<GameObject> liveBlobs = new List<GameObject>();

    const string SAVE_KEY = "BlobStack_v1";

    // ── Public accessors ──────────────────────────────────
    public int Height => data.blobs.Count;

    /// <summary>Returns the world Y of the TOP of the current stack.</summary>
    public float GetTopY()
    {
        return groundY + (data.blobs.Count * blobRadius * 2f);
    }

    // ── Persistence ───────────────────────────────────────
    public void LoadAndBuild()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        data = string.IsNullOrEmpty(json)
            ? new StackSaveData()
            : JsonUtility.FromJson<StackSaveData>(json);

        RebuildVisuals();
    }

    public void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    // ── Stack operations ──────────────────────────────────
    /// <summary>Record a landed blob and spawn its visual/collider.</summary>
    public void AddBlob(Vector2 worldPos)
    {
        // Y is deterministic from stack count — prevents save/load drift
        float stackedY = groundY + (data.blobs.Count * blobRadius * 2f) + blobRadius;
        var record = new BlobRecord { x = Mathf.Clamp(worldPos.x, -5f, 5f), y = stackedY };
        data.blobs.Add(record);

        SpawnVisual(record, data.blobs.Count - 1);
        RefreshColors();   // re-colour entire stack so gradient stays consistent
    }

    /// <summary>Returns true if the most-recent blobs have drifted too far.</summary>
    public bool IsTipped()
    {
        int n = data.blobs.Count;
        // Check last 3 blobs (or all, if fewer)
        for (int i = Mathf.Max(0, n - 3); i < n; i++)
        {
            if (Mathf.Abs(data.blobs[i].x) > tipThreshold)
                return true;
        }
        return false;
    }

    /// <summary>Wipe the stack (called after tip).</summary>
    public void Clear()
    {
        data = new StackSaveData();
        foreach (var go in liveBlobs)
            if (go != null) Destroy(go);
        liveBlobs.Clear();
        Save();
    }

    // ── Internal helpers ──────────────────────────────────
    void RebuildVisuals()
    {
        foreach (var go in liveBlobs)
            if (go != null) Destroy(go);
        liveBlobs.Clear();

        for (int i = 0; i < data.blobs.Count; i++)
            SpawnVisual(data.blobs[i], i);

        RefreshColors();
    }

    void SpawnVisual(BlobRecord record, int index)
    {
        var go = Instantiate(
            stackBlobPrefab,
            new Vector3(record.x, record.y, 0f),
            Quaternion.identity
        );

        // Scale to match blobRadius (assumes prefab uses default 1-unit circle)
        go.transform.localScale = Vector3.one * (blobRadius * 2f);

        // Tag so BlobDropper can detect landing
        go.tag = "StackBlob";

        // Make sure Rigidbody2D is kinematic so the stack doesn't fall over
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.isKinematic = true;

        liveBlobs.Add(go);
    }

    void RefreshColors()
    {
        // Evaluate gradient across the whole stack so colors are always relative
        int n = liveBlobs.Count;
        for (int i = 0; i < n; i++)
        {
            if (liveBlobs[i] == null) continue;
            var sr = liveBlobs[i].GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            float t = n > 1 ? (float)i / (n - 1) : 1f;
            sr.color = ageGradient.Evaluate(t);
        }
    }
}
