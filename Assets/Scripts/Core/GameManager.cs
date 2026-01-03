using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Init,
        Playing,
        GameOver
    }

    public GameState currentState;

    void Start()
    {
        ChangeState(GameState.Init);
    }

    void Update()
    {
        // Restart game when in GameOver state and R is pressed
        if (currentState == GameState.GameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.Log("Restarting game...");
                ChangeState(GameState.Init);
            }
        }
    }

    void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State changed to: " + currentState);

        switch (currentState)
        {
            case GameState.Init:
                InitGame();
                break;

            case GameState.Playing:
                StartGame();
                break;

            case GameState.GameOver:
                EndGame();
                break;
        }
    }

    void InitGame()
    {
        Debug.Log("Initializing game...");
        ChangeState(GameState.Playing);
    }

    void StartGame()
    {
        Debug.Log("Game is now playing");
    }

    void EndGame()
    {
        Debug.Log("Game Over");
    }
}
