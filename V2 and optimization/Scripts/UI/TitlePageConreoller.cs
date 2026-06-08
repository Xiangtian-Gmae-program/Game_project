// Script note: Loads the main menu when any key is pressed on the title page.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;
using UnityEngine.SceneManagement;

// Class responsibility: TitlePageController contains this script's gameplay behavior.
public class TitlePageController : MonoBehaviour
{
    private bool hasEntered = false;

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {
        if (hasEntered) return;

        if (Input.anyKeyDown)
        {
            hasEntered = true;
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}