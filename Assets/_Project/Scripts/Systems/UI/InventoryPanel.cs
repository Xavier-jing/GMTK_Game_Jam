using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryPanel : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField]
    private Transform slotContainer;

    [SerializeField]
    private GameObject slotPrefab;

    [Header("Slot Settings")]
    [SerializeField]
    private Vector2 slotSize = new Vector2(64f, 64f);

    [SerializeField]
    private float slotSpacing = 8f;

    [Header("Empty State")]
    [SerializeField]
    private GameObject emptyStateHint;

    private Inventory inventory;
    private readonly List<GameObject> activeSlots = new List<GameObject>();
    private readonly Stack<GameObject> slotPool = new Stack<GameObject>();

    private void Awake()
    {
        if (slotContainer == null)
        {
            slotContainer = transform;
        }
    }

    private void Start()
    {
        inventory = AppContext.Instance.Inventory;
    }

    private void OnEnable()
    {
        if (inventory == null && AppContext.HasInstance)
        {
            inventory = AppContext.Instance.Inventory;
        }

        if (inventory != null)
        {
            inventory.Changed += HandleInventoryChanged;
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.Changed -= HandleInventoryChanged;
        }

        ReturnAllSlotsToPool();
    }

    private void HandleInventoryChanged()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        ReturnAllSlotsToPool();

        if (inventory == null)
        {
            SetEmptyHintVisible(true);
            return;
        }

        IReadOnlyList<ItemDefinition> items = inventory.VisibleItems;

        if (items == null || items.Count == 0)
        {
            SetEmptyHintVisible(true);
            return;
        }

        SetEmptyHintVisible(false);

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];
            if (item == null)
            {
                continue;
            }

            GameObject slot = GetOrCreateSlot();
            ConfigureSlot(slot, item);
        }
    }

    private GameObject GetOrCreateSlot()
    {
        if (slotPool.Count > 0)
        {
            GameObject slot = slotPool.Pop();
            slot.SetActive(true);
            activeSlots.Add(slot);
            return slot;
        }

        GameObject newSlot;
        if (slotPrefab != null)
        {
            newSlot = Instantiate(slotPrefab, slotContainer);
        }
        else
        {
            newSlot = CreateDefaultSlot();
        }

        activeSlots.Add(newSlot);
        return newSlot;
    }

    private GameObject CreateDefaultSlot()
    {
        GameObject slot = new GameObject("ItemSlot", typeof(RectTransform));
        slot.transform.SetParent(slotContainer, false);

        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.sizeDelta = slotSize;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(slot.transform, false);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;

        return slot;
    }

    private void ConfigureSlot(GameObject slot, ItemDefinition item)
    {
        slot.name = item.DisplayName;

        Image iconImage = slot.GetComponentInChildren<Image>(true);
        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = item.Icon != null;
        }
    }

    private void ReturnAllSlotsToPool()
    {
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            GameObject slot = activeSlots[i];
            slot.SetActive(false);
            slotPool.Push(slot);
        }

        activeSlots.Clear();
    }

    private void SetEmptyHintVisible(bool visible)
    {
        if (emptyStateHint != null)
        {
            emptyStateHint.SetActive(visible);
        }
    }
}
