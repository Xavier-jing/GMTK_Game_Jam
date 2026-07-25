using System;
using UnityEngine;

[Serializable]
public struct ItemRequirement
{
    [SerializeField]
    private ItemDefinition requiredItem;

    [SerializeField]
    private bool consumeOnSuccess;

    [SerializeField]
    [TextArea]
    private string blockedPrompt;

    public ItemDefinition RequiredItem => requiredItem;

    public bool ConsumeOnSuccess => consumeOnSuccess;

    public string BlockedPrompt => blockedPrompt;

    public bool IsSatisfied(Inventory inventory)
    {
        return requiredItem == null ||
               (inventory != null && inventory.Contains(requiredItem));
    }

    public bool TryConsume(Inventory inventory)
    {
        if (requiredItem == null || !consumeOnSuccess)
        {
            return true;
        }

        return inventory != null && inventory.TryConsume(requiredItem);
    }
}
