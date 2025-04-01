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

    private bool wasGrounded = true;

    private Transform cameraTransform = null;
    //private float mouseSensitivity = 100f;
    public Vector3 cameraOffset = new Vector3(0, 3, -5);
    private float cameraPitch = 0f;

    public LayerMask groundLayer;
    private Vector3 velocity = Vector3.zero;
    private float gravity = -19.81f;
    RaycastHit hit;
    Vector3 sphereOrigin;
    private Stats playerStats = null;

    [SerializeField] private float mouseSensitivity = 10.0f;
    [SerializeField] private float controllerSensitivity = 300.0f;
    private float currentSensitivity;

    private enum InputType { Mouse, Controller }
    private InputType currentInputType = InputType.Mouse; // Default to mouse

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
                jump.Enable();
                if (jump != null)
                {
                    jump.performed += OnJump;
                }

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
        jump.Disable();
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
        currentInputType = InputType.Mouse;
        currentSensitivity = mouseSensitivity;

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
            if (playerStats.IsWinner.Value)
                PlayFootstepSound();
        }
        else if (!hasInput && isMoving)
        {
            isMoving = false;
            AudioManager.Instance.StopSound(eSound.WalkSpeed);
        }

        if (hasInput && isMoving)
        {
            if (playerStats.IsWinner.Value)
                UpdateFootstepSpeed();
        }

        if (!hasInput)
        {
            return;
        }
        if (!playerStats.IsWinner.Value)
        {
            input = new Vector2 { x = 0, y = 0 };
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
        isGrounded = IsGrounded();

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0f)
        {
            velocity.y = -0.5f;
        }

        if (isGrounded && !wasGrounded && isMoving)
        {
            AudioManager.Instance.PlaySound(eSound.WalkSpeed);
        }

        wasGrounded = isGrounded;

        controller.Move(velocity * Time.deltaTime);
    }



    private void HandleJumping()
    {
        if (isJumping) return;
        if (!playerStats.IsWinner.Value) return;


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

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!SceneHandler.Instance.IsLocalGame)
            if (!IsOwner)
                return;

        if (!playerStats.IsWinner.Value) return;


        if (isGrounded)
        {
            AudioManager.Instance.PlaySound(eSound.Jump);
            isJumping = false;
            HandleJumping();
        }
    }

    private bool IsGrounded()
    {
        if (controller.isGrounded)
        {
            isJumping = false;
            return true;
        }

        float groundCheckRadius = 0.1f;
        float groundCheckDistance = 1.09f;
        sphereOrigin = transform.position + Vector3.up * 0.11f; // why I do this? It works though

        isGrounded = Physics.SphereCast(sphereOrigin, groundCheckRadius, Vector3.down,
                                           out hit, groundCheckDistance, groundLayer);
        if (isGrounded)
            isJumping = false;
        return isGrounded;
    }

    void Update()
    {
        if (!SceneHandler.Instance.IsLocalGame)
            if (!IsOwner) return;


        if (playerStats.HP.Value <= 0 && !isDead)
        {
            playerStats.IsWinner.Value = false;
            isDead = true;
            AudioManager.Instance.PlaySound(eSound.Death);
            //return;
        }

        HandleController();

        isGrounded = IsGrounded();
        //Debug.Log("Is grounded = " + isGrounded);
        //Debug.Log("Is jumping = " + isJumping);
        //Debug.Log("Is dead = " + !playerStats.IsWinner.Value);
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
            float yawRotation = mouseInput.x * currentSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * yawRotation);

            cameraPitch -= mouseInput.y * currentSensitivity * Time.deltaTime;
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

    private void HandleController()
    {
        bool isUsingController = Gamepad.current != null && (
            Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f ||
            Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f ||
            Gamepad.current.buttonSouth.isPressed ||
            Gamepad.current.buttonWest.isPressed ||
            Gamepad.current.buttonNorth.isPressed ||
            Gamepad.current.buttonEast.isPressed
        );

        bool isUsingMouse = Mouse.current != null && (
            Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f ||
            Mouse.current.leftButton.isPressed ||
            Mouse.current.rightButton.isPressed
        );

        if (isUsingMouse && currentInputType != InputType.Mouse)
        {
            currentInputType = InputType.Mouse;
            currentSensitivity = mouseSensitivity;
            Debug.Log("Switched to Mouse");
        }
        else if (isUsingController && currentInputType != InputType.Controller)
        {
            currentInputType = InputType.Controller;
            currentSensitivity = controllerSensitivity;
            Debug.Log("Switched to Controller");
        }
        else if (!isUsingMouse && !isUsingController) // If nothing is active, reset to last input type
        {
            if (currentInputType == InputType.Controller && Gamepad.current == null)
            {
                currentInputType = InputType.Mouse;
                currentSensitivity = mouseSensitivity;
                Debug.Log("No controller detected, switching back to Mouse");
            }
        }
    }
}
