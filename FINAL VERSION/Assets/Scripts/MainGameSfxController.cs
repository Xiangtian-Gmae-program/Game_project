// Purpose: Plays short combat sound effects such as sword swings and successful guards.
using UnityEngine;

public class MainGameSfxController : MonoBehaviour
{
    private static MainGameSfxController instance;

    private AudioSource source;
    private AudioClip swordSwingClip;
    private AudioClip guardBlockClip;
    private float swordSwingVolume = 0.75f;
    private float guardBlockVolume = 0.85f;

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

        GameObject existing = GameObject.Find("MainGameSfxController");
        if (existing != null)
        {
            instance = existing.GetComponent<MainGameSfxController>();
            if (instance != null)
            {
                return;
            }
        }

        GameObject go = new GameObject("MainGameSfxController");
        instance = go.AddComponent<MainGameSfxController>();
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

        source = gameObject.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        GameSettingsController.ApplyAudio();

        swordSwingClip = Resources.Load<AudioClip>("Audio/SFX/SFX_SwordSwing");
        guardBlockClip = Resources.Load<AudioClip>("defence");
    }

    public static void PlaySwordSwing()
    {
        Ensure();
        if (instance == null || instance.source == null)
        {
            return;
        }

        if (instance.swordSwingClip == null)
        {
            instance.swordSwingClip = Resources.Load<AudioClip>("Audio/SFX/SFX_SwordSwing");
        }

        if (instance.swordSwingClip != null)
        {
            instance.source.PlayOneShot(instance.swordSwingClip, instance.swordSwingVolume);
        }
    }

    public static void PlayGuardBlock()
    {
        Ensure();
        if (instance == null || instance.source == null)
        {
            return;
        }

        if (instance.guardBlockClip == null)
        {
            instance.guardBlockClip = Resources.Load<AudioClip>("defence");
        }

        if (instance.guardBlockClip != null)
        {
            instance.source.PlayOneShot(instance.guardBlockClip, instance.guardBlockVolume);
        }
    }
}

