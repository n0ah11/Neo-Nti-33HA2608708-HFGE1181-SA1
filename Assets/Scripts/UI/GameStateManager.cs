using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameStateManager : MonoBehaviour
{
    public PlayerInputActions InputActions;
    public static GameStateManager Instance;
    
    [SerializeField] private GameState gameState = GameState.Playing;

    private void Awake()
    {
        InputActions = new PlayerInputActions();
        
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        Time.timeScale = 1f; // SA1: reset after a restart from the pause menu
    }

    private void OnEnable()
    {
        InputActions.UI.Enable();
        
        // **CREATE A PAUSE BUTTON IN YOUR INPUT ACTIONS**  (SA1: done - Esc / P / gamepad Start)
        InputActions.UI.Pause.performed += OnPause;
        

    }
    
    private void OnDisable()
    {
        InputActions.UI.Pause.performed -= OnPause;
        InputActions.UI.Disable();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetGameState(GameState newState)
    {
        gameState = newState;
        // SA1: freeze physics, food attraction and timers while paused
        Time.timeScale = gameState == GameState.Paused ? 0f : 1f;
    }
    
    public GameState GetGameState()
    {
        return gameState;
    }
    
    private void OnPause(InputAction.CallbackContext obj)
    {
        TogglePause();
    }

    // SA1: public so the on-screen pause button (mobile) can call it
    public void TogglePause()
    {
        if (gameState == GameState.GameOver) return; // can't pause on the end screens

        if (gameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
        else
        {
            SetGameState(GameState.Paused);
        }
    }

}
