// Purpose: Provides an older test player controller used for prototype movement and animation behavior.
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]

public class TestPlayerController : MonoBehaviour
{
    [Header("Walk / Run Speeds")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.5f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Directional Multipliers")]
    public float forwardMultiplier = 1.0f;
    public float backwardMultiplier = 0.75f;
    public float strafeMultiplier = 0.9f;

    [Header("Acceleration / Deceleration")]
    public float walkAcceleration = 14f;
    public float walkDeceleration = 16f;
    public float runAcceleration = 12f;
    public float runDeceleration = 14f;

    [Header("Turning")]
    public float walkTurnSpeed = 240f;
    public float runTurnSpeed = 190f;

    [Header("Air Control")]
    [Range(0f, 1f)] public float airControl = 0.5f;

    [Header("Mouse Facing")]
    public bool lockCursorOnStart = true;

    [Header("Jump")]
    public float jumpHeight = 1.2f;
    public float jumpLockTime = 0.2f;

    [Header("Jump Assist")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTime = 0.12f;

    private float jumpBufferCounter = 0f;
    private float coyoteCounter = 0f;

    [Header("Landing")]
    public float landingTriggerVelocity = -6f;
    public float landingLockTime = 0.12f;
    public float landingHorizontalDamp = 0.4f;

    private bool wasGroundedLastFrame = true;

    [Header("Action Lock")]
    public float attackLockTime = 0.65f;
    public float hitLockTime = 0.4f;

    [Header("Combo")]
    public float comboInputWindowStart = 0.1f;
    public float comboInputWindowEnd = 0.6f;

    private CharacterController controller;
    private Animator animator;
    private PlayerAttack playerAttack;

    private Vector3 verticalVelocity;
    private Vector3 currentPlanarVelocity;

    private bool isActionLocked = false;
    private float actionLockTimer = 0f;

    private bool isDead = false;

    private bool isComboAttacking = false;
    private bool comboInputQueued = false;
    private int currentComboStep = 0;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
    }

    void Start()
    {
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        wasGroundedLastFrame = controller != null && controller.isGrounded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (animator == null || controller == null)
            return;

        if (isDead)
        {
            HandleVerticalMovement();
            return;
        }

        UpdateJumpAssistTimers();
        UpdateActionLock();

        HandleMovement();
        HandleInputActions();
        TryConsumeBufferedJump();
    }

    float GetDirectionalMultiplier(float h, float v)
    {
        if (Mathf.Abs(v) >= Mathf.Abs(h))
        {
            if (v > 0.01f) return forwardMultiplier;
            if (v < -0.01f) return backwardMultiplier;
        }

        if (Mathf.Abs(h) > 0.01f)
            return strafeMultiplier;

        return 0f;
    }

    void UpdateAnimatorMoveParams()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(currentPlanarVelocity);

        float sideBase = runSpeed * strafeMultiplier;
        float forwardBase = runSpeed * forwardMultiplier;
        float backBase = runSpeed * backwardMultiplier;

        float moveX = 0f;
        float moveY = 0f;

        if (sideBase > 0.001f)
            moveX = Mathf.Clamp(localVelocity.x / sideBase, -1f, 1f);

        if (localVelocity.z >= 0f)
        {
            if (forwardBase > 0.001f)
                moveY = Mathf.Clamp(localVelocity.z / forwardBase, -1f, 1f);
        }
        else
        {
            if (backBase > 0.001f)
                moveY = Mathf.Clamp(localVelocity.z / backBase, -1f, 1f);
        }

        animator.SetFloat("MoveX", moveX, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveY, 0.1f, Time.deltaTime);
    }

