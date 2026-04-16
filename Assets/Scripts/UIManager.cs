using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    [Header("End Screen UI")]
    public GameObject endScreenPanel;
    public TextMeshProUGUI resultMessage;
    public Button restartButton;

    private void Start()
    {
        // Start with a clean screen
        if (endScreenPanel) endScreenPanel.SetActive(false);

        // Hook up the button via code to avoid losing references in the Inspector
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);

        // Subscribe to the GameManager here to ensure it exists first
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;
            GameManager.Instance.OnTimeChanged += UpdateTimerUI;
            GameManager.Instance.OnGameOver += ShowEndScreen;

            // Update UI immediately with total count
            int total = FindObjectsByType<CollectibleCube>(FindObjectsSortMode.None).Length;
            UpdateScoreUI(0, total);
        }
    }

    private void OnDestroy()
    {
        // Always clean up event listeners to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnTimeChanged -= UpdateTimerUI;
            GameManager.Instance.OnGameOver -= ShowEndScreen;
        }
    }

    private void UpdateScoreUI(int current, int total)
    {
        if (scoreText) scoreText.text = $"{current} / {total}";
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (!timerText) return;

        // Convert raw seconds into a standard 00:00 digital clock format
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ShowEndScreen(string message, bool isWin)
    {
        if (endScreenPanel) endScreenPanel.SetActive(true);

        if (resultMessage)
        {
            resultMessage.text = message;
            // Visual feedback: Green for a win, Red for a loss
            resultMessage.color = isWin ? Color.green : Color.red;
        }
    }

    public void OnRestartClicked()
    {
        // The game freezes when it ends, so we must thaw time before reloading
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}