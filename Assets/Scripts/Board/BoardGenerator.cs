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
    public Color tileColor = new Color(0.22f, 0.22f, 0.26f, 1f);
    public Color tileOutlineColor = new Color(0.45f, 0.45f, 0.55f, 1f);
    public Color startTileColor = new Color(0.15f, 0.45f, 0.25f, 1f);

    public List<BoardTile> Tiles { get; } = new List<BoardTile>();

    void Awake()
    {
        Instance = this;
        GenerateBoard();
    }

    void GenerateBoard()
    {
        Sprite fillSprite = MakeDiamondSprite(128, 64, 0.04f);
        Sprite outlineSprite = MakeDiamondSprite(128, 64, 0.00f);
        List<Vector2Int> path = BuildPath();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int gp = path[i];
            int depth = gp.x + gp.y;
            bool isStart = i == 0;

            // Outline (drawn behind)
            GameObject outlineGO = new GameObject("Tile_" + i.ToString("D2") + "_Outline");
            outlineGO.transform.SetParent(transform);
            outlineGO.transform.position = GridToIso(gp.x, gp.y);
            SpriteRenderer outlineSR = outlineGO.AddComponent<SpriteRenderer>();
            outlineSR.sprite = outlineSprite;
            outlineSR.color = tileOutlineColor;
            outlineSR.sortingOrder = depth * 2;

            // Fill
            GameObject tileGO = new GameObject("Tile_" + i.ToString("D2"));
            tileGO.transform.SetParent(transform);
            tileGO.transform.position = GridToIso(gp.x, gp.y);
            SpriteRenderer sr = tileGO.AddComponent<SpriteRenderer>();
            sr.sprite = fillSprite;
            sr.color = isStart ? startTileColor : tileColor;
            sr.sortingOrder = depth * 2 + 1;

            BoardTile tile = tileGO.AddComponent<BoardTile>();
            tile.pathIndex = i;
            tile.gridPos = gp;
            if (isStart) tile.tileType = TileType.Normal; // mark as start via color
            Tiles.Add(tile);
        }
    }

    List<Vector2Int> BuildPath()
    {
        int last = tilesPerSide - 1;
        var path = new List<Vector2Int>();

        // Start at bottom corner, go clockwise
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
            0f
        );
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
