// Script note: Stores main-game level progress, records, unlocks, and lightweight growth values with PlayerPrefs.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;

// Class responsibility: MainGameProgress contains this script's gameplay behavior.
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

    // Resets progress and starts from the first level.
    public static void StartNewGame()
    {
        PlayerPrefs.SetInt("MainGameCurrentLevel", 1);
        PlayerPrefs.SetInt("MainGameUnlockedLevel", Mathf.Max(UnlockedLevel, 1));
        PlayerPrefs.Save();
    }

    // Loads the current saved main-game level.
    public static void ContinueGame()
    {
        PlayerPrefs.SetInt("MainGameCurrentLevel", UnlockedLevel);
        PlayerPrefs.Save();
    }

    // Checks whether another level exists after the current one.
    public static bool HasNextLevel()
    {
        return CurrentLevel < MaxLevel;
    }

    // Saves score, unlocks the next level, and applies growth rewards.
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

    // Returns the player maximum HP after saved growth rewards.
    public static int GetMaxHP()
    {
        return 20 + PlayerPrefs.GetInt("MainGameGrowthHP", 0);
    }

    // Returns the player maximum guard value after saved growth rewards.
    public static float GetMaxGuard()
    {
        return 100f + PlayerPrefs.GetInt("MainGameGrowthGuard", 0);
    }

    // Returns the display title for the current level.
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

    // Returns the mission brief for the current level.
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

    // Builds the PlayerPrefs key for a level best score.
    public static string GetBestScoreKey(int level)
    {
        return "MainGameBestScore_Level" + Mathf.Clamp(level, 1, MaxLevel);
    }
}
