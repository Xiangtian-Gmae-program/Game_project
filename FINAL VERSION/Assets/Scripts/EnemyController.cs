// Purpose: Controls enemy AI, difficulty values, combat actions, damage response, patrol, and main-game behavior.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;

public enum EnemyDifficulty
{
    Easy,
    Normal,
    Hard
}

public class EnemyController : PlayerController
{

    private enum EnemyState
    {
        Idle,
        Patrol,
        Alert,
        Chase,
        Strafe,
        Attack,
        Defend,
        Evade,
        Stagger,
        Return,
        Dead
    }

    private enum CombatIntent
    {
        Pressure,
        Circle,
        Attack,
        BackStep,
        Guard,
        Evade
    }

    public PlayerController target;
    public NavMeshAgent agent;
    public float attackCD;
    public float lastAttackTime;
    public EnemyDifficulty difficulty = EnemyDifficulty.Normal;
    public int attackDamage = 1;
    public float detectRange = 8f;
    public float attackRange = 2f;
    public float returnRange = 10f;
    public float attackWindup = 0.35f;
    public float comboInterval = 0.35f;
    public int attackComboCount = 1;
    public int maxHP = 20;
    public bool canPatrol = true;
    public float patrolRange = 4f;
    public float patrolWaitTime = 2f;
    public float hitStunTime = 0.35f;
    public float heavyHitStunTime = 0.65f;
    public EnemyVisualController.VisualType visualType = EnemyVisualController.VisualType.Auto;
    public bool useSavedDifficulty = true;
    public float minKeepDistance = 1.35f;
    public float strafeDistance = 2.8f;
    public float alertTime = 0.55f;
    public float attackRecoverTime = 0.65f;
    public float defendTime = 0.65f;
    public float defendCooldown = 3f;
    public float evadeTime = 0.35f;
    public float evadeCooldown = 4f;
    public float evadeSpeed = 5.2f;
    public float leapCooldown = 5.5f;
    public float leapSpeed = 6.2f;
    public float knockBackSpeed = 3.6f;

    public int easyHP = 12;
    public int normalHP = 20;
    public int hardHP = 35;
    public int easyAttackDamage = 1;
    public int normalAttackDamage = 1;
    public int hardAttackDamage = 2;
    public float easyAttackCD = 6f;
    public float normalAttackCD = 4f;
    public float hardAttackCD = 2.5f;
    public float easyAttackWindup = 0.55f;
    public float normalAttackWindup = 0.35f;
    public float hardAttackWindup = 0.25f;
    public float easyDetectRange = 6f;
    public float normalDetectRange = 8f;
    public float hardDetectRange = 11f;
    public float easyAttackRange = 1.8f;
    public float normalAttackRange = 2f;
    public float hardAttackRange = 2.3f;
    public float easyReturnRange = 8f;
    public float normalReturnRange = 10f;
    public float hardReturnRange = 13f;
    public int easyAttackComboCount = 1;
    public int normalAttackComboCount = 1;
    public int hardAttackComboCount = 2;
    public float easyMoveSpeed = 2.6f;
    public float normalMoveSpeed = 3.5f;
    public float hardMoveSpeed = 4.2f;
    private EnemyState enemyState;
    private Vector3 startPosition;
    private Vector3[] patrolPositions;
    private int patrolIndex;
    private float patrolWaitUntil;
    private bool returningStart;
    private Coroutine attackRoutine;
    private float stunnedUntil;
    private EnemyVisualController visualController;
    private float alertUntil;
    private float attackRecoverUntil;
    private float defendUntil;
    private float nextDefendTime;
    private float evadeUntil;
    private float nextEvadeTime;
    private float nextLeapTime;
    private float nextStrafeTime;
    private float lastSeenTargetTime;
    private float lastHitTakenTime;
    private int recentHitCount;
    private int strafeDirection = 1;
    private bool counterAfterDefend;
    private Vector3 evadeDirection;
    private float activeEvadeSpeed;
    private CombatIntent combatIntent = CombatIntent.Pressure;
    private float combatIntentUntil;
    private const float MinIntentTime = 0.65f;
    private const float MaxIntentTime = 1.25f;
    private const float AdaptiveChanceMax = 0.1f;
    private const float AdaptiveAttackCooldownReductionMax = 0.12f;
    private const float BaseAttackCooldownReduction = 0.18f;
    private const float AttackPostImpactHold = 0.18f;

    public override void Start()
    {
        base.Start();
        startPosition = transform.position;
        agent = GetComponent<NavMeshAgent>();
        if (SceneManager.GetActiveScene().name == "maingame")
        {
            ApplyDifficulty();
            ApplyAIProfile();
            BuildVisual();
        }
        BuildPatrolPositions();
        SetState(EnemyState.Idle);

    }

