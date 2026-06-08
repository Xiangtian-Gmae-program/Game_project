// Script note: Builds and updates main-game runtime UI for objectives, health, guard, prompts, pause, and results.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Class responsibility: MainGameUIController contains this script's gameplay behavior.
public class MainGameUIController : MonoBehaviour
{
    // Enum purpose: UI objective stages used by the main-game HUD.
    private enum MainGameObjectiveStage
    {
        Approach,
        Duel,
        Finish
    }

    private static MainGameUIController instance;

    private PlayerController player;
    private EnemyController enemy;
    private Text hpText;
    private Text guardText;
    private Text chapterText;
    private Text objectiveText;
    private Text enemyHpText;
    private Text enemyInfoText;
    private Text resultTitleText;
    private Text resultMessageText;
    private Text feedbackText;
    private Image hpFill;
    private Image guardFill;
    private Image enemyHpFill;
    private GameObject resultPanel;
    private GameObject storyPanel;
    private GameObject pausePanel;
    private GameObject feedbackPanel;
    private Button nextLevelButton;
    private bool flowEnded;
    private bool storyDismissed;
    private bool isPaused;
    private bool metricsReady;
    private MainGameObjectiveStage objectiveStage;
    private float flowEndTime;
    private float resultDelay = 1.2f;
    private float startTime;
    private float feedbackUntil;
    private string feedbackOverride;
    private int lastPlayerHP;
    private float lastGuard;
    private int damageTaken;
    private int guardSpent;

    // Checks whether the active scene is the main-game scene.
    public static bool IsMainGameScene()
    {
        return SceneManager.GetActiveScene().name == "maingame";
    }

    // Ensures the runtime controller singleton exists.
    public static void Ensure(PlayerController target)
    {
        if (!IsMainGameScene())
        {
            return;
        }

        if (instance == null)
        {
            GameObject go = new GameObject("MainGameUIController");
            instance = go.AddComponent<MainGameUIController>();
        }

        instance.player = target;
        instance.enemy = target.enemy;
        if (instance.enemy == null)
        {
            instance.enemy = FindObjectOfType<EnemyController>();
            target.enemy = instance.enemy;
        }

        instance.ApplyPlayerGrowth();
        MainGameEncounterController.Ensure(target, instance.enemy);
    }

    // Shows incoming enemy attack warning UI.
    public static void NotifyIncomingAttack(float duration)
    {
        NotifyCombat("INCOMING ATTACK  Block or roll", duration);
    }

    // Shows enemy hit and health feedback UI.
    public static void NotifyEnemyHit(int damage, int enemyHP)
    {
        NotifyCombat("CLEAN HIT  -" + damage + "  Enemy HP " + Mathf.Max(enemyHP, 0), 1.2f);
    }

    // Shows enemy stagger feedback UI.
    public static void NotifyEnemyStagger(float duration)
    {
        NotifyCombat("ENEMY STAGGERED  Press the advantage", duration);
    }

    // Shows enemy defend feedback UI.
    public static void NotifyEnemyDefend()
    {
        NotifyCombat("ENEMY GUARD  Change timing or charge", 1.1f);
    }

    // Shows enemy evade feedback UI.
    public static void NotifyEnemyEvade()
    {
        NotifyCombat("ENEMY EVADED  Track and reset", 1.0f);
    }

    // Shows player miss feedback UI.
    public static void NotifyAttackMiss()
    {
        NotifyCombat("ATTACK MISSED  Reset your stance", 1.0f);
    }

    // Shows player damage feedback UI.
    public static void NotifyPlayerHit(int damage)
    {
        NotifyCombat("HIT TAKEN  -" + damage + " HP", 1.3f);
    }

    // Shows successful guard feedback UI.
    public static void NotifyGuardBlock(int guardCost)
    {
        NotifyCombat("BLOCKED  Guard -" + guardCost, 1.1f);
    }

    // Shows guard-break feedback UI.
    public static void NotifyGuardBroken()
    {
        NotifyCombat("GUARD BROKEN  Create distance", 1.4f);
    }

