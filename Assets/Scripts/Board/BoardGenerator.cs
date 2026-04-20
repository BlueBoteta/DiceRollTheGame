using UnityEngine;
using System.Collections.Generic;

public class BoardGenerator : MonoBehaviour
{
    public static BoardGenerator Instance { get; private set; }

    [Header("Board Settings")]
    public int tilesPerSide = 10;
    public float tileWidth = 1.0f;
    public float tileHeight = 0.5f;

    [Header("Tile Appearance")]
    public Color tileColor        = new Color(0.22f, 0.22f, 0.26f, 1f);
    public Color tileOutlineColor = new Color(0.45f, 0.45f, 0.55f, 1f);
    public Color startTileColor   = new Color(0.15f, 0.45f, 0.25f, 1f);
    public Color combatTileColor  = new Color(0.45f, 0.10f, 0.10f, 1f);
    public Color lootTileColor    = new Color(0.35f, 0.28f, 0.08f, 1f);
    public Color bossTileColor    = new Color(0.30f, 0.05f, 0.35f, 1f);
    public Color storyTileColor     = new Color(0.10f, 0.25f, 0.35f, 1f);
    public Color blackjackTileColor  = new Color(0.08f, 0.22f, 0.12f, 1f);
    public Color safeHouseTileColor  = new Color(0.38f, 0.30f, 0.14f, 1f);
    public Color scavengeTileColor   = new Color(0.22f, 0.18f, 0.30f, 1f);
    public Color tileHighlightColor  = new Color(1f, 0.85f, 0.2f, 1f);

    [Header("Tile Distribution")]
    [Range(0, 100)] public int combatChance = 40;
    [Range(0, 100)] public int lootChance   = 30;

    public List<BoardTile> Tiles { get; } = new List<BoardTile>();
    public BoardTile BossTile { get; private set; }

    void Awake()
    {
        Instance = this;
        GenerateBoard();
        GenerateBossTile();
        SpawnEnemyMarkers();
    }

