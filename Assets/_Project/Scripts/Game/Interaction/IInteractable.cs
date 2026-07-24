using System.Collections.Generic;

public interface IInteractable
{
    void GetInteractionOptions(Player player, List<InteractionOption> options);
}