    public new void TakeDamage(int attackDamage)
    {
        if (SceneManager.GetActiveScene().name != "DockThing")
        {
            if (SceneManager.GetActiveScene().name == "maingame")
            {
                if (Time.time < evadeUntil)
                {
                    MainGameUIController.NotifyEnemyEvade();
                    return;
                }

                if (Time.time < defendUntil)
                {
                    MainGameUIController.NotifyEnemyDefend();
                    if (attackDamage <= 2)
                    {
                        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
                        {
                            counterAfterDefend = true;
                            attackRecoverUntil = 0f;
                        }
                        return;
                    }

                    attackDamage = Mathf.Max(1, attackDamage / 2);
                }
            }

            base.TakeDamage(attackDamage);
            if (SceneManager.GetActiveScene().name == "maingame" && visualController != null)
            {
                if (isDead)
                {
                    visualController.PlayDead();
                }
                else
                {
                    visualController.PlayHit();
                }
            }
            if (SceneManager.GetActiveScene().name == "maingame" && !isDead)
            {
                RegisterHitTaken();
                float stun = attackDamage >= 4 ? heavyHitStunTime : hitStunTime;
                if (visualType == EnemyVisualController.VisualType.GuoJingBoss && attackDamage < 4)
                {
                    stun *= 0.35f;
                }
                Stagger(stun);
            }
            return;
        }

        HP -= attackDamage;
        if (HP <= 0)
        {
            HP = 1;
        }

        isDead = false;
        PracticeStatsController.RecordHit(attackDamage);
        animator.SetTrigger("Hit");
        animator.SetBool("Dead", false);
        bloodPS.Play();
    }

    private void Update()
    {
        if (isDead)
        {
            SetState(EnemyState.Dead);
            StopMove();
            return;
        }

        if (target == null)
        {
            SetState(EnemyState.Idle);
            StopMove();
            return;
        }

        if (target.isDead)
        {
            SetState(EnemyState.Idle);
            StopMove();
            return;
        }

        if (Time.time < evadeUntil)
        {
            SetState(EnemyState.Evade);
            MoveInDirection(evadeDirection, activeEvadeSpeed);
            LookAtTarget();
            return;
        }

        if (Time.time < defendUntil)
        {
            SetState(EnemyState.Defend);
            StopMove();
            LookAtTarget();
            return;
        }

        if (Time.time < stunnedUntil)
        {
            SetState(EnemyState.Stagger);
            MoveInDirection((transform.position - target.transform.position).normalized, knockBackSpeed);
            LookAtTarget();
            return;
        }

        float dis = Vector3.Distance(target.transform.position, transform.position);
        float startDis = Vector3.Distance(startPosition, transform.position);
        if (returningStart || startDis > returnRange)
        {
            ReturnStartPosition();
        }
        else if (dis <= detectRange)
        {
            returningStart = false;
            if (Time.time - lastSeenTargetTime > 0.2f)
            {
                alertUntil = Time.time + alertTime;
            }
            lastSeenTargetTime = Time.time;
            CombatUpdate(dis);
        }
        else if (canPatrol)
        {
            Patrol();
        }
        else
        {
            SetState(EnemyState.Idle);
            StopMove();
        }
    }

    private void CombatUpdate(float dis)
    {
        if (Time.time < alertUntil)
        {
            SetState(EnemyState.Alert);
            StopMove();
            LookAtTarget();
            return;
        }

        if (attackRoutine != null)
        {
            return;
        }

        if (dis <= minKeepDistance * 0.95f)
        {
            ResolveCloseRangePressure(dis);
            return;
        }

        if (counterAfterDefend && dis <= attackRange + 0.35f && Time.time >= attackRecoverUntil)
        {
            counterAfterDefend = false;
            StopMove();
            SetState(EnemyState.Attack);
            attackRoutine = StartCoroutine(AttackRoutine());
            return;
        }

        if (Time.time >= combatIntentUntil)
        {
            ChooseCombatIntent(dis);
        }

        ExecuteCombatIntent(dis);
    }

    private void AttackTarget()
    {
        LookAtTarget();
        StopMove();
        SetState(EnemyState.Attack);
        if (CanAttackNow())
        {
            attackRoutine = StartCoroutine(AttackRoutine());
        }
    }

