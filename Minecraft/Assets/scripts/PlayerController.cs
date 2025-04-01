using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -20f;
    
    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;
    
    [Header("Interaction")]
    public float interactionDistance = 5f;
    public LayerMask interactionLayer;
    
    // Components
    private CharacterController characterController;
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private Vector3 playerVelocity;
    private bool isGrounded;
    
    // Block placement
    private GameObject selectedBlock;
    private GameObject blockToPlace;
    
    void Start()
    {
        // Get required components
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        // Check if player is grounded
        isGrounded = characterController.isGrounded;
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // Small negative value to keep player grounded
        }
        
        // Handle mouse look
        HandleMouseLook();
        
        // Handle movement
        HandleMovement();
        
        // Handle jumping
        HandleJump();
        
        // Apply gravity
        ApplyGravity();
        
        // Handle block interaction
        HandleBlockInteraction();
    }
    
    void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate camera vertically
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    void HandleMovement()
    {
        // Get movement input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        
        // Calculate movement direction relative to player orientation
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        
        // Determine speed (run or walk)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        
        // Apply movement
        characterController.Move(move * currentSpeed * Time.deltaTime);
    }
    
    void HandleJump()
    {
        // Jump when space is pressed and player is grounded
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }
    
    void ApplyGravity()
    {
        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }
    
    void HandleBlockInteraction()
    {
        RaycastHit hit;
        
        // Cast ray from camera center
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, interactionLayer))
        {
            // Highlight selected block (implement visual feedback in your game)
            selectedBlock = hit.collider.gameObject;
            
            // Destroy block on left click
            if (Input.GetMouseButtonDown(0))
            {
                Destroy(selectedBlock);
            }
            
            // Place block on right click
            if (Input.GetMouseButtonDown(1))
            {
                // Calculate position for new block based on hit normal
                Vector3 placePosition = hit.point + hit.normal * 0.5f;
                
                // Round to nearest block position (assuming 1 unit blocks)
                placePosition = new Vector3(
                    Mathf.Round(placePosition.x),
                    Mathf.Round(placePosition.y),
                    Mathf.Round(placePosition.z)
                );
                
                // Place block (you'll need to implement a block placement system)
                PlaceBlock(placePosition);
            }
        }
        else
        {
            selectedBlock = null;
        }
    }
    
    void PlaceBlock(Vector3 position)
    {
        // This is a placeholder - implement your block placement logic here
        // It might involve instantiating a cube prefab or calling a world generator function
        
        GameObject newBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newBlock.transform.position = position;
        newBlock.layer = interactionLayer;
        
        // You'll likely want to replace this with your own block system
    }
}