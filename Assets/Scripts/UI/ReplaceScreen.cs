using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReplaceScreen : MonoBehaviour
{
    public static ReplaceScreen Instance { get; private set; }

    Canvas   _canvas;
    Text     _incomingName;
    Text     _incomingCat;
    Button[] _slotBtns = new Button[8];
    Text[]   _slotTxt  = new Text[8];
    Image[]  _slotBg   = new Image[8];
    int      _choice;   // -2 = waiting, -1 = leave, 0-7 = slot to replace

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    // Callers yield this coroutine; it handles the Replace or Leave decision.
    public IEnumerator Prompt(string incomingId, int qty)
    {
        if (Inventory.Instance == null) yield break;
        var item = ItemFactory.Create(incomingId, qty);
        if (item == null) yield break;

        _incomingName.text = (qty > 1 ? qty + "x  " : "") + item.displayName.ToUpper();
        _incomingCat.text  = item.category.ToString().ToUpper();

        for (int i = 0; i < 8; i++)
        {
            var slot = Inventory.Instance.GetSlot(i);
            if (slot != null)
            {
                _slotTxt[i].text = slot.stackable && slot.quantity > 1
                    ? slot.displayName + "  \u00d7" + slot.quantity
                    : slot.displayName;
                _slotBg[i].color       = new Color(0.14f, 0.10f, 0.08f);
                _slotBtns[i].interactable = true;
            }
            else
            {
                _slotTxt[i].text = "\u2014 empty \u2014";
                _slotBg[i].color       = new Color(0.08f, 0.08f, 0.11f);
                _slotBtns[i].interactable = false;
            }
        }

        _choice = -2;
        _canvas.gameObject.SetActive(true);
        while (_choice == -2) yield return null;
        _canvas.gameObject.SetActive(false);

        if (_choice >= 0)
            Inventory.Instance.ReplaceSlot(_choice, incomingId, qty);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("ReplaceCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 28;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        var dim = Mk("Dim", cGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var panel = C("Panel", cGO.transform, Vector2.zero, new Vector2(840, 490));
        panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.09f, 0.98f);

        // Top accent stripe
        var top = Mk("Top", panel);
        top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
        top.pivot = new Vector2(0.5f, 1); top.sizeDelta = new Vector2(0, 3);
        top.anchoredPosition = Vector2.zero;
        top.gameObject.AddComponent<Image>().color = new Color(0.80f, 0.20f, 0.20f);

        // Header
        T(C("Header", panel, new Vector2(0, 213), new Vector2(800, 38)),
          "INVENTORY FULL  —  DROP A SLOT TO MAKE ROOM", 21, FontStyle.Bold,
          new Color(0.85f, 0.32f, 0.32f), TextAnchor.MiddleCenter);

        // Incoming item info bar
        var inBox = C("IncomingBox", panel, new Vector2(0, 168), new Vector2(800, 44));
        inBox.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.06f);

        T(C("InLbl", inBox, new Vector2(-295, 0), new Vector2(150, 38)),
          "INCOMING:", 13, FontStyle.Bold, new Color(0.40f, 0.40f, 0.48f), TextAnchor.MiddleLeft);

        _incomingName = T(C("InName", inBox, new Vector2(60, 0), new Vector2(360, 38)),
          "", 19, FontStyle.Bold, new Color(1f, 0.88f, 0.32f), TextAnchor.MiddleLeft);

        _incomingCat = T(C("InCat", inBox, new Vector2(320, 0), new Vector2(140, 38)),
          "", 13, FontStyle.Normal, new Color(0.45f, 0.45f, 0.58f), TextAnchor.MiddleLeft);

        // Slot grid label
        T(C("SlotHdr", panel, new Vector2(-80, 120), new Vector2(640, 24)),
          "SELECT A SLOT TO REPLACE:", 14, FontStyle.Bold,
          new Color(0.40f, 0.40f, 0.52f), TextAnchor.MiddleLeft);

        // 8 slots — 4 cols × 2 rows
        float sw = 178f, sh = 66f, gx = 10f, gy = 8f;
        float startX = -(4 * sw + 3 * gx) / 2f + sw / 2f;
        for (int i = 0; i < 8; i++)
        {
            int   col = i % 4, row = i / 4;
            float x   = startX + col * (sw + gx);
            float y   = 62f - row * (sh + gy);

            var s = C("Slot" + i, panel, new Vector2(x, y), new Vector2(sw, sh));
            _slotBg[i] = s.gameObject.AddComponent<Image>();
            _slotBg[i].color = new Color(0.14f, 0.10f, 0.08f);

            var st = C("Txt", s, Vector2.zero, new Vector2(sw - 12f, sh - 8f));
            _slotTxt[i] = T(st, "", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            _slotTxt[i].horizontalOverflow = HorizontalWrapMode.Wrap;

            _slotBtns[i] = s.gameObject.AddComponent<Button>();
            var bc = _slotBtns[i].colors;
            bc.highlightedColor = new Color(1.25f, 1.05f, 0.75f);
            bc.pressedColor     = new Color(0.70f, 0.55f, 0.35f);
            _slotBtns[i].colors = bc;
            int idx = i;
            _slotBtns[i].onClick.AddListener(() => _choice = idx);
        }

        // LEAVE IT button
        var lv = C("LeaveBtn", panel, new Vector2(0, -212), new Vector2(200, 44));
        lv.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var lvBtn = lv.gameObject.AddComponent<Button>();
        var lc = lvBtn.colors;
        lc.highlightedColor = new Color(0.26f, 0.26f, 0.32f);
        lc.pressedColor     = new Color(0.12f, 0.12f, 0.16f);
        lvBtn.colors = lc;
        lvBtn.onClick.AddListener(() => _choice = -1);
        T(C("Lbl", lv, Vector2.zero, lv.sizeDelta),
          "LEAVE IT", 19, FontStyle.Bold, new Color(0.55f, 0.55f, 0.60f), TextAnchor.MiddleCenter);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform Mk(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform C(string n, Transform p, Vector2 pos, Vector2 sz)
    {
        var rt = Mk(n, p);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;
        return rt;
    }

    static Text T(RectTransform rt, string val, int sz, FontStyle fs, Color col, TextAnchor a)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = val; t.fontSize = sz; t.fontStyle = fs; t.color = col; t.alignment = a;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }
}
