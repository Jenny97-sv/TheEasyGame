using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance = null;

    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset inputActionAsset;

    [Header("Action Map Name References")]
    //[SerializeField] private string actionMapName = "Game";

    [Header("Action Name References")]
    [SerializeField] private string move = "Move";
    [SerializeField] private string look = "Look";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string shoot = "Shoot";
    [SerializeField] private string shift = "Shift";

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    public InputAction shootAction; // Ugly, but how else?
    private InputAction shiftAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool ShootTriggered { get; private set; }
    public bool ShiftTriggered { get; private set; }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        moveAction = inputActionAsset.FindAction(move);
        lookAction = inputActionAsset.FindAction(look);
        jumpAction = inputActionAsset.FindAction(jump);
        shootAction = inputActionAsset.FindAction(shoot);
        shiftAction = inputActionAsset.FindAction(shift);

        RegisterInputActions();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        shootAction.Enable();
        jumpAction.performed += context => JumpTriggered = true;
        jumpAction.canceled += context => JumpTriggered = false;

        shootAction.performed += context => ShootTriggered = true;
        shootAction.canceled += context => ShootTriggered = false;
        shiftAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.performed -= context => JumpTriggered = true;
        jumpAction.canceled -= context => JumpTriggered = false;

        shootAction.performed -= context => ShootTriggered = true;
        shootAction.canceled -= context => ShootTriggered = false;
        jumpAction.Disable();
        shootAction.Disable();
        shiftAction.Disable();
    }
    void RegisterInputActions()
    {
        moveAction.performed += context => MoveInput = context.ReadValue<Vector2>();
        moveAction.canceled += context => MoveInput = Vector2.zero;

        lookAction.performed += context => LookInput = context.ReadValue<Vector2>();
        lookAction.canceled += context => LookInput = Vector2.zero;

        jumpAction.performed += context => JumpTriggered = true;
        jumpAction.canceled += context => JumpTriggered = false;

        shootAction.performed += context => ShootTriggered = true;
        shootAction.canceled += context => ShootTriggered = false;

        shiftAction.performed += context => ShiftTriggered = true;
        shiftAction.canceled += context => ShiftTriggered = false;

        //shootAction.started += context => OnShootPressed?.Invoke();
    }
}
