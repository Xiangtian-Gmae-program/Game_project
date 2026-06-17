// Purpose: Plays and switches background music for menu, practice, story, battle, and boss scenes.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameBgmController : MonoBehaviour
{
    private enum BgmMode
    {
        None,
        MainMenu,
        Practice,
        Battle,
        Boss,
        Story
    }

    private static MainGameBgmController instance;

    private AudioSource source;
    private AudioClip mainMenuClip;
    private AudioClip practiceClip;
    private AudioClip battleClip;
    private AudioClip bossClip;
    private AudioClip storyClip;
    private BgmMode currentMode = BgmMode.None;
    private Coroutine fadeRoutine;
    private float defaultVolume = 0.55f;
    private float fadeTime = 0.8f;

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

        GameObject existing = GameObject.Find("MainGameBgmController");
        if (existing != null)
        {
            instance = existing.GetComponent<MainGameBgmController>();
            if (instance != null)
            {
                return;
            }
        }

        GameObject go = new GameObject("MainGameBgmController");
        instance = go.AddComponent<MainGameBgmController>();
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
        source.loop = true;
        source.playOnAwake = false;
        source.volume = defaultVolume;
        GameSettingsController.ApplyAudio();

        LoadClips();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void LoadClips()
    {
        mainMenuClip = Resources.Load<AudioClip>("Audio/BGM/BGM_MainMenu");
        practiceClip = Resources.Load<AudioClip>("Audio/BGM/BGM_PracticeMode");
        battleClip = Resources.Load<AudioClip>("Audio/BGM/BGM_Battle");
        bossClip = Resources.Load<AudioClip>("Audio/BGM/BGM_BossBattle");
        storyClip = Resources.Load<AudioClip>("Audio/BGM/BGM_Story");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    public static void PlayForCurrentScene()
    {
        Ensure();
        instance.PlayForScene(SceneManager.GetActiveScene().name);
    }

    public static void PlayStory()
    {
        Ensure();
        instance.PlayMode(BgmMode.Story);
    }

    private void PlayForScene(string sceneName)
    {
        if (sceneName == "MainMenuScene")
        {
            PlayMode(BgmMode.MainMenu);
            return;
        }

        if (sceneName == "PracticeIntro")
        {
            PlayMode(BgmMode.Story);
            return;
        }

        if (sceneName == "DockThing")
        {
            PlayMode(BgmMode.Practice);
            return;
        }

        if (sceneName == "maingame")
        {
            PlayMode(MainGameProgress.CurrentLevel >= MainGameProgress.MaxLevel ? BgmMode.Boss : BgmMode.Battle);
            return;
        }

        StopMusic();
    }

    private void PlayMode(BgmMode mode)
    {
        AudioClip clip = GetClip(mode);
        if (clip == null)
        {
            return;
        }

        if (currentMode == mode && source.clip == clip && source.isPlaying)
        {
            return;
        }

        currentMode = mode;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeToClip(clip));
    }

    private void StopMusic()
    {
        currentMode = BgmMode.None;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }
    }

    private AudioClip GetClip(BgmMode mode)
    {
        if (mode == BgmMode.MainMenu)
        {
            return mainMenuClip;
        }

        if (mode == BgmMode.Practice)
        {
            return practiceClip;
        }

        if (mode == BgmMode.Battle)
        {
            return battleClip;
        }

        if (mode == BgmMode.Boss)
        {
            return bossClip;
        }

        if (mode == BgmMode.Story)
        {
            return storyClip;
        }

        return null;
    }

    private IEnumerator FadeToClip(AudioClip clip)
    {
        float startVolume = source.volume;
        float timer = 0f;
        while (timer < fadeTime && source.isPlaying)
        {
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }

        source.clip = clip;
        source.volume = 0f;
        source.Play();

        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, defaultVolume, timer / fadeTime);
            yield return null;
        }

        source.volume = defaultVolume;
        fadeRoutine = null;
    }
}

