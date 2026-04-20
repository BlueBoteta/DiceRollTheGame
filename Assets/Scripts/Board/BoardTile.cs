using System.Collections;
using UnityEngine;

public enum TileType { Normal, Combat, Loot, Story, Boss, Blackjack, SafeHouse, Scavenge }

public class BoardTile : MonoBehaviour
{
    public int pathIndex;
    public TileType tileType = TileType.Normal;
    public Vector2Int gridPos;

    public GameObject EnemyMarker;

    SpriteRenderer _sr;
    Color _baseColor;
    Coroutine _highlightRoutine;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
    }

    public void SetBaseColor(Color c)
    {
        _baseColor = c;
        if (_sr != null) _sr.color = c;
    }

    public void Highlight()
    {
        if (_sr == null) return;
        if (_highlightRoutine != null) StopCoroutine(_highlightRoutine);
        _highlightRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Color flash = BoardGenerator.Instance != null
            ? BoardGenerator.Instance.tileHighlightColor
            : new Color(1f, 0.85f, 0.2f);
        float duration = 0.4f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _sr.color = Color.Lerp(flash, _baseColor, t / duration);
            yield return null;
        }
        _sr.color = _baseColor;
    }
}
