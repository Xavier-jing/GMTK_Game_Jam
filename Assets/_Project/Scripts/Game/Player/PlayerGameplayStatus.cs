using System;
using UnityEngine;

public enum PlayerWorldLayer
{
    Lower,
    Upper
}

public enum PlayerSlotItemKind
{
    None,
    FloatingSmallItem
}

[DisallowMultipleComponent]
public sealed class PlayerGameplayStatus : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField]
    private bool hasWrench;

    [SerializeField]
    private bool railRemoved;

    [Header("Position State")]
    [SerializeField]
    private PlayerWorldLayer currentLayer = PlayerWorldLayer.Lower;

    [Header("Item Slot")]
    [SerializeField]
    private PlayerSlotItemKind slotItemKind = PlayerSlotItemKind.None;

    public event Action Changed;

    public bool HasWrench => hasWrench;
    public bool RailRemoved => railRemoved;
    public PlayerWorldLayer CurrentLayer => currentLayer;
    public PlayerSlotItemKind SlotItemKind => slotItemKind;

    public bool IsUpperLayer => currentLayer == PlayerWorldLayer.Upper;
    public bool IsLowerLayer => currentLayer == PlayerWorldLayer.Lower;
    public bool HasSlotItem => slotItemKind != PlayerSlotItemKind.None;
    public bool HasFloatingSmallItem => slotItemKind == PlayerSlotItemKind.FloatingSmallItem;

    public bool CanFloatSwim => hasWrench && railRemoved && IsUpperLayer && !HasSlotItem;
    public bool ShouldSink => hasWrench && railRemoved && IsUpperLayer && HasFloatingSmallItem;
    public bool ShouldRise => hasWrench && railRemoved && IsLowerLayer && !HasSlotItem;
    public bool CanUseWeightedGroundJump => hasWrench && railRemoved && IsLowerLayer && HasFloatingSmallItem;

    public void AcquireWrench()
    {
        SetHasWrench(true);
    }

    public void SetHasWrench(bool value)
    {
        if (hasWrench == value)
        {
            return;
        }

        hasWrench = value;
        NotifyChanged();
    }

    public void MarkRailRemoved()
    {
        SetRailRemoved(true);
    }

    public void SetRailRemoved(bool value)
    {
        if (railRemoved == value)
        {
            return;
        }

        railRemoved = value;
        NotifyChanged();
    }

    public void SetCurrentLayer(PlayerWorldLayer layer)
    {
        if (currentLayer == layer)
        {
            return;
        }

        currentLayer = layer;
        NotifyChanged();
    }

    public void PutItemInSlot(PlayerSlotItemKind itemKind)
    {
        if (itemKind == PlayerSlotItemKind.None)
        {
            ClearItemSlot();
            return;
        }

        SetSlotItemKind(itemKind);
    }

    public void ClearItemSlot()
    {
        SetSlotItemKind(PlayerSlotItemKind.None);
    }

    private void SetSlotItemKind(PlayerSlotItemKind itemKind)
    {
        if (slotItemKind == itemKind)
        {
            return;
        }

        slotItemKind = itemKind;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
