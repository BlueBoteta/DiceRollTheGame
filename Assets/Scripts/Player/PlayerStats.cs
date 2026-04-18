using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Stats")]
    public int maxHp = 100;
    public int hp    = 100;

    public event Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TakeDamage(int amount)
    {
        hp = Mathf.Max(0, hp - amount);
        OnStatsChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        OnStatsChanged?.Invoke();
    }
}
