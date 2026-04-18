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

    public bool BossFightPending { get; private set; }

    // Called by PlayerToken after it finishes moving
    public void HandleTileLanding(BoardTile tile)
    {
        OnTileLanded?.Invoke(tile.tileType);
        ApplyTileEffect(tile.tileType);
    }

    void ApplyTileEffect(TileType type)
    {
        if (PlayerStats.Instance == null) return;
        if (type == TileType.Loot)
        {
            if (PlayerStats.Instance.hp < PlayerStats.Instance.maxHp)
                PlayerStats.Instance.Heal(1);
            else
                Inventory.Instance?.Add("ammo", 2);
        }
        // Combat is handled by CombatScreen
    }

    public void RegisterLoop()
    {
        LoopCount++;
        OnLoopCompleted?.Invoke(LoopCount);
        if (LoopCount % loopsPerBossFight == 0)
        {
            BossFightPending = true;
            OnBossFightReady?.Invoke(LoopCount);
        }
    }

    public void ClearBossFightPending() => BossFightPending = false;
}
