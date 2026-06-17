// Purpose: Controls the main menu settings panel, sliders, buttons, layout, records, and reset actions.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuPanelController : MonoBehaviour
{
    public GameObject settingsPanel;
    public Slider masterVolumeSlider;
    public Slider mouseSensitivitySlider;
    private Button fullscreenButton;
    private Button qualityButton;
    private Button resetButton;
    private Button resetProgressButton;
    private TextMeshProUGUI masterVolumeLabel;
    private TextMeshProUGUI mouseSensitivityLabel;
    private TextMeshProUGUI fullscreenText;
    private TextMeshProUGUI qualityText;
    private TextMeshProUGUI resetProgressText;
    private float resetProgressConfirmUntil;
    private const float ResetProgressConfirmTime = 3f;

    private void Awake()
    {
        GameSettingsController.ApplyAudio();
        GameSettingsController.ApplyVideo();
        BindSettingsSliders();
        BuildAdvancedSettingsControls();
        BuildSettingsReadableLayout();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            RefreshSettingsSliders();
            settingsPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ResetSettings()
    {
        GameSettingsController.ResetSettings();
        RefreshSettingsSliders();
        RefreshAdvancedSettingsText();
    }

    public void OnMasterVolumeChanged(float value)
    {
        GameSettingsController.SetMasterVolume(value);
    }

    public void OnMouseSensitivityChanged(float value)
    {
        GameSettingsController.SetMouseSensitivity(value);
    }

    public void ToggleFullscreen()
    {
        GameSettingsController.SetFullscreen(!GameSettingsController.IsFullscreen);
        RefreshAdvancedSettingsText();
    }

    public void CycleQuality()
    {
        GameSettingsController.CycleQuality();
        RefreshAdvancedSettingsText();
    }

    public void ResetProgress()
    {
        if (Time.unscaledTime > resetProgressConfirmUntil)
        {
            resetProgressConfirmUntil = Time.unscaledTime + ResetProgressConfirmTime;
            RefreshAdvancedSettingsText();
            return;
        }

        resetProgressConfirmUntil = 0f;
        MainGameProgress.ResetProgress();
        RefreshAdvancedSettingsText();
    }

    private void BindSettingsSliders()
    {
        FindSettingsSliders();
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }

        RefreshSettingsSliders();
    }

    private void FindSettingsSliders()
    {
        if (settingsPanel == null || (masterVolumeSlider != null && mouseSensitivitySlider != null))
        {
            return;
        }

        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        for (int i = 0; i < sliders.Length; i++)
        {
            string sliderName = sliders[i].gameObject.name.ToUpperInvariant();
            if (masterVolumeSlider == null && sliderName.Contains("MASTER"))
            {
                masterVolumeSlider = sliders[i];
            }

            if (mouseSensitivitySlider == null && sliderName.Contains("MOUSE"))
            {
                mouseSensitivitySlider = sliders[i];
            }
        }

        if (masterVolumeSlider == null && sliders.Length > 0)
        {
            masterVolumeSlider = sliders[0];
        }

        if (mouseSensitivitySlider == null && sliders.Length > 1)
        {
            mouseSensitivitySlider = sliders[1];
        }
    }

    private void RefreshSettingsSliders()
    {
        FindSettingsSliders();
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(GameSettingsController.MasterVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(GameSettingsController.MouseSensitivityNormalized);
        }

        RefreshAdvancedSettingsText();
    }

    private void BuildSettingsReadableLayout()
    {
        if (settingsPanel == null)
        {
            return;
        }

        FindSettingsSliders();
        LayoutSettingsTitle();
        LayoutSettingsSlider(masterVolumeSlider, new Vector2(0f, 116f), new Vector2(480f, 28f));
        LayoutSettingsSlider(mouseSensitivitySlider, new Vector2(0f, 16f), new Vector2(480f, 28f));

        masterVolumeLabel = CreateSettingsDescriptionLabel("MasterVolumeDescription", "MASTER VOLUME", new Vector2(0f, 154f));
        mouseSensitivityLabel = CreateSettingsDescriptionLabel("MouseSensitivityDescription", "MOUSE SENSITIVITY", new Vector2(0f, 54f));

        LayoutAdvancedSettingsGroup();
        LayoutSettingsBackButton();
        RefreshAdvancedSettingsText();
    }

    private void LayoutSettingsTitle()
    {
        Transform title = settingsPanel.transform.Find("TITLE");
        if (title == null)
        {
            return;
        }

        RectTransform rect = title.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 246f);
            rect.sizeDelta = new Vector2(420f, 70f);
        }

        TextMeshProUGUI text = title.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.fontSize = 54f;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
        }
    }

    private void LayoutSettingsSlider(Slider slider, Vector2 position, Vector2 size)
    {
        if (slider == null)
        {
            return;
        }

        RectTransform rect = slider.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private TextMeshProUGUI CreateSettingsDescriptionLabel(string name, string text, Vector2 position)
    {
        Transform existing = settingsPanel.transform.Find(name);
        GameObject labelObject = existing != null ? existing.gameObject : new GameObject(name);
        labelObject.layer = settingsPanel.layer;
        labelObject.transform.SetParent(settingsPanel.transform, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = labelObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(520f, 34f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (label == null)
        {
            label = labelObject.AddComponent<TextMeshProUGUI>();
        }

        ApplySettingsReadableFont(label);
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 19f;
        label.fontStyle = FontStyles.Normal;
        label.enableWordWrapping = false;
        label.color = new Color(0.95f, 0.76f, 0.33f, 1f);
        return label;
    }

    private void LayoutAdvancedSettingsGroup()
    {
        Transform group = settingsPanel.transform.Find("AdvancedSettings");
        if (group == null)
        {
            return;
        }

        RectTransform groupRect = group.GetComponent<RectTransform>();
        if (groupRect != null)
        {
            groupRect.anchorMin = new Vector2(0.5f, 0.5f);
            groupRect.anchorMax = new Vector2(0.5f, 0.5f);
            groupRect.pivot = new Vector2(0.5f, 0.5f);
            groupRect.anchoredPosition = new Vector2(0f, -146f);
            groupRect.sizeDelta = new Vector2(380f, 250f);
        }

        LayoutSettingsButton(fullscreenButton, new Vector2(0f, 96f));
        LayoutSettingsButton(qualityButton, new Vector2(0f, 32f));
        LayoutSettingsButton(resetButton, new Vector2(0f, -32f));
        LayoutSettingsButton(resetProgressButton, new Vector2(0f, -96f));
    }

    private void LayoutSettingsButton(Button button, Vector2 position)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(340f, 44f);
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            ApplySettingsReadableFont(label);
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Normal;
            label.enableWordWrapping = false;
        }
    }

    private void LayoutSettingsBackButton()
    {
        Transform back = settingsPanel.transform.Find("BACK");
        if (back == null)
        {
            return;
        }

        RectTransform rect = back.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -318f);
        rect.sizeDelta = new Vector2(260f, 48f);
    }

    private void ApplySettingsReadableFont(TextMeshProUGUI label)
    {
        if (label != null && TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }
    }

    private void BuildAdvancedSettingsControls()
    {
        if (settingsPanel == null || settingsPanel.transform.Find("AdvancedSettings") != null)
        {
            FindAdvancedSettingsControls();
            RefreshAdvancedSettingsText();
            return;
        }

        GameObject group = new GameObject("AdvancedSettings");
        group.layer = settingsPanel.layer;
        group.transform.SetParent(settingsPanel.transform, false);

        RectTransform groupRect = group.AddComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0.5f, 0.5f);
        groupRect.anchorMax = new Vector2(0.5f, 0.5f);
        groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = new Vector2(0f, -146f);
        groupRect.sizeDelta = new Vector2(380f, 250f);

        fullscreenButton = CreateSettingsButton(group.transform, "FullscreenButton", new Vector2(0f, 96f), ToggleFullscreen, out fullscreenText);
        qualityButton = CreateSettingsButton(group.transform, "QualityButton", new Vector2(0f, 32f), CycleQuality, out qualityText);
        resetButton = CreateSettingsButton(group.transform, "ResetSettingsButton", new Vector2(0f, -32f), ResetSettings, out TextMeshProUGUI resetText);
        resetText.text = "RESET SETTINGS";
        resetProgressButton = CreateSettingsButton(group.transform, "ResetProgressButton", new Vector2(0f, -96f), ResetProgress, out resetProgressText);
        RefreshAdvancedSettingsText();
    }

    private void FindAdvancedSettingsControls()
    {
        if (settingsPanel == null)
        {
            return;
        }

        Transform group = settingsPanel.transform.Find("AdvancedSettings");
        if (group == null)
        {
            return;
        }

        if (fullscreenButton == null)
        {
            Transform button = group.Find("FullscreenButton");
            if (button != null)
            {
                fullscreenButton = button.GetComponent<Button>();
                fullscreenText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (qualityButton == null)
        {
            Transform button = group.Find("QualityButton");
            if (button != null)
            {
                qualityButton = button.GetComponent<Button>();
                qualityText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (resetProgressButton == null)
        {
            Transform button = group.Find("ResetProgressButton");
            if (button != null)
            {
                resetProgressButton = button.GetComponent<Button>();
                resetProgressText = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    private Button CreateSettingsButton(Transform parent, string name, Vector2 position, UnityEngine.Events.UnityAction action, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.layer = settingsPanel.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 44f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.06f, 0.045f, 0.035f, 0.88f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.47f, 0.08f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Text");
        textObject.layer = settingsPanel.layer;
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.AddComponent<TextMeshProUGUI>();
        ApplySettingsReadableFont(label);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 16f;
        label.fontStyle = FontStyles.Normal;
        label.enableWordWrapping = false;
        label.color = new Color(0.95f, 0.76f, 0.33f, 1f);
        label.text = name;
        return button;
    }

    private void RefreshAdvancedSettingsText()
    {
        if (fullscreenText != null)
        {
            fullscreenText.text = GameSettingsController.IsFullscreen ? "FULLSCREEN  ON" : "FULLSCREEN  OFF";
        }

        if (qualityText != null)
        {
            qualityText.text = "QUALITY  " + GameSettingsController.QualityName;
        }

        if (resetProgressText != null)
        {
            resetProgressText.text = Time.unscaledTime <= resetProgressConfirmUntil ? "CONFIRM RESET" : "RESET PROGRESS";
        }
    }
}

