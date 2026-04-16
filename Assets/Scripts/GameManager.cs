using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Loop Settings")]
    public int loopsPerBossFight = 3;

    public int LoopCount { get; private set; }

    // Subscribe to these for future systems (tile changes, boss fights, etc.)
    public event Action<int> OnLoopCompleted;   // fires every loop, passes new count
    public event Action<int> OnBossFightReady;  // fires every N loops, passes loop count

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterLoop()
    {
        LoopCount++;
        OnLoopCompleted?.Invoke(LoopCount);

        if (LoopCount % loopsPerBossFight == 0)
            OnBossFightReady?.Invoke(LoopCount);
    }
}
