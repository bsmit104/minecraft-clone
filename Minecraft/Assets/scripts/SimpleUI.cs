using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Attach this to an empty GameObject named "UI Manager"
public class SimpleUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public Inventory playerInventory;
    
    [Header("UI Settings")]
    public Color selectedSlotColor = Color.white;
    public Color normalSlotColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
    public int slotCount = 8;
    
    // UI Elements
    private Canvas mainCanvas;
    private List<Image> slotImages = new List<Image>();
    private List<Text> countTexts = new List<Text>();
    private List<Image> iconImages = new List<Image>();
    private Text blockInfoText;
    
    void Start()
    {
        FindReferences();
        CreateUI();
    }
    
    void FindReferences()
    {
        // Find player controller if not assigned
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
            
        // Find inventory if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<Inventory>();
        
        if (playerController == null || playerInventory == null)
            Debug.LogError("Could not find required player components!");
    }
    
    void CreateUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("UI Canvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create inventory panel
        GameObject panel = new GameObject("Inventory Panel");
        panel.transform.SetParent(mainCanvas.transform, false);
        
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.5f);
        
        // Position panel at bottom center
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 20);
        panelRect.sizeDelta = new Vector2(slotCount * 70, 80);
        
        // Create horizontal layout group
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        
        // Create slots
        for (int i = 0; i < slotCount; i++)
        {
            // Create slot
            GameObject slot = new GameObject($"Slot_{i}");
            slot.transform.SetParent(panel.transform, false);
            
            Image slotImage = slot.AddComponent<Image>();
            slotImage.color = normalSlotColor;
            slotImages.Add(slotImage);
            
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(60, 60);
            
            // Create icon
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = new Color(1, 1, 1, 0);
            iconImages.Add(iconImage);
            
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            
            // Create count text
            GameObject count = new GameObject("Count");
            count.transform.SetParent(slot.transform, false);
            
            Text countText = count.AddComponent<Text>();
            countText.text = "";
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 14;
            countText.alignment = TextAnchor.LowerRight;
            countText.color = Color.white;
            countTexts.Add(countText);
            
            RectTransform countRect = count.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = new Vector2(5, 5);
            countRect.offsetMax = new Vector2(-5, -5);
        }
        
        // Create crosshair
        GameObject crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(mainCanvas.transform, false);
        
        // Create vertical line of plus
        GameObject verticalLine = new GameObject("Vertical");
        verticalLine.transform.SetParent(crosshair.transform, false);
        Image verticalImg = verticalLine.AddComponent<Image>();
        verticalImg.color = new Color(0, 0, 0, 1f);
        RectTransform verticalRect = verticalLine.GetComponent<RectTransform>();
        verticalRect.anchorMin = new Vector2(0.5f, 0.5f);
        verticalRect.anchorMax = new Vector2(0.5f, 0.5f);
        verticalRect.sizeDelta = new Vector2(2, 12);

        // Create horizontal line of plus
        GameObject horizontalLine = new GameObject("Horizontal"); 
        horizontalLine.transform.SetParent(crosshair.transform, false);
        Image horizontalImg = horizontalLine.AddComponent<Image>();
        horizontalImg.color = new Color(0, 0, 0, 1f);
        RectTransform horizontalRect = horizontalLine.GetComponent<RectTransform>();
        horizontalRect.anchorMin = new Vector2(0.5f, 0.5f);
        horizontalRect.anchorMax = new Vector2(0.5f, 0.5f);
        horizontalRect.sizeDelta = new Vector2(12, 2);
        
        // Create block info text
        GameObject blockInfo = new GameObject("Block Info");
        blockInfo.transform.SetParent(mainCanvas.transform, false);
        
        blockInfoText = blockInfo.AddComponent<Text>();
        blockInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        blockInfoText.fontSize = 16;
        blockInfoText.alignment = TextAnchor.UpperCenter;
        blockInfoText.color = Color.white;
        
        RectTransform blockInfoRect = blockInfo.GetComponent<RectTransform>();
        blockInfoRect.anchorMin = new Vector2(0.5f, 1);
        blockInfoRect.anchorMax = new Vector2(0.5f, 1);
        blockInfoRect.pivot = new Vector2(0.5f, 1);
        blockInfoRect.anchoredPosition = new Vector2(0, -20);
        blockInfoRect.sizeDelta = new Vector2(300, 50);
        
        Debug.Log("UI created successfully");
    }
    
    void Update()
    {
        UpdateInventoryUI();
        UpdateBlockInfo();
    }
    
    void UpdateInventoryUI()
    {
        if (playerInventory == null) return;
        
        // Update slot selections
        for (int i = 0; i < slotImages.Count; i++)
        {
            bool isSelected = (i == playerInventory.selectedSlot);
            slotImages[i].color = isSelected ? selectedSlotColor : normalSlotColor;
            
            // Clear slot content by default
            iconImages[i].color = new Color(1, 1, 1, 0);
            countTexts[i].text = "";
            
            // Only show content if slot exists in inventory
            if (i < playerInventory.slots.Count)
            {
                Inventory.InventorySlot slot = playerInventory.slots[i];
                
                // Update count
                if (slot.count > 1)
                {
                    countTexts[i].text = slot.count.ToString();
                }
                
                // Update icon
                iconImages[i].color = GetBlockColor(slot.blockType);
            }
        }
    }
    
    void UpdateBlockInfo()
    {
        if (playerController == null || blockInfoText == null) return;
        
        string text = "";
        
        // Show selected block info
        if (playerInventory != null && playerInventory.slots.Count > 0 && 
            playerInventory.selectedSlot < playerInventory.slots.Count)
        {
            var selectedSlot = playerInventory.slots[playerInventory.selectedSlot];
            text += $"Selected: {selectedSlot.blockType} (x{selectedSlot.count})\n";
        }
        
        // Show targeted block info
        if (playerController.selectedBlock != null)
        {
            var blockType = playerController.GetBlockTypeFromObject(playerController.selectedBlock);
            text += $"Looking at: {blockType}";
        }
        
        blockInfoText.text = text;
    }
    
    Color GetBlockColor(PlayerController.BlockType blockType)
    {
        switch (blockType)
        {
            case PlayerController.BlockType.Grass:
                return new Color(0.2f, 0.8f, 0.2f, 1f);
            case PlayerController.BlockType.Dirt:
                return new Color(0.6f, 0.4f, 0.2f, 1f);
            case PlayerController.BlockType.Stone:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
            case PlayerController.BlockType.Bedrock:
                return new Color(0.3f, 0.3f, 0.3f, 1f);
            default:
                return Color.magenta;
        }
    }
} 