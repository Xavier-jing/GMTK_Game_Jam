using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoryPresenter : MonoBehaviour, IStoryPresenter
{
    [Serializable]
    private sealed class PortraitBinding
    {
        [SerializeField]
        private string portraitId;

        [SerializeField]
        private Sprite sprite;

        public string PortraitId => portraitId;

        public Sprite Sprite => sprite;
    }

    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TMP_Text actorText;

    [SerializeField]
    private TMP_Text dialogText;

    [SerializeField]
    private Transform choiceContainer;

    [SerializeField]
    private Button choiceButtonTemplate;

    [SerializeField]
    private Image cgImage;

    [SerializeField]
    private Image portraitImage;

    [SerializeField]
    private PortraitBinding[] portraitBindings = Array.Empty<PortraitBinding>();

    [SerializeField]
    [Min(0f)]
    private float charactersPerSecond = 40f;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    private Action<int> choiceSelected;
    private float visibleCharacterProgress;
    private int totalVisibleCharacters;
    private bool isTyping;
    private Sprite defaultPortraitSprite;
    private bool defaultPortraitImageEnabled;
    private bool defaultPortraitObjectActive;
    private bool hasCapturedDefaultPortrait;

    public bool IsConfigured =>
        panelRoot != null &&
        dialogText != null &&
        choiceContainer != null &&
        choiceButtonTemplate != null;

    private void Awake()
    {
        CaptureDefaultPortrait();
        HideCg();

        if (choiceButtonTemplate != null)
        {
            choiceButtonTemplate.gameObject.SetActive(false);
        }

        Hide();
    }

    private void Update()
    {
        if (!isTyping || dialogText == null)
        {
            return;
        }

        visibleCharacterProgress += charactersPerSecond * Time.unscaledDeltaTime;
        int visibleCharacters = Mathf.Min(
            totalVisibleCharacters,
            Mathf.FloorToInt(visibleCharacterProgress));
        dialogText.maxVisibleCharacters = visibleCharacters;

        if (visibleCharacters >= totalVisibleCharacters)
        {
            isTyping = false;
        }
    }

    public void Show()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                $"StoryPresenter on '{name}' is missing required references.");
        }

        ResetTransientContent();
        panelRoot.SetActive(true);
    }

    public void ShowDialogue(string actorId, string portraitId, string dialog)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                $"StoryPresenter on '{name}' is missing required references.");
        }

        ClearChoices();
        panelRoot.SetActive(true);
        choiceContainer.gameObject.SetActive(false);

        if (actorText != null)
        {
            bool hasActor = !string.IsNullOrWhiteSpace(actorId);
            actorText.gameObject.SetActive(hasActor);
            actorText.text = hasActor ? actorId : string.Empty;
        }

        ApplyPortrait(portraitId);

        dialogText.gameObject.SetActive(true);
        dialogText.text = dialog ?? string.Empty;
        dialogText.ForceMeshUpdate();

        totalVisibleCharacters = dialogText.textInfo.characterCount;
        visibleCharacterProgress = 0f;

        if (charactersPerSecond <= 0f || totalVisibleCharacters == 0)
        {
            dialogText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        dialogText.maxVisibleCharacters = 0;
        isTyping = true;
    }

    public void ShowChoices(
        IReadOnlyList<StoryChoiceViewModel> choices,
        Action<int> onSelected)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                $"StoryPresenter on '{name}' is missing required references.");
        }

        ClearChoices();
        panelRoot.SetActive(true);
        if (actorText != null)
        {
            actorText.gameObject.SetActive(false);
        }

        dialogText.gameObject.SetActive(false);
        choiceContainer.gameObject.SetActive(true);
        choiceSelected = onSelected;

        for (int index = 0; index < choices.Count; index++)
        {
            StoryChoiceViewModel choice = choices[index];
            Button button = Instantiate(choiceButtonTemplate, choiceContainer);
            button.gameObject.SetActive(true);
            button.interactable = choice.IsInteractable;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = choice.Dialog;
            }
            else
            {
                Debug.LogWarning(
                    $"Story choice button template '{choiceButtonTemplate.name}' has no TMP_Text child.",
                    choiceButtonTemplate);
            }

            int capturedIndex = index;
            button.onClick.AddListener(() => choiceSelected?.Invoke(capturedIndex));
            spawnedChoiceButtons.Add(button);
        }

        SelectFirstInteractableChoice();
    }

    public void LockChoices()
    {
        choiceSelected = null;

        foreach (Button button in spawnedChoiceButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }
    }

    public bool TryCompleteDialogue()
    {
        if (!isTyping)
        {
            return false;
        }

        visibleCharacterProgress = totalVisibleCharacters;
        dialogText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        return true;
    }

    public void ShowCg(Sprite cgSprite)
    {
        if (cgSprite == null)
        {
            Debug.LogWarning(
                $"StoryPresenter '{name}' received an empty CG Sprite. " +
                "The current CG was kept.",
                this);
            return;
        }

        if (cgImage == null)
        {
            Debug.LogWarning(
                $"StoryPresenter '{name}' cannot show CG Sprite '{cgSprite.name}' " +
                "because Cg Image is not assigned.",
                this);
            return;
        }

        cgImage.sprite = cgSprite;
        cgImage.gameObject.SetActive(true);
        cgImage.enabled = true;
    }

    public void HideCg()
    {
        if (cgImage == null)
        {
            return;
        }

        cgImage.sprite = null;
        cgImage.enabled = false;
        cgImage.gameObject.SetActive(false);
    }

    public void ResetPortrait()
    {
        CaptureDefaultPortrait();
        if (portraitImage == null)
        {
            return;
        }

        portraitImage.sprite = defaultPortraitSprite;
        portraitImage.enabled = defaultPortraitImageEnabled;
        portraitImage.gameObject.SetActive(defaultPortraitObjectActive);
    }

    public void Hide()
    {
        ResetTransientContent();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void ApplyPortrait(string portraitId)
    {
        if (string.IsNullOrEmpty(portraitId))
        {
            return;
        }

        string bindingId = StoryPortraitIdMap.ResolveBindingId(portraitId);
        CaptureDefaultPortrait();
        if (portraitImage == null)
        {
            Debug.LogWarning(
                $"StoryPresenter '{name}' cannot apply PortraitId '{portraitId}' " +
                "because Portrait Image is not assigned. The current portrait was kept.",
                this);
            return;
        }

        int bindingCount = portraitBindings?.Length ?? 0;
        for (int index = 0; index < bindingCount; index++)
        {
            PortraitBinding binding = portraitBindings[index];
            if (binding == null ||
                !string.Equals(
                    binding.PortraitId,
                    bindingId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (binding.Sprite == null)
            {
                Debug.LogWarning(
                    $"StoryPresenter '{name}' has no Sprite assigned for PortraitId " +
                    $"'{portraitId}' (binding '{bindingId}'). " +
                    "The current portrait was kept.",
                    this);
                return;
            }

            portraitImage.sprite = binding.Sprite;
            portraitImage.gameObject.SetActive(true);
            portraitImage.enabled = true;
            return;
        }

        Debug.LogWarning(
            $"StoryPresenter '{name}' has no portrait mapping for PortraitId " +
            $"'{portraitId}' (binding '{bindingId}'). " +
            "The current portrait was kept.",
            this);
    }

    private void CaptureDefaultPortrait()
    {
        if (hasCapturedDefaultPortrait || portraitImage == null)
        {
            return;
        }

        defaultPortraitSprite = portraitImage.sprite;
        defaultPortraitImageEnabled = portraitImage.enabled;
        defaultPortraitObjectActive = portraitImage.gameObject.activeSelf;
        hasCapturedDefaultPortrait = true;
    }

    private void ResetTransientContent()
    {
        isTyping = false;
        visibleCharacterProgress = 0f;
        totalVisibleCharacters = 0;
        ClearChoices();

        if (actorText != null)
        {
            actorText.text = string.Empty;
            actorText.gameObject.SetActive(false);
        }

        if (dialogText != null)
        {
            dialogText.text = string.Empty;
            dialogText.maxVisibleCharacters = int.MaxValue;
            dialogText.gameObject.SetActive(false);
        }

        if (choiceContainer != null)
        {
            choiceContainer.gameObject.SetActive(false);
        }
    }

    private void ClearChoices()
    {
        choiceSelected = null;

        foreach (Button button in spawnedChoiceButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
        }

        spawnedChoiceButtons.Clear();
    }

    private void SelectFirstInteractableChoice()
    {
        foreach (Button button in spawnedChoiceButtons)
        {
            if (button != null && button.IsInteractable())
            {
                button.Select();
                return;
            }
        }
    }
}