    private void ChooseCombatIntent(float dis)
    {
        combatIntentUntil = Time.time + UnityEngine.Random.Range(MinIntentTime, MaxIntentTime);

        if (dis < minKeepDistance)
        {
            combatIntent = CombatIntent.BackStep;
            return;
        }

        bool playerPressure = Input.GetMouseButton(0) || recentHitCount >= 2;
        bool playerHeavyTell = Input.GetMouseButton(0) && Time.time - lastHitTakenTime > 0.25f;

        if (difficulty == EnemyDifficulty.Hard && dis <= attackRange && CanAttackNow())
        {
            combatIntent = CombatIntent.Attack;
            return;
        }

        if (playerPressure && CanDefend() && Time.time >= nextDefendTime && dis <= attackRange + 0.9f && UnityEngine.Random.value < GetDefendChance())
        {
            combatIntent = CombatIntent.Guard;
            return;
        }

        if (playerHeavyTell && CanEvade() && Time.time >= nextEvadeTime && dis <= attackRange + 1.8f && UnityEngine.Random.value < GetEvadeChance())
        {
            combatIntent = CombatIntent.Evade;
            return;
        }

        if (CanLeap() && Time.time >= nextLeapTime && dis > attackRange + 1f && dis <= detectRange && UnityEngine.Random.value < GetLeapChance())
        {
            combatIntent = CombatIntent.Evade;
            return;
        }

        if (dis <= attackRange)
        {
            if (CanAttackNow())
            {
                combatIntent = CombatIntent.Attack;
                return;
            }

            combatIntent = difficulty == EnemyDifficulty.Hard ? CombatIntent.Circle : CombatIntent.BackStep;
            return;
        }

        if (dis <= strafeDistance && CanStrafe())
        {
            combatIntent = ShouldStrafe() ? CombatIntent.Circle : CombatIntent.Pressure;
            return;
        }

        combatIntent = CombatIntent.Pressure;
    }

    private void ExecuteCombatIntent(float dis)
    {
        if (combatIntent == CombatIntent.Guard)
        {
            StartDefend(false);
            combatIntentUntil = Time.time;
            return;
        }

        if (combatIntent == CombatIntent.Evade)
        {
            if (dis > attackRange + 1f && CanLeap() && Time.time >= nextLeapTime)
            {
                Vector3 leapDirection = (target.transform.position - transform.position).normalized;
                StartEvade(leapDirection, evadeTime * 1.2f, leapSpeed, true);
                nextLeapTime = Time.time + leapCooldown;
            }
            else
            {
                Vector3 side = Vector3.Cross(Vector3.up, (target.transform.position - transform.position).normalized).normalized;
                StartEvade(side * (UnityEngine.Random.value > 0.5f ? 1f : -1f), evadeTime, evadeSpeed, false);
            }
            combatIntentUntil = Time.time;
            return;
        }

        if (combatIntent == CombatIntent.Attack)
        {
            if (dis <= attackRange)
            {
                if (CanAttackNow())
                {
                    AttackTarget();
                }
                else if (difficulty == EnemyDifficulty.Hard)
                {
                    HardPressureTarget(dis);
                }
                else if (CanStrafe())
                {
                    StrafeTarget();
                }
                else
                {
                    BackStepTarget(dis);
                }
            }
            else
            {
                ChaseTarget();
            }
            return;
        }

        if (combatIntent == CombatIntent.BackStep)
        {
            BackStepTarget(dis);
            return;
        }

        if (combatIntent == CombatIntent.Circle)
        {
            if (dis > strafeDistance + 0.6f)
            {
                PressureTarget(dis);
                return;
            }

            if (dis <= attackRange && difficulty == EnemyDifficulty.Hard && CanAttackNow())
            {
                AttackTarget();
                return;
            }

            StrafeTarget();
            return;
        }

        PressureTarget(dis);
    }

    private void ResolveCloseRangePressure(float dis)
    {
        LookAtTarget();
        if (CanAttackNow())
        {
            if (difficulty == EnemyDifficulty.Hard || visualType == EnemyVisualController.VisualType.GuoJingBoss || UnityEngine.Random.value < 0.55f)
            {
                AttackTarget();
                return;
            }
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            HardPressureTarget(dis);
            return;
        }

        if (CanStrafe() && UnityEngine.Random.value < 0.45f)
        {
            StrafeTarget();
            return;
        }

        BackStepTarget(dis);
    }

    private void PressureTarget(float dis)
    {
        LookAtTarget();
        if (dis <= attackRange)
        {
            if (CanAttackNow())
            {
                AttackTarget();
            }
            else if (difficulty == EnemyDifficulty.Hard)
            {
                HardPressureTarget(dis);
            }
            else if (CanStrafe())
            {
                StrafeTarget();
            }
            else
            {
                BackStepTarget(dis);
            }
            return;
        }

        if (dis <= strafeDistance && CanStrafe())
        {
            Vector3 toTarget = (target.transform.position - transform.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
            MoveInDirection((toTarget * 0.75f + side * 0.25f).normalized, GetMoveSpeed());
            SetState(EnemyState.Chase);
            return;
        }

        ChaseTarget();
    }

    private void BackStepTarget(float dis)
    {
        LookAtTarget();
        Vector3 away = (transform.position - target.transform.position).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, (target.transform.position - transform.position).normalized).normalized * strafeDirection;
        Vector3 direction = dis < minKeepDistance ? (away * 0.75f + side * 0.25f).normalized : (away * 0.45f + side * 0.55f).normalized;
        SetState(EnemyState.Strafe);
        MoveInDirection(direction, GetMoveSpeed() * 0.82f);
    }

