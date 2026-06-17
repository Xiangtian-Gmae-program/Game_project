// Purpose: Connects enemy AI states to runtime visual models and compatible Animator states.
using UnityEngine;

public class EnemyVisualController : MonoBehaviour
{

    public enum VisualType
    {
        Auto,
        ArashiNormal,
        ArashiGuard,
        ArashiElite,
        GuoJingBoss
    }

    private Animator visualAnimator;
    private Transform visualRoot;
    private VisualType currentVisualType;
    private bool moving;
    private bool dead;
    private string currentStateName;
    private float actionLockUntil;
    private float hitLockUntil;
    private float hitVisualUntil;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseLocalRotation;
    private const float HitFeedbackTime = 0.22f;
    private const float AttackVisualLockTime = 0.65f;
    private const float EvadeVisualLockTime = 0.32f;
    private const float LeapVisualLockTime = 0.42f;

    public void Build(VisualType visualType)
    {
        string path = GetResourcePath(visualType);
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            return;
        }

        RemoveCurrentVisual();
        HideOriginalRenderers();
        currentVisualType = visualType;

        GameObject visual = Instantiate(prefab, transform);
        visual.name = "EnemyVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visualRoot = visual.transform;
        visualBaseLocalPosition = visualRoot.localPosition;
        visualBaseLocalRotation = visualRoot.localRotation;
        DisableVisualGameplayComponents(visual);

        visualAnimator = GetAnimatorWithController(visual);
        if (visualAnimator != null)
        {
            visualAnimator.applyRootMotion = false;
        }
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (Time.time < hitVisualUntil)
        {
            float remain = Mathf.Clamp01((hitVisualUntil - Time.time) / HitFeedbackTime);
            float amount = Mathf.SmoothStep(0f, 1f, remain);
            visualRoot.localPosition = visualBaseLocalPosition + Vector3.back * 0.16f * amount;
            visualRoot.localRotation = visualBaseLocalRotation * Quaternion.Euler(-5f * amount, 0f, 0f);
            return;
        }

