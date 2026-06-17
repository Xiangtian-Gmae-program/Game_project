// Purpose: Handles title-screen input and transition into the main menu.
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitlePageController : MonoBehaviour
{
    private bool hasEntered = false;

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

