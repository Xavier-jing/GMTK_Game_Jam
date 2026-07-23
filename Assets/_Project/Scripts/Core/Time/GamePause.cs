using System;
using UnityEngine;

public sealed class GamePause
{
    public event Action<bool> PauseStateChanged;

    public bool IsPaused { get; private set; }

    public void Toggle()
    {
        SetPaused(!IsPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void SetPaused(bool isPaused)
    {
        if (IsPaused == isPaused)
        {
            return;
        }

        IsPaused = isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        AudioListener.pause = isPaused;
        PauseStateChanged?.Invoke(IsPaused);
    }
}