    private void HardPressureTarget(float dis)
    {
        if (CanAttackNow())
        {
            AttackTarget();
            return;
        }

        LookAtTarget();
        if (Time.time >= nextStrafeTime)
        {
            strafeDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
            nextStrafeTime = Time.time + UnityEngine.Random.Range(0.55f, 1.05f);
        }

        Vector3 toTarget = (target.transform.position - transform.position).normalized;
        Vector3 away = (transform.position - target.transform.position).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
        Vector3 pressureDirection = side;
        if (dis < minKeepDistance * 0.85f)
        {
            pressureDirection = (away * 0.65f + side * 0.35f).normalized;
        }
        else
        {
            pressureDirection = (toTarget * 0.35f + side * 0.65f).normalized;
        }

        SetState(EnemyState.Strafe);
        MoveInDirection(pressureDirection, GetMoveSpeed() * 0.8f);
    }

    private bool CanAttackNow()
    {
        return Time.time >= attackRecoverUntil && Time.time - lastAttackTime >= GetEffectiveAttackCD();
    }

    private void BuildVisual()
    {
        visualController = GetComponent<EnemyVisualController>();
        if (visualController == null)
        {
            visualController = gameObject.AddComponent<EnemyVisualController>();
        }

        visualController.Build(GetVisualType());
    }

    public void SetMainGameStage(EnemyDifficulty stageDifficulty, EnemyVisualController.VisualType stageVisualType, int stageHP, int stageAttackDamage, float stageAttackCD, float stageAttackWindup, int stageComboCount, float stageMoveSpeed, float stageDetectRange, float stageAttackRange, float stageReturnRange)
    {
        useSavedDifficulty = false;
        difficulty = stageDifficulty;
        visualType = stageVisualType;
        easyHP = stageHP;
        normalHP = stageHP;
        hardHP = stageHP;
        easyAttackDamage = stageAttackDamage;
        normalAttackDamage = stageAttackDamage;
        hardAttackDamage = stageAttackDamage;
        easyAttackCD = stageAttackCD;
        normalAttackCD = stageAttackCD;
        hardAttackCD = stageAttackCD;
        easyAttackWindup = stageAttackWindup;
        normalAttackWindup = stageAttackWindup;
        hardAttackWindup = stageAttackWindup;
        easyAttackComboCount = stageComboCount;
        normalAttackComboCount = stageComboCount;
        hardAttackComboCount = stageComboCount;
        easyMoveSpeed = stageMoveSpeed;
        normalMoveSpeed = stageMoveSpeed;
        hardMoveSpeed = stageMoveSpeed;
        easyDetectRange = stageDetectRange;
        normalDetectRange = stageDetectRange;
        hardDetectRange = stageDetectRange;
        easyAttackRange = stageAttackRange;
        normalAttackRange = stageAttackRange;
        hardAttackRange = stageAttackRange;
        easyReturnRange = stageReturnRange;
        normalReturnRange = stageReturnRange;
        hardReturnRange = stageReturnRange;
        HP = stageHP;
        maxHP = stageHP;
        attackDamage = stageAttackDamage;
        attackCD = stageAttackCD;
        attackWindup = stageAttackWindup;
        attackComboCount = stageComboCount;
        detectRange = stageDetectRange;
        attackRange = stageAttackRange;
        returnRange = stageReturnRange;
        isDead = false;
        lastAttackTime = 0f;
        if (agent != null)
        {
            agent.speed = stageMoveSpeed;
        }

        ApplyAIProfile();
        ResetAITimers();
        if (visualController != null)
        {
            visualController.Build(GetVisualType());
        }
    }

