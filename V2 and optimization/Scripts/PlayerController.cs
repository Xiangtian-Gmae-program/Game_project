// Script note: Controls player movement, rotation, attacks, defense, rolls, jumping, damage, and guard recovery.
// Comment pass: documents responsibilities and key entry points without changing runtime logic.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class responsibility: Input cache that separates keyboard and mouse values from player gameplay logic.
public class Signal
{
    public bool JumpTriggerFlag;
    public float horizontal_float;
    public float vertical_float;
}

// Class responsibility: Shared motion-value cache for roll movement and animation timing.
public class MotionVarStore
{
    public float roll_x;
    public float roll_y;
    public float roll_anim_spd;
    public float roll_move_spd;
    public float roll_max_time;
    public float roll_current_time;
}

// Class responsibility: PlayerController contains this script's gameplay behavior.
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

    public bool isDefend;
    public bool isRoll;
    public float maxGuard = 100f;
    public float currentGuard = 100f;
    public float guardRecoverSpeed = 12f;
    public float guardCostPerBlock = 25f;
    
    public Signal signal = new Signal();
    public MotionVarStore motionVarStore = new MotionVarStore();
    
    
    // Initializes gameplay state when the scene starts.
    public virtual void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        bloodPS.Stop();
        PracticeStatsController.Ensure();
        if (!(this is EnemyController))
        {
            currentGuard = maxGuard;
            MainGameUIController.Ensure(this);
        }
    }

    // Runs per-frame input, state, AI, or UI updates.
    void Update()
    {
        if (isDead)
        {
            return;
        }
        MovePlayer();
        RotatePlayer();
        RecoverGuard();
        Attack2();
        Defend();
        Jump();
        RollState();
    }

    // Updates active roll movement and roll timeout.
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
            return;
        }
        
        if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {

        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {
     
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (signal.vertical_float == 0 &&
                signal.horizontal_float == 0)
            {
                return;
            }

            isRoll = true;
            motionVarStore.roll_x = signal.horizontal_float>0?1: signal.horizontal_float<0?-1:0;;
            motionVarStore.roll_y = signal.vertical_float>0?1: signal.vertical_float<0?-1:0;;
            motionVarStore.roll_anim_spd = 3f;
            motionVarStore.roll_move_spd = 5f;
            motionVarStore.roll_max_time = 0.3f;
            motionVarStore.roll_current_time = motionVarStore.roll_max_time;
            animator.SetTrigger("Roll");
            animator.SetFloat("RollX",motionVarStore.roll_x);
            animator.SetFloat("RollY",motionVarStore.roll_y);
            animator.SetFloat("RollSpd",motionVarStore.roll_anim_spd);
            
        }

    }

    // Handles player jump trigger input.
    private void Jump()
    {
        if (isRoll || isDefend)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            animator.SetTrigger("Jump");
        }
    }

    // Handles player defense input and Animator values.
    private void Defend()
    {
        if (Input.GetMouseButton(2) && (!MainGameUIController.IsMainGameScene() || currentGuard > 0))
        {
            isDefend = true;
            animator.SetFloat("DefendFloat",1);
        }
        else
        {
            isDefend = false;
            animator.SetFloat("DefendFloat",-1);
        }
    }

    // Handles player movement input, transform movement, and movement animation.
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
        animator.SetFloat("MoveX",signal.horizontal_float);
        animator.SetFloat("MoveY",signal.vertical_float);
        if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {
            animator.SetFloat("MoveState", 0);
            speed = 1.5f;
            
            Vector3 movement = new Vector3(signal.horizontal_float, 0, signal.vertical_float) * Time.deltaTime * speed;
            transform.Translate(movement);
        }
        else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Space))
        {
            animator.SetFloat("MoveState", 1f);
            speed = 3.5f;
            
            Vector3 movement = new Vector3(signal.horizontal_float, 0, signal.vertical_float) * Time.deltaTime * speed;
            transform.Translate(movement);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
   
        }
    }
    // Handles legacy player attack trigger logic.
    public virtual void Attack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger("Attack");
            if (Vector3.Distance(enemy.transform.position, transform.position) <= 2)
            {
                enemy.TakeDamage(2);
            }
        }
    }
    // Applies damage and handles block, evade, hit, and death outcomes.
    public void TakeDamage(int attackDamage)
    {
        if (isDefend)
        {
            if (MainGameUIController.IsMainGameScene())
            {
                if (currentGuard >= guardCostPerBlock)
                {
                    currentGuard -= guardCostPerBlock;
                    MainGameUIController.NotifyGuardBlock(Mathf.RoundToInt(guardCostPerBlock));
                    return;
                }

                MainGameUIController.NotifyGuardBroken();
            }
            else
            {
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
        animator.SetTrigger("Hit");
        bloodPS.Play();
        if (HP <= 0)
        {
            animator.SetBool("Dead", true);
            isDead = true;
        }
    }

    // Regenerates guard over time when not blocking.
    private void RecoverGuard()
    {
        if (!MainGameUIController.IsMainGameScene())
        {
            return;
        }

        if (Input.GetMouseButton(2))
        {
            return;
        }

        currentGuard += guardRecoverSpeed * Time.deltaTime;
        if (currentGuard > maxGuard)
        {
            currentGuard = maxGuard;
        }
    }
    /// <summary>
    /// 处理玩家转向
    /// </summary>
    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    private float chargeTime = 0.2f; // 蓄力时间（秒�?
    private bool charging = false;//是否正在蓄力
    private float currentChargeTime = 0f;//当前蓄力时长
    private bool startCharging;//是否开始蓄�?
    private float attackRecoverUntil;
    private float normalAttackRecover = 0.35f;
    private float heavyAttackRecover = 0.55f;
    // Handles current normal and charged attack logic.
    public virtual void Attack2()
    {
        if (isRoll || isDefend || enemy == null || Time.time < attackRecoverUntil)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            startCharging = true;
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
                animator.SetBool("Charging", false);
                if (Vector3.Distance(enemy.transform.position, transform.position) <= 3.5f)
                {
                    enemy.TakeDamage(4);
                }
                else
                {
                    MainGameUIController.NotifyAttackMiss();
                }
                attackRecoverUntil = Time.time + heavyAttackRecover;
            }
            else
            {
                animator.SetTrigger("Attack");
                if (Vector3.Distance(enemy.transform.position, transform.position) <= 2)
                {
                    enemy.TakeDamage(2);
                }
                else
                {
                    MainGameUIController.NotifyAttackMiss();
                }
                attackRecoverUntil = Time.time + normalAttackRecover;
            }
            charging = false;
            currentChargeTime = 0f;
            startCharging = false;
        }
    }

    // Clears action flags from animation-state callbacks.
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
