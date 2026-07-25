using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDefinition",
    menuName = "Jam/Inventory/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName;

    [SerializeField]
    [TextArea(2, 5)]
    private string description;

    [SerializeField]
    private Sprite icon;

    [SerializeField]
    private bool showInManagedInventory = true;

    [SerializeField]
    private bool canDiscard = true;

    public string DisplayName => displayName;

    public string Description => description;

    public Sprite Icon => icon;

    public bool ShowInManagedInventory => showInManagedInventory;

    public bool CanDiscard => canDiscard;
}
