using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Bedrock
    }
    
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
    public GameObject blockHighlight; // Visual indicator for selected block
    
    // Components
    private CharacterController characterController;
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private Inventory inventory;
    
    // Block placement
    private GameObject _selectedBlock;
    private Vector3 selectedBlockPosition;
    private Vector3 placementPosition;
    private bool canPlace = false;
    
    // Public property to expose selectedBlock
    public GameObject selectedBlock => _selectedBlock;
    
    void Start()
    {
        // Get required components
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<Inventory>();
        
        if (inventory == null)
        {
            Debug.LogError("Inventory component is missing!");
        }
        
        // Create block highlight if not assigned
        if (blockHighlight == null)
        {
            blockHighlight = CreateBlockHighlight();
        }
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // Create a visual highlight for the selected block
    private GameObject CreateBlockHighlight()
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlight.name = "Block Highlight";
        
        // Make it slightly larger than a block
        highlight.transform.localScale = new Vector3(1.01f, 1.01f, 1.01f);
        
        // Setup the material to be transparent
        Renderer renderer = highlight.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1f, 1f, 1f, 0.2f);
        renderer.material = material;
        
        // Make it non-solid
        Collider collider = highlight.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // Hide it initially
        highlight.SetActive(false);
        
        return highlight;
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
        
        // Reset highlight
        blockHighlight.SetActive(false);
        _selectedBlock = null;
        canPlace = false;
        
        // Cast ray from camera center
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance))
        {
            // Highlight selected block
            _selectedBlock = hit.collider.gameObject;
            selectedBlockPosition = hit.collider.transform.position;
            
            // Update block highlight position
            blockHighlight.transform.position = selectedBlockPosition;
            blockHighlight.SetActive(true);
            
            // Calculate placement position
            placementPosition = hit.point + hit.normal * 0.5f;
            placementPosition = new Vector3(
                Mathf.Round(placementPosition.x),
                Mathf.Round(placementPosition.y),
                Mathf.Round(placementPosition.z)
            );
            
            // Check if placement position is different from hit position
            if (Vector3.Distance(placementPosition, selectedBlockPosition) > 0.1f)
            {
                canPlace = true;
            }
            
            // Destroy block on left click
            if (Input.GetMouseButtonDown(0))
            {
                DestroyBlock(_selectedBlock);
            }
            
            // Place block on right click
            if (Input.GetMouseButtonDown(1) && canPlace)
            {
                PlaceBlock(placementPosition);
            }
        }
    }
    
    void DestroyBlock(GameObject block)
    {
        if (block == null) return;
        
        // Prevent destroying the player or other non-block objects
        // Check if the object has a parent with "Chunk" in the name
        Transform parent = block.transform.parent;
        if (parent == null || !parent.name.Contains("Chunk"))
        {
            Debug.Log("Not a destroyable block");
            return;
        }
        
        // Get the block type from the hit object
        BlockType blockType = GetBlockTypeFromObject(block);
        if (blockType != BlockType.Air && blockType != BlockType.Bedrock) // Can't destroy bedrock
        {
            // Add the block to inventory
            if (inventory != null)
            {
                inventory.AddBlock(blockType, 1);
                Debug.Log($"Added {blockType} to inventory");
            }
            
            // Destroy the block
            Destroy(block);
        }
    }
    
    void PlaceBlock(Vector3 position)
    {
        if (!canPlace) return;
        
        // Get the selected block type from inventory
        if (inventory != null)
        {
            BlockType selectedBlockType = inventory.GetSelectedBlockType();
            GameObject blockPrefab = inventory.GetSelectedBlockPrefab();
            
            if (blockPrefab != null && selectedBlockType != BlockType.Air)
            {
                // Check if we're not trying to place inside ourselves
                Bounds playerBounds = characterController.bounds;
                if (playerBounds.Contains(position))
                {
                    Debug.Log("Cannot place block inside player");
                    return;
                }
                
                // Try to remove the block from inventory
                if (inventory.RemoveBlock(selectedBlockType, 1))
                {
                    // Get the appropriate chunk parent
                    Transform chunkParent = GetOrCreateChunkParent(position);
                    
                    // Make sure the block prefab is active
                    if (!blockPrefab.activeSelf)
                    {
                        // Clone the prefab since we can't modify the original
                        GameObject newPrefab = GameObject.Instantiate(blockPrefab);
                        newPrefab.SetActive(true);
                        blockPrefab = newPrefab;
                    }
                    
                    // Place the block
                    GameObject newBlock = Instantiate(blockPrefab, position, Quaternion.identity, chunkParent);
                    
                    // Ensure the block is active
                    newBlock.SetActive(true);
                    
                    // Set the proper layer
                    newBlock.layer = LayerMask.NameToLayer("Default");
                    
                    // Ensure there is a collider
                    if (newBlock.GetComponent<Collider>() == null)
                    {
                        BoxCollider collider = newBlock.AddComponent<BoxCollider>();
                        collider.size = Vector3.one;
                        collider.center = Vector3.zero;
                    }
                    
                    // Ensure the block has a renderer and is visible
                    Renderer renderer = newBlock.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        // If no renderer, add a mesh filter and renderer
                        if (newBlock.GetComponent<MeshFilter>() == null)
                        {
                            MeshFilter meshFilter = newBlock.AddComponent<MeshFilter>();
                            meshFilter.mesh = CreateCubeMesh();
                        }
                        
                        renderer = newBlock.AddComponent<MeshRenderer>();
                        
                        // Assign material based on block type
                        Material material = new Material(Shader.Find("Standard"));
                        material.color = GetBlockColor(selectedBlockType);
                        renderer.material = material;
                    }
                    
                    // Ensure the renderer is enabled
                    renderer.enabled = true;
                    
                    Debug.Log($"Placed {selectedBlockType} block at {position}, Active: {newBlock.activeSelf}, Visible: {renderer.enabled}");
                }
                else
                {
                    Debug.Log($"Not enough {selectedBlockType} blocks in inventory");
                }
            }
            else
            {
                Debug.Log("No valid block selected in inventory. Block type: " + selectedBlockType + ", Prefab: " + (blockPrefab != null ? blockPrefab.name : "null"));
            }
        }
    }
    
    private Color GetBlockColor(BlockType blockType)
    {
        switch (blockType)
        {
            case BlockType.Grass:
                return new Color(0.2f, 0.8f, 0.2f);
            case BlockType.Dirt:
                return new Color(0.6f, 0.4f, 0.2f);
            case BlockType.Stone:
                return new Color(0.5f, 0.5f, 0.5f);
            case BlockType.Bedrock:
                return new Color(0.2f, 0.2f, 0.2f);
            default:
                return Color.magenta;
        }
    }
    
    private Mesh CreateCubeMesh()
    {
        Mesh mesh = new Mesh();
        
        // Define the 8 vertices of a cube
        Vector3[] vertices = new Vector3[8];
        vertices[0] = new Vector3(-0.5f, -0.5f, -0.5f);
        vertices[1] = new Vector3(0.5f, -0.5f, -0.5f);
        vertices[2] = new Vector3(0.5f, 0.5f, -0.5f);
        vertices[3] = new Vector3(-0.5f, 0.5f, -0.5f);
        vertices[4] = new Vector3(-0.5f, -0.5f, 0.5f);
        vertices[5] = new Vector3(0.5f, -0.5f, 0.5f);
        vertices[6] = new Vector3(0.5f, 0.5f, 0.5f);
        vertices[7] = new Vector3(-0.5f, 0.5f, 0.5f);
        mesh.vertices = vertices;
        
        // Define the 12 triangles (2 per face, 6 faces)
        int[] triangles = new int[36];
        // Front face
        triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
        triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
        // Back face
        triangles[6] = 5; triangles[7] = 7; triangles[8] = 4;
        triangles[9] = 5; triangles[10] = 6; triangles[11] = 7;
        // Left face
        triangles[12] = 4; triangles[13] = 3; triangles[14] = 0;
        triangles[15] = 4; triangles[16] = 7; triangles[17] = 3;
        // Right face
        triangles[18] = 1; triangles[19] = 6; triangles[20] = 5;
        triangles[21] = 1; triangles[22] = 2; triangles[23] = 6;
        // Top face
        triangles[24] = 3; triangles[25] = 6; triangles[26] = 2;
        triangles[27] = 3; triangles[28] = 7; triangles[29] = 6;
        // Bottom face
        triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
        triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;
        mesh.triangles = triangles;
        
        // Generate normals
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    Transform GetOrCreateChunkParent(Vector3 position)
    {
        // Calculate chunk coordinates (16x16 chunks)
        int chunkX = Mathf.FloorToInt(position.x / 16);
        int chunkZ = Mathf.FloorToInt(position.z / 16);
        string chunkName = $"Chunk {chunkX},{chunkZ}";
        
        // Find existing chunk
        Transform worldTransform = GameObject.Find("WorldGenerator")?.transform;
        if (worldTransform == null)
        {
            worldTransform = new GameObject("WorldGenerator").transform;
        }
        
        // Look for existing chunk
        foreach (Transform child in worldTransform)
        {
            if (child.name == chunkName)
            {
                return child;
            }
        }
        
        // Create new chunk parent
        GameObject newChunk = new GameObject(chunkName);
        newChunk.transform.parent = worldTransform;
        return newChunk.transform;
    }
    
    public BlockType GetBlockTypeFromObject(GameObject obj)
    {
        // Try to get the name of the object and determine the block type
        string objName = obj.name.ToLower();
        
        if (objName.Contains("grass")) return BlockType.Grass;
        if (objName.Contains("dirt")) return BlockType.Dirt;
        if (objName.Contains("stone")) return BlockType.Stone;
        if (objName.Contains("bedrock")) return BlockType.Bedrock;
        
        // Try to check parent name or material color as fallback
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = renderer.material.color;
            
            // Approximate color matching
            if (color.g > 0.6f && color.r < 0.5f) return BlockType.Grass;
            if (color.r > 0.4f && color.g > 0.2f && color.g < 0.5f) return BlockType.Dirt;
            if (color.r > 0.4f && color.g > 0.4f && color.b > 0.4f && color.r < 0.6f) return BlockType.Stone;
            if (color.r < 0.2f && color.g < 0.2f && color.b < 0.2f) return BlockType.Bedrock;
        }
        
        Debug.LogWarning($"Could not determine block type for {obj.name}. Using Stone as fallback.");
        return BlockType.Stone; // Fallback
    }
}