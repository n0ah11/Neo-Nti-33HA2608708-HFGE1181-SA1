using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    
    public PlayerInputActions InputActions;
    
    [Header("Movement Settings")] 
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private Rigidbody2D rb;
    private float movementMultiplier = 1f;
    private Vector2 moveDirection;
    private Vector2 rotationDirection;

    // SA1: bigger donuts move a little faster so the (zoomed-out) game doesn't feel sluggish late on
    [SerializeField] private float speedPerSizeUnit = 0.6f;
    private SizeHandler sizeHandler;
    
    [Header("Collision Settings")]
    [SerializeField] private float rayDistance = 5f;
    [SerializeField] private LayerMask hitLayers;
    private float targetDistance;
    private bool obstacleInPath = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sizeHandler = GetComponent<SizeHandler>();
        InputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        InputActions.Player.Enable();
        
        InputActions.Player.Move.performed += context => moveDirection = context.ReadValue<Vector2>();
        InputActions.Player.Move.canceled += context => moveDirection = Vector2.zero;
        
        InputActions.Player.Look.performed += context => rotationDirection = context.ReadValue<Vector2>();
        InputActions.Player.Look.canceled += context => rotationDirection = Vector2.zero;
    }

    private void OnDisable()
    {
        InputActions.Player.Disable();
    }
    
    void Update()
    {
        // SA1: also stop on Game Over, and zero the velocity so the player doesn't drift while paused
        if (GameStateManager.Instance.GetGameState() != GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        movementMultiplier = 1f + speedPerSizeUnit * ((sizeHandler != null ? sizeHandler.GetSize() : 1f) - 1f);

        // SA1: mobile control schema - the on-screen joystick steers (donut turns to face the stick)
        // and pushes forward by how far the stick is dragged. Desktop keeps the original W + mouse aim.
        if (MobileControls.UseTouchControls)
        {
            TouchSteeringHandler();
            return;
        }
        
        MovementHandler();
        RotationHandler();
    }

    public void MovementHandler()
    {
        if (!obstacleInPath)
        {
            Vector2 movement = transform.up * moveDirection.y;
            rb.linearVelocity = movement * movementSpeed * movementMultiplier;
        }
    }

    public void RotationHandler()
    {
        if (Mouse.current == null) return; // SA1: no mouse on phones (was a NullReferenceException)

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        // SA1: the camera is perspective (CameraHandler zooms by moving on Z), so the screen point needs
        // the camera's distance to the play plane, otherwise it returns the camera position itself.
        Camera cam = Camera.main;
        Vector3 mouseWorldPosition = cam.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y,
            transform.position.z - cam.transform.position.z));

        Vector3 direction = (mouseWorldPosition - transform.position);
        direction.z = 0; 

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.up = Vector3.Slerp(transform.up, direction.normalized, rotationSpeed * Time.deltaTime);
        }
    }

    // SA1: touch steering used by the mobile build
    public void TouchSteeringHandler()
    {
        float amount = Mathf.Clamp01(moveDirection.magnitude);
        if (amount > 0.1f)
        {
            Vector3 dir = new Vector3(moveDirection.x, moveDirection.y, 0f).normalized;
            transform.up = Vector3.Slerp(transform.up, dir, rotationSpeed * Time.deltaTime);
            rb.linearVelocity = (Vector2)transform.up * amount * movementSpeed * movementMultiplier;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

}
