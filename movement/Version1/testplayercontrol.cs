using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    [Header("Mouse Facing")]
    public float mouseTurnSpeed = 180f;
    public bool lockCursorOnStart = true;

    [Header("Run")]
    public float runSpeed = 4f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;
    public float jumpLockTime = 0.2f;

    [Header("Action Lock")]
    public float attackLockTime = 0.65f;
    public float hitLockTime = 0.4f;

    private CharacterController controller;
    private Animator animator;

    private Vector3 verticalVelocity;

    private bool isActionLocked = false;
    private float actionLockTimer = 0f;

    private bool isDead = false;
    private PlayerAttack playerAttack;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerAttack = GetComponent<PlayerAttack>();
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

        UpdateActionLock();

        HandleMovement();
        HandleInputActions();
    }

    void HandleMovement()
    {
        if (isActionLocked || animator.GetBool("IsDefense"))
        {
            animator.SetFloat("MoveX", 0f, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", 0f, 0.1f, Time.deltaTime);
            animator.SetBool("IsRunning", false);
            HandleVerticalMovement();
            return;
        }

        // 鼠标左右控制角色朝向
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(0f, mouseX * mouseTurnSpeed * Time.deltaTime, 0f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        // 只要有移动输入，按住 Shift 就切到 Run
        bool hasMoveInput = Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f;
        bool runPressed = Input.GetKey(KeyCode.LeftShift) && hasMoveInput && isGrounded;
        animator.SetBool("IsRunning", runPressed);

        float currentSpeed = runPressed ? runSpeed : moveSpeed;

        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        if (move.sqrMagnitude > 0.001f)
        {
            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        HandleVerticalMovement();

        animator.SetFloat("MoveX", h, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", v, 0.1f, Time.deltaTime);
        animator.SetFloat("VerticalSpeed", verticalVelocity.y);
    }

    void HandleInputActions()
    {
        // 防御：按住 F
        bool defensePressed = Input.GetKey(KeyCode.F);
        animator.SetBool("IsDefense", defensePressed);

        // 如果当前在防御，就不允许攻击和闪避
        if (defensePressed || isActionLocked)
            return;

        // 普通攻击：鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            animator.SetTrigger("Attack");
            LockAction(attackLockTime);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetBool("IsRunning", false);

            if (playerAttack != null)
            {
                playerAttack.StartAttack();
            }

            return;
        }

        // 跳跃：Space
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            animator.SetTrigger("Jump");
            animator.SetBool("IsGrounded", false);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);

            LockAction(jumpLockTime);
            return;
        }

        // 受击测试：H
        if (Input.GetKeyDown(KeyCode.H))
        {
            animator.SetTrigger("Hit");
            LockAction(hitLockTime);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetBool("IsRunning", false);
            return;
        }

        // 死亡测试：K
        if (Input.GetKeyDown(KeyCode.K))
        {
            isDead = true;
            animator.SetBool("IsDead", true);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            animator.SetBool("IsRunning", false);
        }
    }

    void HandleVerticalMovement()
    {
        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);

        animator.SetBool("IsGrounded", controller.isGrounded);
        animator.SetFloat("VerticalSpeed", verticalVelocity.y);
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

    void Start()
    {
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}