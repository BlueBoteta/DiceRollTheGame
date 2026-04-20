using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LootPickupPrompt : MonoBehaviour
{
    public static LootPickupPrompt Instance { get; private set; }

    Canvas        _canvas;
    RectTransform _panelRT;
    Image         _itemBlock;
    Text          _itemSymbol;
    Text          _itemName;
    Text          _itemCat;
    Text          _itemDesc;
    int           _choice;   // -1 = waiting, 0 = take, 1 = leave

    static readonly Color[] CatColors =
    {
        new Color(1f,   0.82f, 0.10f),  // Resource
        new Color(0.22f,0.85f, 0.38f),  // Consumable
        new Color(0.20f,0.62f, 1.00f),  // UtilityTool
        new Color(1f,   0.22f, 0.22f),  // Weapon
        new Color(0.40f,0.72f, 0.88f),  // Armor
    };

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public IEnumerator Show(string itemId, int qty)
    {
        var item = ItemFactory.Create(itemId, qty);
        if (item == null) yield break;

        Color col = (int)item.category < CatColors.Length
            ? CatColors[(int)item.category] : Color.white;

        _itemBlock.color  = col * 0.35f + new Color(0.05f, 0.05f, 0.08f) * 0.65f;
        _itemSymbol.text  = CatSymbol(item.category);
        _itemSymbol.color = col;
        _itemName.text    = (qty > 1 ? qty + "x  " : "") + item.displayName.ToUpper();
        _itemName.color   = col;
        _itemCat.text     = item.category.ToString().ToUpper();
        _itemDesc.text    = ItemDesc(itemId);

        _choice = -1;
        _panelRT.localScale = Vector3.zero;
        _canvas.gameObject.SetActive(true);
        yield return StartCoroutine(BounceIn());

        while (_choice == -1) yield return null;

        if (_choice == 0) // TAKE IT
        {
            bool added = Inventory.Instance != null && Inventory.Instance.Add(itemId, qty);
            if (!added && ReplaceScreen.Instance != null)
                yield return StartCoroutine(ReplaceScreen.Instance.Prompt(itemId, qty));
        }

        _canvas.gameObject.SetActive(false);
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    IEnumerator BounceIn()
    {
        float t = 0f, dur = 0.28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = p < 0.65f ? Mathf.Lerp(0f, 1.15f, p / 0.65f)
                                 : Mathf.Lerp(1.15f, 1f, (p - 0.65f) / 0.35f);
            _panelRT.localScale = Vector3.one * s;
            yield return null;
        }
        _panelRT.localScale = Vector3.one;
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("LootPickupCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 27;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        var dim = Mk("Dim", cGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);

        _panelRT = C("Panel", cGO.transform, Vector2.zero, new Vector2(560, 230));
        _panelRT.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.09f, 0.98f);

        // Top accent
        var top = Mk("Top", _panelRT);
        top.anchorMin = new Vector2(0,1); top.anchorMax = new Vector2(1,1);
        top.pivot = new Vector2(0.5f,1); top.sizeDelta = new Vector2(0, 3);
        top.anchoredPosition = Vector2.zero;
        top.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.45f, 0.12f);

        // Item color block (left side)
        var blockRT = C("ItemBlock", _panelRT, new Vector2(-192f, 22f), new Vector2(106f, 106f));
        _itemBlock = blockRT.gameObject.AddComponent<Image>();

        var symRT = C("Symbol", blockRT, Vector2.zero, new Vector2(80f, 80f));
        _itemSymbol = T(symRT, "?", 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // Right side: name, category, desc
        _itemName = T(C("ItemName", _panelRT, new Vector2(62f, 74f), new Vector2(318f, 38f)),
            "", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

        _itemCat = T(C("ItemCat", _panelRT, new Vector2(62f, 42f), new Vector2(318f, 22f)),
            "", 13, FontStyle.Normal, new Color(0.45f, 0.45f, 0.55f), TextAnchor.MiddleLeft);

        var sepRT = C("Sep", _panelRT, new Vector2(62f, 24f), new Vector2(300f, 1f));
        sepRT.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.12f);

        _itemDesc = T(C("ItemDesc", _panelRT, new Vector2(62f, -10f), new Vector2(318f, 52f)),
            "", 15, FontStyle.Normal, new Color(0.72f, 0.65f, 0.50f), TextAnchor.UpperLeft);
        _itemDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        _itemDesc.verticalOverflow   = VerticalWrapMode.Overflow;

        // Buttons
        var takeRT = C("TakeBtn", _panelRT, new Vector2(-90f, -88f), new Vector2(170f, 42f));
        takeRT.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.40f, 0.18f);
        var takeBtn = takeRT.gameObject.AddComponent<Button>();
        var tc = takeBtn.colors;
        tc.highlightedColor = new Color(0.18f, 0.58f, 0.26f);
        tc.pressedColor     = new Color(0.08f, 0.28f, 0.12f);
        takeBtn.colors = tc;
        takeBtn.onClick.AddListener(() => _choice = 0);
        T(C("Lbl", takeRT, Vector2.zero, takeRT.sizeDelta),
          "TAKE IT", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var leaveRT = C("LeaveBtn", _panelRT, new Vector2(108f, -88f), new Vector2(170f, 42f));
        leaveRT.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.18f, 0.22f);
        var leaveBtn = leaveRT.gameObject.AddComponent<Button>();
        var lc = leaveBtn.colors;
        lc.highlightedColor = new Color(0.28f, 0.26f, 0.32f);
        lc.pressedColor     = new Color(0.13f, 0.12f, 0.15f);
        leaveBtn.colors = lc;
        leaveBtn.onClick.AddListener(() => _choice = 1);
        T(C("Lbl", leaveRT, Vector2.zero, leaveRT.sizeDelta),
          "LEAVE IT", 20, FontStyle.Bold, new Color(0.58f, 0.55f, 0.60f), TextAnchor.MiddleCenter);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string CatSymbol(ItemCategory cat) => cat switch
    {
        ItemCategory.Weapon      => "W",
        ItemCategory.Armor       => "A",
        ItemCategory.Consumable  => "+",
        ItemCategory.UtilityTool => "U",
        ItemCategory.Resource    => "R",
        _                        => "?"
    };

    static string ItemDesc(string id) => id switch
    {
        "ammo"       => "Count them. Every round is a decision.",
        "scrap"      => "Bent metal, broken things.\nThis world runs on broken things.",
        "meds"       => "Gauze soaked brown. Restores 20 HP.",
        "battery"    => "Still has juice. So do you.",
        "medkit"     => "Restores 30 HP.\nHold together a little longer.",
        "food"       => "Expired rations. Restores 10 HP.",
        "pills"      => "No label. Could be anything.\nRestores 15 HP. Works.",
        "lockpick"   => "Some doors don't want to open.\nMake them.",
        "pistol"     => "Cold. Reliable.\nDoesn't ask questions.",
        "shotgun"    => "Loud. Final.\nLeaves nothing behind.",
        "knife"      => "Up close. Personal.\nThe quietest way.",
        "vest"       => "Blocks 3 damage per hit.\nBetter than nothing.",
        "helmet"     => "Blocks 2 damage per hit.\nDented. Someone wore this before.",
        "riot_gear"  => "Blocks 5 damage per hit.\nHeavy. Miserable. Worth it.",
        "flashlight" => "Reveals things in the dark.\nSpot them before they spot you.",
        _            => ""
    };

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
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
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
