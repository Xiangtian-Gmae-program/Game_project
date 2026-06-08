// Script note: Controls pause UI, resume, practice restart, and returning to the main menu.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;
using UnityEngine.SceneManagement;

// Class responsibility: PauseMenuController contains this script's gameplay behavior.
public class PauseMenuController : MonoBehaviour
{
    public GameObject pausePanel;
    private bool isPaused = false;

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // Toggles pause state.
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    // Resumes gameplay from the pause menu.
    public void ResumeGame()
    {
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Reloads the practice scene.
    public void RestartPractice()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("PracticeScene");
    }

    // Restores time scale and loads the main menu.
    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}