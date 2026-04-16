using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // This lets other scripts find the "Brain" easily
    public static GameManager Instance { get; private set; }

    // --- Broadcasters ---
    // These are code-only, so they don't need Inspector headers
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
        // Simple Singleton: If another Brain exists, destroy this one
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Time.timeScale = 1f; // Make sure the game isn't paused

        // Count how many cubes I placed in the scene so we know when to win
        totalCubes = FindObjectsByType<CollectibleCube>(FindObjectsSortMode.None).Length;

        // Tell the UI to show 0 cubes at the start
        OnScoreChanged?.Invoke(collectedCubes, totalCubes);
    }

    private void Update()
    {
        if (isGameOver) return;

        // Count down the time
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

    // Every time a cube is touched, it calls this function
    public void AddCube()
    {
        collectedCubes++;
        OnScoreChanged?.Invoke(collectedCubes, totalCubes);

        // If we got them all, you win!
        if (collectedCubes >= totalCubes)
        {
            TriggerGameOver("Level Complete!", true);
        }
    }

    public void TriggerGameOver(string message, bool isWin)
    {
        if (isGameOver) return;
        isGameOver = true;

        Time.timeScale = 0f; // Freeze the game world

        // Give the player their mouse back so they can restart
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnGameOver?.Invoke(message, isWin);
    }
}