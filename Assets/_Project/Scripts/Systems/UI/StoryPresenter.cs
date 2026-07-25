using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoryPresenter : MonoBehaviour, IStoryPresenter
{
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
    [Min(0f)]
    private float charactersPerSecond = 40f;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    private Action<int> choiceSelected;
    private float visibleCharacterProgress;
    private int totalVisibleCharacters;
    private bool isTyping;

    public bool IsConfigured =>
        panelRoot != null &&
        actorText != null &&
        dialogText != null &&
        choiceContainer != null &&
        choiceButtonTemplate != null;

    private void Awake()
    {
        if (choiceButtonTemplate != null)
        {
            choiceButtonTemplate.gameObject.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
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

    public void ShowDialogue(string actorId, string dialog)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                $"StoryPresenter on '{name}' is missing required references.");
        }

        ClearChoices();
        panelRoot.SetActive(true);
        choiceContainer.gameObject.SetActive(false);

        bool hasActor = !string.IsNullOrWhiteSpace(actorId);
        actorText.gameObject.SetActive(hasActor);
        actorText.text = hasActor ? actorId : string.Empty;

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
        actorText.gameObject.SetActive(false);
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

    public void Hide()
    {
        isTyping = false;
        choiceSelected = null;
        ClearChoices();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
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
