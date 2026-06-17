// Purpose: Manages scene transitions between title, menu, practice, and main-game scenes.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFlowManager : MonoBehaviour
{
    private Text difficultyText;
    private Text recordsText;
    private Button easyButton;
    private Button normalButton;
    private Button hardButton;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenuScene")
        {
            BuildDifficultyUI();
            UpdateDifficultyUI();
        }
    }

    public void LoadScene(string sceneName)
    {
        if (sceneName == "maingame")
        {
            MainGameProgress.StartNewGame();
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ContinueMainGame()
    {
        MainGameProgress.ContinueGame();
        SceneManager.LoadScene("maingame");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetEasyDifficulty()
    {
        SetDifficulty(0);
    }

    public void SetNormalDifficulty()
    {
        SetDifficulty(1);
    }

    public void SetHardDifficulty()
    {
        SetDifficulty(2);
    }

    private void SetDifficulty(int difficulty)
    {
        PlayerPrefs.SetInt("MainGameDifficulty", difficulty);
        PlayerPrefs.Save();
        UpdateDifficultyUI();
    }

    private void BuildDifficultyUI()
    {
        if (GameObject.Find("MainGameDifficultyPanel") != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panel = new GameObject("MainGameDifficultyPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(34, 34);
        rect.sizeDelta = new Vector2(360, 142);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.045f, 0.035f, 0.78f);

        CreateText("Title", panel.transform, "DIFFICULTY", font, 20, new Vector2(18, -12), new Vector2(324, 30), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        difficultyText = CreateText("Current", panel.transform, "CURRENT  NORMAL", font, 16, new Vector2(18, -46), new Vector2(324, 24), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));

        easyButton = CreateButton("EasyButton", panel.transform, "EASY", font, new Vector2(18, -86), new Vector2(96, 38), SetEasyDifficulty);
        normalButton = CreateButton("NormalButton", panel.transform, "NORMAL", font, new Vector2(132, -86), new Vector2(96, 38), SetNormalDifficulty);
        hardButton = CreateButton("HardButton", panel.transform, "HARD", font, new Vector2(246, -86), new Vector2(96, 38), SetHardDifficulty);

        AddBorder(panel.transform, rect.sizeDelta);

        GameObject recordPanel = new GameObject("MainGameRecordPanel");
        recordPanel.transform.SetParent(canvas.transform, false);

        RectTransform recordRect = recordPanel.AddComponent<RectTransform>();
        recordRect.anchorMin = new Vector2(1, 0);
        recordRect.anchorMax = new Vector2(1, 0);
        recordRect.pivot = new Vector2(1, 0);
        recordRect.anchoredPosition = new Vector2(-34, 34);
        recordRect.sizeDelta = new Vector2(390, 210);

        Image recordImage = recordPanel.AddComponent<Image>();
        recordImage.color = new Color(0.06f, 0.045f, 0.035f, 0.78f);

        CreateText("RecordsTitle", recordPanel.transform, "RECORDS", font, 20, new Vector2(18, -12), new Vector2(354, 30), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        recordsText = CreateText("Records", recordPanel.transform, "", font, 16, new Vector2(18, -46), new Vector2(354, 92), TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        CreateButton("ContinueMainGameButton", recordPanel.transform, "CONTINUE", font, new Vector2(18, -154), new Vector2(160, 38), ContinueMainGame);
        CreateButton("NewMainGameButton", recordPanel.transform, "NEW GAME", font, new Vector2(212, -154), new Vector2(160, 38), StartMainGame);
        AddBorder(recordPanel.transform, recordRect.sizeDelta);
    }

    private void StartMainGame()
    {
        MainGameProgress.StartNewGame();
        SceneManager.LoadScene("maingame");
    }

    private Text CreateText(string name, Transform parent, string value, Font font, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string value, Font font, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.095f, 0.07f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText("Text", buttonObject.transform, value, font, 15, Vector2.zero, size, TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        text.raycastTarget = false;
        AddBorder(buttonObject.transform, size);
        return button;
    }

    private void UpdateDifficultyUI()
    {
        int difficulty = PlayerPrefs.GetInt("MainGameDifficulty", 1);
        if (difficultyText != null)
        {
            if (difficulty == 0)
            {
                difficultyText.text = "CURRENT  EASY";
            }
            else if (difficulty == 2)
            {
                difficultyText.text = "CURRENT  HARD";
            }
            else
            {
                difficultyText.text = "CURRENT  NORMAL";
            }
        }

        SetButtonColor(easyButton, difficulty == 0);
        SetButtonColor(normalButton, difficulty == 1);
        SetButtonColor(hardButton, difficulty == 2);

        if (recordsText != null)
        {
            recordsText.text =
                "UNLOCKED LEVEL  " + MainGameProgress.UnlockedLevel + "/" + MainGameProgress.MaxLevel + "\n" +
                "CLEARS          " + PlayerPrefs.GetInt("MainGameClearCount", 0) + "\n" +
                "BEST L1         " + PlayerPrefs.GetInt(MainGameProgress.GetBestScoreKey(1), 0) + "\n" +
                "BEST L2         " + PlayerPrefs.GetInt(MainGameProgress.GetBestScoreKey(2), 0) + "\n" +
                "BEST L3         " + PlayerPrefs.GetInt(MainGameProgress.GetBestScoreKey(3), 0) + "\n" +
                "BEST BOSS       " + PlayerPrefs.GetInt(MainGameProgress.GetBestScoreKey(4), 0);
        }
    }

    private void SetButtonColor(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected ? new Color(0.34f, 0.21f, 0.08f, 0.98f) : new Color(0.12f, 0.095f, 0.07f, 0.96f);
        }
    }

    private void AddBorder(Transform parent, Vector2 size)
    {
        AddBorderLine(parent, "Top", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(size.x, 2));
        AddBorderLine(parent, "Bottom", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, new Vector2(size.x, 2));
        AddBorderLine(parent, "Left", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(2, size.y));
        AddBorderLine(parent, "Right", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(2, size.y));
    }

    private void AddBorderLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject line = new GameObject("Border_" + name);
        line.transform.SetParent(parent, false);

        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = line.AddComponent<Image>();
        image.color = new Color(0.72f, 0.48f, 0.2f, 0.95f);
    }
}

