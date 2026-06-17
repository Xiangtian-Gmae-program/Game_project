// Purpose: Controls player movement, camera rotation, attacks, charging, defense, rolling, jumping, damage, and guard recovery.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Signal
{
    public bool JumpTriggerFlag;
    public float horizontal_float;
    public float vertical_float;
}

public class MotionVarStore
{
    public float roll_x;
    public float roll_y;
    public float roll_anim_spd;
    public float roll_move_spd;
    public float roll_max_time;
    public float roll_current_time;
}

public class PlayerController : MonoBehaviour
{
    public float speed;
    public float sensitivity;
    public Animator animator;
    public bool canMove;
    public int HP = 20;
    public bool isDead;
    public ParticleSystem bloodPS;
    public EnemyController enemy;
    private const float MovementBoundaryMinX = -14f;
    private const float MovementBoundaryMaxX = 14f;
    private const float MovementBoundaryMinZ = -14f;
    private const float MovementBoundaryMaxZ = 14f;

    public bool isDefend;
    public bool isRoll;
    public float maxGuard = 100f;
    public float currentGuard = 100f;
    public float guardRecoverSpeed = 0.6666667f;
    public float guardCostPerBlock = 25f;

    public Signal signal = new Signal();
    public MotionVarStore motionVarStore = new MotionVarStore();

