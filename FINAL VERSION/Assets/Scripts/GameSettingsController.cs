// Purpose: Saves and applies audio, display, quality, and mouse sensitivity settings.
using UnityEngine;

public static class GameSettingsController
{
    private const string MasterVolumeKey = "Setting_MasterVolume";
    private const string MouseSensitivityKey = "Setting_MouseSensitivity";
    private const string FullscreenKey = "Setting_Fullscreen";
    private const string QualityKey = "Setting_Quality";
    private const float DefaultMasterVolume = 0.8f;
    private const float DefaultMouseSensitivity = 0.5f;
    private const int DefaultFullscreen = 1;
    private const int DefaultQuality = 1;
    private const float MinMouseSensitivity = 0.7f;
    private const float MaxMouseSensitivity = 4f;

    public static float MasterVolume
    {
        get { return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)); }
    }

    public static float MouseSensitivityNormalized
    {
        get { return Mathf.Clamp01(PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity)); }
    }

    public static float MouseSensitivity
    {
        get { return Mathf.Lerp(MinMouseSensitivity, MaxMouseSensitivity, MouseSensitivityNormalized); }
    }

    public static bool IsFullscreen
    {
        get { return PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen) == 1; }
    }

    public static int QualityLevel
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, DefaultQuality), 0, 2); }
    }

    public static string QualityName
    {
        get
        {
            if (QualityLevel == 0)
            {
                return "LOW";
            }

            if (QualityLevel == 2)
            {
                return "HIGH";
            }

            return "MEDIUM";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        ApplyAudio();
        ApplyVideo();
    }

    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        ApplyAudio();
    }

    public static void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static void SetFullscreen(bool value)
    {
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVideo();
    }

    public static void CycleQuality()
    {
        PlayerPrefs.SetInt(QualityKey, (QualityLevel + 1) % 3);
        PlayerPrefs.Save();
        ApplyVideo();
    }

    public static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
    }

    public static void ApplyVideo()
    {
        Screen.fullScreen = IsFullscreen;
        if (QualitySettings.names != null && QualitySettings.names.Length > 0)
        {
            QualitySettings.SetQualityLevel(GetUnityQualityIndex(QualityLevel), true);
        }
    }

    public static void ApplyPlayerSensitivity(PlayerController player)
    {
        if (player == null || player is EnemyController)
        {
            return;
        }

        player.sensitivity = MouseSensitivity;
    }

    public static void ResetSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, DefaultMasterVolume);
        PlayerPrefs.SetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
        PlayerPrefs.SetInt(FullscreenKey, DefaultFullscreen);
        PlayerPrefs.SetInt(QualityKey, DefaultQuality);
        PlayerPrefs.Save();
        ApplyAudio();
        ApplyVideo();
    }

    private static int GetUnityQualityIndex(int qualityLevel)
    {
        int maxIndex = QualitySettings.names.Length - 1;
        if (maxIndex <= 0)
        {
            return 0;
        }

        if (qualityLevel <= 0)
        {
            return 0;
        }

        if (qualityLevel >= 2)
        {
            return maxIndex;
        }

        return Mathf.RoundToInt(maxIndex * 0.5f);
    }
}