    // Shows player roll evade feedback UI.
    public static void NotifyRollEvade()
    {
        NotifyCombat("EVADED  Reposition and punish", 1.1f);
    }

    // Writes a short timed combat message to the HUD.
    private static void NotifyCombat(string value, float duration)
    {
        if (!IsMainGameScene() || instance == null)
        {
            return;
        }

        instance.ShowFeedbackMessage(value, duration);
    }

    // Handles the NotifyChapterStart logic.
    public static void NotifyChapterStart()
    {
        NotifyCombat("CHAPTER 1  Locate the inner gate guard", 2.5f);
    }

    // Handles the NotifyBossPhase logic.
    public static void NotifyBossPhase()
    {
        NotifyCombat("BOSS PHASE  Guo Jing changes rhythm", 2.5f);
    }

    // Initializes component references and runtime-only setup.
    void Awake()
    {
        Time.timeScale = 1f;
        BuildUI();
        EnsureEventSystem();
        ShowStoryIntro();
    }

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {
        UpdateStoryInput();
        UpdatePauseInput();

        if (!IsMainGameScene() || player == null)
        {
            return;
        }

        UpdateUI();
        UpdateObjectiveState();
        UpdateFeedback();

        if (isPaused)
        {
            return;
        }

        UpdateMetrics();
        UpdateFlowState();
    }

