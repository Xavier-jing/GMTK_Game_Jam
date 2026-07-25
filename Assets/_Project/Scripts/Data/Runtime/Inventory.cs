using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum InventoryAddResult
{
    Added,
    InvalidItem,
    AlreadyOwned,
    ManagedSlotsFull
}

public sealed class Inventory
{
    public const int DefaultManagedSlotCapacity = 20;

    private readonly HashSet<ItemDefinition> ownedItems =
        new HashSet<ItemDefinition>();

    private readonly List<ItemDefinition> visibleItems =
        new List<ItemDefinition>();

    private readonly ReadOnlyCollection<ItemDefinition> visibleItemsView;

    public Inventory(int managedSlotCapacity = DefaultManagedSlotCapacity)
    {
        if (managedSlotCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(managedSlotCapacity),
                managedSlotCapacity,
                "Managed slot capacity cannot be negative.");
        }

        ManagedSlotCapacity = managedSlotCapacity;
        visibleItemsView = visibleItems.AsReadOnly();
    }

    public event Action Changed;

    public int ManagedSlotCapacity { get; }

    public int UsedManagedSlots => visibleItems.Count;

    public int TotalItemCount => ownedItems.Count;

    public IReadOnlyList<ItemDefinition> VisibleItems => visibleItemsView;

    public bool Contains(ItemDefinition item)
    {
        return item != null && ownedItems.Contains(item);
    }

    public InventoryAddResult TryAdd(ItemDefinition item)
    {
        if (item == null)
        {
            return InventoryAddResult.InvalidItem;
        }

        if (ownedItems.Contains(item))
        {
            return InventoryAddResult.AlreadyOwned;
        }

        if (item.ShowInManagedInventory &&
            visibleItems.Count >= ManagedSlotCapacity)
        {
            return InventoryAddResult.ManagedSlotsFull;
        }

        ownedItems.Add(item);
        if (item.ShowInManagedInventory)
        {
            visibleItems.Add(item);
        }

        Changed?.Invoke();
        return InventoryAddResult.Added;
    }

    public bool TryDiscard(ItemDefinition item)
    {
        if (item == null ||
            !item.ShowInManagedInventory ||
            !item.CanDiscard ||
            !ownedItems.Remove(item))
        {
            return false;
        }

        visibleItems.Remove(item);
        Changed?.Invoke();
        return true;
    }

    public bool TryConsume(ItemDefinition item)
    {
        if (item == null || !ownedItems.Remove(item))
        {
            return false;
        }

        visibleItems.Remove(item);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (ownedItems.Count == 0)
        {
            return;
        }

        ownedItems.Clear();
        visibleItems.Clear();
        Changed?.Invoke();
    }
}
