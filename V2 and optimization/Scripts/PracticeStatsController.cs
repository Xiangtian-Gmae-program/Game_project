// Script note: Builds practice-mode UI and records damage, hit count, blocks, roll dodges, time, and enemy difficulty.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Class responsibility: PracticeStatsController contains this script's gameplay behavior.
public class PracticeStatsController : MonoBehaviour
{
    private static PracticeStatsController instance;

    private Text statsText;
    private Text eventText;
    private Text difficultyText;
    private GameObject pausePanel;
    private bool isPaused;
    private int practiceDifficulty = 1;
    private bool difficultyApplied;

    private int totalDamage;
    private int hitCount;
    private int lastHit;
    private int currentCombo;
    private int maxCombo;
    private int defendCount;
    private int rollDodgeCount;
    private float sessionTime;
    private float lastHitTime = -100f;
    private string lastEvent = "Ready";

    private const float comboTimeout = 2f;

    // Ensures the runtime controller singleton exists.
    public static void Ensure()
    {
        if (!IsPracticeScene())
        {
            return;
        }

        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("PracticeStatsController");
        instance = go.AddComponent<PracticeStatsController>();
    }

    // Handles the RecordHit logic.
    public static void RecordHit(int damage)
    {
        Ensure();

        if (instance == null)
        {
            return;
        }

        instance.AddHit(damage);
    }

    // Handles the RecordDefend logic.
    public static void RecordDefend()
    {
        Ensure();

        if (instance == null)
        {
            return;
        }

        instance.defendCount++;
        instance.lastEvent = "Blocked";
        instance.UpdateStatsText();
    }

    // Handles the RecordRollDodge logic.
    public static void RecordRollDodge()
    {
        Ensure();

        if (instance == null)
        {
            return;
        }

        instance.rollDodgeCount++;
        instance.lastEvent = "Dodged";
        instance.UpdateStatsText();
    }

    // Handles the IsPracticeScene logic.
    private static bool IsPracticeScene()
    {
        return SceneManager.GetActiveScene().name == "DockThing";
    }

    // Initializes component references and runtime-only setup.
    void Awake()
    {
        practiceDifficulty = PlayerPrefs.GetInt("PracticeDifficulty", 1);
        BuildUI();
        ResetPractice();
    }

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {
        if (!IsPracticeScene())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        ApplyPracticeDifficulty();
        sessionTime += Time.deltaTime;

        if (currentCombo > 0 && Time.time - lastHitTime > comboTimeout)
        {
            currentCombo = 0;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPractice();
        }

        UpdateStatsText();
    }

    // Handles the ResetPractice logic.
    public void ResetPractice()
    {
        totalDamage = 0;
        hitCount = 0;
        lastHit = 0;
        currentCombo = 0;
        maxCombo = 0;
        defendCount = 0;
        rollDodgeCount = 0;
        sessionTime = 0f;
        lastHitTime = -100f;
        lastEvent = "Reset";
        UpdateStatsText();
    }

    // Handles the AddHit logic.
    private void AddHit(int damage)
    {
        totalDamage += damage;
        hitCount++;
        lastHit = damage;

        if (Time.time - lastHitTime <= comboTimeout)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 1;
        }