    void GenerateBoard()
    {
        Sprite fillSprite    = MakeDiamondSprite(128, 64, 0.04f);
        Sprite outlineSprite = MakeDiamondSprite(128, 64, 0.00f);
        List<Vector2Int> path = BuildPath();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int gp   = path[i];
            int depth       = gp.x + gp.y;
            TileType type   = AssignType(i, path.Count);
            Color fillColor = ColorForType(type, i == 0);

            GameObject outlineGO = new GameObject("Tile_" + i.ToString("D2") + "_Outline");
            outlineGO.transform.SetParent(transform);
            outlineGO.transform.position = GridToIso(gp.x, gp.y);
            var outlineSR = outlineGO.AddComponent<SpriteRenderer>();
            outlineSR.sprite = outlineSprite;
            outlineSR.color  = tileOutlineColor;
            outlineSR.sortingOrder = depth * 2;

            GameObject tileGO = new GameObject("Tile_" + i.ToString("D2"));
            tileGO.transform.SetParent(transform);
            tileGO.transform.position = GridToIso(gp.x, gp.y);
            var sr = tileGO.AddComponent<SpriteRenderer>();
            sr.sprite = fillSprite;
            sr.sortingOrder = depth * 2 + 1;

            BoardTile tile = tileGO.AddComponent<BoardTile>();
            tile.pathIndex = i;
            tile.gridPos   = gp;
            tile.tileType  = type;
            tile.SetBaseColor(fillColor);
            Tiles.Add(tile);
        }
    }

    TileType AssignType(int index, int total)
    {
        if (index == 0)  return TileType.Normal;
        if (index == 5)  return TileType.Scavenge;
        if (index == 9)  return TileType.SafeHouse;
        if (index == 15) return TileType.Story;
        if (index == 18) return TileType.Blackjack;
        if (index == 22) return TileType.Scavenge;
        if (index == 27) return TileType.SafeHouse;
        if (index == 32) return TileType.Scavenge;

        int roll = Random.Range(0, 100);
        if (roll < combatChance)              return TileType.Combat;
        if (roll < combatChance + lootChance) return TileType.Loot;
        return TileType.Normal;
    }

    void GenerateBossTile()
    {
        // Center of the 10x10 isometric grid: col=5,row=5 → (0, -2.5), nudged up slightly
        Vector3 center = new Vector3(0f, -2.25f, 0f);
        int depth = 9;

        Sprite bigFill    = MakeDiamondSprite(256, 128, 0.04f);
        Sprite bigOutline = MakeDiamondSprite(256, 128, 0.00f);

        var outlineGO = new GameObject("BossTile_Outline");
        outlineGO.transform.SetParent(transform);
        outlineGO.transform.position = center;
        var outlineSR = outlineGO.AddComponent<SpriteRenderer>();
        outlineSR.sprite = bigOutline;
        outlineSR.color  = new Color(0.65f, 0.1f, 0.75f, 1f);
        outlineSR.sortingOrder = depth * 2;

        var tileGO = new GameObject("BossTile");
        tileGO.transform.SetParent(transform);
        tileGO.transform.position = center;
        var sr = tileGO.AddComponent<SpriteRenderer>();
        sr.sprite = bigFill;
        sr.sortingOrder = depth * 2 + 1;

        BossTile = tileGO.AddComponent<BoardTile>();
        BossTile.pathIndex = -1;
        BossTile.tileType  = TileType.Boss;
        BossTile.SetBaseColor(bossTileColor);
    }

    Color ColorForType(TileType type, bool isStart)
    {
        if (isStart) return startTileColor;
        return type switch
        {
            TileType.Combat => combatTileColor,
            TileType.Loot   => lootTileColor,
            TileType.Boss   => bossTileColor,
            TileType.Story     => storyTileColor,
            TileType.Blackjack => blackjackTileColor,
            TileType.SafeHouse => safeHouseTileColor,
            TileType.Scavenge  => scavengeTileColor,
            _                  => tileColor,
        };
    }

    List<Vector2Int> BuildPath()
    {
        int last = tilesPerSide - 1;
        var path = new List<Vector2Int>();

        for (int c = last; c >= 0; c--)
            path.Add(new Vector2Int(c, last));
        for (int r = last - 1; r >= 0; r--)
            path.Add(new Vector2Int(0, r));
        for (int c = 1; c <= last; c++)
            path.Add(new Vector2Int(c, 0));
        for (int r = 1; r < last; r++)
            path.Add(new Vector2Int(last, r));

        return path;
    }

    public Vector3 GridToIso(int col, int row)
    {
        return new Vector3(
            (col - row) * tileWidth * 0.5f,
            -(col + row) * tileHeight * 0.5f,
            0f);
    }

    void SpawnEnemyMarkers()
    {
        Sprite skull = MakeSkullSprite();
        foreach (var tile in Tiles)
        {
            if (tile.tileType != TileType.Combat) continue;
            var go = new GameObject("EnemyMarker_" + tile.pathIndex);
            go.transform.SetParent(tile.transform);
            go.transform.position = tile.transform.position + new Vector3(0f, 0.26f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = skull;
            sr.color        = new Color(1f, 0.3f, 0.3f);
            sr.sortingOrder = 58;
            tile.EnemyMarker = go;
        }
    }

    static Sprite MakeSkullSprite()
    {
        // Small circle with an X drawn in it as a placeholder skull/enemy icon
        int sz = 32, r = sz / 2;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[sz * sz];
        float cx = r - 0.5f, cy = r - 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d > r) { px[y * sz + x] = Color.clear; continue; }
            // X pattern
            float nx = Mathf.Abs(x - cx), ny = Mathf.Abs(y - cy);
            bool onX = Mathf.Abs(nx - ny) < 1.8f && d < r - 1f;
            px[y * sz + x] = onX ? Color.white : new Color(1f, 1f, 1f, 0.18f);
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, 100f);
    }

    static Sprite MakeDiamondSprite(int w, int h, float gap)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (float)x / w - 0.5f;
            float ny = (float)y / h - 0.5f;
            px[y * w + x] = (Mathf.Abs(nx) + Mathf.Abs(ny) < 0.5f - gap)
                ? Color.white : Color.clear;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