    public void SetMainGameCombatStats(int stageAttackDamage, float stageAttackCD, float stageAttackWindup, int stageComboCount, float stageMoveSpeed, float stageDetectRange, float stageAttackRange, float stageReturnRange)
    {
        attackDamage = stageAttackDamage;
        attackCD = stageAttackCD;
        attackWindup = stageAttackWindup;
        attackComboCount = stageComboCount;
        detectRange = stageDetectRange;
        attackRange = stageAttackRange;
        returnRange = stageReturnRange;
        easyAttackDamage = stageAttackDamage;
        normalAttackDamage = stageAttackDamage;
        hardAttackDamage = stageAttackDamage;
        easyAttackCD = stageAttackCD;
        normalAttackCD = stageAttackCD;
        hardAttackCD = stageAttackCD;
        easyAttackWindup = stageAttackWindup;
        normalAttackWindup = stageAttackWindup;
        hardAttackWindup = stageAttackWindup;
        easyAttackComboCount = stageComboCount;
        normalAttackComboCount = stageComboCount;
        hardAttackComboCount = stageComboCount;
        easyMoveSpeed = stageMoveSpeed;
        normalMoveSpeed = stageMoveSpeed;
        hardMoveSpeed = stageMoveSpeed;
        easyDetectRange = stageDetectRange;
        normalDetectRange = stageDetectRange;
        hardDetectRange = stageDetectRange;
        easyAttackRange = stageAttackRange;
        normalAttackRange = stageAttackRange;
        hardAttackRange = stageAttackRange;
        easyReturnRange = stageReturnRange;
        normalReturnRange = stageReturnRange;
        hardReturnRange = stageReturnRange;
        if (agent != null)
        {
            agent.speed = stageMoveSpeed;
        }
        ApplyAIProfile();
    }

    private void ApplyAIProfile()
    {
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
        {
            minKeepDistance = 1.55f;
            strafeDistance = 3.1f;
            alertTime = 0.35f;
            attackRecoverTime = 0.45f;
            defendTime = 0.75f;
            defendCooldown = 2.2f;
            evadeTime = 0.38f;
            evadeCooldown = 2.8f;
            leapCooldown = 4.2f;
            return;
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            minKeepDistance = 1.45f;
            strafeDistance = 2.9f;
            alertTime = 0.35f;
            attackRecoverTime = 0.5f;
            defendTime = 0.55f;
            defendCooldown = 2.8f;
            evadeTime = 0.32f;
            evadeCooldown = 3.2f;
            leapCooldown = 5f;
            return;
        }

        if (difficulty == EnemyDifficulty.Normal)
        {
            minKeepDistance = 1.35f;
            strafeDistance = 2.6f;
            alertTime = 0.55f;
            attackRecoverTime = 0.65f;
            defendTime = 0.65f;
            defendCooldown = 3.5f;
            evadeCooldown = 4.8f;
            return;
        }

        minKeepDistance = 1.25f;
        strafeDistance = 2.4f;
        alertTime = 0.7f;
        attackRecoverTime = 0.85f;
        defendCooldown = 4.5f;
        evadeCooldown = 6f;
    }

    private void ResetAITimers()
    {
        alertUntil = 0f;
        attackRecoverUntil = 0f;
        defendUntil = 0f;
        evadeUntil = 0f;
        nextDefendTime = 0f;
        nextEvadeTime = 0f;
        nextLeapTime = 0f;
        recentHitCount = 0;
        counterAfterDefend = false;
        combatIntent = CombatIntent.Pressure;
        combatIntentUntil = 0f;
    }

    private EnemyVisualController.VisualType GetVisualType()
    {
        if (visualType != EnemyVisualController.VisualType.Auto)
        {
            return visualType;
        }

        if (difficulty == EnemyDifficulty.Easy)
        {
            return EnemyVisualController.VisualType.ArashiNormal;
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            return EnemyVisualController.VisualType.ArashiElite;
        }

        return EnemyVisualController.VisualType.ArashiGuard;
    }

