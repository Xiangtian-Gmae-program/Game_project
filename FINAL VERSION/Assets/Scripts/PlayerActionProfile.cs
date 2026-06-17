// Purpose: Tracks player combat habits so enemy AI can adapt within controlled probability ranges.
using System;
using System.IO;
using UnityEngine;

public static class PlayerActionProfile
{
    [Serializable]
    private class ActionData
    {
        public int normalAttack;
        public int heavyAttack;
        public int defend;
        public int roll;
        public int jump;
    }

    private static ActionData data;
    private static bool loaded;
    private static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "player_action_profile.json"); }
    }

    public static void RecordNormalAttack()
    {
        EnsureLoaded();
        data.normalAttack++;
        SaveSoon();
    }

    public static void RecordHeavyAttack()
    {
        EnsureLoaded();
        data.heavyAttack++;
        SaveSoon();
    }

    public static void RecordDefend()
    {
        EnsureLoaded();
        data.defend++;
        SaveSoon();
    }

    public static void RecordRoll()
    {
        EnsureLoaded();
        data.roll++;
        SaveSoon();
    }

    public static void RecordJump()
    {
        EnsureLoaded();
        data.jump++;
        SaveSoon();
    }

    public static float AttackPreference()
    {
        EnsureLoaded();
        return Ratio(data.normalAttack + data.heavyAttack);
    }

    public static float HeavyAttackPreference()
    {
        EnsureLoaded();
        return Ratio(data.heavyAttack);
    }

    public static float DefendPreference()
    {
        EnsureLoaded();
        return Ratio(data.defend);
    }

    public static float EvadePreference()
    {
        EnsureLoaded();
        return Ratio(data.roll + data.jump);
    }

    public static float ChanceBonus(float preference, float maxBonus)
    {
        return Mathf.Clamp01(preference) * Mathf.Max(0f, maxBonus);
    }

    public static void ResetProfile()
    {
        data = new ActionData();
        loaded = true;
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to reset player action profile: " + exception.Message);
        }
    }

    private static float Ratio(int value)
    {
        int total = data.normalAttack + data.heavyAttack + data.defend + data.roll + data.jump;
        if (total <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)value / total);
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        data = new ActionData();
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                ActionData loadedData = JsonUtility.FromJson<ActionData>(json);
                if (loadedData != null)
                {
                    data = loadedData;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load player action profile: " + exception.Message);
        }
    }

    private static void SaveSoon()
    {
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to save player action profile: " + exception.Message);
        }
    }
}