    public virtual void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        bloodPS.Stop();
        PracticeStatsController.Ensure();
        if (!(this is EnemyController))
        {
            GameSettingsController.ApplyPlayerSensitivity(this);
            currentGuard = maxGuard;
            MainGameUIController.Ensure(this);
        }
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }
        CaptureBufferedInputs();
        MovePlayer();
        RotatePlayer();
        RecoverGuard();
        Attack2();
        Defend();
        Jump();
        RollState();
    }

    private void ClampToMovementBoundary()
    {
        if (this is EnemyController)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, MovementBoundaryMinX, MovementBoundaryMaxX);
        position.z = Mathf.Clamp(position.z, MovementBoundaryMinZ, MovementBoundaryMaxZ);
        transform.position = position;
    }

    private void RollState()
    {
        if (isRoll)
        {
            motionVarStore.roll_current_time-=Time.deltaTime;
            if (motionVarStore.roll_current_time <= 0)
            {
                isRoll = false;
                return;
            }
            Vector3 movement = new Vector3(motionVarStore.roll_x, 0, motionVarStore.roll_y) * Time.deltaTime * motionVarStore.roll_move_spd;
            transform.Translate(movement);
            ClampToMovementBoundary();
            return;
        }

        bool rollRequested = Input.GetKeyDown(KeyCode.Space) || Time.time <= bufferedRollUntil;
        if (startCharging || (IsPlayerActionLocked() && !CanRollCancelActionLock()))
        {
            return;
        }

        if (isDefend && !rollRequested)
        {
            return;
        }

        if (rollRequested)
        {
            float rollX = Input.GetKeyDown(KeyCode.Space) ? signal.horizontal_float : bufferedRollX;
            float rollY = Input.GetKeyDown(KeyCode.Space) ? signal.vertical_float : bufferedRollY;
            bufferedRollUntil = 0f;
            if (rollY == 0 &&
                rollX == 0)
            {
                return;
            }

            isDefend = false;
            profileDefendHeld = false;
            animator.SetFloat("DefendFloat",-1);
            isRoll = true;
            motionVarStore.roll_x = rollX>0?1: rollX<0?-1:0;;
            motionVarStore.roll_y = rollY>0?1: rollY<0?-1:0;;
            motionVarStore.roll_anim_spd = 3f;
            motionVarStore.roll_move_spd = 5f;
            motionVarStore.roll_max_time = 0.3f;
            motionVarStore.roll_current_time = motionVarStore.roll_max_time;
            StartPlayerActionLock(motionVarStore.roll_max_time);
            animator.SetTrigger("Roll");
            animator.SetFloat("RollX",motionVarStore.roll_x);
            animator.SetFloat("RollY",motionVarStore.roll_y);
            animator.SetFloat("RollSpd",motionVarStore.roll_anim_spd);
            PlayerActionProfile.RecordRoll();

        }
        else if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {

        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {

        }

    }

    private void Jump()
    {
        if (isRoll || isDefend || startCharging || IsPlayerActionLocked())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1) || Time.time <= bufferedJumpUntil)
        {
            bufferedJumpUntil = 0f;
            StartPlayerActionLock(jumpActionLock);
            ClearMoveParams();
            animator.SetTrigger("Jump");
            PlayerActionProfile.RecordJump();
        }
    }

    private void Defend()
    {
        if (isRoll || startCharging || IsPlayerActionLocked())
        {
            profileDefendHeld = false;
            isDefend = false;
            animator.SetFloat("DefendFloat",-1);
            return;
        }

        if (Input.GetKey(KeyCode.E) && (!MainGameUIController.IsMainGameScene() || currentGuard > 0))
        {
            if (!profileDefendHeld)
            {
                PlayerActionProfile.RecordDefend();
            }
            profileDefendHeld = true;
            isDefend = true;
            animator.SetFloat("DefendFloat",1);
        }
        else
        {
            profileDefendHeld = false;
            isDefend = false;
            animator.SetFloat("DefendFloat",-1);
        }
    }

    private void MovePlayer()
    {
        if (!canMove)
        {
            return;
        }

        if (isRoll)
        {
            return;
        }

        signal.horizontal_float = Input.GetAxis("Horizontal");
        signal.vertical_float = Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.E) || Input.GetMouseButton(0) || isDefend || startCharging || IsPlayerActionLocked())
        {
            ClearMoveParams();
            return;
        }

        animator.SetFloat("MoveX",signal.horizontal_float);
        animator.SetFloat("MoveY",signal.vertical_float);
        if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {
            animator.SetFloat("MoveState", 0);
            speed = 1.5f;

            Vector3 movement = new Vector3(signal.horizontal_float, 0, signal.vertical_float) * Time.deltaTime * speed;
            transform.Translate(movement);
            ClampToMovementBoundary();
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {
            animator.SetFloat("MoveState", 1f);
            speed = 3.5f;

            Vector3 movement = new Vector3(signal.horizontal_float, 0, signal.vertical_float) * Time.deltaTime * speed;
            transform.Translate(movement);
            ClampToMovementBoundary();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {

        }
    }

    public virtual void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Attack");
            MainGameSfxController.PlaySwordSwing();
            if (Vector3.Distance(enemy.transform.position, transform.position) <= 2)
            {
                enemy.TakeDamage(2);
            }
        }
    }

    public void TakeDamage(int attackDamage)
    {
        if (isDefend)
        {
            if (MainGameUIController.IsMainGameScene())
            {
                if (currentGuard >= guardCostPerBlock)
                {
                    currentGuard -= guardCostPerBlock;
                    MainGameSfxController.PlayGuardBlock();
                    MainGameUIController.NotifyGuardBlock(Mathf.RoundToInt(guardCostPerBlock));
                    return;
                }

                MainGameUIController.NotifyGuardBroken();
            }
            else
            {
                MainGameSfxController.PlayGuardBlock();
                PracticeStatsController.RecordDefend();
                return;
            }

            isDefend = false;
            animator.SetFloat("DefendFloat",-1);
        }

        if (isRoll)
        {
            if (MainGameUIController.IsMainGameScene())
            {
                MainGameUIController.NotifyRollEvade();
            }
            else
            {
                PracticeStatsController.RecordRollDodge();
            }
            return;
        }

        HP -= attackDamage;
        if (this is EnemyController)
        {
            MainGameUIController.NotifyEnemyHit(attackDamage, HP);
        }
        else
        {
            MainGameUIController.NotifyPlayerHit(attackDamage);
        }
        if (!(this is EnemyController))
        {
            StartPlayerActionLock(hitActionLock);
            attackRecoverUntil = Time.time + hitActionLock;
            isDefend = false;
            isRoll = false;
            charging = false;
            startCharging = false;
            currentChargeTime = 0f;
            animator.SetBool("Charging", false);
            animator.SetFloat("DefendFloat",-1);
            ClearMoveParams();
        }
        animator.SetTrigger("Hit");
        bloodPS.Play();
        if (HP <= 0)
        {
            animator.SetBool("Dead", true);
            isDead = true;
        }
    }

    private void RecoverGuard()
    {
        if (!MainGameUIController.IsMainGameScene())
        {
            return;
        }

        if (Input.GetKey(KeyCode.E))
        {
            return;
        }

        currentGuard += guardRecoverSpeed * Time.deltaTime;
        if (currentGuard > maxGuard)
        {
            currentGuard = maxGuard;
        }
    }

    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    private float chargeTime = 0.2f;
    private bool charging = false;
    private float currentChargeTime = 0f;
    private bool startCharging;
    private float attackRecoverUntil;
    private float normalAttackRecover = 0.35f;
    private float heavyAttackRecover = 0.55f;
    private bool profileDefendHeld;
    private float actionLockUntil;
    private float normalAttackLock = 0.35f;
    private float heavyAttackLock = 0.55f;
    private float jumpActionLock = 0.45f;
    private float hitActionLock = 0.35f;
    private const float InputBufferTime = 0.16f;
    private const float RollCancelWindow = 0.12f;
    private float bufferedAttackUntil;
    private float bufferedRollUntil;
    private float bufferedJumpUntil;
    private float bufferedRollX;
    private float bufferedRollY;

    private void CaptureBufferedInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            bufferedAttackUntil = Time.time + InputBufferTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bufferedRollX = Input.GetAxis("Horizontal");
            bufferedRollY = Input.GetAxis("Vertical");
            bufferedRollUntil = Time.time + InputBufferTime;
        }

        if (Input.GetMouseButtonDown(1))
        {
            bufferedJumpUntil = Time.time + InputBufferTime;
        }
    }

    public virtual void Attack2()
    {
        if (isRoll || isDefend || enemy == null || Time.time < attackRecoverUntil || IsPlayerActionLocked())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Time.time <= bufferedAttackUntil)
        {
            startCharging = true;
            bufferedAttackUntil = 0f;
            if (!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
            {
                PerformNormalAttack();
                return;
            }
        }

        if (startCharging)
        {
            currentChargeTime += Time.deltaTime;
            if (currentChargeTime >= chargeTime)
            {
                charging = true;
                animator.SetBool("Charging", true);
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (charging)
            {
                PerformHeavyAttack();
            }
            else
            {
                PerformNormalAttack();
            }
        }
    }

    private void PerformNormalAttack()
    {
        ClearMoveParams();
        animator.SetTrigger("Attack");
        MainGameSfxController.PlaySwordSwing();
        if (Vector3.Distance(enemy.transform.position, transform.position) <= 2)
        {
            enemy.TakeDamage(2);
        }
        else
        {
            MainGameUIController.NotifyAttackMiss();
        }
        PlayerActionProfile.RecordNormalAttack();
        attackRecoverUntil = Time.time + normalAttackRecover;
        StartPlayerActionLock(normalAttackLock);
        ResetAttackCharge();
    }

    private void PerformHeavyAttack()
    {
        ClearMoveParams();
        animator.SetBool("Charging", false);
        MainGameSfxController.PlaySwordSwing();
        if (Vector3.Distance(enemy.transform.position, transform.position) <= 3.5f)
        {
            enemy.TakeDamage(4);
        }
        else
        {
            MainGameUIController.NotifyAttackMiss();
        }
        PlayerActionProfile.RecordHeavyAttack();
        attackRecoverUntil = Time.time + heavyAttackRecover;
        StartPlayerActionLock(heavyAttackLock);
        ResetAttackCharge();
    }

    private void ResetAttackCharge()
    {
        animator.SetBool("Charging", false);
        charging = false;
        currentChargeTime = 0f;
        startCharging = false;
    }

    private void StartPlayerActionLock(float duration)
    {
        actionLockUntil = Mathf.Max(actionLockUntil, Time.time + duration);
    }

    private bool IsPlayerActionLocked()
    {
        return Time.time < actionLockUntil;
    }

    private bool CanRollCancelActionLock()
    {
        return IsPlayerActionLocked() && actionLockUntil - Time.time <= RollCancelWindow;
    }

    private void ClearMoveParams()
    {
        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);
        animator.SetFloat("MoveState", 0f);
    }

    public void OnAnimStateTriggerClearFlag(List<string> clearStringArray)
    {
        foreach (string clearString in clearStringArray)
        {
            if (clearString == "isRoll")
            {
                isRoll = false;
            }

            if (clearString == "isDefend")
            {
                isDefend = false;
            }
        }
    }
}

