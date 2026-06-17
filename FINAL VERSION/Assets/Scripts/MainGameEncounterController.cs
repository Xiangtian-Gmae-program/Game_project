// Purpose: Configures staged main-game encounters, enemy stats, and level progression setup.
using UnityEngine;
using UnityEngine.AI;

public class MainGameEncounterController : MonoBehaviour
{

    private struct EncounterStep
    {
        public string name;
        public EnemyDifficulty difficulty;
        public EnemyVisualController.VisualType visualType;
        public int hp;
        public int attackDamage;
        public float attackCD;
        public float attackWindup;
        public int comboCount;
        public float moveSpeed;
        public float detectRange;
        public float attackRange;
        public float returnRange;
    }

    private static MainGameEncounterController instance;

    private PlayerController player;
    private EnemyController currentEnemy;
    private GameObject enemyTemplate;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private EncounterStep[] steps;
    private int stepIndex;
    private bool waitingNext;
    private bool complete;
    private bool bossPhaseChanged;
    private float nextSpawnTime;
    private float spawnDelay = 1.6f;

    public static void Ensure(PlayerController player, EnemyController firstEnemy)
    {
        if (player == null || firstEnemy == null)
        {
            return;
        }

        if (instance == null)
        {
            GameObject go = new GameObject("MainGameEncounterController");
            instance = go.AddComponent<MainGameEncounterController>();
        }

        instance.Setup(player, firstEnemy);
    }

    public static EnemyController GetCurrentEnemy()
    {
        if (instance == null)
        {
            return null;
        }

        return instance.currentEnemy;
    }

    public static bool IsComplete()
    {
        return instance != null && instance.complete;
    }

    public static bool IsWaitingNext()
    {
        return instance != null && instance.waitingNext;
    }

    public static string GetChapterText()
    {
        if (instance == null)
        {
            return "CHAPTER 1  INNER GATE";
        }

        return MainGameProgress.GetLevelTitle() + "  " + (instance.stepIndex + 1) + "/" + instance.steps.Length;
    }

    public static string GetTargetName()
    {
        if (instance == null || instance.steps == null || instance.steps.Length == 0)
        {
            return "GUARD";
        }

        return instance.steps[Mathf.Clamp(instance.stepIndex, 0, instance.steps.Length - 1)].name;
    }

    private void Setup(PlayerController newPlayer, EnemyController firstEnemy)
    {
        if (player == newPlayer && currentEnemy != null)
        {
            return;
        }

        player = newPlayer;
        currentEnemy = firstEnemy;
        startPosition = firstEnemy.transform.position;
        startRotation = firstEnemy.transform.rotation;
        steps = BuildSteps();
        stepIndex = 0;
        waitingNext = false;
        complete = false;
        bossPhaseChanged = false;

        if (enemyTemplate == null)
        {
            enemyTemplate = Instantiate(firstEnemy.gameObject);
            enemyTemplate.name = "MainGameEnemyTemplate";
            enemyTemplate.SetActive(false);
        }

        ConfigureEnemy(currentEnemy, steps[stepIndex], startPosition);
        player.enemy = currentEnemy;
    }

    private void Update()
    {
        if (complete || player == null || currentEnemy == null)
        {
            return;
        }

        if (currentEnemy.isDead && !waitingNext)
        {
            if (stepIndex >= steps.Length - 1)
            {
                complete = true;
                return;
            }

            waitingNext = true;
            nextSpawnTime = Time.time + spawnDelay;
        }

        UpdateBossPhase();

        if (waitingNext && Time.time >= nextSpawnTime)
        {
            SpawnNextEnemy();
        }
    }

