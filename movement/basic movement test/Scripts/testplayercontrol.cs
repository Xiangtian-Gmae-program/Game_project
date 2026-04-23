using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;

    [Header("Action Lock")]
    public float attackLockTime = 0.65f;
    public float dodgeLockTime = 0.55f;
    public float hitLockTime = 0.4f;

    private CharacterController controller;
    private Animator animator;

    private Vector3 verticalVelocity;

    private bool isActionLocked = false;
    private float actionLockTimer = 0f;

    private bool isDead = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || controller == null)
            return;

        if (isDead)
        {
            HandleGravityOnly();
            return;
        }

        UpdateActionLock();

        HandleMovement();
        HandleInputActions();
    }

    void HandleMovement()
    {
        // 防御、攻击、闪避、受击时，可以先锁住移动
        if (isActionLocked || animator.GetBool("IsDefense"))
        {
            animator.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
            HandleGravityOnly();
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v).normalized;
        Vector3 move = input;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            controller.Move(move * moveSpeed * Time.deltaTime);
        }

        HandleGravityOnly();

        float targetSpeed = input.magnitude;
        animator.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);
    }

    void HandleInputActions()
    {
        // 防御：按住 F
        bool defensePressed = Input.GetKey(KeyCode.F);
        animator.SetBool("IsDefense", defensePressed);

        // 如果当前在防御，就不允许攻击和闪避
        if (defensePressed || isActionLocked)
            return;

        // 普通攻击：J
        if(Input.GetKeyDown(KeyCode.J))
        {
            animator.SetTrigger("Attack");
            LockAction(attackLockTime);
            animator.SetFloat("Speed", 0f);
            return;
        }

        // 闪避：左 Shift
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            animator.SetTrigger("Dodge");
            LockAction(dodgeLockTime);
            animator.SetFloat("Speed", 0f);
            return;
        }

        // 受击测试：H
        if (Input.GetKeyDown(KeyCode.H))
        {
            animator.SetTrigger("Hit");
            LockAction(hitLockTime);
            animator.SetFloat("Speed", 0f);
            return;
        }

        // 死亡测试：K
        if (Input.GetKeyDown(KeyCode.K))
        {
            isDead = true;
            animator.SetBool("IsDead", true);
            animator.SetFloat("Speed", 0f);
        }
    }

    void HandleGravityOnly()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    void LockAction(float duration)
    {
        isActionLocked = true;
        actionLockTimer = duration;
    }

    void UpdateActionLock()
    {
        if (!isActionLocked) return;

        actionLockTimer -= Time.deltaTime;
        if (actionLockTimer <= 0f)
        {
            isActionLocked = false;
            actionLockTimer = 0f;
        }
    }
}