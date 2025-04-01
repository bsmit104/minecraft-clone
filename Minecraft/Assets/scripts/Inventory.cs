using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public PlayerController.BlockType blockType;
        public int count;
        public GameObject blockPrefab;
    }
    
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int selectedSlot = 0;
    public int maxStackSize = 64; // Maximum stack size
    
    void Start()
    {
        // Initialize inventory with dirt blocks
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        // Clear any existing slots
        slots.Clear();
        
        // Add 64 dirt blocks for testing
        AddBlock(PlayerController.BlockType.Dirt, 64);
        
        // Also add some other blocks in smaller quantities
        AddBlock(PlayerController.BlockType.Grass, 32);
        AddBlock(PlayerController.BlockType.Stone, 16);
    }
    
    public void AddBlock(PlayerController.BlockType type, int count)
    {
        // Find existing slot with same block type that isn't full
        InventorySlot existingSlot = slots.Find(slot => slot.blockType == type && slot.count < maxStackSize);
        
        if (existingSlot != null)
        {
            // Calculate how many blocks can fit in this slot
            int spaceInSlot = maxStackSize - existingSlot.count;
            int blocksToAdd = Mathf.Min(count, spaceInSlot);
            
            // Add blocks to existing slot
            existingSlot.count += blocksToAdd;
            count -= blocksToAdd;
        }
        
        // If we still have blocks to add, find or create new slots
        while (count > 0)
        {
            // Create new slot
            InventorySlot newSlot = new InventorySlot
            {
                blockType = type,
                count = Mathf.Min(count, maxStackSize),
                blockPrefab = GetBlockPrefab(type)
            };
            
            slots.Add(newSlot);
            count -= newSlot.count;
        }
    }
    
    public bool RemoveBlock(PlayerController.BlockType type, int count = 1)
    {
        // Check if we have enough blocks
        int availableCount = 0;
        foreach (InventorySlot slot in slots)
        {
            if (slot.blockType == type)
            {
                availableCount += slot.count;
            }
        }
        
        if (availableCount < count)
        {
            Debug.Log($"Not enough {type} blocks. Need {count}, have {availableCount}");
            return false;
        }
        
        // Remove blocks
        int remainingToRemove = count;
        
        // First find non-selected slots to remove from
        for (int i = slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            // Skip the selected slot first to preserve it
            if (i == selectedSlot) continue;
            
            InventorySlot slot = slots[i];
            if (slot.blockType == type)
            {
                int removeFromThisSlot = Mathf.Min(remainingToRemove, slot.count);
                slot.count -= removeFromThisSlot;
                remainingToRemove -= removeFromThisSlot;
                
                // Remove empty slots
                if (slot.count <= 0)
                {
                    slots.RemoveAt(i);
                }
            }
        }
        
        // If we still need to remove blocks, try the selected slot
        if (remainingToRemove > 0 && selectedSlot < slots.Count)
        {
            InventorySlot selectedSlotObj = slots[selectedSlot];
            if (selectedSlotObj.blockType == type)
            {
                selectedSlotObj.count -= remainingToRemove;
                remainingToRemove = 0;
                
                // Remove empty slots
                if (selectedSlotObj.count <= 0)
                {
                    slots.RemoveAt(selectedSlot);
                    // Adjust selected slot if needed
                    if (selectedSlot >= slots.Count && slots.Count > 0)
                    {
                        selectedSlot = slots.Count - 1;
                    }
                }
            }
        }
        
        return remainingToRemove == 0;
    }
    
    public GameObject GetSelectedBlockPrefab()
    {
        if (selectedSlot >= 0 && selectedSlot < slots.Count)
        {
            return slots[selectedSlot].blockPrefab;
        }
        Debug.Log("No block selected or inventory empty");
        return null;
    }
    
    public PlayerController.BlockType GetSelectedBlockType()
    {
        if (selectedSlot >= 0 && selectedSlot < slots.Count)
        {
            return slots[selectedSlot].blockType;
        }
        return PlayerController.BlockType.Air; // Default to Air if no block selected
    }
    
    public bool HasBlock(PlayerController.BlockType type)
    {
        InventorySlot slot = slots.Find(s => s.blockType == type);
        return slot != null && slot.count > 0;
    }
    
    private GameObject GetBlockPrefab(PlayerController.BlockType type)
    {
        // Find the world generator to get block prefabs
        WorldGenerator worldGen = FindObjectOfType<WorldGenerator>();
        if (worldGen == null)
        {
            Debug.LogError("WorldGenerator not found! Creating temporary prefabs.");
            return CreateTemporaryBlockPrefab(type);
        }
        
        GameObject prefab = null;
        
        switch (type)
        {
            case PlayerController.BlockType.Grass:
                prefab = worldGen.grassBlock;
                break;
            case PlayerController.BlockType.Dirt:
                prefab = worldGen.dirtBlock;
                break;
            case PlayerController.BlockType.Stone:
                prefab = worldGen.stoneBlock;
                break;
            case PlayerController.BlockType.Bedrock:
                prefab = worldGen.bedrockBlock;
                break;
            default:
                Debug.LogWarning($"No prefab found for block type: {type}");
                break;
        }
        
        // If no prefab was found, create a temporary one
        if (prefab == null)
        {
            Debug.LogWarning($"Creating temporary prefab for {type}");
            prefab = CreateTemporaryBlockPrefab(type);
        }
        
        return prefab;
    }
    
    private GameObject CreateTemporaryBlockPrefab(PlayerController.BlockType type)
    {
        // Create a basic cube with appropriate color based on block type
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"Temp_{type}Block";
        
        Renderer renderer = cube.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        
        // Set color based on block type
        switch (type)
        {
            case PlayerController.BlockType.Grass:
                material.color = new Color(0.2f, 0.8f, 0.2f);
                break;
            case PlayerController.BlockType.Dirt:
                material.color = new Color(0.6f, 0.4f, 0.2f);
                break;
            case PlayerController.BlockType.Stone:
                material.color = new Color(0.5f, 0.5f, 0.5f);
                break;
            case PlayerController.BlockType.Bedrock:
                material.color = new Color(0.2f, 0.2f, 0.2f);
                break;
            default:
                material.color = Color.magenta; // Error color
                break;
        }
        
        renderer.material = material;
        
        // Set the cube inactive so it can be used as a prefab
        cube.SetActive(false);
        
        return cube;
    }
    
    void Update()
    {
        // Handle slot selection with number keys (1-9, 0)
        for (int i = 0; i < 10; i++)
        {
            // Map 1-9 to 0-8 and 0 to 9
            int keyIndex = (i == 9) ? 0 : i + 1;
            if (Input.GetKeyDown(KeyCode.Alpha1 + keyIndex - 1))
            {
                selectedSlot = i;
                if (selectedSlot >= slots.Count)
                {
                    selectedSlot = slots.Count - 1;
                }
            }
        }
        
        // Handle slot selection with mouse wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && slots.Count > 0)
        {
            selectedSlot = (int)Mathf.Repeat(selectedSlot - Mathf.Sign(scroll), slots.Count);
        }
    }
} 