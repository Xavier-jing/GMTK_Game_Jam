using System;
using UnityEngine;

public sealed class StoryLoadResult
{
    private StoryLoadResult(StoryDocumentData document, string error)
    {
        Document = document;
        Error = error ?? string.Empty;
    }

    public StoryDocumentData Document { get; }

    public string Error { get; }

    public bool Succeeded => Document != null && string.IsNullOrEmpty(Error);

    public static StoryLoadResult Success(StoryDocumentData document)
    {
        return new StoryLoadResult(document, string.Empty);
    }

    public static StoryLoadResult Failure(string error)
    {
        return new StoryLoadResult(null, error);
    }
}

public sealed class StoryLoader
{
    private const string ResourceFolder = "Story";

    public StoryLoadResult Load(string scriptId)
    {
        if (!StoryValidator.IsValidId(scriptId))
        {
            return StoryLoadResult.Failure(
                $"Script id '{scriptId}' must contain only letters, numbers, underscores, or hyphens.");
        }

        TextAsset asset = Resources.Load<TextAsset>($"{ResourceFolder}/{scriptId}");
        if (asset == null)
        {
            return StoryLoadResult.Failure(
                $"Story resource '{ResourceFolder}/{scriptId}' was not found.");
        }

        return Parse(asset.text, asset.name);
    }

    public StoryLoadResult Parse(string json, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return StoryLoadResult.Failure(
                $"Story source '{sourceName}' is empty.");
        }

        try
        {
            StoryDocumentData document = JsonUtility.FromJson<StoryDocumentData>(json);
            if (document == null)
            {
                return StoryLoadResult.Failure(
                    $"Story source '{sourceName}' could not be parsed.");
            }

            return StoryLoadResult.Success(document);
        }
        catch (Exception exception)
        {
            return StoryLoadResult.Failure(
                $"Story source '{sourceName}' contains invalid JSON: {exception.Message}");
        }
    }
}