    private void ApplyDifficulty()
    {
        if (useSavedDifficulty)
        {
            int savedDifficulty = PlayerPrefs.GetInt("MainGameDifficulty", (int)difficulty);
            difficulty = (EnemyDifficulty)Mathf.Clamp(savedDifficulty, 0, 2);
        }

        if (difficulty == EnemyDifficulty.Easy)
        {
            HP = easyHP;
            maxHP = easyHP;
            attackDamage = easyAttackDamage;
            attackCD = easyAttackCD;
            attackWindup = easyAttackWindup;
            detectRange = easyDetectRange;
            attackRange = easyAttackRange;
            returnRange = easyReturnRange;
            attackComboCount = easyAttackComboCount;
            if (agent != null)
            {
                agent.speed = easyMoveSpeed;
            }
        }
        else if (difficulty == EnemyDifficulty.Hard)
        {
            HP = hardHP;
            maxHP = hardHP;
            attackDamage = hardAttackDamage;
            attackCD = hardAttackCD;
            attackWindup = hardAttackWindup;
            detectRange = hardDetectRange;
            attackRange = hardAttackRange;
            returnRange = hardReturnRange;
            attackComboCount = hardAttackComboCount;
            if (agent != null)
            {
                agent.speed = hardMoveSpeed;
            }
        }
        else
        {
            HP = normalHP;
            maxHP = normalHP;
            attackDamage = normalAttackDamage;
            attackCD = normalAttackCD;
            attackWindup = normalAttackWindup;
            detectRange = normalDetectRange;
            attackRange = normalAttackRange;
            returnRange = normalReturnRange;
            attackComboCount = normalAttackComboCount;
            if (agent != null)
            {
                agent.speed = normalMoveSpeed;
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        lastAttackTime = Time.time;
        int combo = GetComboCount();
        for (int i = 0; i < combo; i++)
        {
            if (isDead || target == null || target.isDead || Vector3.Distance(target.transform.position, transform.position) > attackRange + 0.45f)
            {
                break;
            }

            LookAtTarget();
            MainGameUIController.NotifyIncomingAttack(attackWindup + 0.45f);
            animator.SetTrigger("Attack");
            if (visualController != null)
            {
                visualController.PlayAttack();
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, attackWindup * 0.72f));
            LookAtTarget();
            MainGameSfxController.PlaySwordSwing();
            yield return new WaitForSeconds(Mathf.Max(0.02f, attackWindup * 0.28f));
            if (!isDead && target != null && !target.isDead && Vector3.Distance(target.transform.position, transform.position) <= attackRange)
            {
                target.TakeDamage(attackDamage);
            }

            yield return new WaitForSeconds(AttackPostImpactHold);
            if (i < combo - 1)
            {
                yield return new WaitForSeconds(comboInterval);
            }
        }
        if (visualController != null)
        {
            visualController.PlayIdle();
        }
        attackRecoverUntil = Time.time + attackRecoverTime;
        attackRoutine = null;
        if (counterAfterDefend && !isDead && target != null && !target.isDead && Vector3.Distance(target.transform.position, transform.position) <= attackRange + 0.35f)
        {
            counterAfterDefend = false;
            attackRoutine = StartCoroutine(AttackRoutine());
        }
        else
        {
            counterAfterDefend = false;
        }
    }

    private int GetComboCount()
    {
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss && HP <= maxHP * 0.5f)
        {
            return Mathf.Max(attackComboCount, 3);
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            return Mathf.Max(attackComboCount, 2);
        }

        if (difficulty == EnemyDifficulty.Easy)
        {
            return 1;
        }

        return attackComboCount;
    }

    private void RegisterHitTaken()
    {
        if (Time.time - lastHitTakenTime <= 1.4f)
        {
            recentHitCount++;
        }
        else
        {
            recentHitCount = 1;
        }

        lastHitTakenTime = Time.time;
        if (recentHitCount >= 3)
        {
            stunnedUntil += 0.2f;
        }
    }

