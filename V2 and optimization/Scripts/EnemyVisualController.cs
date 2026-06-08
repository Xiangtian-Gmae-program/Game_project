// Script note: Bridges enemy AI states to runtime visual models and Animator states.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using UnityEngine;

// Class responsibility: EnemyVisualController contains this script's gameplay behavior.
public class EnemyVisualController : MonoBehaviour
{
    // Enum purpose: Runtime enemy visual selection for normal, guard, elite, and boss models.
    public enum VisualType
    {
        Auto,
        ArashiNormal,
        ArashiGuard,
        ArashiElite,
        GuoJingBoss
    }

    private Animator visualAnimator;
    private bool moving;
    private bool dead;
    private string currentStateName;

    // Builds a runtime visual or controller object from the selected type.
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

        GameObject visual = Instantiate(prefab, transform);
        visual.name = "EnemyVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        visualAnimator = GetAnimatorWithController(visual);
        if (visualAnimator != null)
        {
            visualAnimator.applyRootMotion = false;
        }
    }

    // Removes the currently attached runtime enemy visual.
    private void RemoveCurrentVisual()
    {
        Transform oldVisual = transform.Find("EnemyVisual");
        if (oldVisual == null)
        {
            return;
        }

        Destroy(oldVisual.gameObject);
        visualAnimator = null;
        moving = false;
        dead = false;
        currentStateName = null;
    }

    // Switches visual animation between moving and idle states.
    public void SetMoving(bool value)
    {
        if (dead || visualAnimator == null || moving == value)
        {
            return;
        }

        moving = value;
        SetFloatIfExists("inputH", 0f);
        SetFloatIfExists("inputV", moving ? 1f : 0f);
        SetBoolIfExists("run", moving);
        if (moving)
        {
            CrossFadeFirstExisting("run", "Run_Weapon", "Run_nonWeapon", "walk", "Wak_Weapon", "Walk_nonWeapon");
        }
        else
        {
            CrossFadeFirstExisting("Idle", "Idle_Weapon", "Idle_nonWeapon", "idle");
        }
    }

    // Sends local movement direction to compatible Animator parameters.
    public void SetMoveDirection(Vector3 localDirection)
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        localDirection.y = 0f;
        if (localDirection.sqrMagnitude > 1f)
        {
            localDirection.Normalize();
        }

        moving = true;
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
                CrossFadeFirstExisting("Strafe_L", "Crouch_Move_L", "Crouch_move_L", "Run_Left", "Run_L", "run", "Run_Weapon", "Run_nonWeapon");
            }
            else
            {
                CrossFadeFirstExisting("Strafe_R", "Crouch_Move_R", "Crouch_move_R", "Run_Right", "Run_R", "run", "Run_Weapon", "Run_nonWeapon");
            }
            return;
        }

        if (localDirection.z < -0.25f)
        {
            CrossFadeFirstExisting("Run_Back", "Run_B", "walk", "Wak_Weapon", "Walk_nonWeapon", "run", "Run_Weapon", "Run_nonWeapon");
            return;
        }

        CrossFadeFirstExisting("run", "Run_Weapon", "Run_nonWeapon", "Run_F", "walk", "Wak_Weapon", "Walk_nonWeapon");
    }

    // Plays an attack animation if the visual controller supports one.
    public void PlayAttack()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        CrossFadeFirstExistingRestart("Attack_01", "Skill1", "skill1", "Attack");
    }

    // Plays hit reaction animation.
    public void PlayHit()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        CrossFadeFirstExistingRestart("Dame_01", "Hit", "Damage");
    }

    // Sets defense parameters and plays a guard animation.
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

    // Plays evade, roll, or lateral movement animation.
    public void PlayEvade()
    {
        PlayEvade(Vector3.left);
    }

    // Plays evade, roll, or lateral movement animation.
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

    // Plays jump or leap movement animation.
    public void PlayLeap()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        SetTriggerIfExists("Jump");
        SetBoolIfExists("jump", true);
        CrossFadeFirstExistingRestart("Jump_start", "Jump_Start", "Jump_loop", "Jump_Loop", "run", "Run_Weapon", "Run_nonWeapon");
    }

    // Plays death animation and locks the visual as dead.
    public void PlayDead()
    {
        if (visualAnimator == null)
        {
            return;
        }

        dead = true;
        CrossFadeFirstExisting("Death_01", "Dead", "Death");
    }

    // Returns the visual to idle and clears transient movement flags.
    public void PlayIdle()
    {
        if (dead || visualAnimator == null)
        {
            return;
        }

        SetFloatIfExists("MoveX", 0f);
        SetFloatIfExists("MoveY", 0f);
        SetFloatIfExists("MoveState", 0f);
        SetFloatIfExists("inputH", 0f);
        SetFloatIfExists("inputV", 0f);
        SetFloatIfExists("DefendFloat", -1f);
        SetBoolIfExists("Defend", false);
        SetBoolIfExists("run", false);
        SetBoolIfExists("jump", false);
        CrossFadeFirstExisting("Idle", "Idle_Weapon", "Idle_nonWeapon", "idle");
    }

    // Returns the Resources path for the selected visual type.
    private string GetResourcePath(VisualType visualType)
    {
        if (visualType == VisualType.ArashiGuard)
        {
            return "EnemyVisuals/Character_Color_02";
        }

        if (visualType == VisualType.ArashiElite)
        {
            return "EnemyVisuals/Character_Color_05";
        }

        if (visualType == VisualType.GuoJingBoss)
        {
            return "EnemyVisuals/GuoJin";
        }

        return "EnemyVisuals/Character_Color_01";
    }

    // Hides existing renderers so the runtime visual does not overlap the source model.
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

    // Finds the first child Animator that has a runtime controller.
    private Animator GetAnimatorWithController(GameObject visual)
    {
        Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i].runtimeAnimatorController != null)
            {
                return animators[i];
            }
        }

        if (animators.Length > 0)
        {
            return animators[0];
        }

        return null;
    }

    // Safely writes a float Animator parameter when it exists.
    private void SetFloatIfExists(string name, float value)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Float))
        {
            return;
        }

        visualAnimator.SetFloat(name, value);
    }

    // Safely writes a bool Animator parameter when it exists.
    private void SetBoolIfExists(string name, bool value)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        visualAnimator.SetBool(name, value);
    }

    // Safely triggers an Animator parameter when it exists.
    private void SetTriggerIfExists(string name)
    {
        if (!HasParameter(name, AnimatorControllerParameterType.Trigger))
        {
            return;
        }

        visualAnimator.SetTrigger(name);
    }

    // Plays an Animator state only when that state exists.
    private void PlayIfExists(string stateName)
    {
        if (visualAnimator.HasState(0, Animator.StringToHash(stateName)))
        {
            visualAnimator.Play(stateName, 0, 0f);
        }
    }

    // Plays the first available Animator state from a priority list.
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

    // Crossfades to an Animator state if it exists.
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

    // Crossfades to the first available looping animation state.
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

    // Restarts the first available one-shot animation state.
    private void CrossFadeFirstExistingRestart(params string[] stateNames)
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            if (visualAnimator.HasState(0, Animator.StringToHash(stateNames[i])))
            {
                CrossFadeIfExists(stateNames[i], 0.08f, true);
                return;
            }
        }
    }

    // Checks whether the Animator has a parameter with the requested name and type.
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
