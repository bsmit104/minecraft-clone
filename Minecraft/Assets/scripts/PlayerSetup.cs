using UnityEngine;

// Add this script to your player object
public class PlayerSetup : MonoBehaviour
{
    public WorldGenerator worldGenerator;
    public float heightAboveGround = 2f;
    
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
        
        // Position player at the center of the world, above the terrain
        float worldCenterX = worldGenerator.worldSizeX * worldGenerator.chunkSize / 2f;
        float worldCenterZ = worldGenerator.worldSizeZ * worldGenerator.chunkSize / 2f;
        
        // Sample terrain height at this position (simplified)
        float perlinHeight = Mathf.PerlinNoise(worldCenterX / worldGenerator.terrainScale, 
                                               worldCenterZ / worldGenerator.terrainScale) * 
                                               worldGenerator.terrainHeight + 5;
        
        // Position player above the ground
        transform.position = new Vector3(worldCenterX, perlinHeight + heightAboveGround, worldCenterZ);
        
        Debug.Log($"Player positioned at {transform.position}");
    }
}