        visualRoot.localPosition = visualBaseLocalPosition;
        visualRoot.localRotation = visualBaseLocalRotation;
    }

    private void RemoveCurrentVisual()
    {
        Transform oldVisual = transform.Find("EnemyVisual");
        if (oldVisual == null)
        {
            return;
        }

        Destroy(oldVisual.gameObject);
        visualAnimator = null;
        visualRoot = null;
        moving = false;
        dead = false;
        currentStateName = null;
        actionLockUntil = 0f;
        hitLockUntil = 0f;
        hitVisualUntil = 0f;
    }

    public void SetMoving(bool value)
    {
        if (dead || visualAnimator == null || IsActionLocked() || moving == value)
        {
            return;
        }

        moving = value;
        SetBoolIfExists("front", moving);
        SetBoolIfExists("back", false);
        SetFloatIfExists("inputH", 0f);
        SetFloatIfExists("inputV", moving ? 1f : 0f);
        SetBoolIfExists("run", moving);
        if (moving)
        {
            CrossFadeFirstExisting("run_front", "run", "Run_Weapon", "Run_nonWeapon", "walk", "Wak_Weapon", "Walk_nonWeapon");
        }
        else
        {
            SetFloatIfExists("DefendFloat", -1f);
            SetBoolIfExists("Defend", false);
            CrossFadeFirstExisting("Idle", "Idle_Weapon", "Idle_nonWeapon", "idle");
        }
    }

    public void SetMoveDirection(Vector3 localDirection)
    {
        if (dead || visualAnimator == null || IsActionLocked())
        {
            return;
        }

        localDirection.y = 0f;
        if (localDirection.sqrMagnitude > 1f)
        {
            localDirection.Normalize();
        }

        moving = true;
        SetBoolIfExists("front", localDirection.z >= -0.25f);
        SetBoolIfExists("back", localDirection.z < -0.25f);
        SetFloatIfExists("MoveX", localDirection.x);
        SetFloatIfExists("MoveY", localDirection.z);
        SetFloatIfExists("MoveState", 1f);
        SetFloatIfExists("inputH", localDirection.x);
        SetFloatIfExists("inputV", Mathf.Abs(localDirection.z) > 0.2f ? localDirection.z : 1f);
        SetBoolIfExists("run", true);

        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z) && Mathf.Abs(localDirection.x) > 0.35f)
        {
            if (localDirection.x < 0f)
            {
                CrossFadeFirstExisting("Strafe_L", "Crouch_Move_L", "Crouch_move_L", "Run_Left", "Run_L", "run_front", "run", "Run_Weapon", "Run_nonWeapon");
            }
            else
            {
                CrossFadeFirstExisting("Strafe_R", "Crouch_Move_R", "Crouch_move_R", "Run_Right", "Run_R", "run_front", "run", "Run_Weapon", "Run_nonWeapon");
            }
            return;
        }

        if (localDirection.z < -0.25f)
        {
            CrossFadeFirstExisting("run_back", "Run_Back", "Run_B", "walk", "Wak_Weapon", "Walk_nonWeapon", "run", "Run_Weapon", "Run_nonWeapon");
            return;
        }

        CrossFadeFirstExisting("run_front", "run", "Run_Weapon", "Run_nonWeapon", "Run_F", "walk", "Wak_Weapon", "Walk_nonWeapon");
    }

    public void PlayAttack()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        StartActionLock(currentVisualType == VisualType.GuoJingBoss ? 0.75f : AttackVisualLockTime);
        ClearMoveParams();
        if (currentVisualType == VisualType.GuoJingBoss)
        {
            CrossFadeFirstExistingRestart("HeavyAttack", "Attack2", "Attack1", "Sword-Attack-R4", "Attack_07", "Attack_06", "Attack_04", "Attack");
            return;
        }

        CrossFadeFirstExistingRestart("Attack2", "Attack1", "HeavyAttack", "Sword-Attack-R4", "Attack_04", "Attack_03", "Attack_02", "Attack_01", "Attack_05", "Attack_06", "Attack_07", "attack_04_1", "combo_01_1", "combo_02_1", "combo_03_1", "Skill1", "skill1", "Attack");
    }

    public void PlayHit()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        hitLockUntil = Time.time + HitFeedbackTime;
        hitVisualUntil = Time.time + HitFeedbackTime;
        StartActionLock(HitFeedbackTime);
        moving = false;
        ClearMoveParams();
        SetFloatIfExists("DefendFloat", -1f);
        SetBoolIfExists("Defend", false);
        CrossFadeFirstExistingRestart("Dame_01", "Dame_02", "Damage_01", "Damage_02", "Hit", "hit", "Damage", "Hurt", "hurt", "GetHit");
    }

    public void PlayDefend()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        SetFloatIfExists("DefendFloat", 1f);
        SetBoolIfExists("Defend", true);
        CrossFadeFirstExisting("Crouch", "Defend", "Guard", "Idle");
    }

    public void PlayEvade()
    {
        PlayEvade(Vector3.left);
    }

    public void PlayEvade(Vector3 localDirection)
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        localDirection.y = 0f;
        if (localDirection.sqrMagnitude <= 0.001f)
        {
            localDirection = Vector3.back;
        }
        localDirection.Normalize();
        SetFloatIfExists("RollX", localDirection.x);
        SetFloatIfExists("RollY", localDirection.z);
        SetFloatIfExists("RollSpd", 1f);
        SetTriggerIfExists("Roll");
        StartActionLock(EvadeVisualLockTime);

        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
        {
            if (localDirection.x < 0f)
            {
                CrossFadeFirstExistingRestart("roll_left", "Crouch_Move_L", "Crouch_move_L", "Strafe_L", "Jump_start", "Jump_Start", "run", "Run_Weapon", "Run_nonWeapon");
            }
            else
            {
                CrossFadeFirstExistingRestart("roll_right", "Crouch_Move_R", "Crouch_move_R", "Strafe_R", "Jump_start", "Jump_Start", "run", "Run_Weapon", "Run_nonWeapon");
            }
            return;
        }

        if (localDirection.z > 0f)
        {
            CrossFadeFirstExistingRestart("roll_forward", "Jump_start", "Jump_Start", "Jump_loop", "Jump_Loop", "run", "Run_Weapon", "Run_nonWeapon");
        }
        else
        {
            CrossFadeFirstExistingRestart("roll_backwards", "Jump_start", "Jump_Start", "Jump_loop", "Jump_Loop", "run", "Run_Weapon", "Run_nonWeapon");
        }
    }

    public void PlayLeap()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        SetTriggerIfExists("Jump");
        SetBoolIfExists("jump", true);
        StartActionLock(LeapVisualLockTime);
        CrossFadeFirstExistingRestart("Jump_start", "Jump_Start", "Jump_loop", "Jump_Loop", "run", "Run_Weapon", "Run_nonWeapon");
    }

    public void PlayDead()
    {
        if (visualAnimator == null)
        {
            return;
        }

        dead = true;
        CrossFadeFirstExisting("Death_01", "Dead", "Death");
    }

    public void PlayIdle()
    {
        if (dead || visualAnimator == null || IsActionLocked())
        {
            return;
        }

        ClearMoveParams();
        SetFloatIfExists("DefendFloat", -1f);
        SetBoolIfExists("Defend", false);
        SetBoolIfExists("jump", false);
        CrossFadeFirstExisting("Idle", "Idle_Weapon", "Idle_nonWeapon", "idle");
    }

    private string GetResourcePath(VisualType visualType)
    {
        if (visualType == VisualType.GuoJingBoss)
        {
            return "EnemyVisuals/Enemy";
        }

        if (visualType == VisualType.ArashiNormal)
        {
            return "EnemyVisuals/Character_Color_01";
        }

        if (visualType == VisualType.ArashiGuard)
        {
            return "EnemyVisuals/Character_Color_02";
        }

        if (visualType == VisualType.ArashiElite)
        {
            return "EnemyVisuals/Character_Color_05";
        }

        return "EnemyVisuals/Character_Color_01";
    }

    private void HideOriginalRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer)
            {
                continue;
            }

            renderers[i].enabled = false;
        }
    }

    private Animator GetAnimatorWithController(GameObject visual)
    {
        Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
        Animator fallback = null;
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i].runtimeAnimatorController != null)
            {
                if (fallback == null)
                {
                    fallback = animators[i];
                }

                if (HasAnyState(animators[i], "Idle", "idle", "run", "walk", "Attack_01", "Attack_04", "Dame_01", "Crouch"))
                {
                    return animators[i];
                }
            }
        }

        if (fallback != null)
        {
            return fallback;
        }

        if (animators.Length > 0)
        {
            return animators[0];
        }

        return null;
    }

    private bool HasAnyState(Animator targetAnimator, params string[] stateNames)
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (targetAnimator.HasState(0, Animator.StringToHash(stateNames[i])))
            {
                return true;
            }
        }

        return false;
    }

    private void DisableVisualGameplayComponents(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }
    }

    private void SetFloatIfExists(string name, float value)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Float))
        {
            return;
        }

        visualAnimator.SetFloat(name, value);
    }

    private void SetBoolIfExists(string name, bool value)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        visualAnimator.SetBool(name, value);
    }

    private void SetTriggerIfExists(string name)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        visualAnimator.SetTrigger(name);
    }

    private void PlayIfExists(string stateName)
    {
        if (visualAnimator.HasState(0, Animator.StringToHash(stateName)))
        {
            visualAnimator.Play(stateName, 0, 0f);
        }
    }

    private void PlayFirstExisting(params string[] stateNames)
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (visualAnimator.HasState(0, Animator.StringToHash(stateNames[i])))
            {
                visualAnimator.Play(stateNames[i], 0, 0f);
                return;
            }
        }
    }

    private void CrossFadeIfExists(string stateName, float transitionTime, bool restart)
    {
        if (!visualAnimator.HasState(0, Animator.StringToHash(stateName)))
        {
            return;
        }

        if (!restart && currentStateName == stateName)
        {
            return;
        }

        currentStateName = stateName;
        visualAnimator.CrossFade(stateName, transitionTime, 0);
    }

    private void CrossFadeFirstExisting(params string[] stateNames)
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (visualAnimator.HasState(0, Animator.StringToHash(stateNames[i])))
            {
                CrossFadeIfExists(stateNames[i], 0.12f, false);
                return;
            }
        }
    }

    private bool CrossFadeFirstExistingRestart(params string[] stateNames)
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (visualAnimator.HasState(0, Animator.StringToHash(stateNames[i])))
            {
                CrossFadeIfExists(stateNames[i], 0.08f, true);
                return true;
            }
        }

        return false;
    }

    private void StartActionLock(float duration)
    {
        actionLockUntil = Mathf.Max(actionLockUntil, Time.time + duration);
    }

    private void ClearMoveParams()
    {
        SetFloatIfExists("MoveX", 0f);
        SetFloatIfExists("MoveY", 0f);
        SetFloatIfExists("MoveState", 0f);
        SetFloatIfExists("inputH", 0f);
        SetFloatIfExists("inputV", 0f);
        SetBoolIfExists("front", false);
        SetBoolIfExists("back", false);
        SetBoolIfExists("run", false);
    }

    private bool IsActionLocked()
    {
        return Time.time < actionLockUntil || Time.time < hitLockUntil;
    }

    private bool HasParameter(string name, AnimatorControllerParameterType type)
    {
        if (visualAnimator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = visualAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
    }
}

