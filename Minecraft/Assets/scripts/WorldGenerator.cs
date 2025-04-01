using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Bedrock
    }
    
    [Header("World Settings")]
    public int worldSizeX = 16;    // Size in chunks
    public int worldSizeZ = 16;
    public int chunkSize = 16;     // Size of a chunk in blocks
    public int worldHeight = 128;
    
    [Header("Terrain Generation")]
    public float terrainScale = 20f;      // Perlin noise scale
    public float terrainHeight = 20f;     // Max terrain height
    public int dirtLayerDepth = 3;        // Depth of dirt below surface
    
    [Header("Block Prefabs")]
    public GameObject grassBlock;
    public GameObject dirtBlock;
    public GameObject stoneBlock;
    public GameObject bedrockBlock;
    
    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public int maxBlocksPerFrame = 1000;
    public Transform playerTransform;
    
    // Internal variables
    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();
    private Queue<BlockData> blockQueue = new Queue<BlockData>();
    private HashSet<Vector3Int> blockSet = new HashSet<Vector3Int>();
    private bool isGenerating = false;
    
    public bool IsGenerating => isGenerating;
    
    // Struct to hold block data in queue
    private struct BlockData
    {
        public Vector3Int position;
        public BlockType type;
        public GameObject parent;
        
        public BlockData(Vector3Int pos, BlockType t, GameObject p)
        {
            position = pos;
            type = t;
            parent = p;
        }
    }
    
    void Start()
    {
        // Create basic block prefabs if not assigned
        CreateDefaultBlockPrefabs();
        
        if (generateOnStart)
        {
            // If no player transform is set, use the main camera position
            if (playerTransform == null)
            {
                playerTransform = Camera.main.transform;
                Debug.Log("No player transform set, using main camera");
            }
            
            // Generate initial chunks around player
            StartCoroutine(GenerateWorld());
        }
    }
    
    void CreateDefaultBlockPrefabs()
    {
        if (grassBlock == null) grassBlock = CreateFallbackBlock(Color.green);
        if (dirtBlock == null) dirtBlock = CreateFallbackBlock(new Color(0.5f, 0.25f, 0));
        if (stoneBlock == null) stoneBlock = CreateFallbackBlock(Color.gray);
        if (bedrockBlock == null) bedrockBlock = CreateFallbackBlock(Color.black);
    }
    
    IEnumerator GenerateWorld()
    {
        isGenerating = true;
        Debug.Log("Starting world generation");
        
        // Get player chunk position
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(playerTransform.position.x / chunkSize),
            Mathf.FloorToInt(playerTransform.position.z / chunkSize)
        );
        
        // Generate chunks in a square around player
        int viewDistance = 2; // Reduced for testing
        
        for (int x = playerChunk.x - viewDistance; x <= playerChunk.x + viewDistance; x++)
        {
            for (int z = playerChunk.y - viewDistance; z <= playerChunk.y + viewDistance; z++)
            {
                Vector2Int chunkPos = new Vector2Int(x, z);
                GenerateChunk(chunkPos);
                Debug.Log($"Generated chunk at {chunkPos}");
            }
            
            // Wait a frame between generating rows of chunks to prevent freezing
            yield return null;
        }
        
        // Process block queue
        Debug.Log($"Queued {blockQueue.Count} blocks for creation");
        StartCoroutine(ProcessBlockQueue());
    }
    
    void GenerateChunk(Vector2Int chunkPos)
    {
        // Create chunk parent object
        GameObject chunkObject = new GameObject("Chunk " + chunkPos.x + "," + chunkPos.y);
        chunkObject.transform.parent = transform;
        chunks.Add(chunkPos, chunkObject);
        
        // Determine block positions for this chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Calculate world position
                int worldX = chunkPos.x * chunkSize + x;
                int worldZ = chunkPos.y * chunkSize + z;
                
                // Generate terrain height at this position
                float perlinHeight = Mathf.PerlinNoise(worldX / terrainScale, worldZ / terrainScale) * terrainHeight;
                int height = Mathf.FloorToInt(perlinHeight) + 5; // Start at y=5 instead of worldHeight/2 for testing
                
                // Queue blocks for creation
                // Bedrock at bottom
                QueueBlock(new Vector3Int(worldX, 0, worldZ), BlockType.Bedrock, chunkObject);
                
                // Stone from bottom up to a few blocks below surface
                for (int y = 1; y < height - dirtLayerDepth; y++)
                {
                    QueueBlock(new Vector3Int(worldX, y, worldZ), BlockType.Stone, chunkObject);
                }
                
                // Dirt layers
                for (int y = height - dirtLayerDepth; y < height; y++)
                {
                    QueueBlock(new Vector3Int(worldX, y, worldZ), BlockType.Dirt, chunkObject);
                }
                
                // Grass on top
                QueueBlock(new Vector3Int(worldX, height, worldZ), BlockType.Grass, chunkObject);
            }
        }
    }
    
    void QueueBlock(Vector3Int position, BlockType type, GameObject parent)
    {
        // Add block position and type to queue
        if (!blockSet.Contains(position))
        {
            blockQueue.Enqueue(new BlockData(position, type, parent));
            blockSet.Add(position);
        }
    }
    
    IEnumerator ProcessBlockQueue()
    {
        int blocksThisFrame = 0;
        int totalCreated = 0;
        
        while (blockQueue.Count > 0)
        {
            BlockData blockData = blockQueue.Dequeue();
            Vector3Int blockPos = blockData.position;
            BlockType blockType = blockData.type;
            GameObject parentChunk = blockData.parent;
            
            // Create the block
            GameObject blockPrefab = GetBlockPrefab(blockType);
            
            // Instantiate block at the correct position
            GameObject block = Instantiate(blockPrefab, new Vector3(blockPos.x, blockPos.y, blockPos.z), Quaternion.identity);
            block.SetActive(true); // Make sure the block is active
            
            // Set the parent to the correct chunk
            block.transform.parent = parentChunk.transform;
            
            // Make sure the block is visible and has a renderer
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Block at {blockPos} has no renderer component");
            }
            
            // Add a collider if it doesn't have one
            if (block.GetComponent<Collider>() == null)
            {
                block.AddComponent<BoxCollider>();
            }
            
            totalCreated++;
            
            // Check if we need to wait a frame
            blocksThisFrame++;
            if (blocksThisFrame >= maxBlocksPerFrame)
            {
                blocksThisFrame = 0;
                yield return null;
            }
        }
        
        blockSet.Clear();
        Debug.Log($"World generation complete! Created {totalCreated} blocks");
        isGenerating = false;
    }
    
    GameObject GetBlockPrefab(BlockType type)
    {
        // Return the appropriate block prefab based on type
        switch (type)
        {
            case BlockType.Grass:
                return grassBlock;
            case BlockType.Dirt:
                return dirtBlock;
            case BlockType.Stone:
                return stoneBlock;
            case BlockType.Bedrock:
                return bedrockBlock;
            default:
                return CreateFallbackBlock(Color.magenta);
        }
    }
    
    GameObject CreateFallbackBlock(Color color)
    {
        // Create a basic cube with the specified color
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = cube.GetComponent<Renderer>();
        
        // Create a new material with the specified color
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        renderer.material = material;
        
        // Set the cube inactive so it can be used as a prefab
        cube.SetActive(false);
        DontDestroyOnLoad(cube); // Prevent it from being destroyed
        
        return cube;
    }
    
    // Debug gizmos to visualize the world bounds
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3(worldSizeX * chunkSize / 2f, worldHeight / 2f, worldSizeZ * chunkSize / 2f),
            new Vector3(worldSizeX * chunkSize, worldHeight, worldSizeZ * chunkSize)
        );
    }
}