    // Builds all runtime UI elements for this controller.
    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panel = CreatePanel("MainGameStatusPanel", transform, new Vector2(26, -26), new Vector2(390, 220), new Vector2(0, 1));
        CreateText("Title", panel.transform, "MAIN GAME", font, 24, new Vector2(18, -16), new Vector2(350, 34), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        hpText = CreateText("HPText", panel.transform, "HP", font, 18, new Vector2(18, -60), new Vector2(90, 26), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        guardText = CreateText("GuardText", panel.transform, "GUARD", font, 18, new Vector2(18, -108), new Vector2(90, 26), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        hpFill = CreateBar(panel.transform, new Vector2(112, -60), new Vector2(240, 22), new Color(0.58f, 0.12f, 0.08f, 1f));
        guardFill = CreateBar(panel.transform, new Vector2(112, -108), new Vector2(240, 22), new Color(0.72f, 0.5f, 0.18f, 1f));
        chapterText = CreateText("Chapter", panel.transform, "CHAPTER 1  INNER GATE", font, 17, new Vector2(18, -144), new Vector2(350, 24), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        objectiveText = CreateText("Objective", panel.transform, "OBJECTIVE  Locate the guard", font, 16, new Vector2(18, -172), new Vector2(350, 40), TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.78f, 1f));

        GameObject targetPanel = CreatePanel("MainGameTargetPanel", transform, new Vector2(-26, -26), new Vector2(390, 132), new Vector2(1, 1));
        CreateText("TargetTitle", targetPanel.transform, "TARGET", font, 24, new Vector2(18, -16), new Vector2(350, 34), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        enemyHpText = CreateText("EnemyHPText", targetPanel.transform, "HP", font, 18, new Vector2(18, -60), new Vector2(90, 26), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        enemyHpFill = CreateBar(targetPanel.transform, new Vector2(112, -60), new Vector2(240, 22), new Color(0.58f, 0.12f, 0.08f, 1f));
        enemyInfoText = CreateText("EnemyInfoText", targetPanel.transform, "DIFFICULTY  Normal", font, 17, new Vector2(18, -98), new Vector2(350, 24), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));

        GameObject controlPanel = CreatePanel("MainGameControlPanel", transform, new Vector2(-26, 26), new Vector2(390, 154), new Vector2(1, 0));
        CreateText("ControlTitle", controlPanel.transform, "CONTROLS", font, 22, new Vector2(18, -14), new Vector2(350, 30), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("ControlBody", controlPanel.transform, "LMB Attack / Charge\nRMB Jump    MMB Defend\nSpace Roll    Esc Pause", font, 17, new Vector2(18, -50), new Vector2(350, 86), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));

        feedbackPanel = CreatePanel("MainGameFeedbackPanel", transform, new Vector2(0, -150), new Vector2(420, 54), new Vector2(0.5f, 1));
        feedbackText = CreateText("FeedbackText", feedbackPanel.transform, "LOW HP", font, 22, new Vector2(16, -10), new Vector2(388, 34), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        feedbackPanel.SetActive(false);

        resultPanel = CreatePanel("MainGameResultPanel", transform, new Vector2(0, 0), new Vector2(620, 342), new Vector2(0.5f, 0.5f));
        resultTitleText = CreateText("ResultTitle", resultPanel.transform, "VICTORY", font, 34, new Vector2(28, -28), new Vector2(564, 46), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        resultMessageText = CreateText("ResultMessage", resultPanel.transform, "The enemy has been defeated.", font, 20, new Vector2(38, -88), new Vector2(544, 122), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.78f, 1f));
        nextLevelButton = CreateButton("NextLevelButton", resultPanel.transform, "NEXT", font, new Vector2(55, -244), new Vector2(150, 50), ContinueNextLevel);
        CreateButton("RestartButton", resultPanel.transform, "RESTART", font, new Vector2(235, -244), new Vector2(150, 50), RestartMainGame);
        CreateButton("MainMenuButton", resultPanel.transform, "MAIN MENU", font, new Vector2(415, -244), new Vector2(150, 50), BackMainMenu);
        nextLevelButton.gameObject.SetActive(false);
        resultPanel.SetActive(false);

        storyPanel = CreatePanel("MainGameStoryPanel", transform, new Vector2(0, 0), new Vector2(700, 390), new Vector2(0.5f, 0.5f));
        CreateText("StoryTitle", storyPanel.transform, "HIJIN", font, 38, new Vector2(32, -28), new Vector2(636, 52), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("StoryBody", storyPanel.transform, MainGameProgress.GetLevelBrief(), font, 21, new Vector2(54, -96), new Vector2(592, 112), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.78f, 1f));
        CreateText("StoryObjective", storyPanel.transform, BuildStoryObjectiveText(), font, 20, new Vector2(54, -210), new Vector2(592, 34), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("StoryRecord", storyPanel.transform, BuildRecordText(), font, 18, new Vector2(54, -250), new Vector2(592, 30), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.78f, 1f));
        CreateButton("StoryContinueButton", storyPanel.transform, "BEGIN", font, new Vector2(265, -316), new Vector2(170, 50), ContinueMainGame);
        storyPanel.SetActive(false);

        pausePanel = CreatePanel("MainGamePausePanel", transform, new Vector2(0, 0), new Vector2(500, 330), new Vector2(0.5f, 0.5f));
        CreateText("PauseTitle", pausePanel.transform, "PAUSED", font, 34, new Vector2(28, -28), new Vector2(444, 46), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("PauseMessage", pausePanel.transform, "Take a breath, then return to the duel.", font, 20, new Vector2(38, -88), new Vector2(424, 46), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.78f, 1f));
        CreateButton("ContinueButton", pausePanel.transform, "CONTINUE", font, new Vector2(165, -150), new Vector2(170, 48), ResumeMainGame);
        CreateButton("PauseRestartButton", pausePanel.transform, "RESTART", font, new Vector2(82, -224), new Vector2(150, 48), RestartMainGame);
        CreateButton("PauseMainMenuButton", pausePanel.transform, "MAIN MENU", font, new Vector2(268, -224), new Vector2(150, 48), BackMainMenu);
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

        Text text = CreateText("Text", buttonObject.transform, value, font, 18, new Vector2(0, 0), size, TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        text.raycastTarget = false;
        AddBorder(buttonObject.transform, size);
        return button;
    }

    // Handles the CreateBar logic.
    private Image CreateBar(Transform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
    {
        GameObject background = new GameObject("BarBackground");
        background.transform.SetParent(parent, false);

        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 1);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.pivot = new Vector2(0, 1);
        backgroundRect.anchoredPosition = anchoredPosition;
        backgroundRect.sizeDelta = size;

        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.12f, 0.095f, 0.07f, 0.92f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(background.transform, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        return fillImage;
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

    // Handles the UpdateUI logic.
    private void UpdateUI()
    {
        FindEnemy();

        if (hpFill != null)
        {
            hpFill.fillAmount = Mathf.Clamp01((float)player.HP / 20f);
            hpFill.fillAmount = Mathf.Clamp01((float)player.HP / MainGameProgress.GetMaxHP());
        }

        if (guardFill != null)
        {
            guardFill.fillAmount = Mathf.Clamp01(player.currentGuard / player.maxGuard);
        }

        if (hpText != null)
        {
            hpText.text = "HP  " + Mathf.Max(player.HP, 0);
        }

        if (guardText != null)
        {
            guardText.text = "GUARD  " + Mathf.RoundToInt(player.currentGuard);
        }

        if (enemyHpFill != null)
        {
            if (enemy != null)
            {
                enemyHpFill.fillAmount = Mathf.Clamp01((float)enemy.HP / enemy.maxHP);
            }
            else
            {
                enemyHpFill.fillAmount = 0f;
            }
        }

        if (enemyHpText != null)
        {
            if (enemy != null)
            {
                enemyHpText.text = "HP  " + Mathf.Max(enemy.HP, 0) + " / " + enemy.maxHP;
            }
            else
            {
                enemyHpText.text = "HP  --";
            }
        }

        if (enemyInfoText != null)
        {
            if (enemy != null)
            {
                float dis = Vector3.Distance(player.transform.position, enemy.transform.position);
                string stateText = enemy.isDead ? "DEFEATED" : enemy.difficulty.ToString().ToUpper();
                enemyInfoText.text = MainGameEncounterController.GetTargetName() + "  " + stateText + "  " + Mathf.RoundToInt(dis) + "m";
            }
            else
            {
                enemyInfoText.text = MainGameEncounterController.IsWaitingNext() ? "NEXT TARGET APPROACHING" : "INFO  NO TARGET";
            }
        }

        if (chapterText != null)
        {
            chapterText.text = MainGameEncounterController.GetChapterText();
        }

        if (objectiveText != null)
        {
            objectiveText.text = BuildObjectiveText();
        }
    }

    // Handles the UpdateObjectiveState logic.
    private void UpdateObjectiveState()
    {
        if (player == null || flowEnded || isPaused || storyPanel != null && storyPanel.activeSelf)
        {
            return;
        }

        FindEnemy();
        if (enemy == null)
        {
            return;
        }

        MainGameObjectiveStage nextStage = objectiveStage;
        float dis = Vector3.Distance(player.transform.position, enemy.transform.position);
        if (enemy.HP <= enemy.maxHP * 0.35f)
        {
            nextStage = MainGameObjectiveStage.Finish;
        }
        else if (enemy.HP < enemy.maxHP || dis <= enemy.detectRange)
        {
            nextStage = MainGameObjectiveStage.Duel;
        }

        if (nextStage != objectiveStage)
        {
            objectiveStage = nextStage;
            ShowFeedbackMessage("OBJECTIVE UPDATED  " + GetObjectiveTitle(), 2.5f);
        }
    }

    // Handles the UpdateFeedback logic.
    private void UpdateFeedback()
    {
        if (feedbackPanel == null || feedbackText == null || player == null || flowEnded || isPaused)
        {
            return;
        }

        if (!string.IsNullOrEmpty(feedbackOverride) && Time.unscaledTime < feedbackUntil)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = feedbackOverride;
        }
        else if (player.HP <= 5)
        {
            feedbackOverride = null;
            feedbackPanel.SetActive(true);
            feedbackText.text = "LOW HP  Defend or create distance";
        }
        else if (player.currentGuard <= player.maxGuard * 0.25f)
        {
            feedbackOverride = null;
            feedbackPanel.SetActive(true);
            feedbackText.text = "GUARD LOW  Release defense to recover";
        }
        else
        {
            feedbackOverride = null;
            feedbackPanel.SetActive(false);
        }
    }

    // Handles the UpdateFlowState logic.
    private void UpdateFlowState()
    {
        FindEnemy();
        if (!flowEnded)
        {
            if (player.isDead)
            {
                flowEnded = true;
                flowEndTime = Time.unscaledTime;
                SetResult("DEFEAT", BuildResultMessage(false), false);
            }
            else if (MainGameEncounterController.IsComplete() || (enemy != null && enemy.isDead && MainGameEncounterController.GetCurrentEnemy() == null))
            {
                flowEnded = true;
                flowEndTime = Time.unscaledTime;
                bool hasNextLevel = MainGameProgress.HasNextLevel();
                SetResult(hasNextLevel ? "VICTORY" : "ENDING", BuildResultMessage(true), hasNextLevel);
            }
        }

        if (flowEnded && resultPanel != null && !resultPanel.activeSelf && Time.unscaledTime - flowEndTime >= resultDelay)
        {
            resultPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }
    }

    // Handles the UpdateMetrics logic.
    private void UpdateMetrics()
    {
        if (player == null || flowEnded)
        {
            return;
        }

        if (!metricsReady)
        {
            metricsReady = true;
            startTime = Time.unscaledTime;
            lastPlayerHP = player.HP;
            lastGuard = player.currentGuard;
            return;
        }

        if (player.HP < lastPlayerHP)
        {
            damageTaken += lastPlayerHP - player.HP;
        }

        if (player.currentGuard < lastGuard)
        {
            guardSpent += Mathf.RoundToInt(lastGuard - player.currentGuard);
        }

        lastPlayerHP = player.HP;
        lastGuard = player.currentGuard;
    }

    // Handles the BuildResultMessage logic.
    private string BuildResultMessage(bool victory)
    {
        int clearTime = Mathf.RoundToInt(Time.unscaledTime - startTime);
        int score = victory ? 1000 : 200;
        score -= clearTime * 5;
        score -= damageTaken * 80;
        score -= guardSpent;
        if (score < 0)
        {
            score = 0;
        }

        int level = MainGameProgress.CurrentLevel;
        int bestScore = PlayerPrefs.GetInt(MainGameProgress.GetBestScoreKey(level), 0);
        int clearCount = PlayerPrefs.GetInt("MainGameClearCount", 0);
        if (victory)
        {
            MainGameProgress.AdvanceAfterVictory(score);
            bestScore = Mathf.Max(bestScore, score);
            clearCount = PlayerPrefs.GetInt("MainGameClearCount", clearCount + 1);
        }

        string result = victory ? BuildVictoryText(level) : "The inner gate remains guarded. Recover your stance and try again.";
        return result + "\nTIME  " + clearTime + "s    DAMAGE  " + damageTaken + "    GUARD  " + guardSpent + "\nSCORE  " + score + "    BEST  " + bestScore + "    CLEARS  " + clearCount;
    }

    // Handles the BuildRecordText logic.
    private string BuildRecordText()
    {
        return "DIFFICULTY  " + GetDifficultyName() + "    LEVEL  " + MainGameProgress.CurrentLevel + "/" + MainGameProgress.MaxLevel + "    UNLOCKED  " + MainGameProgress.UnlockedLevel;
    }

    // Handles the BuildStoryObjectiveText logic.
    private string BuildStoryObjectiveText()
    {
        return MainGameProgress.GetLevelTitle() + "  " + GetDifficultyName() + " difficulty.";
    }

    // Handles the BuildObjectiveText logic.
    private string BuildObjectiveText()
    {
        if (flowEnded)
        {
            return "OBJECTIVE  " + (player != null && player.isDead ? "Regain your stance" : "Gate secured");
        }

        if (MainGameEncounterController.IsWaitingNext())
        {
            return "OBJECTIVE  Hold position\nNext opponent approaching";
        }

        if (objectiveStage == MainGameObjectiveStage.Finish)
        {
            return "OBJECTIVE  Finish " + MainGameEncounterController.GetTargetName() + "\nKeep enough guard to survive";
        }

        if (objectiveStage == MainGameObjectiveStage.Duel)
        {
            return "OBJECTIVE  Defeat " + MainGameEncounterController.GetTargetName() + "\nBlock, roll, then punish openings";
        }

        return "OBJECTIVE  Locate " + MainGameEncounterController.GetTargetName() + "\nApproach with your guard ready";
    }

    // Handles the GetObjectiveTitle logic.
    private string GetObjectiveTitle()
    {
        if (objectiveStage == MainGameObjectiveStage.Finish)
        {
            return "FINISH THE DUEL";
        }

        if (objectiveStage == MainGameObjectiveStage.Duel)
        {
            return "ENGAGE THE GUARD";
        }

        return "LOCATE THE GUARD";
    }

    // Handles the GetDifficultyName logic.
    private string GetDifficultyName()
    {
        int difficulty = PlayerPrefs.GetInt("MainGameDifficulty", 1);
        if (difficulty == 0)
        {
            return "EASY";
        }

        if (difficulty == 2)
        {
            return "HARD";
        }

        return "NORMAL";
    }

    // Handles the ShowFeedbackMessage logic.
    private void ShowFeedbackMessage(string value, float duration)
    {
        feedbackOverride = value;
        feedbackUntil = Time.unscaledTime + duration;
    }

    // Handles the BuildVictoryText logic.
    private string BuildVictoryText(int level)
    {
        if (level == 4)
        {
            return "Guo Jing withdraws. The final gate is open, and HIJIN is complete.";
        }

        if (level == 3)
        {
            return "The elite guard falls. Only Guo Jing remains beyond the final gate.";
        }

        if (level == 2)
        {
            return "The patrol yard is secure. The elite hall opens ahead.";
        }

        return "The inner gate guard is defeated. The patrol yard is unlocked.";
    }

    // Handles the SetResult logic.
    private void SetResult(string title, string message, bool showNext)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = title;
        }

        if (resultMessageText != null)
        {
            resultMessageText.text = message;
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(showNext);
        }
    }

    // Handles the FindEnemy logic.
    private void FindEnemy()
    {
        EnemyController encounterEnemy = MainGameEncounterController.GetCurrentEnemy();
        if (encounterEnemy != null)
        {
            enemy = encounterEnemy;
            if (player != null)
            {
                player.enemy = enemy;
            }
            return;
        }

        if (enemy == null && player != null)
        {
            enemy = player.enemy;
        }

        if (enemy == null)
        {
            enemy = FindObjectOfType<EnemyController>();
        }
    }

    // Handles the ApplyPlayerGrowth logic.
    private void ApplyPlayerGrowth()
    {
        if (player == null)
        {
            return;
        }

        player.maxGuard = MainGameProgress.GetMaxGuard();
        player.currentGuard = player.maxGuard;
        player.HP = MainGameProgress.GetMaxHP();
    }

    // Handles the ContinueNextLevel logic.
    private void ContinueNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("maingame");
    }

    // Handles the RestartMainGame logic.
    private void RestartMainGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("maingame");
    }

    // Handles the BackMainMenu logic.
    private void BackMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    // Handles the ShowStoryIntro logic.
    private void ShowStoryIntro()
    {
        if (storyPanel == null || storyDismissed)
        {
            return;
        }

        storyPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    // Handles the UpdateStoryInput logic.
    private void UpdateStoryInput()
    {
        if (storyPanel == null || !storyPanel.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ContinueMainGame();
        }
    }

    // Handles the ContinueMainGame logic.
    private void ContinueMainGame()
    {
        storyDismissed = true;
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        ShowFeedbackMessage("CHAPTER 1  Locate the inner gate guard", 2.5f);
    }

    // Handles the UpdatePauseInput logic.
    private void UpdatePauseInput()
    {
        if (flowEnded || storyPanel != null && storyPanel.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeMainGame();
            }
            else
            {
                PauseMainGame();
            }
        }
    }

    // Handles the PauseMainGame logic.
    private void PauseMainGame()
    {
        isPaused = true;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    // Handles the ResumeMainGame logic.
    private void ResumeMainGame()
    {
        isPaused = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    // Ensures runtime UI buttons can receive events.
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
