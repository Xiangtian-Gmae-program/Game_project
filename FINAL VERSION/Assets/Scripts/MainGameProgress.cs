// Purpose: Stores main-game progress, records, difficulty, and lightweight reward data.
using UnityEngine;

public static class MainGameProgress
{
    public const int MaxLevel = 4;

    public static int CurrentLevel
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("MainGameCurrentLevel", 1), 1, MaxLevel); }
    }

    public static int UnlockedLevel
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("MainGameUnlockedLevel", 1), 1, MaxLevel); }
    }

    public static void StartNewGame()
    {
        PlayerPrefs.SetInt("MainGameCurrentLevel", 1);
        PlayerPrefs.SetInt("MainGameUnlockedLevel", Mathf.Max(UnlockedLevel, 1));
        PlayerPrefs.Save();
    }

    public static void ContinueGame()
    {
        PlayerPrefs.SetInt("MainGameCurrentLevel", UnlockedLevel);
        PlayerPrefs.Save();
    }

    public static bool HasNextLevel()
    {
        return CurrentLevel < MaxLevel;
    }

    public static void AdvanceAfterVictory(int score)
    {
        int level = CurrentLevel;
        int best = PlayerPrefs.GetInt(GetBestScoreKey(level), 0);
        if (score > best)
        {
            PlayerPrefs.SetInt(GetBestScoreKey(level), score);
        }

        PlayerPrefs.SetInt("MainGameClearCount", PlayerPrefs.GetInt("MainGameClearCount", 0) + 1);
        if (level < MaxLevel)
        {
            PlayerPrefs.SetInt("MainGameUnlockedLevel", Mathf.Max(UnlockedLevel, level + 1));
            PlayerPrefs.SetInt("MainGameCurrentLevel", level + 1);
        }

        PlayerPrefs.SetInt("MainGameGrowthHP", Mathf.Min(PlayerPrefs.GetInt("MainGameGrowthHP", 0) + 2, 8));
        PlayerPrefs.SetInt("MainGameGrowthGuard", Mathf.Min(PlayerPrefs.GetInt("MainGameGrowthGuard", 0) + 10, 40));
        PlayerPrefs.Save();
    }

    public static int GetMaxHP()
    {
        return 20 + PlayerPrefs.GetInt("MainGameGrowthHP", 0);
    }

    public static float GetMaxGuard()
    {
        return 100f + PlayerPrefs.GetInt("MainGameGrowthGuard", 0);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.SetInt("MainGameCurrentLevel", 1);
        PlayerPrefs.SetInt("MainGameUnlockedLevel", 1);
        PlayerPrefs.SetInt("MainGameClearCount", 0);
        PlayerPrefs.SetInt("MainGameGrowthHP", 0);
        PlayerPrefs.SetInt("MainGameGrowthGuard", 0);
        for (int i = 1; i <= MaxLevel; i++)
        {
            PlayerPrefs.SetInt(GetBestScoreKey(i), 0);
        }

        PlayerActionProfile.ResetProfile();
        PlayerPrefs.Save();
    }

    public static string GetLevelTitle()
    {
        int level = CurrentLevel;
        if (level == 2)
        {
            return "CHAPTER 2  PATROL YARD";
        }

        if (level == 3)
        {
            return "CHAPTER 3  ELITE HALL";
        }

        if (level == 4)
        {
            return "FINAL CHAPTER  GUO JING";
        }

        return "CHAPTER 1  INNER GATE";
    }

    public static string GetLevelBrief()
    {
        int level = CurrentLevel;
        if (level == 2)
        {
            return "The patrol yard is awake. Break the roaming guards before they surround the gate.";
        }

        if (level == 3)
        {
            return "An elite guard waits inside the hall. His rhythm is faster and his openings are shorter.";
        }

        if (level == 4)
        {
            return "Guo Jing stands at the final gate. Survive his changing pace and force him to withdraw.";
        }

        return "The training hall has gone silent. A hostile sword guard blocks the inner gate, and your first duty is to break through without losing your stance.";
    }

    public static string GetBestScoreKey(int level)
    {
        return "MainGameBestScore_Level" + Mathf.Clamp(level, 1, MaxLevel);
    }
}

