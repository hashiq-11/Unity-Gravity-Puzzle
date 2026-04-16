using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton access for global game state management
    public static GameManager Instance { get; private set; }

    // --- Event Broadcasters ---
    // Decoupled architecture: The manager broadcasts state changes, and UI/Camera tune in.
    public event Action<int, int> OnScoreChanged;
    public event Action<float> OnTimeChanged;
    public event Action<string, bool> OnGameOver;

    [Header("Rules")]
    public float timeLimit = 120f;

    private int totalCubes;
    private int collectedCubes;
    private bool isGameOver = false;

    private void Awake()
    {
        // Enforce the Singleton pattern to ensure only one active manager exists
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f; // Ensure gameplay physics are active

        // Dynamically count all collectibles in the level to set the win condition
        totalCubes = FindObjectsByType<CollectibleCube>(FindObjectsSortMode.None).Length;

        // Initialize the UI with the starting score
        OnScoreChanged?.Invoke(collectedCubes, totalCubes);
    }

    private void Update()
    {
        if (isGameOver) return;

        // Handle the level countdown timer
        if (timeLimit > 0)
        {
            timeLimit -= Time.deltaTime;
            OnTimeChanged?.Invoke(timeLimit);
        }
        else
        {
            TriggerGameOver("Time's Up!", false);
        }
    }

    // Called externally by CollectibleCube objects when picked up
    public void AddCube()
    {
        collectedCubes++;
        OnScoreChanged?.Invoke(collectedCubes, totalCubes);

        // Check if the win condition is met
        if (collectedCubes >= totalCubes)
        {
            TriggerGameOver("Level Complete!", true);
        }
    }

    public void TriggerGameOver(string message, bool isWin)
    {
        if (isGameOver) return;
        isGameOver = true;

        // Freeze the game world (halts physics and movement)
        Time.timeScale = 0f;

        // Restore mouse cursor control so the player can interact with the UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Broadcast the game over state to listening scripts (UI, Camera)
        OnGameOver?.Invoke(message, isWin);
    }
}