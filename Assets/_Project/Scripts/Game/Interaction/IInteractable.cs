using UnityEngine;

public readonly struct InteractionContext
{
    public InteractionContext(GameObject interactor, Inventory inventory)
    {
        Interactor = interactor;
        Inventory = inventory;
    }

    public GameObject Interactor { get; }

    public Inventory Inventory { get; }
}

public interface IInteractable
{
    Transform InteractionPoint { get; }

    string GetInteractionPrompt(InteractionContext context);

    bool CanInteract(InteractionContext context);

    void Interact(InteractionContext context);
}
