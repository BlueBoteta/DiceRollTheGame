using UnityEngine;

public class MiniGameResources : MonoBehaviour
{
    public static MiniGameResources Instance { get; private set; }

    public int ammo  = 2;
    public int scrap = 2;
    public int meds  = 2;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