    void HandleMovement()
    {
        bool isDefense = animator.GetBool("IsDefense");
        bool isGrounded = controller.isGrounded;

        animator.SetBool("IsGrounded", isGrounded);

        float mouseX = Input.GetAxis("Mouse X");
        float currentTurnSpeed = animator.GetBool("IsRunning") ? runTurnSpeed : walkTurnSpeed;

        if (!isActionLocked && !isDefense)
        {
            transform.Rotate(0f, mouseX * currentTurnSpeed * Time.deltaTime, 0f);
        }

        if (isActionLocked || isDefense)
        {
            currentPlanarVelocity = Vector3.MoveTowards(
                currentPlanarVelocity,
                Vector3.zero,
                walkDeceleration * Time.deltaTime
            );

            controller.Move(currentPlanarVelocity * Time.deltaTime);
            HandleVerticalMovement();
            UpdateAnimatorMoveParams();
            animator.SetBool("IsRunning", false);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool hasMoveInput = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        bool runPressed = Input.GetKey(KeyCode.LeftShift) && hasMoveInput && isGrounded;
        animator.SetBool("IsRunning", runPressed);

        float baseSpeed = runPressed ? runSpeed : walkSpeed;
        float directionalMultiplier = GetDirectionalMultiplier(h, v);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        Vector3 worldMoveDir = (transform.right * inputDir.x + transform.forward * inputDir.z).normalized;
        Vector3 targetVelocity = worldMoveDir * (baseSpeed * directionalMultiplier);

        if (!isGrounded)
        {
            targetVelocity *= airControl;
        }

        float accel = runPressed ? runAcceleration : walkAcceleration;
        float decel = runPressed ? runDeceleration : walkDeceleration;
        float rate = hasMoveInput ? accel : decel;

        currentPlanarVelocity = Vector3.MoveTowards(
            currentPlanarVelocity,
            targetVelocity,
            rate * Time.deltaTime
        );

        controller.Move(currentPlanarVelocity * Time.deltaTime);
        HandleVerticalMovement();
        UpdateAnimatorMoveParams();
    }

    void HandleInputActions()
    {
        bool defensePressed = Input.GetKey(KeyCode.F);
        animator.SetBool("IsDefense", defensePressed);

        if (Input.GetMouseButtonDown(0) && !defensePressed)
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (!isComboAttacking)
            {
                if (!isActionLocked)
                {
                    StartCoroutine(ComboRoutine());
                }
            }
            else
            {
                comboInputQueued = true;
            }

            return;
        }

        if (defensePressed || isActionLocked)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            animator.SetTrigger("Hit");
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetBool("IsRunning", false);
            currentPlanarVelocity = Vector3.zero;

            LockAction(hitLockTime);
            return;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            isDead = true;
            animator.SetBool("IsDead", true);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetBool("IsRunning", false);
            currentPlanarVelocity = Vector3.zero;
        }
    }

    void HandleVerticalMovement()
    {
        float verticalBeforeMove = verticalVelocity.y;

        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);

        bool groundedNow = controller.isGrounded;

        if (!wasGroundedLastFrame && groundedNow && verticalBeforeMove <= landingTriggerVelocity)
        {
            animator.SetBool("IsRunning", false);

            currentPlanarVelocity *= landingHorizontalDamp;

        }

        animator.SetBool("IsGrounded", groundedNow);
        animator.SetFloat("VerticalSpeed", verticalVelocity.y);

        wasGroundedLastFrame = groundedNow;
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

    IEnumerator ComboRoutine()
    {
        isComboAttacking = true;
        comboInputQueued = false;
        currentComboStep = 1;

        while (currentComboStep <= 3)
        {
            PlayComboStep(currentComboStep);

            yield return new WaitForSeconds(comboInputWindowStart);

            if (playerAttack != null)
            {
                playerAttack.StartAttack();
            }

            float remain = comboInputWindowEnd - comboInputWindowStart;
            if (remain > 0f)
                yield return new WaitForSeconds(remain);

            if (comboInputQueued && currentComboStep < 3)
            {
                comboInputQueued = false;
                currentComboStep++;
                continue;
            }

            break;
        }

        isComboAttacking = false;
        comboInputQueued = false;
        currentComboStep = 0;
    }

    void PlayComboStep(int step)
    {
        Debug.Log("PlayComboStep: " + step);

        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);
        animator.SetBool("IsRunning", false);
        currentPlanarVelocity = Vector3.zero;

        switch (step)
        {
            case 1:
                animator.SetTrigger("Attack1");
                LockAction(0.45f);
                break;
            case 2:
                animator.SetTrigger("Attack2");
                LockAction(0.45f);
                break;
            case 3:
                animator.SetTrigger("Attack3");
                LockAction(0.5f);
                break;
        }
    }

    void UpdateJumpAssistTimers()
    {
        if (controller.isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    void TryConsumeBufferedJump()
    {
        bool isDefense = animator.GetBool("IsDefense");

        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isActionLocked && !isDefense)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            animator.SetTrigger("Jump");
            animator.SetBool("IsGrounded", false);
            animator.SetBool("IsRunning", false);

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }
}

