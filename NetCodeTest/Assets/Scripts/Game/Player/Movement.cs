using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using Unity.VisualScripting;
using FMOD.Studio;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections;

public class Movement : NetworkBehaviour
{
    public float acceleration = 5f;
    private float jumpForce = 8f;
    private bool isJumping = false;
    private bool isMoving = false;

    private bool wasGrounded = true; // Start assuming the player is on the ground
    private bool justLanded = false; // True only for the first frame after landing

    private Transform cameraTransform = null;
    private float mouseSensitivity = 100f;
    public Vector3 cameraOffset = new Vector3(0, 3, -5);
    private float cameraPitch = 0f;

    public LayerMask groundLayer;
    private Vector3 velocity = Vector3.zero;
    private float gravity = -19.81f;
    RaycastHit hit;
    Vector3 sphereOrigin;
    private Stats playerStats = null;


    private CharacterController controller = null;
    [SerializeField] private InputActionAsset actionAsset = null;
    [SerializeField] private InputActionMap game = null;
    [SerializeField] private InputAction jump = null;
    [SerializeField] private InputAction shift = null;
    [SerializeField] private InputAction move = null;
    [SerializeField] private InputAction look = null;


    private InputHandler inputHandler = null;

    private bool isDead = false;
    private bool isGrounded = false;
    private void OnEnable()
    {
        if (SceneHandler.Instance.IsLocalGame)
        {
            actionAsset = this.GetComponent<PlayerInput>().actions;
            if (actionAsset)
            {
                game = actionAsset.FindActionMap("Game");

                jump = game.FindAction("Jump");
                jump.performed += OnJump;

                shift = game.FindAction("Shift");
                move = game.FindAction("Move");
                look = game.FindAction("Look");
            }
        }
        else
        {
            inputHandler = InputHandler.Instance;
        }
    }

    private void OnDisable()
    {
        jump.performed -= OnJump;
    }
    void Start()
    {
        ReStart();
    }

    void SetupCameraForLocalMultiplayer()
    {
        playerStats = GetComponent<Stats>();
        Camera cam = GetComponentInChildren<Camera>();
        int playerIndex = GetComponent<Stats>().ID.Value;

        switch (SceneHandler.Instance.MaxPlayerCount)
        {
            case 1: // Fullscreen
                cam.rect = new Rect(0, 0, 1, 1);
                break;
            case 2: // Two players, top and bottom split
                cam.rect = (playerIndex == 0) ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
                break;
            case 3: // Three players (Left, Right, Bottom)
                if (playerIndex == 0) cam.rect = new Rect(0, 0.5f, 0.5f, 0.5f);
                else if (playerIndex == 1) cam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                else cam.rect = new Rect(0, 0, 1, 0.5f);
                break;
            case 4: // Four players (2x2 grid)
                float xPos = (playerIndex % 2 == 0) ? 0 : 0.5f;
                float yPos = (playerIndex < 2) ? 0.5f : 0;
                cam.rect = new Rect(xPos, yPos, 0.5f, 0.5f);
                break;
        }
    }

    public void ReStart()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = GetComponentInChildren<Camera>().transform;
        Camera camera = GetComponentInChildren<Camera>();
        camera.enabled = true;
        playerStats = GetComponent<Stats>();

