using UnityEngine;
using System.Collections;

// Add this script to your player object
public class PlayerSetup : MonoBehaviour
{
    public WorldGenerator worldGenerator;
    public float heightAboveGround = 5f;
    
    // Increase these values if needed
    public int maxHeight = 100;
    public int searchRadius = 10;
    
    private CharacterController characterController;
    private PlayerController playerController;
    
    void Awake()
    {
        // Disable character controller and player controller
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        
        if (characterController) 
        {
            characterController.enabled = false;
            
            // Disable physics if there's a rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
        }
        
        if (playerController) playerController.enabled = false;
        
        // Move player to a holding position during generation
        transform.position = new Vector3(0, 1000, 0); // Far above the world, not -9999 which could cause issues
        
        Debug.Log("Player controls and physics disabled until properly positioned");
    }
    
    void Start()
    {
        if (worldGenerator == null)
        {
            worldGenerator = FindFirstObjectByType<WorldGenerator>();
            if (worldGenerator == null)
            {
                Debug.LogError("No WorldGenerator found in scene!");
                return;
            }
        }
        
        // Start delayed positioning with multiple attempts
        StartCoroutine(PositionPlayerWithDelay());
    }
    
    IEnumerator PositionPlayerWithDelay()
    {
        // Wait for world to start generating
        yield return new WaitForSeconds(1f);
        
        // Wait until world generation is complete
        while (worldGenerator.IsGenerating)
        {
            Debug.Log("Waiting for world generation to complete...");
            yield return new WaitForSeconds(0.5f);
        }
        
        // Wait a bit more for physics to settle
        yield return new WaitForSeconds(1.5f);
        
        Debug.Log("Attempting to find generated terrain and position player...");

        // Search for terrain at multiple potential locations
        Vector3[] potentialPositions = new Vector3[]
        {
            new Vector3(0, 0, 0),                                       // Origin
            new Vector3(8, 0, 8),                                       // Simple 8,8 coordinates
            new Vector3(worldGenerator.chunkSize/2, 0, worldGenerator.chunkSize/2), // Half chunk
            new Vector3(worldGenerator.worldSizeX * worldGenerator.chunkSize / 2f,  // Calculated center
                        0, 
                        worldGenerator.worldSizeZ * worldGenerator.chunkSize / 2f)
        };

        bool foundPosition = false;
        Vector3 spawnPos = Vector3.zero;

        foreach (Vector3 testPos in potentialPositions)
        {
            Debug.Log($"Testing position: {testPos}");
            
            // Find highest block at this position
            float highestY = FindHighestBlockWithRaycast(testPos.x, testPos.z);
            
            if (highestY > 0)
            {
                // Found valid terrain
                spawnPos = new Vector3(testPos.x, highestY + heightAboveGround, testPos.z);
                Debug.Log($"FOUND VALID TERRAIN at {testPos.x}, {testPos.z} with height {highestY}");
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition)
        {
            // Fallback: Do a grid search to find ANY land
            Debug.Log("No terrain found at expected positions. Performing grid search for any terrain...");
            
            // Search in a grid pattern
            int gridSize = 32;
            int step = 4;
            
            for (int x = 0; x < gridSize; x += step)
            {
                for (int z = 0; z < gridSize; z += step)
                {
                    float highestY = FindHighestBlockWithRaycast(x, z);
                    
                    if (highestY > 0)
                    {
                        spawnPos = new Vector3(x, highestY + heightAboveGround, z);
                        Debug.Log($"GRID SEARCH: Found terrain at {x}, {z} with height {highestY}");
                        foundPosition = true;
                        break;
                    }
                }
                
                if (foundPosition) break;
            }
        }

        if (!foundPosition)
        {
            // Ultimate fallback - use default position
            spawnPos = new Vector3(0, 10, 0);
            Debug.LogWarning("NO TERRAIN FOUND ANYWHERE! Using fallback position at origin.");
        }

        // Position player
        transform.position = spawnPos;
        Debug.Log($"FINAL PLAYER POSITION: {spawnPos}");
        
        // Re-enable controls
        yield return new WaitForSeconds(0.5f);
        if (characterController) characterController.enabled = true;
        if (playerController) playerController.enabled = true;
        
        // Re-enable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;
        
        Debug.Log("Player controls and physics re-enabled. Player is ready!");
    }
    
    float FindHighestBlockWithRaycast(float centerX, float centerZ)
    {
        // Cast rays in a grid pattern to find the highest point
        float highestY = 0;
        bool foundBlock = false;
        
        Debug.Log($"Searching for highest block at X={centerX}, Z={centerZ}");
        
        // Increase the search height and distance
        float searchHeight = 200f; // Much higher to catch any terrain
        float searchDistance = 400f; // Much longer distance
        
        // Cast a ray straight down
        RaycastHit hit;
        
        // Try with layermask that includes everything
        int layerMask = Physics.DefaultRaycastLayers;
        
        if (Physics.Raycast(new Vector3(centerX, searchHeight, centerZ), Vector3.down, out hit, searchDistance, layerMask))
        {
            highestY = hit.point.y;
            foundBlock = true;
            Debug.Log($"HIT! Found block at ({centerX}, {highestY}, {centerZ}), Object={hit.collider.gameObject.name}, Tag={hit.collider.gameObject.tag}");
            
            // Print parent information if any
            if (hit.collider.gameObject.transform.parent != null)
            {
                Debug.Log($"Parent object: {hit.collider.gameObject.transform.parent.name}");
            }
        }
        else
        {
            Debug.Log($"No block found directly below ({centerX}, {centerZ})");
            
            // Search in small expanding spiral
            for (float radius = 0.5f; radius <= 5f; radius += 0.5f)
            {
                bool foundInSpiral = false;
                
                for (float angle = 0; angle < 360; angle += 45f)
                {
                    float x = centerX + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float z = centerZ + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
                    
                    if (Physics.Raycast(new Vector3(x, searchHeight, z), Vector3.down, out hit, searchDistance, layerMask))
                    {
                        if (!foundBlock || hit.point.y > highestY)
                        {
                            highestY = hit.point.y;
                            foundBlock = true;
                            foundInSpiral = true;
                            Debug.Log($"SPIRAL HIT! Found block at offset ({x-centerX},{z-centerZ}): Y={highestY}, Object={hit.collider.gameObject.name}");
                        }
                    }
                }
                
                if (foundInSpiral) break;
            }
        }
        
        if (!foundBlock)
        {
            Debug.Log($"No blocks found around ({centerX}, {centerZ})");
        }
        
        return foundBlock ? highestY : -1; // Return -1 if no block found
    }
}