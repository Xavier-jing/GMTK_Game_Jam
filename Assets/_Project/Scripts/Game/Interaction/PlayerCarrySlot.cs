using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCarrySlot : MonoBehaviour
{
    [SerializeField]
    private Transform dropAnchor;

    private Player player;

    public WorldStoryInteractable CurrentProp { get; private set; }

    public bool HasProp => CurrentProp != null;

    public event Action<WorldStoryInteractable> Changed;

    private void Awake()
    {
        player = GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError(
                $"PlayerCarrySlot on '{name}' must be attached to a Player.",
                this);
            enabled = false;
        }
    }

    public bool CanTake(WorldStoryInteractable prop)
    {
        return player != null &&
               prop != null &&
               !HasProp &&
               !prop.IsCarried &&
               WorldPropRules.IsCarryable(prop.PropId) &&
               WorldPropRules.GetSlotItemKind(prop.PropId) != PlayerSlotItemKind.None;
    }

    public bool TryTake(WorldStoryInteractable prop)
    {
        if (!CanTake(prop))
        {
            return false;
        }

        PlayerSlotItemKind itemKind = WorldPropRules.GetSlotItemKind(prop.PropId);
        if (!player.TryStartCarryingSlotItem(itemKind))
        {
            return false;
        }

        CurrentProp = prop;
        prop.SetCarried(true);
        Changed?.Invoke(CurrentProp);
        return true;
    }

    public bool CanDrop(WorldStoryInteractable prop)
    {
        return player != null &&
               prop != null &&
               CurrentProp == prop &&
               player.GameplayStatus.SlotItemKind ==
               WorldPropRules.GetSlotItemKind(prop.PropId);
    }

    public bool TryDrop(WorldStoryInteractable prop)
    {
        Vector3 dropPosition = dropAnchor != null
            ? dropAnchor.position
            : transform.position;
        return TryDropAt(prop, dropPosition, true);
    }

    public bool TryDropAt(
        WorldStoryInteractable prop,
        Vector3 position,
        bool remainsInteractable)
    {
        if (!CanDrop(prop))
        {
            return false;
        }

        PlayerSlotItemKind itemKind = WorldPropRules.GetSlotItemKind(prop.PropId);
        if (!player.TryDropCarriedSlotItem(itemKind))
        {
            return false;
        }

        CurrentProp = null;
        prop.ReleaseFromCarrySlot(position, remainsInteractable);
        Changed?.Invoke(null);
        return true;
    }
}
