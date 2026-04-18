using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LootCarScreen : MonoBehaviour
{
    public static LootCarScreen Instance { get; private set; }

    Canvas        _canvas;
    RectTransform _trunkLid;
    GameObject    _lootItemGO;
    Text          _messageText;
    Button        _openBtn;
    Button        _continueBtn;
    bool          _done;

    static readonly string[] CarLootPool =
    {
        "ammo","ammo","ammo","scrap","scrap","medkit","food","food","pills","lockpick"
    };

    static readonly string[] Messages =
    {
        "Half a protein bar and a box of hollow-points.\nThe bar expired two years ago.\nThe hollow-points didn't.\n\nToday's looking up.",
        "Emergency roadside kit. Flares, cables, someone's diary.\nDay 3: 'They're getting closer.'\nDay 4 is blank.\n\nYou take the flares.",
        "A child's backpack in the trunk.\nCrayons. Fruit snacks. A loaded .38.\n\nYou don't ask questions.\nYou take the .38.",
        "Spare tyre, jumper cables, three warm beers.\nUnder the spare — a full magazine.\n\nThe beers are terrible.\nYou drink them all.",
    };

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open()
    {
        _done = false;
        _trunkLid.localEulerAngles = Vector3.zero;
        _lootItemGO.SetActive(false);
        _lootItemGO.GetComponent<RectTransform>().localScale = Vector3.one;
        _messageText.text = "";
        _openBtn.gameObject.SetActive(true);
        _openBtn.interactable = true;
        _continueBtn.gameObject.SetActive(false);
        _canvas.gameObject.SetActive(true);

        while (!_done)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    void OnOpenClicked()
    {
        _openBtn.interactable = false;
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        float t = 0f, dur = 0.55f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float angle = Mathf.Lerp(0f, 65f, Mathf.SmoothStep(0f, 1f, t / dur));
            _trunkLid.localEulerAngles = new Vector3(0f, 0f, angle);
            yield return null;
        }
        _trunkLid.localEulerAngles = new Vector3(0f, 0f, 65f);

        yield return new WaitForSeconds(0.25f);

        _lootItemGO.SetActive(true);
        yield return StartCoroutine(ScaleBounce(_lootItemGO.GetComponent<RectTransform>()));

        yield return new WaitForSeconds(0.2f);
        _openBtn.gameObject.SetActive(false);

        // Pick and give loot
        string lootId = CarLootPool[Random.Range(0, CarLootPool.Length)];
        int qty = lootId == "ammo" ? Random.Range(2, 5) : 1;
        bool added = Inventory.Instance != null && Inventory.Instance.Add(lootId, qty);
        string itemName = ItemFactory.Create(lootId)?.displayName ?? lootId;
        string itemLine = added
            ? (qty > 1 ? $"\n\n+ {qty}x {itemName} added to inventory."
                       : $"\n\n+ {itemName} added to inventory.")
            : "\n\nInventory full. You leave it behind.";

        string msg = Messages[Random.Range(0, Messages.Length)] + itemLine;
        foreach (char c in msg)
        {
            _messageText.text += c;
            yield return new WaitForSeconds(c == '\n' ? 0.09f : 0.028f);
        }

        _continueBtn.gameObject.SetActive(true);
    }

    IEnumerator ScaleBounce(RectTransform rt)
    {
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = p < 0.6f ? Mathf.Lerp(0f, 1.3f, p / 0.6f)
                               : Mathf.Lerp(1.3f, 1f, (p - 0.6f) / 0.4f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("LootCarCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 25;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var dim = Rt("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.90f);

        var panel = Ctr("Panel", canvasGO.transform, Vector2.zero, new Vector2(920, 520));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.98f);

        var brd = Rt("Border", panel);
        brd.anchorMin = new Vector2(0, 1); brd.anchorMax = new Vector2(1, 1);
        brd.pivot = new Vector2(0.5f, 1); brd.sizeDelta = new Vector2(0, 3);
        brd.anchoredPosition = Vector2.zero;
        brd.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.10f);

        Txt(Ctr("Title", panel, new Vector2(0, 228), new Vector2(840, 46)),
            "YOU FOUND A CAR!", 28, FontStyle.Bold,
            new Color(0.70f, 0.58f, 0.25f), TextAnchor.MiddleCenter);

        BuildCar(panel);

        // OPEN button — sits to the right of the trunk
        var openRT = Ctr("OpenBtn", panel, new Vector2(265, 60), new Vector2(110, 44));
        openRT.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.42f, 0.20f);
        _openBtn = openRT.gameObject.AddComponent<Button>();
        var oc = _openBtn.colors;
        oc.highlightedColor = new Color(0.35f, 0.58f, 0.28f);
        oc.pressedColor     = new Color(0.14f, 0.28f, 0.12f);
        _openBtn.colors = oc;
        _openBtn.onClick.AddListener(OnOpenClicked);
        Txt(Ctr("Lbl", openRT, Vector2.zero, openRT.sizeDelta),
            "OPEN", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // Message area
        var msgBg = Ctr("MsgBg", panel, new Vector2(0, -158), new Vector2(860, 155));
        msgBg.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.07f);
        var msgTR = Ctr("MsgText", msgBg, Vector2.zero, new Vector2(830, 138));
        _messageText = msgTR.gameObject.AddComponent<Text>();
        _messageText.fontSize           = 21;
        _messageText.lineSpacing        = 1.4f;
        _messageText.color              = new Color(0.88f, 0.80f, 0.65f);
        _messageText.alignment          = TextAnchor.UpperLeft;
        _messageText.font               = DefaultFont();
        _messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _messageText.verticalOverflow   = VerticalWrapMode.Overflow;

        var contRT = Ctr("ContBtn", panel, new Vector2(315, -226), new Vector2(160, 44));
        contRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.42f, 0.18f);
        _continueBtn = contRT.gameObject.AddComponent<Button>();
        var cc = _continueBtn.colors;
        cc.highlightedColor = new Color(0.25f, 0.58f, 0.25f);
        cc.pressedColor     = new Color(0.12f, 0.28f, 0.12f);
        _continueBtn.colors = cc;
        _continueBtn.onClick.AddListener(() => _done = true);
        Txt(Ctr("Lbl", contRT, Vector2.zero, contRT.sizeDelta),
            "CONTINUE", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        _continueBtn.gameObject.SetActive(false);
    }

    void BuildCar(RectTransform panel)
    {
        // Car body: center (-20, 55), size 420x78
        // Right edge = -20+210 = 190, top = 55+39 = 94
        // Cabin: center (-95, 113), size 192x66, right edge = -95+96 = 1
        // Trunk x range: [5, 185], hinge at x=5, y=94

        Ctr("CarBody", panel, new Vector2(-20f, 55f), new Vector2(420f, 78f))
            .gameObject.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.26f);
        Ctr("Under", panel, new Vector2(-20f, 33f), new Vector2(390f, 10f))
            .gameObject.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.17f);

        Ctr("WheelF", panel, new Vector2(-135f, 20f), new Vector2(54f, 36f))
            .gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
        Ctr("WheelFHub", panel, new Vector2(-135f, 20f), new Vector2(22f, 22f))
            .gameObject.AddComponent<Image>().color = new Color(0.42f, 0.42f, 0.46f);
        Ctr("WheelR", panel, new Vector2(115f, 20f), new Vector2(54f, 36f))
            .gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
        Ctr("WheelRHub", panel, new Vector2(115f, 20f), new Vector2(22f, 22f))
            .gameObject.AddComponent<Image>().color = new Color(0.42f, 0.42f, 0.46f);

        Ctr("Cabin", panel, new Vector2(-95f, 113f), new Vector2(192f, 66f))
            .gameObject.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.30f);
        Ctr("WinF", panel, new Vector2(-158f, 116f), new Vector2(54f, 46f))
            .gameObject.AddComponent<Image>().color = new Color(0.14f, 0.22f, 0.32f, 0.85f);
        Ctr("WinR", panel, new Vector2(-38f, 116f), new Vector2(54f, 46f))
            .gameObject.AddComponent<Image>().color = new Color(0.14f, 0.22f, 0.32f, 0.85f);

        Ctr("Headlight", panel, new Vector2(-218f, 62f), new Vector2(14f, 18f))
            .gameObject.AddComponent<Image>().color = new Color(0.92f, 0.88f, 0.55f);
        Ctr("Taillight", panel, new Vector2(189f, 62f), new Vector2(12f, 18f))
            .gameObject.AddComponent<Image>().color = new Color(0.88f, 0.15f, 0.10f);

        // Trunk lid — pivot at bottom-left (hinge)
        var lidGO = new GameObject("TrunkLid");
        lidGO.transform.SetParent(panel, false);
        _trunkLid = lidGO.AddComponent<RectTransform>();
        _trunkLid.anchorMin        = _trunkLid.anchorMax = new Vector2(0.5f, 0.5f);
        _trunkLid.pivot            = new Vector2(0f, 0f);
        _trunkLid.anchoredPosition = new Vector2(5f, 94f);
        _trunkLid.sizeDelta        = new Vector2(180f, 12f);
        lidGO.AddComponent<Image>().color = new Color(0.30f, 0.30f, 0.34f);
        var lidEdge = Rt("LidEdge", _trunkLid);
        lidEdge.anchorMin = new Vector2(0, 1); lidEdge.anchorMax = new Vector2(1, 1);
        lidEdge.pivot = new Vector2(0.5f, 1); lidEdge.sizeDelta = new Vector2(-4, 3);
        lidEdge.anchoredPosition = Vector2.zero;
        lidEdge.gameObject.AddComponent<Image>().color = new Color(0.48f, 0.48f, 0.52f);

        // Loot crate (hidden until trunk opens, sits inside trunk)
        var lootRT = Ctr("LootCrate", panel, new Vector2(97f, 82f), new Vector2(50f, 44f));
        lootRT.gameObject.AddComponent<Image>().color = new Color(0.52f, 0.38f, 0.14f);
        Ctr("CrateH", lootRT, Vector2.zero, new Vector2(42f, 4f))
            .gameObject.AddComponent<Image>().color = new Color(0.30f, 0.20f, 0.07f);
        Ctr("CrateV", lootRT, Vector2.zero, new Vector2(4f, 36f))
            .gameObject.AddComponent<Image>().color = new Color(0.30f, 0.20f, 0.07f);
        Txt(Ctr("Star", lootRT, new Vector2(0, 10), new Vector2(36f, 18f)),
            "★", 13, FontStyle.Bold, new Color(1f, 0.88f, 0.2f), TextAnchor.MiddleCenter);
        _lootItemGO = lootRT.gameObject;
        _lootItemGO.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform Rt(string n, Transform p)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Ctr(string n, Transform p, Vector2 pos, Vector2 sz)
    {
        var rt = Rt(n, p);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sz;
        return rt;
    }

    static Text Txt(RectTransform rt, string val, int sz, FontStyle fs, Color col, TextAnchor a)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = val; t.fontSize = sz; t.fontStyle = fs; t.color = col; t.alignment = a;
        t.font = DefaultFont();
        return t;
    }

    static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
