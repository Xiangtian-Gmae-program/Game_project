// Script note: Opens and closes the settings panel in the menu UI.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;

// Class responsibility: MenuPanelController contains this script's gameplay behavior.
public class MenuPanelController : MonoBehaviour
{
    public GameObject settingsPanel;

    // Shows the settings panel.
    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    // Hides the settings panel.
    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}