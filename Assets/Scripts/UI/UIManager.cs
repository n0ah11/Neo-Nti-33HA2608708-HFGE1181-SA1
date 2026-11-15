using System;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject hud;
    [SerializeField] private PlayerSizeHandler playerSizeHandler;
    [SerializeField] private TextMeshProUGUI tmpSize;
    [SerializeField] private TextMeshProUGUI tmpTimer;
    private float timer = 0;
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        GameState state = GameStateManager.Instance.GetGameState();
        if (state == GameState.Playing)
        {
            Cursor.visible = MobileControls.UseTouchControls; // SA1: keep cursor in the editor's touch simulation
            hud.SetActive(true);
            pauseMenu.SetActive(false);
            UpdateTimer();
            UpdateSize();  
        }
        else if (state == GameState.Paused)
        {
            Cursor.visible = true;
            hud.SetActive(false);
            pauseMenu.SetActive(true);
        }
        else // SA1: Game Over / Win - GameFlowManager shows the end panel
        {
            Cursor.visible = true;
            hud.SetActive(false);
            pauseMenu.SetActive(false);
        }
        
    }

    public void UpdateTimer()
    {
        timer += Time.deltaTime;
        tmpTimer.text = "Timer: " + TimeSpan.FromSeconds(timer).ToString(@"mm\:ss");
    }

    public void UpdateSize()
    {
        tmpSize.text = "Size: " + playerSizeHandler.GetSize().ToString("0.00"); // SA1: tidy number format
    }

    // SA1: used by the end screens
    public float GetTimer()
    {
        return timer;
    }

    public void Resume()
    {
        GameStateManager.Instance.SetGameState(GameState.Playing);
    }
    
    
}
