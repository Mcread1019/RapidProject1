// =========================================================
//  GameManager.cs  —  Blob Stack  (Unity 6 compatible)
//  Attach to: empty GameObject "GameManager" in scene
// =========================================================
using UnityEngine;
using TMPro;   // Unity 6: use TextMeshPro instead of UnityEngine.UI.Text

public enum GameState { Idle, Dropping, Result }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────
    [Header("Game Settings")]
    public float runDuration = 5f;

    [Header("Prefabs & Scene Refs")]
    public GameObject blobPrefab;       // BlobDropper prefab (see BlobDropper.cs)
    public StackManager stackManager;   // Drag StackManager object here

    [Header("UI — wire up in Inspector")]
    public TMP_Text timerText;
    public TMP_Text heightText;
    public TMP_Text bestText;
    public TMP_Text messageText;
    public UnityEngine.UI.Button actionButton;
    public TMP_Text actionButtonLabel;

    // ── Private state ─────────────────────────────────────
    public GameState State { get; private set; } = GameState.Idle;
    float timeLeft;
    int bestHeight;

    // ── Unity lifecycle ───────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        bestHeight = PlayerPrefs.GetInt("BestHeight", 0);
    }

    void Start()
    {
        stackManager.LoadAndBuild();
        RefreshStatUI();
        EnterState(GameState.Idle);
    }

    void Update()
    {
        if (State != GameState.Dropping) return;

        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.Max(0f, timeLeft).ToString("F1");

        if (timeLeft <= 0f)
            OnTimerExpired();
    }

    // ── Public — called from UI Button (OnClick) ──────────
    public void OnActionButton()
    {
        if (State == GameState.Dropping) return;
        BeginDrop();
    }

    // ── Drop flow ─────────────────────────────────────────
    void BeginDrop()
    {
        EnterState(GameState.Dropping);
        timeLeft = runDuration;

        // Spawn blob above the top of the current stack
        float spawnX = Random.Range(-0.8f, 0.8f);
        float spawnY = stackManager.GetTopY() + 7f;
        Instantiate(blobPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }

    // Called by BlobDropper when it hits Ground or StackBlob
    public void OnBlobLanded(BlobDropper blob)
    {
        if (State != GameState.Dropping) return;

        stackManager.AddBlob(blob.transform.position);
        stackManager.Save();

        bool tipped = stackManager.IsTipped();
        if (tipped)
        {
            int reached = stackManager.Height;
            stackManager.Clear();
            EndRun($"Stack toppled! Height reached: {reached}");
        }
        else
        {
            int h = stackManager.Height;
            if (h > bestHeight)
            {
                bestHeight = h;
                PlayerPrefs.SetInt("BestHeight", bestHeight);
            }
            EndRun($"Landed!  Stack height: {h}");
        }
    }

    void OnTimerExpired()
    {
        // Blob still falling when time ran out — destroy it, end run
        // Unity 6: FindObjectsByType replaces deprecated FindObjectsOfType
        BlobDropper[] stragglers = FindObjectsByType<BlobDropper>(FindObjectsSortMode.None);
        foreach (var b in stragglers) Destroy(b.gameObject);
        EndRun("Time's up! Blob missed.");
    }

    void EndRun(string msg)
    {
        messageText.text = msg;
        RefreshStatUI();
        EnterState(GameState.Result);
    }

    // ── State helpers ─────────────────────────────────────
    void EnterState(GameState s)
    {
        State = s;

        actionButton.interactable = (s != GameState.Dropping);

        switch (s)
        {
            case GameState.Idle:
                actionButtonLabel.text    = "Drop!";
                messageText.text          = "Stack persists between sessions.\n" +
                                            "Land blobs precisely — tip the stack and start over.";
                timerText.text            = runDuration.ToString("F1");
                break;

            case GameState.Dropping:
                actionButtonLabel.text    = "···";
                messageText.text          = "";
                break;

            case GameState.Result:
                actionButtonLabel.text    = "Drop Again";
                break;
        }
    }

    void RefreshStatUI()
    {
        heightText.text = $"Stack: {stackManager.Height}";
        bestText.text   = $"Best:  {bestHeight}";
    }
}
