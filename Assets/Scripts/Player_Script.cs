using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField] private float runSpeed = 5.0f;

    [SerializeField] private float runAcceleration = 30f;

    [SerializeField] private float runDeceleration = 40f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpSpeed = 5.0f;

    [SerializeField][Range(0.1f, 1f)] private float jumpCutMultiplier = 0.5f;

    [SerializeField] private float coyoteTime = 0.1f;

    [SerializeField] private float jumpBufferTime = 0.1f;

    [SerializeField] private float fallGravityMultiplier = 2.0f;

    float gravityScaleAtStart;

    float lastGroundedTime;
    float jumpBufferTimer;

    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private InputActionAsset inputActions;

    InputAction moveAction;

    InputAction jumpAction;

    public bool JumpPressedThisFrame => jumpAction != null && jumpAction.WasPressedThisFrame();

    public LayerMask GroundLayer => groundLayer.value != 0 ? groundLayer : LayerMask.GetMask("Ground");

    public Vector2 MoveInput { get; private set; }

    Rigidbody2D playerCharacter;

    Animator playerAnimator;

    BoxCollider2D playerFeetCollider;

    // Initializes its contents before the game begins
    void Awake()
    {
        playerCharacter = GetComponent<Rigidbody2D>();

        playerAnimator = GetComponentInChildren<Animator>();

        playerFeetCollider = GetComponent<BoxCollider2D>();

        gravityScaleAtStart = playerCharacter.gravityScale;

        InputActionMap playerMap = inputActions.FindActionMap("Player", true);

        moveAction = playerMap.FindAction("Move", true);

        jumpAction = playerMap.FindAction("Jump", true);

        playerMap.Enable();

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        MoveInput = moveAction.ReadValue<Vector2>();

        Run();
        Jump();
        BetterGravity();
        FlipSprite();
    }

    private void Run()
    {
        float hMovement = MoveInput.x;

        float targetSpeed = MoveInput.x * runSpeed;

        float speedChange;

        if (Mathf.Abs(targetSpeed) > Mathf.Epsilon)
        {
            speedChange = runAcceleration;
        }
        else
        {
            speedChange = runDeceleration;
        }

        float newSpeed = Mathf.MoveTowards(playerCharacter.linearVelocity.x, targetSpeed, speedChange * Time.deltaTime);

        playerCharacter.linearVelocity = new Vector2(newSpeed, playerCharacter.linearVelocity.y);

        bool hSpeed = Mathf.Abs(playerCharacter.linearVelocity.x) > Mathf.Epsilon;

        playerAnimator.SetBool("run", hSpeed);
    }

    private void FlipSprite()
    {
        bool hMovement = Mathf.Abs(playerCharacter.linearVelocity.x) > Mathf.Epsilon;

        if (hMovement)
        {
            transform.localScale = new Vector2(Mathf.Sign(playerCharacter.linearVelocity.x), 1f);
        }
    }

    private void Jump()
    {
        if (jumpAction.WasReleasedThisFrame() && playerCharacter.linearVelocity.y > 0)
        {
            playerCharacter.linearVelocity = new Vector2(playerCharacter.linearVelocity.x, playerCharacter.linearVelocity.y * jumpCutMultiplier);
        }

        bool isGrounded = playerFeetCollider.IsTouchingLayers(GroundLayer);

        if (isGrounded)
        {
            // Remember a brief window after leaving a platform
            lastGroundedTime = coyoteTime;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

        if (JumpPressedThisFrame)
        {
            // Remember a jump pressed before landing
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (lastGroundedTime <= 0 || jumpBufferTimer <= 0)
        {
            return;
        }

        playerCharacter.linearVelocity = new Vector2(playerCharacter.linearVelocity.x, jumpSpeed);

        lastGroundedTime = 0;
        jumpBufferTimer = 0;
    }

    private void BetterGravity()
    {
        // Use stronger gravity when falling, then cap fall speed
        float gravityMultiplier = playerCharacter.linearVelocity.y < 0 ? fallGravityMultiplier : 1f;

        playerCharacter.gravityScale = gravityScaleAtStart * gravityMultiplier;

        if (playerCharacter.linearVelocity.y < -jumpSpeed * fallGravityMultiplier)
        {
            playerCharacter.linearVelocity = new Vector2(playerCharacter.linearVelocity.x, -jumpSpeed * fallGravityMultiplier);
        }
    }

}