    private EncounterStep[] BuildSteps()
    {
        int level = MainGameProgress.CurrentLevel;
        if (level == 2)
        {
            EncounterStep[] patrolSteps = new EncounterStep[2];
            patrolSteps[0] = CreateStep("PATROL GUARD", EnemyDifficulty.Normal, EnemyVisualController.VisualType.ArashiGuard, 20, 1, 3.8f, 0.38f, 1, 3.5f, 10f, 2f, 12f);
            patrolSteps[1] = CreateStep("PATROL CAPTAIN", EnemyDifficulty.Hard, EnemyVisualController.VisualType.ArashiElite, 28, 2, 3.0f, 0.3f, 2, 4.1f, 11f, 2.2f, 13f);
            return patrolSteps;
        }

        if (level == 3)
        {
            EncounterStep[] eliteSteps = new EncounterStep[1];
            eliteSteps[0] = CreateStep("ELITE GUARD", EnemyDifficulty.Hard, EnemyVisualController.VisualType.ArashiElite, 42, 2, 2.7f, 0.26f, 2, 4.2f, 12f, 2.3f, 14f);
            return eliteSteps;
        }

        if (level == 4)
        {
            EncounterStep[] bossSteps = new EncounterStep[1];
            bossSteps[0] = CreateStep("GUO JING", EnemyDifficulty.Hard, EnemyVisualController.VisualType.GuoJingBoss, 70, 2, 2.5f, 0.28f, 2, 3.8f, 13f, 2.5f, 15f);
            return bossSteps;
        }

        EncounterStep[] result = new EncounterStep[1];
        result[0] = CreateStep("INNER GATE GUARD", EnemyDifficulty.Normal, EnemyVisualController.VisualType.ArashiGuard, 24, 1, 3.9f, 0.42f, 1, 3.3f, 9f, 2f, 11f);
        return result;
    }

    private EncounterStep CreateStep(string name, EnemyDifficulty difficulty, EnemyVisualController.VisualType visualType, int hp, int attackDamage, float attackCD, float attackWindup, int comboCount, float moveSpeed, float detectRange, float attackRange, float returnRange)
    {
        EncounterStep step = new EncounterStep();
        step.name = name;
        step.difficulty = difficulty;
        step.visualType = visualType;
        step.hp = hp;
        step.attackDamage = attackDamage;
        step.attackCD = attackCD;
        step.attackWindup = attackWindup;
        step.comboCount = comboCount;
        step.moveSpeed = moveSpeed;
        step.detectRange = detectRange;
        step.attackRange = attackRange;
        step.returnRange = returnRange;
        return step;
    }

    private void SpawnNextEnemy()
    {
        if (currentEnemy != null)
        {
            currentEnemy.gameObject.SetActive(false);
        }

        stepIndex++;
        Vector3 spawnPosition = GetSpawnPosition(stepIndex);
        GameObject enemyObject = Instantiate(enemyTemplate, spawnPosition, startRotation);
        enemyObject.name = "Enemy_" + steps[stepIndex].name.Replace(" ", "_");
        enemyObject.SetActive(true);

        currentEnemy = enemyObject.GetComponent<EnemyController>();
        ConfigureEnemy(currentEnemy, steps[stepIndex], spawnPosition);
        player.enemy = currentEnemy;
        waitingNext = false;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        Vector3 offset = Vector3.zero;
        if (index == 1)
        {
            offset = Vector3.right * 3f;
        }
        else if (index == 2)
        {
            offset = Vector3.left * 3f + Vector3.forward * 2f;
        }
        else if (index == 3)
        {
            offset = Vector3.forward * 5f;
        }

        NavMeshHit hit;
        Vector3 position = startPosition + offset;
        if (NavMesh.SamplePosition(position, out hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return startPosition;
    }

    private void ConfigureEnemy(EnemyController enemy, EncounterStep step, Vector3 position)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.transform.position = position;
        enemy.transform.rotation = startRotation;
        enemy.SetMainGameStage(step.difficulty, step.visualType, step.hp, step.attackDamage, step.attackCD, step.attackWindup, step.comboCount, step.moveSpeed, step.detectRange, step.attackRange, step.returnRange);
        enemy.target = player;
    }

    private void UpdateBossPhase()
    {
        if (MainGameProgress.CurrentLevel != 4 || bossPhaseChanged || currentEnemy == null || currentEnemy.isDead)
        {
            return;
        }

        if (currentEnemy.HP > currentEnemy.maxHP * 0.5f)
        {
            return;
        }

        bossPhaseChanged = true;
        currentEnemy.SetMainGameCombatStats(2, 1.8f, 0.18f, 3, 4.4f, 14f, 2.65f, 16f);
        MainGameUIController.NotifyBossPhase();
    }
}

