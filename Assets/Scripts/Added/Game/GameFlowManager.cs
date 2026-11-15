using System.Collections;
using TMPro;
using UnityEngine;

/// SA1 addition: owns the win/lose flow. Win = survive the timer (10 minutes). Lose = eaten by a bigger enemy.
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [Header("Goal")]
    [SerializeField] private float surviveSeconds = 600f;

    [Header("References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerSizeHandler player;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TextMeshProUGUI winStats;
    [SerializeField] private TextMeshProUGUI loseStats;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private GameObject deathParticlePrefab;

    private float survived;
    private bool finished;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
    }

    private void Update()
    {
        if (finished || GameStateManager.Instance.GetGameState() != GameState.Playing) return;

        survived += Time.deltaTime;
        if (objectiveText)
        {
            float left = Mathf.Max(0f, surviveSeconds - survived);
            objectiveText.text = "Survive: " + System.TimeSpan.FromSeconds(left).ToString(@"mm\:ss");
        }

        if (survived >= surviveSeconds) PlayerWon();
    }

    public void PlayerWon()
    {
        if (finished) return;
        finished = true;
        GameStateManager.Instance.SetGameState(GameState.GameOver);
        SoundManager.Instance?.Play("Win");
        if (winStats) winStats.text = "You survived 10:00!\nFinal size: " + player.GetSize().ToString("0.00");
        StartCoroutine(ShowPanel(winPanel, 0.4f));
    }

    public void PlayerDied(GameObject playerObject)
    {
        if (finished) return;
        finished = true;
        GameStateManager.Instance.SetGameState(GameState.GameOver);

        SoundManager.Instance?.Stop("Music"); // music only plays while the player is alive
        SoundManager.Instance?.Play("Death");

        if (deathParticlePrefab)
        {
            GameObject fx = Instantiate(deathParticlePrefab, playerObject.transform.position, Quaternion.identity);
            fx.transform.localScale = playerObject.transform.localScale;
            Destroy(fx, 3f);
        }

        // hide the eaten player but keep the object alive (camera + indicators still reference it)
        foreach (var r in playerObject.GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        foreach (var c in playerObject.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        var rb = playerObject.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        float t = uiManager != null ? uiManager.GetTimer() : survived;
        if (loseStats) loseStats.text = "You were eaten!\nSurvived " + System.TimeSpan.FromSeconds(t).ToString(@"mm\:ss") +
                                        "   Size " + player.GetSize().ToString("0.00");
        StartCoroutine(ShowPanel(losePanel, 1.2f));
    }

    private IEnumerator ShowPanel(GameObject panel, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (panel) panel.SetActive(true);
    }
}