        maxCombo = Mathf.Max(maxCombo, currentCombo);
        lastHitTime = Time.time;
        lastEvent = "Hit +" + damage;
        UpdateStatsText();
    }

    // Builds all runtime UI elements for this controller.
    private void BuildUI()
    {
        EnsureEventSystem();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject statsPanel = CreatePanel("PracticeStatsPanel", transform, new Vector2(26, -26), new Vector2(340, 280), new Vector2(0, 1));
        CreateText("StatsTitle", statsPanel.transform, "PRACTICE MODE", font, 24, new Vector2(18, -16), new Vector2(304, 34), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        statsText = CreateText("StatsText", statsPanel.transform, "", font, 19, new Vector2(18, -58), new Vector2(304, 170), TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        eventText = CreateText("EventText", statsPanel.transform, "", font, 20, new Vector2(18, -232), new Vector2(304, 30), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));

        GameObject keyPanel = CreatePanel("KeyGuidePanel", transform, new Vector2(26, 26), new Vector2(390, 382), new Vector2(0, 0));
        CreateText("KeysTitle", keyPanel.transform, "TRAINING NOTES", font, 23, new Vector2(18, -16), new Vector2(354, 34), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateKeyRow(keyPanel.transform, font, 62, "WASD", "Move");
        CreateKeyRow(keyPanel.transform, font, 100, "SHIFT", "Run");
        CreateKeyRow(keyPanel.transform, font, 138, "LMB", "Attack / Charge");
        CreateKeyRow(keyPanel.transform, font, 176, "RMB", "Jump");
        CreateKeyRow(keyPanel.transform, font, 214, "MMB", "Defend");
        CreateKeyRow(keyPanel.transform, font, 252, "SPACE", "Roll");
        CreateKeyRow(keyPanel.transform, font, 290, "R", "Reset stats");
        difficultyText = CreateText("DifficultyText", keyPanel.transform, "", font, 17, new Vector2(18, -326), new Vector2(354, 24), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateButton("EasyPracticeButton", keyPanel.transform, "EASY", font, new Vector2(18, -352), new Vector2(96, 28), new Vector2(0, 1), SetEasyPractice);
        CreateButton("NormalPracticeButton", keyPanel.transform, "NORMAL", font, new Vector2(132, -352), new Vector2(96, 28), new Vector2(0, 1), SetNormalPractice);
        CreateButton("HardPracticeButton", keyPanel.transform, "HARD", font, new Vector2(246, -352), new Vector2(96, 28), new Vector2(0, 1), SetHardPractice);

        CreateButton("PauseButton", transform, "PAUSE", font, new Vector2(-26, -26), new Vector2(130, 44), new Vector2(1, 1), TogglePause);

        pausePanel = CreatePanel("PracticePausePanel", transform, Vector2.zero, new Vector2(420, 260), new Vector2(0.5f, 0.5f));
        CreateText("PauseTitle", pausePanel.transform, "PRACTICE PAUSED", font, 26, new Vector2(28, -26), new Vector2(364, 42), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateButton("ContinueButton", pausePanel.transform, "CONTINUE PRACTICE", font, new Vector2(60, -104), new Vector2(300, 48), new Vector2(0, 1), ResumePractice);
        CreateButton("MainMenuButton", pausePanel.transform, "MAIN MENU", font, new Vector2(60, -170), new Vector2(300, 48), new Vector2(0, 1), BackToMainMenu);
        pausePanel.SetActive(false);
    }

    // Creates a styled UI panel.
    private GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.045f, 0.035f, 0.78f);

        AddBorder(panel.transform, size);
        return panel;
    }

    // Creates a styled UI text element.
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

    // Creates a styled UI button.
    private void CreateButton(string name, Transform parent, string value, Font font, Vector2 anchoredPosition, Vector2 size, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.095f, 0.07f, 0.94f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        CreateText("Text", buttonObject.transform, value, font, 18, Vector2.zero, size, TextAnchor.MiddleCenter, new Color(0.96f, 0.82f, 0.48f, 1f));
    }

    // Creates one row in the practice key guide.
    private void CreateKeyRow(Transform parent, Font font, float y, string key, string action)
    {
        GameObject keyBox = new GameObject("Key_" + key);
        keyBox.transform.SetParent(parent, false);

        RectTransform keyRect = keyBox.AddComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0, 1);
        keyRect.anchorMax = new Vector2(0, 1);
        keyRect.pivot = new Vector2(0, 1);
        keyRect.anchoredPosition = new Vector2(18, -y);
        keyRect.sizeDelta = new Vector2(92, 28);

        Image keyImage = keyBox.AddComponent<Image>();
        keyImage.color = new Color(0.12f, 0.095f, 0.07f, 0.92f);

        CreateText("Label", keyBox.transform, key, font, 16, new Vector2(0, 0), new Vector2(92, 28), TextAnchor.MiddleCenter, new Color(0.96f, 0.82f, 0.48f, 1f));
        CreateText("Action_" + key, parent, action, font, 18, new Vector2(126, -y), new Vector2(230, 28), TextAnchor.MiddleLeft, new Color(0.9f, 0.86f, 0.76f, 1f));
    }

    // Adds a rectangular border to a UI element.
    private void AddBorder(Transform parent, Vector2 size)
    {
        AddBorderLine(parent, "Top", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(size.x, 2));
        AddBorderLine(parent, "Bottom", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(size.x, 2));
        AddBorderLine(parent, "Left", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(2, size.y));
        AddBorderLine(parent, "Right", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(2, size.y));
    }

    // Creates one line of a UI border.
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

    // Refreshes practice statistics text.
    private void UpdateStatsText()
    {
        if (statsText == null || eventText == null)
        {
            return;
        }

        statsText.text =
            "Enemy Lost HP   " + totalDamage + "\n" +
            "Hits            " + hitCount + "\n" +
            "Last Hit        " + lastHit + "\n" +
            "Combo           " + currentCombo + "\n" +
            "Max Combo       " + maxCombo + "\n" +
            "Blocks          " + defendCount + "\n" +
            "Roll Dodges     " + rollDodgeCount + "\n" +
            "Time            " + FormatTime(sessionTime);

        eventText.text = lastEvent;
        if (difficultyText != null)
        {
            difficultyText.text = "ENEMY DIFFICULTY  " + GetPracticeDifficultyName();
        }
    }

    // Selects easy enemy tuning for practice mode.
    private void SetEasyPractice()
    {
        SetPracticeDifficulty(0);
    }

    // Selects normal enemy tuning for practice mode.
    private void SetNormalPractice()
    {
        SetPracticeDifficulty(1);
    }

    // Selects hard enemy tuning for practice mode.
    private void SetHardPractice()
    {
        SetPracticeDifficulty(2);
    }

    // Stores and applies the selected practice difficulty.
    private void SetPracticeDifficulty(int value)
    {
        practiceDifficulty = value;
        PlayerPrefs.SetInt("PracticeDifficulty", practiceDifficulty);
        PlayerPrefs.Save();
        difficultyApplied = false;
        ResetPractice();
    }

    // Applies practice difficulty values to the current enemy.
    private void ApplyPracticeDifficulty()
    {
        if (difficultyApplied)
        {
            return;
        }

        EnemyController enemy = FindObjectOfType<EnemyController>();
        if (enemy == null)
        {
            return;
        }

        if (practiceDifficulty == 0)
        {
            enemy.SetMainGameStage(EnemyDifficulty.Easy, EnemyVisualController.VisualType.Auto, 999, 1, 5.5f, 0.55f, 1, 2.5f, 6f, 1.8f, 8f);
        }
        else if (practiceDifficulty == 2)
        {
            enemy.SetMainGameStage(EnemyDifficulty.Hard, EnemyVisualController.VisualType.Auto, 999, 2, 2.4f, 0.25f, 2, 4.2f, 11f, 2.3f, 13f);
        }
        else
        {
            enemy.SetMainGameStage(EnemyDifficulty.Normal, EnemyVisualController.VisualType.Auto, 999, 1, 3.8f, 0.38f, 1, 3.4f, 8f, 2f, 10f);
        }

        difficultyApplied = true;
    }

    // Returns the current practice difficulty label.
    private string GetPracticeDifficultyName()
    {
        if (practiceDifficulty == 0)
        {
            return "EASY";
        }

        if (practiceDifficulty == 2)
        {
            return "HARD";
        }

        return "NORMAL";
    }

    // Toggles pause state.
    private void TogglePause()
    {
        if (pausePanel == null)
        {
            return;
        }

        if (isPaused)
        {
            ResumePractice();
            return;
        }

        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Resumes practice mode from pause.
    private void ResumePractice()
    {
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Restores time scale and loads the main menu.
    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    // Ensures runtime UI buttons can receive events.
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    // Formats seconds as a minute-second string.
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}
