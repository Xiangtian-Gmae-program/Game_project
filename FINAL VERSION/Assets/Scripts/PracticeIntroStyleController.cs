// Purpose: Applies the themed background and readable typography for the practice intro scene.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PracticeIntroStyleController : MonoBehaviour
{
    private static PracticeIntroStyleController instance;
    private TMP_FontAsset styleFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("PracticeIntroStyleController");
        instance = go.AddComponent<PracticeIntroStyleController>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "PracticeIntro")
        {
            StartCoroutine(ApplyNextFrame());
        }
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null;
        ApplyPracticeIntroStyle();
    }

    private void ApplyPracticeIntroStyle()
    {
        Canvas canvas = GameObject.Find("Canvas_PracticeIntro")?.GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        LoadStyleFont();
        ApplyBackground(canvas.transform);
        EnsureReadingPanel(canvas.transform);
        ApplyIntroText(canvas.transform);
        ApplyButtons(canvas.transform);
    }

    private void LoadStyleFont()
    {
        if (styleFont != null)
        {
            return;
        }

        Font source = Resources.Load<Font>("Fonts/HIJIN_Style");
        if (source != null)
        {
            styleFont = TMP_FontAsset.CreateFontAsset(source);
        }
    }

    private void ApplyBackground(Transform canvas)
    {
        Image background = null;
        Transform backgroundTransform = canvas.Find("Image");
        if (backgroundTransform != null)
        {
            background = backgroundTransform.GetComponent<Image>();
        }

        if (background == null)
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>("PracticeIntroBackground");
        if (sprite != null)
        {
            background.sprite = sprite;
        }

        background.color = Color.white;
        background.preserveAspect = false;
        background.raycastTarget = false;

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.SetAsFirstSibling();
    }

    private void EnsureReadingPanel(Transform canvas)
    {
        Transform existing = canvas.Find("PracticeIntroPanel");
        if (existing != null)
        {
            return;
        }

        GameObject panel = new GameObject("PracticeIntroPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.sizeDelta = new Vector2(760f, 390f);

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.025f, 0.02f, 0.017f, 0.82f);
        image.raycastTarget = false;

        AddBorder(panel.transform, new Vector2(760f, 390f));
        panel.transform.SetSiblingIndex(1);
    }

    private void AddBorder(Transform parent, Vector2 size)
    {
        CreateBorderLine(parent, "Top", new Vector2(0f, size.y * 0.5f), new Vector2(size.x, 2f));
        CreateBorderLine(parent, "Bottom", new Vector2(0f, -size.y * 0.5f), new Vector2(size.x, 2f));
        CreateBorderLine(parent, "Left", new Vector2(-size.x * 0.5f, 0f), new Vector2(2f, size.y));
        CreateBorderLine(parent, "Right", new Vector2(size.x * 0.5f, 0f), new Vector2(2f, size.y));
    }

    private void CreateBorderLine(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject line = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        line.transform.SetParent(parent, false);

        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = line.GetComponent<Image>();
        image.color = new Color(0.88f, 0.55f, 0.12f, 0.95f);
        image.raycastTarget = false;
    }

    private void ApplyIntroText(Transform canvas)
    {
        TextMeshProUGUI intro = FindTextContaining("Practice Mode");
        if (intro != null)
        {
            intro.text = "Train your movement, blade timing, defense, jump, and roll before entering the real duel.\n\nUse the practice enemy to test attacks, guard timing, combo rhythm, and hit feedback.\n\nWhen ready, begin the practice session.";
            intro.fontSize = 23f;
            intro.fontStyle = FontStyles.Normal;
            intro.color = new Color(0.94f, 0.88f, 0.76f, 1f);
            intro.alignment = TextAlignmentOptions.Center;
            intro.enableWordWrapping = true;
            intro.lineSpacing = 8f;
            intro.margin = new Vector4(18f, 0f, 18f, 0f);

            RectTransform rect = intro.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(650f, 210f);
        }

        TextMeshProUGUI title = EnsureText(canvas, "PracticeIntroTitle");
        title.text = "Practice Mode";
        title.color = Color.white;
        title.fontSize = 50f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 34f;
        title.fontSizeMax = 56f;
        title.alignment = TextAlignmentOptions.Center;
        title.characterSpacing = 2f;
        if (styleFont != null)
        {
            title.font = styleFont;
        }

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 150f);
        titleRect.sizeDelta = new Vector2(680f, 72f);
        title.transform.SetSiblingIndex(2);
    }

    private TextMeshProUGUI EnsureText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.GetComponent<TextMeshProUGUI>();
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TextMeshProUGUI>();
    }

    private void ApplyButtons(Transform canvas)
    {
        ApplyButtonText(FindTextContaining("Start practice"), "START PRACTICE", new Vector2(-130f, -220f));
        ApplyButtonText(FindTextContaining("Back"), "BACK", new Vector2(130f, -220f));
    }

    private void ApplyButtonText(TextMeshProUGUI text, string value, Vector2 buttonPosition)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
        text.fontSize = 24f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 26f;
        text.color = new Color(0.96f, 0.74f, 0.32f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        if (styleFont != null)
        {
            text.font = styleFont;
        }

        Button button = text.GetComponentInParent<Button>();
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = buttonPosition;
        buttonRect.sizeDelta = new Vector2(220f, 44f);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.17f, 0.09f, 0.02f, 0.96f);
        }
    }

    private TextMeshProUGUI FindTextContaining(string value)
    {
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (!string.IsNullOrEmpty(texts[i].text) && texts[i].text.Contains(value))
            {
                return texts[i];
            }
        }

        return null;
    }
}
