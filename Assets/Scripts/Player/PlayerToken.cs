using System;
using System.Collections;
using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    public static PlayerToken Instance { get; private set; }

    public int CurrentTile { get; private set; }

    public event Action<BoardTile> OnLandedOnTile;

    const float StepDuration = 0.18f;

    void Awake()
    {
        Instance = this;
        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = BuildCircleSprite(20, new Color(1f, 0.85f, 0.1f));
        sr.sortingOrder = 60;
    }

    void Start() => PlaceOnTile(0);

    public void PlaceOnTile(int index)
    {
        CurrentTile = index;
        transform.position = TileWorldPos(index);
    }

    public IEnumerator MoveSteps(int steps)
    {
        int tileCount = BoardGenerator.Instance.Tiles.Count;

        for (int i = 0; i < steps; i++)
        {
            int next = CurrentTile + 1;
            if (next >= tileCount)
            {
                GameManager.Instance?.RegisterLoop();
                next = 0;
            }
            CurrentTile = next;

            Vector3 from = transform.position;
            Vector3 to   = TileWorldPos(next);
            float t = 0f;
            while (t < StepDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to,
                    Mathf.SmoothStep(0f, 1f, t / StepDuration));
                yield return null;
            }
            transform.position = to;

            BoardGenerator.Instance.Tiles[next].Highlight();

            // Fire landing event only on the final tile
            if (i == steps - 1)
                OnLandedOnTile?.Invoke(BoardGenerator.Instance.Tiles[next]);
            else
                yield return new WaitForSeconds(0.04f);
        }
    }

    Vector3 TileWorldPos(int index)
    {
        Vector3 p = BoardGenerator.Instance.Tiles[index].transform.position;
        p.y += 0.18f;
        return p;
    }

    static Sprite BuildCircleSprite(int radius, Color color)
    {
        int sz = radius * 2;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[sz * sz];
        float cx = radius - 0.5f, cy = radius - 0.5f;

        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d <= radius - 1.5f)
                px[y * sz + x] = color;
            else if (d <= radius)
                px[y * sz + x] = Color.Lerp(color, Color.clear,
                    (d - (radius - 1.5f)) / 1.5f);
            else
                px[y * sz + x] = Color.clear;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, 100f);
    }
}
