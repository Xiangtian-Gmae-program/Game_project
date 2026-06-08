// Script note: Editor helper that creates runtime enemy visual prefabs from imported model prefabs.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Class responsibility: HijinModelSetup contains this script's gameplay behavior.
public class HijinModelSetup
{
    private const string ResourceFolder = "Assets/Resources/EnemyVisuals";

    // Handles the CreateEnemyVisualResources logic.
    public static void CreateEnemyVisualResources()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "EnemyVisuals");

        CreateVisualPrefab("Assets/Arashi_Character/Prefab/Character_Color_01.prefab", ResourceFolder + "/Character_Color_01.prefab");
        CreateVisualPrefab("Assets/Arashi_Character/Prefab/Character_Color_02.prefab", ResourceFolder + "/Character_Color_02.prefab");
        CreateVisualPrefab("Assets/Arashi_Character/Prefab/Character_Color_05.prefab", ResourceFolder + "/Character_Color_05.prefab");
        CreateVisualPrefab("Assets/GuoJing/GuoJin.FBX", ResourceFolder + "/GuoJin.prefab", "Assets/GuoJing/GuoJingController.controller");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // Handles the CreateVisualPrefab logic.
    private static void CreateVisualPrefab(string sourcePath, string targetPath, string controllerPath = null)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
        {
            Debug.LogWarning("Missing visual source: " + sourcePath);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(source);
        }

        instance.name = System.IO.Path.GetFileNameWithoutExtension(targetPath);
        CleanupGameplayComponents(instance);
        ApplyController(instance, controllerPath);
        PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
        Object.DestroyImmediate(instance);
    }

    // Handles the ApplyController logic.
    private static void ApplyController(GameObject root, string controllerPath)
    {
        if (string.IsNullOrEmpty(controllerPath))
        {
            return;
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogWarning("Missing visual controller: " + controllerPath);
            return;
        }

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            animator = root.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
    }

    // Handles the CleanupGameplayComponents logic.
    private static void CleanupGameplayComponents(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = behaviours.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(behaviours[i]);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = rigidbodies.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(rigidbodies[i]);
        }

        CharacterController[] characterControllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = characterControllers.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(characterControllers[i]);
        }

        NavMeshAgent[] agents = root.GetComponentsInChildren<NavMeshAgent>(true);
        for (int i = agents.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(agents[i]);
        }

        AudioSource[] audioSources = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = audioSources.Length - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(audioSources[i]);
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].applyRootMotion = false;
        }
    }

    // Handles the EnsureFolder logic.
    private static void EnsureFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
