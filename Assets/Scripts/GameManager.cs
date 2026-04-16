using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Loop Settings")]
    public int loopsPerBossFight = 3;

    public int LoopCount { get; private set; }

    public event Action<int>       OnLoopCompleted;    // every loop
    public event Action<int>       OnBossFightReady;   // every N loops
    public event Action<TileType>  OnTileLanded;       // every landing

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        // Hook into player landing once PlayerToken exists (called after Awake ordering)
    }

    // Called by PlayerToken after it finishes moving
    public void HandleTileLanding(BoardTile tile)
    {
        OnTileLanded?.Invoke(tile.tileType);
    }

    public void RegisterLoop()
    {
        LoopCount++;
        OnLoopCompleted?.Invoke(LoopCount);
        if (LoopCount % loopsPerBossFight == 0)
            OnBossFightReady?.Invoke(LoopCount);
    }
}