    private void Stagger(float stunTime)
    {
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunTime);
        lastAttackTime = Time.time;
        defendUntil = 0f;
        evadeUntil = 0f;
        counterAfterDefend = false;
        combatIntentUntil = 0f;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        StopMove();
        SetState(EnemyState.Stagger);
        MainGameUIController.NotifyEnemyStagger(stunTime);
    }

    private bool TryStartDefend(float dis)
    {
        if (!CanDefend() || Time.time < nextDefendTime || dis > attackRange + 0.9f)
        {
            return false;
        }

        bool playerPressure = Input.GetMouseButton(0) || recentHitCount >= 2;
        if (!playerPressure)
        {
            return false;
        }

        float chance = GetDefendChance();
        if (UnityEngine.Random.value > chance)
        {
            nextDefendTime = Time.time + defendCooldown * 0.5f;
            return false;
        }

        StartDefend(false);
        return true;
    }

    private void StartDefend(bool counter)
    {
        counterAfterDefend = counter;
        defendUntil = Time.time + defendTime;
        nextDefendTime = Time.time + defendCooldown;
        StopMove();
        SetState(EnemyState.Defend);
        if (visualController != null)
        {
            visualController.PlayDefend();
        }
        MainGameUIController.NotifyEnemyDefend();
    }

    private bool TryStartEvade(float dis)
    {
        if (!CanEvade() || Time.time < nextEvadeTime || dis > attackRange + 1.8f)
        {
            return false;
        }

        bool playerAttackTell = Input.GetMouseButton(0);
        bool lowHp = HP <= maxHP * 0.35f;
        if (!playerAttackTell && !lowHp)
        {
            return false;
        }

        float chance = GetEvadeChance();
        if (lowHp)
        {
            chance += 0.2f;
        }

        if (UnityEngine.Random.value > chance)
        {
            nextEvadeTime = Time.time + evadeCooldown * 0.5f;
            return false;
        }

        Vector3 side = Vector3.Cross(Vector3.up, (target.transform.position - transform.position).normalized).normalized;
        StartEvade(side * (UnityEngine.Random.value > 0.5f ? 1f : -1f), evadeTime, evadeSpeed, false);
        return true;
    }

    private bool TryStartLeap(float dis)
    {
        if (!CanLeap() || Time.time < nextLeapTime || dis <= attackRange + 1f || dis > detectRange)
        {
            return false;
        }

        if (UnityEngine.Random.value > GetLeapChance())
        {
            return false;
        }

        Vector3 direction = (target.transform.position - transform.position).normalized;
        StartEvade(direction, evadeTime * 1.2f, leapSpeed, true);
        nextLeapTime = Time.time + leapCooldown;
        return true;
    }

    private void StartEvade(Vector3 direction, float duration, float speed, bool leap)
    {
        evadeDirection = direction;
        if (evadeDirection.sqrMagnitude <= 0.001f)
        {
            evadeDirection = -transform.forward;
        }

        evadeDirection.y = 0;
        evadeDirection.Normalize();
        evadeUntil = Time.time + duration;
        nextEvadeTime = Time.time + evadeCooldown;
        activeEvadeSpeed = speed;
        StopMove();
        SetState(EnemyState.Evade);
        if (visualController != null)
        {
            if (leap)
            {
                visualController.PlayLeap();
            }
            else
            {
                visualController.PlayEvade(transform.InverseTransformDirection(evadeDirection));
            }
        }
        MainGameUIController.NotifyEnemyEvade();
    }

    private bool CanStrafe()
    {
        return difficulty != EnemyDifficulty.Easy || visualType == EnemyVisualController.VisualType.GuoJingBoss;
    }

    private bool ShouldStrafe()
    {
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
        {
            return UnityEngine.Random.value < 0.3f;
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            return UnityEngine.Random.value < 0.18f;
        }

        return UnityEngine.Random.value < 0.45f;
    }

    private bool CanDefend()
    {
        return difficulty != EnemyDifficulty.Easy || visualType == EnemyVisualController.VisualType.GuoJingBoss;
    }

    private bool CanEvade()
    {
        return difficulty == EnemyDifficulty.Hard || visualType == EnemyVisualController.VisualType.GuoJingBoss;
    }

    private bool CanLeap()
    {
        return visualType == EnemyVisualController.VisualType.GuoJingBoss || difficulty == EnemyDifficulty.Hard;
    }

    private float GetDefendChance()
    {
        float adaptiveBonus = PlayerActionProfile.ChanceBonus(PlayerActionProfile.AttackPreference(), AdaptiveChanceMax);
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
        {
            return Mathf.Clamp(0.35f + adaptiveBonus, 0.05f, 0.45f);
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            return Mathf.Clamp(0.24f + adaptiveBonus, 0.05f, 0.34f);
        }

        return Mathf.Clamp(0.16f + adaptiveBonus, 0.03f, 0.26f);
    }

    private float GetEvadeChance()
    {
        float adaptiveBonus = PlayerActionProfile.ChanceBonus(PlayerActionProfile.HeavyAttackPreference(), AdaptiveChanceMax);
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
        {
            return Mathf.Clamp(0.42f + adaptiveBonus, 0.1f, 0.52f);
        }

        return Mathf.Clamp(0.3f + adaptiveBonus, 0.08f, 0.4f);
    }

    private float GetLeapChance()
    {
        float adaptiveBonus = PlayerActionProfile.ChanceBonus(PlayerActionProfile.DefendPreference() + PlayerActionProfile.EvadePreference() * 0.5f, AdaptiveChanceMax);
        if (visualType == EnemyVisualController.VisualType.GuoJingBoss)
        {
            return Mathf.Clamp(0.32f + adaptiveBonus, 0.08f, 0.45f);
        }

        return Mathf.Clamp(0.24f + adaptiveBonus, 0.05f, 0.36f);
    }

    private float GetEffectiveAttackCD()
    {
        float adaptiveReduction = PlayerActionProfile.ChanceBonus(PlayerActionProfile.DefendPreference(), AdaptiveAttackCooldownReductionMax);
        float multiplier = 1f - BaseAttackCooldownReduction - adaptiveReduction;
        return attackCD * Mathf.Clamp(multiplier, 0.78f, 1f);
    }

    private void StrafeTarget()
    {
        SetState(EnemyState.Strafe);
        LookAtTarget();
        if (Time.time >= nextStrafeTime)
        {
            strafeDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
            nextStrafeTime = Time.time + UnityEngine.Random.Range(0.9f, 1.6f);
        }

        Vector3 toTarget = (target.transform.position - transform.position).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
        Vector3 desired = target.transform.position - toTarget * strafeDistance + side * 1.4f;
        MoveToPosition(desired);
    }

    private void MoveInDirection(Vector3 direction, float moveSpeed)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            StopMove();
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.Move(direction.normalized * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void MoveToPosition(Vector3 position)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 1.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                agent.isStopped = false;
            }
        }
    }

    private float GetMoveSpeed()
    {
        if (agent != null)
        {
            return agent.speed;
        }

        if (difficulty == EnemyDifficulty.Hard)
        {
            return hardMoveSpeed;
        }

        if (difficulty == EnemyDifficulty.Easy)
        {
            return easyMoveSpeed;
        }

        return normalMoveSpeed;
    }

    private void ChaseTarget()
    {
        if (canMove)
        {
            SetState(EnemyState.Chase);
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(target.transform.position);
                agent.isStopped = false;
            }
        }
        else
        {
            StopMove();
        }
    }

    private void ReturnStartPosition()
    {
        SetState(EnemyState.Return);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(startPosition);
            agent.isStopped = false;
        }

        if (Vector3.Distance(startPosition, transform.position) <= 0.4f)
        {
            returningStart = false;
            patrolIndex = 0;
            patrolWaitUntil = Time.time + patrolWaitTime;
        }
    }

    private void Patrol()
    {
        if (patrolPositions == null || patrolPositions.Length == 0 || !canMove)
        {
            StopMove();
            SetState(EnemyState.Idle);
            return;
        }

        if (Time.time < patrolWaitUntil)
        {
            StopMove();
            SetState(EnemyState.Idle);
            return;
        }

        SetState(EnemyState.Patrol);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(patrolPositions[patrolIndex]);
            agent.isStopped = false;
        }

        if (Vector3.Distance(patrolPositions[patrolIndex], transform.position) <= 0.5f)
        {
            patrolIndex++;
            if (patrolIndex >= patrolPositions.Length)
            {
                patrolIndex = 0;
            }
            patrolWaitUntil = Time.time + patrolWaitTime;
        }
    }

    private void BuildPatrolPositions()
    {
        patrolPositions = new Vector3[5];
        patrolPositions[0] = startPosition;
        patrolPositions[1] = GetNavMeshPosition(startPosition + transform.right * patrolRange);
        patrolPositions[2] = GetNavMeshPosition(startPosition + transform.forward * patrolRange);
        patrolPositions[3] = GetNavMeshPosition(startPosition - transform.right * patrolRange);
        patrolPositions[4] = GetNavMeshPosition(startPosition - transform.forward * patrolRange);
    }

    private Vector3 GetNavMeshPosition(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return startPosition;
    }

    private void StopMove()
    {
        animator.SetFloat("MoveY", 0);
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    private void LookAtTarget()
    {
        transform.LookAt(new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z));
    }

    private void SetState(EnemyState state)
    {
        enemyState = state;
        if (visualController != null)
        {
            if (enemyState == EnemyState.Chase || enemyState == EnemyState.Patrol || enemyState == EnemyState.Strafe || enemyState == EnemyState.Return || enemyState == EnemyState.Evade)
            {
                visualController.SetMoveDirection(transform.InverseTransformDirection(GetVisualMoveDirection()));
            }
            else
            {
                visualController.SetMoving(false);
            }
        }

        if (enemyState == EnemyState.Chase || enemyState == EnemyState.Patrol || enemyState == EnemyState.Strafe || enemyState == EnemyState.Return || enemyState == EnemyState.Evade)
        {
            animator.SetFloat("MoveState", 1);
            animator.SetFloat("MoveY", 1);
        }
        else
        {
            animator.SetFloat("MoveY", 0);
        }
    }

    private Vector3 GetVisualMoveDirection()
    {
        if (enemyState == EnemyState.Evade)
        {
            return evadeDirection;
        }

        if (enemyState == EnemyState.Strafe && target != null)
        {
            Vector3 toTarget = (target.transform.position - transform.position).normalized;
            if (Vector3.Distance(target.transform.position, transform.position) < minKeepDistance)
            {
                return (transform.position - target.transform.position).normalized;
            }

            return Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
        }

        if (enemyState == EnemyState.Return)
        {
            return (startPosition - transform.position).normalized;
        }

        if (enemyState == EnemyState.Patrol && patrolPositions != null && patrolPositions.Length > 0)
        {
            return (patrolPositions[patrolIndex] - transform.position).normalized;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            if (agent.desiredVelocity.sqrMagnitude > 0.01f)
            {
                return agent.desiredVelocity.normalized;
            }

            if (agent.velocity.sqrMagnitude > 0.01f)
            {
                return agent.velocity.normalized;
            }
        }

        if (target != null)
        {
            return (target.transform.position - transform.position).normalized;
        }

        return transform.forward;
    }
}

