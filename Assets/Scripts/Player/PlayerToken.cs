using System;
using System.Collections;
using UnityEngine;

public class PlayerToken : MonoBehaviour
{
    public static PlayerToken Instance { get; private set; }

    public int CurrentTile { get; private set; }

    public event Action<BoardTile> OnLandedOnTile;

    const float StepDuration = 0.22f;
    const float IdleFps      = 10f;
    // y offset so the character's feet sit on the tile surface
    const float TileYOffset  = 0.0f;

    SpriteRenderer _sr;
    Sprite[]       _idleFrames;
    Coroutine      _idleRoutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 60;

        _idleFrames = LoadIdleFrames();
        _sr.sprite  = _idleFrames.Length > 0 ? _idleFrames[0] : null;
    }

    void Start()
    {
        PlaceOnTile(0);
        PlayIdle();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlaceOnTile(int index)
    {
        CurrentTile = index;
        transform.position = TileWorldPos(index);
    }

    public IEnumerator MoveSteps(int steps)
    {
        StopIdle();
        _sr.sprite = _idleFrames[0]; // freeze on first frame during movement

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

            if (i == steps - 1)
                OnLandedOnTile?.Invoke(BoardGenerator.Instance.Tiles[next]);
            else
                yield return new WaitForSeconds(0.04f);
        }

        PlayIdle();
    }

    public IEnumerator MoveToBoss()
    {
        StopIdle();
        _sr.sprite = _idleFrames[0];

        Vector3 from    = transform.position;
        Vector3 bossPos = BoardGenerator.Instance.BossTile.transform.position;
        bossPos.y += TileYOffset;

        float duration = 1.4f, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, bossPos,
                Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.position = bossPos;
        PlayIdle();
        OnLandedOnTile?.Invoke(BoardGenerator.Instance.BossTile);
    }

    // ── Idle animation ────────────────────────────────────────────────────────

    void PlayIdle()
    {
        StopIdle();
        _idleRoutine = StartCoroutine(IdleLoop());
    }

    void StopIdle()
    {
        if (_idleRoutine != null) { StopCoroutine(_idleRoutine); _idleRoutine = null; }
    }

    IEnumerator IdleLoop()
    {
        float interval = 1f / IdleFps;
        int   frame    = 0;
        while (true)
        {
            _sr.sprite = _idleFrames[frame % _idleFrames.Length];
            frame++;
            yield return new WaitForSeconds(interval);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 TileWorldPos(int index)
    {
        Vector3 p = BoardGenerator.Instance.Tiles[index].transform.position;
        p.y += TileYOffset;
        return p;
    }

    // ── Sprite loading ────────────────────────────────────────────────────────

    static Sprite[] LoadIdleFrames()
    {
        var tex = Resources.Load<Texture2D>("Character/IDLECharacter");
        if (tex == null) return new[] { BuildFallback() };

        // Get slice rects from the imported sprites, ignore their broken pivots
        var raw = Resources.LoadAll<Sprite>("Character/IDLECharacter");
        System.Array.Sort(raw, (a, b) => ParseIdx(a.name).CompareTo(ParseIdx(b.name)));

        // Filter out oversized edge artifacts (width > 120px)
        var clean = System.Array.FindAll(raw, s => s.rect.width < 120f);
        if (clean.Length == 0) clean = raw;

        // Re-create sprites with correct pivot (bottom-center at 12% from bottom = feet)
        // and PPU=300 so the character is ~0.26 units wide
        const float Ppu      = 300f;
        const float PivotY   = 0.12f; // normalized: near feet

        var result = new Sprite[clean.Length];
        for (int i = 0; i < clean.Length; i++)
        {
            result[i] = Sprite.Create(
                tex,
                clean[i].rect,
                new Vector2(0.5f, PivotY),
                Ppu);
        }
        return result;
    }

    static int ParseIdx(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
    }

    static Sprite BuildFallback()
    {
        int sz = 40;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var px  = new Color[sz * sz];
        float cx = sz * 0.5f - 0.5f, cy = sz * 0.5f - 0.5f, r = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            px[y * sz + x] = d <= r - 2f ? new Color(1f, 0.85f, 0.1f)
                           : d <= r      ? Color.Lerp(new Color(1f, 0.85f, 0.1f),
                                           Color.clear, (d - (r - 2f)) / 2f)
                           : Color.clear;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.12f), 100f);
    }
}
