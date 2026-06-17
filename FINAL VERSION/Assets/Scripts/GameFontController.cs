// Purpose: Applies the project style font to title, menu, and selected UI text.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFontController : MonoBehaviour
{
    private static GameFontController instance;
    private Font uiFont;
    private TMP_FontAsset tmpFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Ensure();
    }

    public static void Ensure()
    {
        if (instance != null)
        {
            return;
        }

        GameObject existing = GameObject.Find("GameFontController");
        if (existing != null)
        {
            instance = existing.GetComponent<GameFontController>();
            if (instance != null)
            {
                return;
            }
        }

        GameObject go = new GameObject("GameFontController");
        instance = go.AddComponent<GameFontController>();
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
        DontDestroyOnLoad(gameObject);
        LoadFonts();
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
        StartCoroutine(ApplyFontOverStartup());
    }

    private IEnumerator ApplyFontOverStartup()
    {
        for (int i = 0; i < 8; i++)
        {
            ApplyFontNow();
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void LoadFonts()
    {
        uiFont = Resources.Load<Font>("Fonts/HIJIN_Style");
        if (uiFont != null && tmpFont == null)
        {
            tmpFont = TMP_FontAsset.CreateFontAsset(uiFont);
            if (tmpFont != null)
            {
                tmpFont.name = "HIJIN_Style_TMP";
            }
        }
    }

    public static void ApplyFontNow()
    {
        Ensure();
        if (instance == null)
        {
            return;
        }

        instance.ApplyLegacyTextFont();
        instance.ApplyTextMeshProFont();
    }

    private void ApplyLegacyTextFont()
    {
        if (uiFont == null)
        {
            LoadFonts();
        }

        if (uiFont == null)
        {
            return;
        }

        Text[] texts = FindObjectsOfType<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (ShouldUseStyleFont(texts[i].gameObject.name, texts[i].text))
            {
                texts[i].font = uiFont;
            }
        }
    }

    private void ApplyTextMeshProFont()
    {
        if (tmpFont == null)
        {
            LoadFonts();
        }

        if (tmpFont == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (ShouldUseStyleFont(texts[i].gameObject.name, texts[i].text))
            {
                texts[i].font = tmpFont;
                ApplyTextPolish(texts[i]);
            }
        }
    }

    private bool ShouldUseStyleFont(string objectName, string value)
    {
        bool isStyledMenuScene = SceneManager.GetActiveScene().name == "MainMenuScene" || SceneManager.GetActiveScene().name == "TitleScene";
        if (isStyledMenuScene)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(objectName) && objectName == "Menutitle")
        {
            return true;
        }

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        string upper = value.ToUpperInvariant();
        return upper.Contains("HIJIN-BATTLE") || upper.Contains("HIJIN TAIKETSU");
    }

    private void ApplyTextPolish(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (text.gameObject.name == "Menutitle")
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(330f, -105f);
                rect.sizeDelta = new Vector2(660f, 96f);
            }

            text.margin = Vector4.zero;
            text.enableAutoSizing = true;
            text.fontSize = 52f;
            text.fontSizeMin = 34f;
            text.fontSizeMax = 58f;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            return;
        }

        if (text.gameObject.name == "Gametitle")
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 115f);
                rect.sizeDelta = new Vector2(980f, 140f);
            }

            text.margin = Vector4.zero;
            text.enableAutoSizing = true;
            text.fontSize = 86f;
            text.fontSizeMin = 54f;
            text.fontSizeMax = 96f;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            return;
        }

        if (text.gameObject.name == "Presskeytext")
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -120f);
                rect.sizeDelta = new Vector2(560f, 72f);
            }

            text.margin = Vector4.zero;
            text.enableAutoSizing = true;
            text.fontSize = 34f;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 42f;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
            return;
        }

        if (SceneManager.GetActiveScene().name != "MainMenuScene" && SceneManager.GetActiveScene().name != "TitleScene")
        {
            return;
        }

        text.fontStyle = FontStyles.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = Mathf.Clamp(text.fontSize, 18f, 32f);
        text.enableWordWrapping = text.text != null && text.text.Length > 18;
    }
}

