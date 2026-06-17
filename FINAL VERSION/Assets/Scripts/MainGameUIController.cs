// Purpose: Builds and updates the main-game HUD, story panels, pause menu, results, and combat feedback.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainGameUIController : MonoBehaviour
{

    private struct StoryPage
    {
        public string imagePath;
        public string speaker;
        public string body;

        public StoryPage(string newImagePath, string newSpeaker, string newBody)
        {
            imagePath = newImagePath;
            speaker = newSpeaker;
            body = newBody;
        }
    }

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
    private Text storySpeakerText;
    private Text storyBodyText;
    private Text storyHintText;
    private Image hpFill;
    private Image guardFill;
    private Image enemyHpFill;
    private Image storyImage;
    private GameObject resultPanel;
    private GameObject storyPanel;
    private GameObject pausePanel;
    private GameObject feedbackPanel;
    private GameObject controlPanel;
    private Button nextLevelButton;
    private StoryPage[] storyPages;
    private int storyPageIndex;
    private bool flowEnded;
    private bool storyDismissed;
    private bool storyHoldingResult;
    private bool resultReady;
    private bool isPaused;
    private bool metricsReady;
    private bool controlPanelTimerStarted;
    private bool controlPanelHidden;
    private MainGameObjectiveStage objectiveStage;
    private float flowEndTime;
    private float resultDelay = 1.2f;
    private float startTime;
    private float feedbackUntil;
    private float controlPanelHideTime;
    private string feedbackOverride;
    private int lastPlayerHP;
    private float lastGuard;
    private int damageTaken;
    private int guardSpent;
    private const float ControlPanelVisibleDuration = 15f;

    public static bool IsMainGameScene()
    {
        return SceneManager.GetActiveScene().name == "maingame";
    }

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

    public static void NotifyIncomingAttack(float duration)
    {
        NotifyCombat("INCOMING ATTACK  Block or roll", duration);
    }

    public static void NotifyEnemyHit(int damage, int enemyHP)
    {
        NotifyCombat("CLEAN HIT  -" + damage + "  Enemy HP " + Mathf.Max(enemyHP, 0), 1.2f);
    }

    public static void NotifyEnemyStagger(float duration)
    {
        NotifyCombat("ENEMY STAGGERED  Press the advantage", duration);
    }

    public static void NotifyEnemyDefend()
    {
        NotifyCombat("ENEMY GUARD  Change timing or charge", 1.1f);
    }

    public static void NotifyEnemyEvade()
    {
        NotifyCombat("ENEMY EVADED  Track and reset", 1.0f);
    }

    public static void NotifyAttackMiss()
    {
        NotifyCombat("ATTACK MISSED  Reset your stance", 1.0f);
    }

    public static void NotifyPlayerHit(int damage)
    {
        NotifyCombat("HIT TAKEN  -" + damage + " HP", 1.3f);
    }

    public static void NotifyGuardBlock(int guardCost)
    {
        NotifyCombat("BLOCKED  Guard -" + guardCost, 1.1f);
    }

    public static void NotifyGuardBroken()
    {
        NotifyCombat("GUARD BROKEN  Create distance", 1.4f);
    }

    public static void NotifyRollEvade()
    {
        NotifyCombat("EVADED  Reposition and punish", 1.1f);
    }

    private static void NotifyCombat(string value, float duration)
    {
        if (!IsMainGameScene() || instance == null)
        {
            return;
        }

        instance.ShowFeedbackMessage(value, duration);
    }

    public static void NotifyChapterStart()
    {
        NotifyCombat("CHAPTER 1  Locate the inner gate guard", 2.5f);
    }

    public static void NotifyBossPhase()
    {
        NotifyCombat("BOSS PHASE  Guo Jing changes rhythm", 2.5f);
    }

    void Awake()
    {
        Time.timeScale = 1f;
        BuildUI();
        EnsureEventSystem();
        ShowStoryIntro();
    }

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
        if (storyDismissed)
        {
            StartControlPanelTimer();
            UpdateControlPanelTimer();
        }

        if (isPaused)
        {
            return;
        }

        UpdateMetrics();
        UpdateFlowState();
    }

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

        controlPanel = CreatePanel("MainGameControlPanel", transform, new Vector2(-26, 26), new Vector2(390, 154), new Vector2(1, 0));
        CreateText("ControlTitle", controlPanel.transform, "CONTROLS", font, 22, new Vector2(18, -14), new Vector2(350, 30), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("ControlBody", controlPanel.transform, "LMB Attack / Charge\nRMB Jump    E Defend\nSpace Roll    Esc Pause", font, 17, new Vector2(18, -50), new Vector2(350, 86), TextAnchor.MiddleLeft, new Color(0.92f, 0.88f, 0.78f, 1f));

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

        storyPanel = CreateStoryPanel(font);
        storyPanel.SetActive(false);

        pausePanel = CreatePanel("MainGamePausePanel", transform, new Vector2(0, 0), new Vector2(500, 330), new Vector2(0.5f, 0.5f));
        CreateText("PauseTitle", pausePanel.transform, "PAUSED", font, 34, new Vector2(28, -28), new Vector2(444, 46), TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        CreateText("PauseMessage", pausePanel.transform, "Take a breath, then return to the duel.", font, 20, new Vector2(38, -88), new Vector2(424, 46), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.78f, 1f));
        CreateButton("ContinueButton", pausePanel.transform, "CONTINUE", font, new Vector2(165, -150), new Vector2(170, 48), ResumeMainGame);
        CreateButton("PauseRestartButton", pausePanel.transform, "RESTART", font, new Vector2(82, -224), new Vector2(150, 48), RestartMainGame);
        CreateButton("PauseMainMenuButton", pausePanel.transform, "MAIN MENU", font, new Vector2(268, -224), new Vector2(150, 48), BackMainMenu);
        pausePanel.SetActive(false);
    }

    private void StartControlPanelTimer()
    {
        if (controlPanel == null || controlPanelTimerStarted || controlPanelHidden)
        {
            return;
        }

        controlPanel.SetActive(true);
        controlPanelTimerStarted = true;
        controlPanelHideTime = Time.time + ControlPanelVisibleDuration;
    }

    private void UpdateControlPanelTimer()
    {
        if (controlPanel == null || !controlPanelTimerStarted || controlPanelHidden)
        {
            return;
        }

        if (Time.time >= controlPanelHideTime)
        {
            controlPanel.SetActive(false);
            controlPanelHidden = true;
        }
    }

    private GameObject CreateStoryPanel(Font font)
    {
        GameObject panel = new GameObject("MainGameStoryPanel");
        panel.transform.SetParent(transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image background = panel.AddComponent<Image>();
        background.color = Color.black;

        GameObject imageObject = new GameObject("StoryImage");
        imageObject.transform.SetParent(panel.transform, false);

        RectTransform imageRect = imageObject.AddComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = Vector2.zero;

        storyImage = imageObject.AddComponent<Image>();
        storyImage.color = Color.white;
        storyImage.preserveAspect = true;

        GameObject shade = new GameObject("StoryShade");
        shade.transform.SetParent(panel.transform, false);

        RectTransform shadeRect = shade.AddComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.pivot = new Vector2(0.5f, 0.5f);
        shadeRect.anchoredPosition = Vector2.zero;
        shadeRect.sizeDelta = Vector2.zero;

        Image shadeImage = shade.AddComponent<Image>();
        shadeImage.color = new Color(0f, 0f, 0f, 0.16f);

        GameObject dialoguePanel = CreatePanel("StoryDialoguePanel", panel.transform, new Vector2(0, 44), new Vector2(1420, 246), new Vector2(0.5f, 0));
        storySpeakerText = CreateText("StorySpeaker", dialoguePanel.transform, "NARRATOR", font, 24, new Vector2(34, -20), new Vector2(1348, 34), TextAnchor.MiddleLeft, new Color(0.94f, 0.76f, 0.42f, 1f));
        storyBodyText = CreateText("StoryBody", dialoguePanel.transform, "", font, 25, new Vector2(34, -70), new Vector2(1348, 108), TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.78f, 1f));
        storyHintText = CreateText("StoryHint", dialoguePanel.transform, "Space / Enter / Click", font, 17, new Vector2(34, -184), new Vector2(930, 30), TextAnchor.MiddleLeft, new Color(0.72f, 0.62f, 0.48f, 1f));
        CreateButton("StoryContinueButton", dialoguePanel.transform, "NEXT", font, new Vector2(1196, -174), new Vector2(170, 48), ContinueMainGame);

        return panel;
    }

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

        Text text = CreateText("Text", buttonObject.transform, value, font, 18, new Vector2(0, 0), size, TextAnchor.MiddleCenter, new Color(0.94f, 0.76f, 0.42f, 1f));
        text.raycastTarget = false;
        AddBorder(buttonObject.transform, size);
        return button;
    }

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

    private void AddBorder(Transform parent, Vector2 size)
    {
        AddBorderLine(parent, "Top", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(size.x, 2));
        AddBorderLine(parent, "Bottom", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(size.x, 2));
        AddBorderLine(parent, "Left", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(2, size.y));
        AddBorderLine(parent, "Right", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(2, size.y));
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

    private void UpdateUI()
    {
        FindEnemy();

        if (hpFill != null)
        {
            SetBarAmount(hpFill, (float)player.HP / MainGameProgress.GetMaxHP());
        }

        if (guardFill != null)
        {
            SetBarAmount(guardFill, player.currentGuard / player.maxGuard);
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
                SetBarAmount(enemyHpFill, (float)enemy.HP / enemy.maxHP);
            }
            else
            {
                SetBarAmount(enemyHpFill, 0f);
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

    private void SetBarAmount(Image barFill, float amount)
    {
        if (barFill == null)
        {
            return;
        }

        amount = Mathf.Clamp01(amount);
        barFill.fillAmount = amount;

        RectTransform rect = barFill.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(amount, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

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

    private void UpdateFlowState()
    {
        FindEnemy();
        if (!flowEnded)
        {
            if (player.isDead)
            {
                int resultLevel = MainGameProgress.CurrentLevel;
                int resultDifficulty = GetDifficultyValue();
                flowEnded = true;
                resultReady = false;
                SetResult("DEFEAT", BuildResultMessage(false), false);
                StartStory(BuildEndStoryPages(false, resultLevel, resultDifficulty), true);
            }
            else if (MainGameEncounterController.IsComplete() || (enemy != null && enemy.isDead && MainGameEncounterController.GetCurrentEnemy() == null))
            {
                int resultLevel = MainGameProgress.CurrentLevel;
                int resultDifficulty = GetDifficultyValue();
                flowEnded = true;
                resultReady = false;
                bool hasNextLevel = MainGameProgress.HasNextLevel();
                SetResult(hasNextLevel ? "VICTORY" : "ENDING", BuildResultMessage(true), hasNextLevel);
                StartStory(BuildEndStoryPages(true, resultLevel, resultDifficulty), true);
            }
        }

        if (flowEnded && resultReady && resultPanel != null && !resultPanel.activeSelf && Time.unscaledTime - flowEndTime >= resultDelay)
        {
            ShowResultPanel();
        }
    }

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

    private string BuildRecordText()
    {
        return "DIFFICULTY  " + GetDifficultyName() + "    LEVEL  " + MainGameProgress.CurrentLevel + "/" + MainGameProgress.MaxLevel + "    UNLOCKED  " + MainGameProgress.UnlockedLevel;
    }

    private string BuildStoryObjectiveText()
    {
        return MainGameProgress.GetLevelTitle() + "  " + GetDifficultyName() + " difficulty.";
    }

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

    private int GetDifficultyValue()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt("MainGameDifficulty", 1), 0, 2);
    }

    private void ShowFeedbackMessage(string value, float duration)
    {
        feedbackOverride = value;
        feedbackUntil = Time.unscaledTime + duration;
    }

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

    private StoryPage[] BuildStartStoryPages(int level, int difficulty)
    {
        if (level == 2)
        {
            return BuildLevelTwoStartPages(difficulty);
        }

        if (level == 3)
        {
            return BuildLevelThreeStartPages(difficulty);
        }

        if (level == 4)
        {
            return BuildLevelFourStartPages(difficulty);
        }

        return BuildLevelOneStartPages(difficulty);
    }

    private StoryPage[] BuildEndStoryPages(bool victory, int level, int difficulty)
    {
        if (!victory)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_12_bad_ending_loop", "NARRATOR", BuildDefeatNarration(level, difficulty))
            };
        }

        if (level == 4)
        {
            return BuildFinalVictoryPages(difficulty);
        }

        if (level == 3)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_06_disciple_remnant_after_duel", "FELLOW SHADE", BuildLevelThreeVictoryNarration(difficulty))
            };
        }

        if (level == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_05_patrol_lantern_aftermath", "NIGHT PATROL", BuildLevelTwoVictoryNarration(difficulty))
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_04_memory_reflection", "NARRATOR", BuildLevelOneVictoryNarration(difficulty))
        };
    }

    private StoryPage[] BuildLevelOneStartPages(int difficulty)
    {
        if (difficulty == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_07_moonless_dojo_entrance", "NARRATOR", "The moon is gone. The dojo waits like an old wound, and you have stood before this gate too many times without entering."),
                new StoryPage("StoryImages/story_02_awakening_katana", "NARRATOR", "The old blade lies beside your hand. It reflects no moon tonight, only the shape of a man who still refuses to turn back."),
                new StoryPage("StoryImages/story_03_gate_guard_remnant", "GATE SHADE", "Will you run from this gate again, or will your sword finally speak?")
            };
        }

        if (difficulty == 1)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_01_crescent_dojo_entrance", "NARRATOR", "A broken dojo sleeps beneath the crescent moon. Rain, timber, and dying lanterns wait for someone who should have returned long ago."),
                new StoryPage("StoryImages/story_02_awakening_katana", "NARRATOR", "You wake with an unsheathed katana at your side. It feels less like a weapon than a memory that refused to rust."),
                new StoryPage("StoryImages/story_03_gate_guard_remnant", "GATE SHADE", "Young master. Those within the gate have waited for you for years.")
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_01_crescent_dojo_entrance", "NARRATOR", "You wake before a ruined dojo after the rain. The crescent moon is cold, and steel whispers behind the gate."),
            new StoryPage("StoryImages/story_02_awakening_katana", "NARRATOR", "An old katana rests in the mud. You do not remember it, but your hand knows it must be raised."),
            new StoryPage("StoryImages/story_03_gate_guard_remnant", "NARRATOR", "The samurai at the gate says nothing. He draws his blade, as if this duel was promised before you woke.")
        };
    }

    private StoryPage[] BuildLevelTwoStartPages(int difficulty)
    {
        if (difficulty == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NARRATOR", "The patrol lantern still moves through the bamboo. It is not searching for an enemy. It is asking whether you still beg to be forgiven."),
                new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NIGHT PATROL", "If you still need the dead to understand you, you will never reach the final platform. Draw.")
            };
        }

        if (difficulty == 1)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NARRATOR", "The lantern in the bamboo recalls a rear gate. On that night, some escaped through it. Others held the line until dawn never came."),
                new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NIGHT PATROL", "You opened the rear gate. Lives were spared, and so the survivors named you traitor.")
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NARRATOR", "A patrol bell drifts from the bamboo behind the dojo. Lantern light sways in the mist like an eye that never closed."),
            new StoryPage("StoryImages/story_09_bamboo_patrol_path", "NARRATOR", "The night guard has found you. His blade carries no anger, only duty.")
        };
    }

    private StoryPage[] BuildLevelThreeStartPages(int difficulty)
    {
        if (difficulty == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_10_full_moon_training_standoff", "NARRATOR", "Even in the moonless trial, the training platform remembers the full moon. This is where your final bout with your brother-in-arms began."),
                new StoryPage("StoryImages/story_10_full_moon_training_standoff", "FELLOW SHADE", "I did not wait here to condemn you. I waited to see whether your sword still turns away.")
            };
        }

        if (difficulty == 1)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_10_full_moon_training_standoff", "NARRATOR", "A familiar shadow stands on the training platform. You cannot name him, but your body remembers his opening stance."),
                new StoryPage("StoryImages/story_10_full_moon_training_standoff", "FELLOW SHADE", "That night, we both chose. You opened the way out. I stayed. Now let our blades finish what our words could not.")
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_10_full_moon_training_standoff", "NARRATOR", "The samurai on the stone platform is quieter than the shades before him. He is not a guard. He is an unfinished duel."),
            new StoryPage("StoryImages/story_10_full_moon_training_standoff", "NARRATOR", "Moonlight lies between two blades. A true duel does not wait for readiness.")
        };
    }

    private StoryPage[] BuildLevelFourStartPages(int difficulty)
    {
        if (difficulty == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_07_moonless_dojo_entrance", "NARRATOR", "In the last night, the moon vanishes. The dojo no longer judges you. It only brings you before yourself."),
                new StoryPage("StoryImages/story_08_shadow_self_final_duel", "MOONLESS SHADE", "You thought you were challenging another warrior? No. You are finally returning to the night you left behind.")
            };
        }

        if (difficulty == 1)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_08_shadow_self_final_duel", "NARRATOR", "A warrior with your sword stands on the keep platform. He is less an enemy than the half of your soul left in the fire."),
                new StoryPage("StoryImages/story_08_shadow_self_final_duel", "MOONLESS SHADE", "You saved them, and abandoned yourself to the night. Come take back what remains.")
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_08_shadow_self_final_duel", "NARRATOR", "The wind is cold on the final platform. The last samurai waits there like the dojo's final breath."),
            new StoryPage("StoryImages/story_08_shadow_self_final_duel", "MOONLESS SHADE", "If you want to leave, step over my blade.")
        };
    }

    private string BuildLevelOneVictoryNarration(int difficulty)
    {
        if (difficulty == 2)
        {
            return "The gate shade kneels and lowers his sword. The way opens, yet you know the warrior who truly barred the path was never him.";
        }

        if (difficulty == 1)
        {
            return "The puddle shows an old black haori. You are not an intruder. You are the man who once escaped through this gate.";
        }

        return "The shade dissolves, and the night behind the gate grows deeper. In the water, a black haori flickers and is gone.";
    }

    private string BuildLevelTwoVictoryNarration(int difficulty)
    {
        if (difficulty == 2)
        {
            return "The patrol lantern remains in the road. The guard bows his head: if no one understands why you drew, then carry the reason yourself.";
        }

        if (difficulty == 1)
        {
            return "He sets the lantern down. You remember the rear gate, the children who fled through it, and the names left inside the flames.";
        }

        return "The patrol falls silent. Only the lantern remains in the bamboo, lighting the next stretch of night.";
    }

    private string BuildLevelThreeVictoryNarration(int difficulty)
    {
        if (difficulty == 2)
        {
            return "The fellow shade smiles. He did not forgive you, because he never truly hated you. He only waited for you to admit you survived.";
        }

        if (difficulty == 1)
        {
            return "A broken sword cord falls into your hand. That night had no clean answer, only the price of two young warriors choosing different paths.";
        }

        return "The shade fades into silver mist. His blade does not fall. It leaves the unfinished duel in your hands.";
    }

    private StoryPage[] BuildFinalVictoryPages(int difficulty)
    {
        if (difficulty == 2)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_08_shadow_self_final_duel", "MOONLESS SHADE", "You did not cut me down."),
                new StoryPage("StoryImages/story_11_dawn_true_ending", "NARRATOR", "The old katana stands in the wet timber. Dawn crosses the ruin. The dojo is not restored, and the dead do not return. But the night is over.")
            };
        }

        if (difficulty == 1)
        {
            return new StoryPage[]
            {
                new StoryPage("StoryImages/story_08_shadow_self_final_duel", "NARRATOR", "Before the shadow fades, you see his face. He is not an enemy, but the self who never escaped the fire."),
                new StoryPage("StoryImages/story_07_moonless_dojo_entrance", "NARRATOR", "You know the truth now, but dawn has not yet come. The final night of Moon-Hidden Style waits deeper within.")
            };
        }

        return new StoryPage[]
        {
            new StoryPage("StoryImages/story_07_moonless_dojo_entrance", "NARRATOR", "You step past the last warrior and leave the dojo. When you look back, the crescent moon still hangs above the ruin.")
        };
    }

    private string BuildDefeatNarration(int level, int difficulty)
    {
        if (difficulty == 2)
        {
            if (level == 4)
            {
                return "The moonless sky sinks into black. You kneel on the final platform, and your shadow stands behind you. Once more, you choose to remain in the night.";
            }

            return "You fall, but the shade does not finish you. If your heart still retreats, there is no road ahead.";
        }

        if (difficulty == 1)
        {
            if (level == 2)
            {
                return "Bamboo shadows sway as the patrol lantern fades away. You saved others, but still have not learned how to save yourself.";
            }

            if (level == 3)
            {
                return "The fellow shade sheathes his blade. You cannot defeat me while you are still defending that night from yourself.";
            }

            return "The shade lowers his sword. You hear a voice in the rain: just like that night, your true cut never came.";
        }

        return "You fall into cold rain, and the dojo becomes silent again. The night does not end. It waits for you to wake once more.";
    }

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

    private void ShowResultPanel()
    {
        resultReady = false;
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

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

    private void ContinueNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("maingame");
    }

    private void RestartMainGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("maingame");
    }

    private void BackMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void ShowStoryIntro()
    {
        if (storyPanel == null || storyDismissed)
        {
            return;
        }

        StartStory(BuildStartStoryPages(MainGameProgress.CurrentLevel, GetDifficultyValue()), false);
    }

    private void UpdateStoryInput()
    {
        if (storyPanel == null || !storyPanel.activeSelf)
        {
            return;
        }

        bool clickAdvance = Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || clickAdvance)
        {
            ContinueMainGame();
        }
    }

    private void ContinueMainGame()
    {
        if (storyPages != null && storyPageIndex < storyPages.Length - 1)
        {
            storyPageIndex++;
            ShowStoryPage();
            return;
        }

        FinishStorySequence();
    }

    private void StartStory(StoryPage[] pages, bool holdResult)
    {
        storyPages = pages;
        storyPageIndex = 0;
        storyHoldingResult = holdResult;

        if (storyPages == null || storyPages.Length == 0)
        {
            FinishStorySequence();
            return;
        }

        if (storyPanel != null)
        {
            storyPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
        MainGameBgmController.PlayStory();
        ShowStoryPage();
    }

    private void ShowStoryPage()
    {
        if (storyPages == null || storyPages.Length == 0)
        {
            return;
        }

        StoryPage page = storyPages[Mathf.Clamp(storyPageIndex, 0, storyPages.Length - 1)];
        if (storySpeakerText != null)
        {
            storySpeakerText.text = page.speaker;
        }

        if (storyBodyText != null)
        {
            storyBodyText.text = page.body;
        }

        if (storyHintText != null)
        {
            storyHintText.text = (storyPageIndex + 1) + " / " + storyPages.Length + "    Space / Enter / Click";
        }

        if (storyImage != null)
        {
            Sprite sprite = LoadStorySprite(page.imagePath);
            if (sprite != null)
            {
                storyImage.sprite = sprite;
                storyImage.color = Color.white;
            }
            else
            {
                storyImage.sprite = null;
                storyImage.color = Color.black;
            }
        }
    }

    private Sprite LoadStorySprite(string imagePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void FinishStorySequence()
    {
        if (storyPanel != null)
        {
            storyPanel.SetActive(false);
        }

        storyPages = null;
        storyPageIndex = 0;
        if (storyHoldingResult)
        {
            storyHoldingResult = false;
            resultReady = true;
            flowEndTime = Time.unscaledTime;
            MainGameBgmController.PlayForCurrentScene();
            return;
        }

        storyDismissed = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        MainGameBgmController.PlayForCurrentScene();
        ShowFeedbackMessage(MainGameProgress.GetLevelTitle() + "  Locate the target", 2.5f);
    }

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

