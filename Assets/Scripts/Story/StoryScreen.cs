using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StoryScreen : MonoBehaviour
{
    public static StoryScreen Instance { get; private set; }

    Canvas     _canvas;
    GameObject _bodyPanel;
    GameObject _messagePanel;
    Text       _messageText;
    Button     _searchBtn;
    Button     _continueBtn;
    bool       _done;

    static readonly string[] FlashlightBonusPool = { "ammo","ammo","meds","food","pills","battery" };

    static readonly string[] Stories =
    {
        "Day 14.\n\nThey came at night. We thought the walls would hold.\n\nThey didn't.\n\nIf you find this, the police station on 5th has supplies in the basement. Stay low. Stay quiet.\n\n— M",
        "The infection spreads faster than they told us. It's not just bites.\n\nThe air near the hospital district... don't breathe it without a mask. I watched my partner turn in six hours.\n\nRun.\n\n— Dr. Hayes, Day 9",
        "Military pulled out three weeks ago. They blew the bridge at dawn. No warning.\n\nThere's a survivor camp in the old factory past the river. They have food.\n\nI don't know if they're still alive.\n\n— Anonymous",
        "To whoever reads this:\n\nWe are not the last. Signals coming from the east — Morse code. Regular intervals. Someone is still out there.\n\nKeep moving. Keep fighting.\n\n— Reyes"
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
        _bodyPanel.SetActive(true);
        _messagePanel.SetActive(false);
        _messageText.text = "";
        _searchBtn.interactable = true;
        _continueBtn.gameObject.SetActive(false);
        _canvas.gameObject.SetActive(true);

        while (!_done)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    void OnSearchClicked()
    {
        _searchBtn.interactable = false;
        StartCoroutine(RevealMessage());
    }

    IEnumerator RevealMessage()
    {
        _bodyPanel.SetActive(false);
        _messagePanel.SetActive(true);

        string story = Stories[Random.Range(0, Stories.Length)];
        _messageText.text = "";

        foreach (char c in story)
        {
            _messageText.text += c;
            yield return new WaitForSeconds(c == '\n' ? 0.12f : 0.032f);
        }

        // Flashlight bonus — sweep the room for hidden items
        var flashlight = PlayerEquipment.Instance?.Get(EquipSlot.Utility);
        if (flashlight?.id == "flashlight")
        {
            string bonusId  = FlashlightBonusPool[Random.Range(0, FlashlightBonusPool.Length)];
            int    bonusQty = bonusId == "ammo" ? Random.Range(2, 5) : 1;

            string bonusLine = "\n\n[Flashlight] Your beam sweeps the room.\nSomething catches the light in the corner.";
            foreach (char c in bonusLine)
            {
                _messageText.text += c;
                yield return new WaitForSeconds(c == '\n' ? 0.10f : 0.030f);
            }

            yield return new WaitForSeconds(0.4f);

            if (LootPickupPrompt.Instance != null)
                yield return StartCoroutine(LootPickupPrompt.Instance.Show(bonusId, bonusQty));
        }

        _continueBtn.gameObject.SetActive(true);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("StoryCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 25;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark overlay
        var dim = Rect("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.90f);

        // Main panel
        var panel = Center("Panel", canvasGO.transform, Vector2.zero, new Vector2(820, 580));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.98f);

        // Top border accent
        var border = Rect("Border", panel);
        border.anchorMin = new Vector2(0, 1); border.anchorMax = new Vector2(1, 1);
        border.pivot = new Vector2(0.5f, 1); border.sizeDelta = new Vector2(0, 3);
        border.anchoredPosition = Vector2.zero;
        border.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.32f, 0.18f);

        // Title
        AddText(Center("Title", panel, new Vector2(0, 250), new Vector2(740, 48)),
                "YOU FOUND SOMETHING...", 28, FontStyle.Bold,
                new Color(0.60f, 0.50f, 0.38f), TextAnchor.MiddleCenter);

        // ── Body panel ────────────────────────────────────────────────────────
        var bodyRT = Center("BodyPanel", panel, new Vector2(0, 30), new Vector2(420, 300));
        _bodyPanel = bodyRT.gameObject;
        BuildBody(bodyRT);

        // SEARCH button
        var searchRT = Center("SearchBtn", panel, new Vector2(0, -220), new Vector2(210, 56));
        searchRT.gameObject.AddComponent<Image>().color = new Color(0.42f, 0.28f, 0.12f);
        _searchBtn = searchRT.gameObject.AddComponent<Button>();
        var sc = _searchBtn.colors;
        sc.highlightedColor = new Color(0.58f, 0.40f, 0.18f);
        sc.pressedColor     = new Color(0.28f, 0.18f, 0.08f);
        _searchBtn.colors   = sc;
        _searchBtn.onClick.AddListener(OnSearchClicked);
        AddText(Center("Lbl", searchRT, Vector2.zero, searchRT.sizeDelta),
                "SEARCH", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // ── Message panel ──────────────────────────────────────────────────────
        var msgRT = Center("MessagePanel", panel, new Vector2(0, 20), new Vector2(720, 420));
        _messagePanel = msgRT.gameObject;
        msgRT.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.11f, 0.08f);

        // Worn-paper inner border
        var innerBorder = Rect("InnerBorder", msgRT);
        innerBorder.anchorMin = new Vector2(0.02f, 0.02f);
        innerBorder.anchorMax = new Vector2(0.98f, 0.98f);
        innerBorder.sizeDelta = Vector2.zero;
        innerBorder.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.15f, 0.10f);

        var msgTxtRT = Center("Text", msgRT, new Vector2(0, 20), new Vector2(640, 310));
        _messageText = msgTxtRT.gameObject.AddComponent<Text>();
        _messageText.fontSize           = 22;
        _messageText.lineSpacing        = 1.4f;
        _messageText.color              = new Color(0.88f, 0.80f, 0.65f);
        _messageText.alignment          = TextAnchor.UpperLeft;
        _messageText.font               = DefaultFont();
        _messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _messageText.verticalOverflow   = VerticalWrapMode.Overflow;

        // CONTINUE button (hidden until typing done)
        var contRT = Center("ContinueBtn", msgRT, new Vector2(260, -175), new Vector2(160, 46));
        contRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.42f, 0.18f);
        _continueBtn = contRT.gameObject.AddComponent<Button>();
        var cc = _continueBtn.colors;
        cc.highlightedColor = new Color(0.25f, 0.58f, 0.25f);
        cc.pressedColor     = new Color(0.12f, 0.28f, 0.12f);
        _continueBtn.colors = cc;
        _continueBtn.onClick.AddListener(() => _done = true);
        AddText(Center("Lbl", contRT, Vector2.zero, contRT.sizeDelta),
                "CONTINUE", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        _continueBtn.gameObject.SetActive(false);

        _messagePanel.SetActive(false);
    }

    void BuildBody(RectTransform parent)
    {
        // Shadow / ground stain
        var shadow = Center("Shadow", parent, new Vector2(5, -120), new Vector2(160, 30));
        shadow.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.03f, 0.02f, 0.7f);

        // Legs
        var legL = Center("LegL", parent, new Vector2(-22, -70), new Vector2(30, 90));
        legL.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.15f, 0.10f);
        var legR = Center("LegR", parent, new Vector2(22, -65), new Vector2(28, 85));
        legR.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.13f, 0.09f);

        // Torso
        var torso = Center("Torso", parent, new Vector2(0, 10), new Vector2(72, 110));
        torso.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.18f, 0.12f);

        // Left arm (reaching out)
        var armL = Center("ArmL", parent, new Vector2(-60, -10), new Vector2(55, 22));
        armL.eulerAngles = new Vector3(0, 0, 20);
        armL.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.10f);

        // Right arm
        var armR = Center("ArmR", parent, new Vector2(52, 20), new Vector2(50, 20));
        armR.eulerAngles = new Vector3(0, 0, -15);
        armR.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.14f, 0.09f);

        // Head (circle-ish)
        var head = Center("Head", parent, new Vector2(-4, 85), new Vector2(58, 62));
        head.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.10f);

        // X eyes
        AddText(Center("EyeL", parent, new Vector2(-16, 90), new Vector2(22, 22)),
                "x", 16, FontStyle.Bold, new Color(0.55f, 0.08f, 0.08f), TextAnchor.MiddleCenter);
        AddText(Center("EyeR", parent, new Vector2(10, 90), new Vector2(22, 22)),
                "x", 16, FontStyle.Bold, new Color(0.55f, 0.08f, 0.08f), TextAnchor.MiddleCenter);

        // Decay patches
        var p1 = Center("Patch1", parent, new Vector2(-18, 20), new Vector2(20, 18));
        p1.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f, 0.85f);
        var p2 = Center("Patch2", parent, new Vector2(20, -20), new Vector2(16, 16));
        p2.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.03f, 0.85f);
        var p3 = Center("Patch3", parent, new Vector2(0, -55), new Vector2(24, 18));
        p3.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f, 0.85f);

        // Flavour label
        AddText(Center("Desc", parent, new Vector2(0, -148), new Vector2(360, 28)),
                "A decayed body lies on the ground.", 17, FontStyle.Italic,
                new Color(0.48f, 0.42f, 0.34f), TextAnchor.MiddleCenter);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Center(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Text AddText(RectTransform rt, string val, int size, FontStyle style,
                        Color color, TextAnchor align)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = val; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align; t.font = DefaultFont();
        return t;
    }

    static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