        if (SceneHandler.Instance.IsLocalGame)
        {
            SetupCameraForLocalMultiplayer();
            playerStats = GetComponent<Stats>();
        }
        else
        {
            if (!IsOwner)
            {
                GetComponentInChildren<Camera>().gameObject.SetActive(false);
            }
            else
            {
                playerStats = GetComponent<Stats>();
            }
        }
    }


    private void HandlePlayerMovement()
    {
        Vector2 input;
        if (SceneHandler.Instance.IsLocalGame)
        {
            input = move.ReadValue<Vector2>();
        }
        else
        {
            input = inputHandler.MoveInput;
        }

        bool hasInput = input.sqrMagnitude > 0.1f;

        if (hasInput && !isMoving)
        {
            isMoving = true;
            PlayFootstepSound();
        }
        else if (!hasInput && isMoving)
        {
            isMoving = false;
            AudioManager.Instance.StopSound(eSound.WalkSpeed);
        }

        if (hasInput && isMoving)
        {
            UpdateFootstepSpeed();
        }

        if (!hasInput)
        {
            return;
        }

        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        float moveSpeed = (SceneHandler.Instance.IsLocalGame) ?
                          (shift.inProgress ? playerStats.MaxSpeed.Value : playerStats.Speed.Value) :
                          (inputHandler.ShiftTriggered ? playerStats.MaxSpeed.Value : playerStats.Speed.Value);

        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
        controller.Move(movement);
    }

    private void PlayFootstepSound()
    {
        AudioManager.Instance.PlaySound(eSound.WalkSpeed);
        if (SceneHandler.Instance.IsLocalGame)
        {
            if (shift.inProgress)
            {
                AudioManager.Instance.SetParameter(eSound.WalkSpeed, 1);
            }
            else
            {
                AudioManager.Instance.SetParameter(eSound.WalkSpeed, 0);
            }
        }
        else
        {
            if (inputHandler.ShiftTriggered)
            {
                AudioManager.Instance.SetParameter(eSound.WalkSpeed, 1);
            }
            else
            {
                AudioManager.Instance.SetParameter(eSound.WalkSpeed, 0);
            }
        }
    }

    private void UpdateFootstepSpeed()
    {
        if (!isGrounded)
        {
            AudioManager.Instance.StopSound(eSound.WalkSpeed);
            return;
        }
        //AudioManager.Instance.PlaySound(eSound.WalkSpeed);
        float speedParam = (SceneHandler.Instance.IsLocalGame) ?
                                (shift.inProgress ? 1 : 0) :
                                (inputHandler.ShiftTriggered ? 1 : 0);
        AudioManager.Instance.SetParameter(eSound.WalkSpeed, speedParam);
    }


    private void ApplyGravity()
    {
        isGrounded = IsGrounded(); // Check if the player is on the ground

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0f)
        {
            velocity.y = -0.5f; // Small downward force to stick to ground
        }

        // Detect landing
        if (isGrounded && !wasGrounded && isMoving)
        {
            justLanded = true; // Player landed this frame
            AudioManager.Instance.PlaySound(eSound.WalkSpeed);
        }
        else
        {
            //AudioManager.Instance.StopSound(eSound.WalkSpeed);
            justLanded = false;
        }

        wasGrounded = isGrounded; // Update last frame’s ground state

        controller.Move(velocity * Time.deltaTime);
    }



    private void HandleJumping()
    {
        if (isJumping) return;

        if (SceneHandler.Instance.IsLocalGame)
        {
            if (isGrounded && jump.triggered)
            {
                velocity.y = jumpForce;
                isJumping = true;
            }
        }
        else
        {
            if (isGrounded && inputHandler.JumpTriggered)
            {
                AudioManager.Instance.PlaySound(eSound.Jump);
                velocity.y = jumpForce;
                isJumping = true;
            }
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (!SceneHandler.Instance.IsLocalGame)
            if (!IsOwner)
                return;

        if (isGrounded)
        {
            AudioManager.Instance.PlaySound(eSound.Jump);
            isJumping = false;
            HandleJumping();
        }
    }

    private bool IsGrounded()
    {
        if (controller.isGrounded) return true;

        float groundCheckRadius = 0.1f;
        float groundCheckDistance = 1.1f;
        sphereOrigin = transform.position + Vector3.up * 0.11f; // why I do this? It works though

        isGrounded = Physics.SphereCast(sphereOrigin, groundCheckRadius, Vector3.down,
                                           out hit, groundCheckDistance, groundLayer);
        return isGrounded;
    }

    void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame)
            if (!IsOwner) return;
        if (isDead) return;


        if (playerStats.HP.Value <= 0)
        {
            isDead = true;
            AudioManager.Instance.PlaySound(eSound.Death);
            return;
        }

        isGrounded = IsGrounded();
        HandlePlayerMovement();
        ApplyGravity();
        HandleJumping();
        HandleCameraMovement();
    }

    private void HandleCameraMovement()
    {
        if (cameraTransform == null)
        {
            Debug.Log("Camera Transform null!");
            return;
        }

        Vector2 mouseInput;
        if (SceneHandler.Instance.IsLocalGame)
        {
            mouseInput = look.ReadValue<Vector2>();
        }
        else
        {
            mouseInput = inputHandler.LookInput;
        }

        if (mouseInput.sqrMagnitude > 0.1f)
        {
            float yawRotation = mouseInput.x * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * yawRotation);

            cameraPitch -= mouseInput.y * mouseSensitivity * Time.deltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch, -100f, 40f);
        }

        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, transform.eulerAngles.y, 0);
        Vector3 desiredCameraPosition = transform.position + cameraRotation * cameraOffset;
        desiredCameraPosition.y = Mathf.Clamp(desiredCameraPosition.y, 1, 100);

        Vector3 direction = desiredCameraPosition - (transform.position + Vector3.up * 1.5f);
        float distance = direction.magnitude;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position + Vector3.up * 1.5f, 0.2f, direction.normalized, out hit, distance))
        {
            desiredCameraPosition = hit.point - direction.normalized * 0.2f;
        }

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredCameraPosition, Time.deltaTime * 10f);
        cameraTransform.LookAt(transform.position + Vector3.up * 1.5f);
    }


}
