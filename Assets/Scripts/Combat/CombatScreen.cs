using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CombatScreen : MonoBehaviour
{
    public static CombatScreen Instance { get; private set; }

    Canvas _canvas;

    RectTransform _playerIcon;
    RectTransform _playerHpFill;
    Text          _playerHpText;

    RectTransform _enemyIcon;
    RectTransform _enemyHpFill;
    Text          _enemyHpText;

    Text   _logText;
    Button _exitBtn;

    bool _exitPressed;
    int  _enemyHp, _enemyMaxHp, _enemyAttack;

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open()
    {
        _enemyMaxHp   = Random.Range(6, 13);
        _enemyHp      = _enemyMaxHp;
        _enemyAttack  = Random.Range(1, 3);
        _exitPressed  = false;
        _logText.text = "";
        _exitBtn.interactable = false;

        RefreshBars();
        _canvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(RunCombat());

        while (!_exitPressed)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    IEnumerator RunCombat()
    {
        Log("A zombie lunges at you!");
        yield return new WaitForSeconds(0.8f);

        while (_enemyHp > 0 && PlayerStats.Instance != null && PlayerStats.Instance.hp > 0)
        {
            // Player attacks
            int pdmg = Random.Range(2, 5);
            _enemyHp = Mathf.Max(0, _enemyHp - pdmg);
            Log("You strike for " + pdmg + " dmg.");
            yield return StartCoroutine(Punch(_playerIcon, new Vector2(35, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.45f);

            if (_enemyHp <= 0) break;

            // Enemy attacks
            PlayerStats.Instance.TakeDamage(_enemyAttack);
            Log("Zombie bites for " + _enemyAttack + " dmg.");
            yield return StartCoroutine(Punch(_enemyIcon, new Vector2(-35, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.45f);
        }

        if (_enemyHp <= 0)
        {
            Log("-- Enemy defeated! +1 Ammo --");
            PlayerStats.Instance?.AddAmmo(1);
        }
        else
        {
            Log("-- You were overwhelmed... --");
        }

        _exitBtn.interactable = true;
    }

    IEnumerator Punch(RectTransform rt, Vector2 dir)
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f, dur = 0.16f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = origin + dir * Mathf.Sin(t / dur * Mathf.PI);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    void RefreshBars()
    {
        float eRatio = _enemyMaxHp > 0 ? (float)_enemyHp / _enemyMaxHp : 0f;
        _enemyHpFill.anchorMax = new Vector2(Mathf.Clamp01(eRatio), 1f);
        _enemyHpText.text = _enemyHp + " / " + _enemyMaxHp;

        if (PlayerStats.Instance != null)
        {
            float pRatio = PlayerStats.Instance.maxHp > 0
                ? (float)PlayerStats.Instance.hp / PlayerStats.Instance.maxHp : 0f;
            _playerHpFill.anchorMax = new Vector2(Mathf.Clamp01(pRatio), 1f);
            _playerHpText.text = PlayerStats.Instance.hp + " / " + PlayerStats.Instance.maxHp;
        }
    }

    void Log(string msg) => _logText.text += msg + "\n";

    // ── UI Construction ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("CombatCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 20;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark overlay
        var dim = Make("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.83f);

        // Panel
        var panel = CenterRect("Panel", canvasGO.transform, Vector2.zero, new Vector2(880, 520));
        panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.11f, 0.97f);

        // Title
        var titleTxt = AddText(Make("Title", panel), "FIGHTING!", 44, FontStyle.Bold,
                               new Color(1f, 0.22f, 0.22f), TextAnchor.MiddleCenter);
        Pos(titleTxt.rectTransform, new Vector2(0, 220), new Vector2(800, 56));

        // Combatants
        BuildCombatant(panel, "Player", new Color(1f, 0.85f, 0.1f), new Vector2(-230, 30),
                       out _playerIcon, out _playerHpFill, out _playerHpText);
        BuildCombatant(panel, "Zombie", new Color(0.85f, 0.15f, 0.15f), new Vector2(230, 30),
                       out _enemyIcon, out _enemyHpFill, out _enemyHpText);

        // VS label
        AddText(Make("VS", panel), "VS", 36, FontStyle.Bold,
                new Color(0.55f, 0.55f, 0.55f), TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0, 50);

        // Log
        var logTxt = AddText(Make("Log", panel), "", 20, FontStyle.Normal,
                             new Color(0.8f, 0.8f, 0.8f), TextAnchor.LowerLeft);
        Pos(logTxt.rectTransform, new Vector2(0, -155), new Vector2(820, 120));
        _logText = logTxt;

        // Exit button
        var exitGO = Make("ExitBtn", panel);
        Pos(exitGO, new Vector2(180, -225), new Vector2(130, 48));
        exitGO.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.5f, 0.22f);
        _exitBtn = exitGO.gameObject.AddComponent<Button>();
        var ec = _exitBtn.colors;
        ec.highlightedColor = new Color(0.18f, 0.65f, 0.3f);
        ec.pressedColor     = new Color(0.08f, 0.35f, 0.15f);
        _exitBtn.colors = ec;
        _exitBtn.onClick.AddListener(() => _exitPressed = true);
        AddText(Make("Lbl", exitGO), "EXIT", 26, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter).rectTransform.anchoredPosition = Vector2.zero;
    }

    void BuildCombatant(RectTransform parent, string label, Color color, Vector2 pos,
                        out RectTransform icon, out RectTransform hpFill, out Text hpText)
    {
        var root = CenterRect(label + "Side", parent, pos, new Vector2(180, 300));

        // Circle sprite
        var iconRT = Make("Icon", root);
        Pos(iconRT, new Vector2(0, 70), new Vector2(110, 110));
        var iconImg = iconRT.gameObject.AddComponent<Image>();
        iconImg.sprite = CircleSprite(color);
        icon = iconRT;

        // HP bar background
        var hpBg = Make("HpBg", root);
        Pos(hpBg, new Vector2(0, -20), new Vector2(160, 18));
        hpBg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.05f, 0.05f);

        // HP fill (anchor-based width)
        var fill = Make("Fill", hpBg);
        fill.anchorMin  = Vector2.zero;
        fill.anchorMax  = Vector2.one;
        fill.sizeDelta  = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = label == "Player"
            ? new Color(0.2f, 0.75f, 0.3f)
            : new Color(0.85f, 0.2f, 0.2f);
        hpFill = fill;

        // HP text
        hpText = AddText(Make("HpTxt", root), "-- / --", 18, FontStyle.Normal,
                         Color.white, TextAnchor.MiddleCenter);
        Pos(hpText.rectTransform, new Vector2(0, -46), new Vector2(160, 24));

        // Name
        AddText(Make("Name", root), label, 20, FontStyle.Bold,
                new Color(0.75f, 0.75f, 0.75f), TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0, -75);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform Make(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform CenterRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Make(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Pos(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Text AddText(RectTransform rt, string val, int size, FontStyle style,
                        Color color, TextAnchor align)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text       = val;
        t.fontSize   = size;
        t.fontStyle  = style;
        t.color      = color;
        t.alignment  = align;
        t.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return t;
    }

    static Sprite CircleSprite(Color color)
    {
        int sz = 128, r = sz / 2;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz * sz];
        float cx = r - 0.5f, cy = r - 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            px[y * sz + x] = d <= r - 2f ? color
                           : d <= r      ? Color.Lerp(color, Color.clear, (d - (r - 2f)) / 2f)
                           : Color.clear;
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f, 100f);
    }
}
