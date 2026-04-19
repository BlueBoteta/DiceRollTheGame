using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SafeHouseScreen : MonoBehaviour
{
    public static SafeHouseScreen Instance { get; private set; }

    Canvas      _canvas;
    CanvasGroup _overlayCG;
    Text        _overlayText;
    GameObject  _loadingOverlay;

    GameObject    _lampGlow1, _lampGlow2, _lampGlow3;
    RectTransform _windowRT;
    RectTransform _doorRT;
    RectTransform _crateLid;
    GameObject    _tradePanel;

    Text _wallMsgText;
    Text _feedbackText;
    Text _restBtnLabel;
    Text _scrapDisplay;

    bool      _hasRested;
    bool      _crateOpen;
    Coroutine _feedbackCo;

    // ── Content ───────────────────────────────────────────────────────────────

    static readonly string[] WallMessages = {
        "COUNT YOUR BULLETS.",
        "THE WALLS BREATHE HERE.",
        "SLEEP IS NOT REST.",
        "YOU SMELL LIKE OUTSIDE.",
        "THE LAST ONE WHO RESTED HERE\nDIDN'T LEAVE.",
        "CHECK YOUR POCKETS.\nSOMETHING IS MISSING.",
        "THE CALENDAR IS GONE.\nYOU DON'T WANT TO KNOW.",
        "THEY KNOW YOU'RE IN HERE.",
        "IT WAS IN THIS ROOM.",
    };

    static readonly string[] WindowMessages = {
        "Scratching. Slow. Methodical.\nThen it stops.",
        "You press your ear to the gap.\nBreathing. It isn't yours.",
        "A shadow passes the crack.\nLarge. Dragging something.",
        "The boards creak outward.\nYou push back. It lets go.",
        "Footsteps. A lot of them.\nGoing past. This time.",
        "Silence.\nThat's the part that scares you most.",
        "They're out there.\nYou already knew that.",
        "Something wet hits the boards.\nYou decide not to look.",
    };

    static readonly string[] RestMessages = {
        "You close your eyes.\nSomething watches.",
        "You dream of a door.\nIt opens from the outside.",
        "The silence is worse\nthan the noise out there.",
        "You sleep for what feels\nlike too long.",
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open()
    {
        _hasRested = false;
        _crateOpen = false;
        if (_restBtnLabel != null) { _restBtnLabel.text = "[ REST ]"; _restBtnLabel.color = new Color(0.5f, 0.8f, 0.5f); }
        if (_crateLid != null)     _crateLid.localEulerAngles = Vector3.zero;
        if (_tradePanel != null)   _tradePanel.SetActive(false);
        if (_feedbackText != null) _feedbackText.color = Color.clear;
        if (_wallMsgText != null)  _wallMsgText.text   = "";

        _loadingOverlay.SetActive(true);
        _overlayCG.alpha  = 1f;
        _overlayText.text = "";

        _canvas.gameObject.SetActive(true);

        yield return StartCoroutine(EnterTransition());

        StartCoroutine(LampFlicker());
        StartCoroutine(AmbientPulse());

        yield return StartCoroutine(Typewriter(_wallMsgText,
            WallMessages[Random.Range(0, WallMessages.Length)], 0.055f));

        yield return new WaitUntil(() => !_canvas.gameObject.activeInHierarchy);
    }

    void Close()
    {
        if (_canvas != null && _canvas.gameObject.activeInHierarchy)
            StartCoroutine(CloseRoutine());
    }

    // ── Transitions ───────────────────────────────────────────────────────────

    IEnumerator EnterTransition()
    {
        yield return StartCoroutine(Typewriter(_overlayText, "SOMEWHERE SAFE.\nFOR NOW.", 0.048f));
        yield return new WaitForSeconds(0.55f);
        float t = 0f;
        while (t < 0.65f)
        {
            t += Time.deltaTime;
            _overlayCG.alpha = 1f - Mathf.Clamp01(t / 0.65f);
            yield return null;
        }
        _overlayCG.alpha = 0f;
        _loadingOverlay.SetActive(false);
    }

    IEnumerator CloseRoutine()
    {
        _overlayText.text = "";
        _loadingOverlay.SetActive(true);
        _overlayCG.alpha  = 0f;
        float t = 0f;
        while (t < 0.40f)
        {
            t += Time.deltaTime;
            _overlayCG.alpha = Mathf.Clamp01(t / 0.40f);
            yield return null;
        }
        _overlayCG.alpha = 1f;
        yield return StartCoroutine(Typewriter(_overlayText, "BACK TO THE STREETS.", 0.048f));
        yield return new WaitForSeconds(0.30f);
        _canvas.gameObject.SetActive(false);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 25;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        Transform root = transform;

        // Background layers
        StaticBg("BG",    root, new Color(0.028f, 0.022f, 0.018f), Vector2.zero,           Vector2.one);
        StaticBg("Wall",  root, new Color(0.075f, 0.060f, 0.048f), new Vector2(0, 0.35f),  Vector2.one);
        StaticBg("Floor", root, new Color(0.058f, 0.046f, 0.036f), Vector2.zero,           new Vector2(1, 0.39f));

        // Lamp glow sits above the table (right side, back wall boundary)
        BuildLampGlow(root, new Vector2(0.70f, 0.54f));
        BuildVignette(root);

        // Room objects
        BuildWindow(root);
        BuildTableAndLamp(root);
        BuildCot(root);
        BuildCrate(root);
        BuildDoor(root);

        // Wall message — right area of back wall
        var wmGO = NewGO("WallMsg", root);
        _wallMsgText = wmGO.AddComponent<Text>();
        _wallMsgText.font      = F();
        _wallMsgText.fontSize  = 22;
        _wallMsgText.fontStyle = FontStyle.Bold;
        _wallMsgText.color     = new Color(0.36f, 0.12f, 0.12f, 0.92f);
        _wallMsgText.alignment = TextAnchor.UpperLeft;
        AnchorsFull(wmGO.GetComponent<RectTransform>(), new Vector2(0.40f, 0.78f), new Vector2(0.84f, 0.95f));

        // Feedback — center
        var fbGO = NewGO("Feedback", root);
        _feedbackText = fbGO.AddComponent<Text>();
        _feedbackText.font      = F();
        _feedbackText.fontSize  = 26;
        _feedbackText.fontStyle = FontStyle.Italic;
        _feedbackText.color     = Color.clear;
        _feedbackText.alignment = TextAnchor.MiddleCenter;
        AnchorsFull(fbGO.GetComponent<RectTransform>(), new Vector2(0.20f, 0.43f), new Vector2(0.80f, 0.60f));

        // Room label — very bottom
        var rlGO  = NewGO("RoomLabel", root);
        var rlTxt = rlGO.AddComponent<Text>();
        rlTxt.text = "SAFE HOUSE"; rlTxt.font = F(); rlTxt.fontSize = 16;
        rlTxt.fontStyle = FontStyle.Bold;
        rlTxt.color     = new Color(0.34f, 0.26f, 0.15f, 0.40f);
        rlTxt.alignment = TextAnchor.MiddleCenter;
        var rlRT = rlGO.GetComponent<RectTransform>();
        rlRT.anchorMin = Vector2.zero; rlRT.anchorMax = new Vector2(1, 0);
        rlRT.pivot = new Vector2(0.5f, 0); rlRT.anchoredPosition = new Vector2(0, 5);
        rlRT.sizeDelta = new Vector2(0, 26);

        BuildTradePanel(root);
        BuildLoadingOverlay(root); // MUST be last — renders on top of everything
    }

    // ── Room Pieces ───────────────────────────────────────────────────────────

    void BuildLampGlow(Transform root, Vector2 center)
    {
        _lampGlow1 = GlowCircle("Glow1", root, center, 700, new Color(0.88f, 0.58f, 0.12f, 0.048f));
        _lampGlow2 = GlowCircle("Glow2", root, center, 390, new Color(0.94f, 0.70f, 0.18f, 0.092f));
        _lampGlow3 = GlowCircle("Glow3", root, center, 165, new Color(1.00f, 0.84f, 0.35f, 0.210f));
    }

    void BuildVignette(Transform root)
    {
        var go  = NewGO("Vignette", root);
        var img = go.AddComponent<Image>();
        img.sprite = VignetteSprite(256); img.color = Color.white; img.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
    }

    void BuildWindow(Transform root)
    {
        // Outer frame — back wall, left side
        var fGO  = NewGO("Window", root);
        var fImg = fGO.AddComponent<Image>();
        fImg.color = new Color(0.09f, 0.07f, 0.055f); fImg.raycastTarget = false;
        _windowRT = fGO.GetComponent<RectTransform>();
        _windowRT.anchorMin = _windowRT.anchorMax = new Vector2(0.18f, 0.70f);
        _windowRT.pivot = new Vector2(0.5f, 0.5f);
        _windowRT.anchoredPosition = Vector2.zero;
        _windowRT.sizeDelta = new Vector2(162, 194f);

        // Dark void outside (child)
        var glGO  = NewGO("Glass", fGO.transform);
        var glImg = glGO.AddComponent<Image>();
        glImg.color = new Color(0.008f, 0.006f, 0.010f); glImg.raycastTarget = false;
        PadStretch(glGO.GetComponent<RectTransform>(), 10f);

        // Wooden barricade planks (children)
        // angle, offsetX, offsetY, width, height
        (float a, float x, float y, float w, float h)[] planks = {
            (  -7f,  0f,  60f, 186f, 14f),
            (  32f,  5f,  12f, 222f, 12f),
            (  -4f,  0f, -32f, 186f, 14f),
            ( -28f, 12f,  10f, 208f, 11f),
        };
        foreach (var p in planks)
        {
            var plGO  = NewGO("Plank", fGO.transform);
            var plImg = plGO.AddComponent<Image>();
            plImg.color = new Color(0.30f, 0.19f, 0.08f); plImg.raycastTarget = false;
            var pRT = plGO.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition  = new Vector2(p.x, p.y);
            pRT.sizeDelta         = new Vector2(p.w, p.h);
            pRT.localEulerAngles  = new Vector3(0, 0, p.a);
        }

        // [ LISTEN ] label (child of frame)
        var lbGO  = NewGO("ListenLbl", fGO.transform);
        var lbTxt = lbGO.AddComponent<Text>();
        lbTxt.text = "[ LISTEN ]"; lbTxt.font = F(); lbTxt.fontSize = 14;
        lbTxt.color = new Color(0.38f, 0.27f, 0.16f, 0.75f);
        lbTxt.alignment = TextAnchor.MiddleCenter;
        var lbRT = lbGO.GetComponent<RectTransform>();
        lbRT.anchorMin = new Vector2(0, 0); lbRT.anchorMax = new Vector2(1, 0);
        lbRT.pivot = new Vector2(0.5f, 1f); lbRT.anchoredPosition = new Vector2(0, -5f);
        lbRT.sizeDelta = new Vector2(0, 20f);

        // WINDOW label above (root level)
        var wlGO  = NewGO("WindowLabel", root);
        var wlTxt = wlGO.AddComponent<Text>();
        wlTxt.text = "WINDOW"; wlTxt.font = F(); wlTxt.fontSize = 13;
        wlTxt.color = new Color(0.34f, 0.25f, 0.14f, 0.52f);
        wlTxt.alignment = TextAnchor.MiddleCenter;
        var wlRT = wlGO.GetComponent<RectTransform>();
        wlRT.anchorMin = wlRT.anchorMax = new Vector2(0.18f, 0.70f);
        wlRT.pivot = new Vector2(0.5f, 0f); wlRT.anchoredPosition = new Vector2(0, 102f);
        wlRT.sizeDelta = new Vector2(162, 17f);

        // Invisible click zone
        var cGO  = NewGO("WindowBtn", root);
        var cImg = cGO.AddComponent<Image>();
        cImg.color = Color.clear;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.18f, 0.70f);
        cRT.pivot = new Vector2(0.5f, 0.5f); cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(170, 228f);
        var cBtn = cGO.AddComponent<Button>();
        cBtn.transition = Selectable.Transition.None;
        cBtn.onClick.AddListener(OnWindowClick);
    }

    void BuildTableAndLamp(Transform root)
    {
        // Table surface — right side at back wall/floor boundary
        var tGO  = NewGO("Table", root);
        var tImg = tGO.AddComponent<Image>();
        tImg.color = new Color(0.19f, 0.13f, 0.065f); tImg.raycastTarget = false;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = new Vector2(0.70f, 0.48f);
        tRT.pivot = new Vector2(0.5f, 1f); tRT.anchoredPosition = Vector2.zero;
        tRT.sizeDelta = new Vector2(238, 15f);

        // Left leg (child)
        TableLeg("LegL", tGO.transform, 0.04f, 0.10f);
        // Right leg (child)
        TableLeg("LegR", tGO.transform, 0.90f, 0.96f);

        // Small book on table surface (flavour, child)
        var bkGO  = NewGO("Book", tGO.transform);
        var bkImg = bkGO.AddComponent<Image>();
        bkImg.color = new Color(0.11f, 0.08f, 0.054f); bkImg.raycastTarget = false;
        var bkRT = bkGO.GetComponent<RectTransform>();
        bkRT.anchorMin = bkRT.anchorMax = new Vector2(0.20f, 1f);
        bkRT.pivot = new Vector2(0.5f, 0f); bkRT.anchoredPosition = Vector2.zero;
        bkRT.sizeDelta = new Vector2(44, 7f);

        // Lamp body — at same anchor as table top, sitting ON it
        var lbGO  = NewGO("LampBody", root);
        var lbImg = lbGO.AddComponent<Image>();
        lbImg.color = new Color(0.19f, 0.14f, 0.07f); lbImg.raycastTarget = false;
        var lbRT = lbGO.GetComponent<RectTransform>();
        lbRT.anchorMin = lbRT.anchorMax = new Vector2(0.70f, 0.48f);
        lbRT.pivot = new Vector2(0.5f, 0f); lbRT.anchoredPosition = Vector2.zero;
        lbRT.sizeDelta = new Vector2(20, 44f);

        // Shade (child of lamp)
        var shGO  = NewGO("Shade", lbGO.transform);
        var shImg = shGO.AddComponent<Image>();
        shImg.color = new Color(0.33f, 0.21f, 0.07f); shImg.raycastTarget = false;
        var shRT = shGO.GetComponent<RectTransform>();
        shRT.anchorMin = shRT.anchorMax = new Vector2(0.5f, 1f);
        shRT.pivot = new Vector2(0.5f, 0f); shRT.anchoredPosition = Vector2.zero;
        shRT.sizeDelta = new Vector2(36, 17f);

        // Flame (child of lamp)
        var flGO  = NewGO("Flame", lbGO.transform);
        var flImg = flGO.AddComponent<Image>();
        flImg.color = new Color(1f, 0.60f, 0.12f, 0.92f);
        flImg.sprite = CircleSprite(32); flImg.raycastTarget = false;
        var flRT = flGO.GetComponent<RectTransform>();
        flRT.anchorMin = flRT.anchorMax = new Vector2(0.5f, 1f);
        flRT.pivot = new Vector2(0.5f, 0f); flRT.anchoredPosition = new Vector2(0, 17f);
        flRT.sizeDelta = new Vector2(12, 16f);

        // Lamp click zone
        var lcGO  = NewGO("LampClick", root);
        var lcImg = lcGO.AddComponent<Image>();
        lcImg.color = Color.clear;
        var lcRT = lcGO.GetComponent<RectTransform>();
        lcRT.anchorMin = lcRT.anchorMax = new Vector2(0.70f, 0.48f);
        lcRT.pivot = new Vector2(0.5f, 0f); lcRT.anchoredPosition = Vector2.zero;
        lcRT.sizeDelta = new Vector2(62, 88f);
        var lcBtn = lcGO.AddComponent<Button>();
        lcBtn.transition = Selectable.Transition.None;
        lcBtn.onClick.AddListener(OnLampClick);
    }

    void TableLeg(string name, Transform parent, float x0, float x1)
    {
        var go  = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.14f, 0.10f, 0.048f); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, 0); rt.anchorMax = new Vector2(x1, 0);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 90f);
    }

    void BuildCot(Transform root)
    {
        var cGO  = NewGO("Cot", root);
        var cImg = cGO.AddComponent<Image>();
        cImg.color = new Color(0.16f, 0.12f, 0.08f); cImg.raycastTarget = false;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.24f, 0.32f);
        cRT.pivot = new Vector2(0.5f, 0.5f); cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(298, 88f);

        // Mattress (child)
        var mGO  = NewGO("Mattress", cGO.transform);
        var mImg = mGO.AddComponent<Image>();
        mImg.color = new Color(0.20f, 0.16f, 0.12f); mImg.raycastTarget = false;
        PadStretch(mGO.GetComponent<RectTransform>(), 5f, 13f);

        // Pillow (child)
        var pGO  = NewGO("Pillow", cGO.transform);
        var pImg = pGO.AddComponent<Image>();
        pImg.color = new Color(0.26f, 0.20f, 0.15f); pImg.raycastTarget = false;
        var pRT = pGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.74f, 0.16f); pRT.anchorMax = new Vector2(0.93f, 0.84f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;

        // COT label
        var lGO  = NewGO("CotLabel", root);
        var lTxt = lGO.AddComponent<Text>();
        lTxt.text = "COT"; lTxt.font = F(); lTxt.fontSize = 13;
        lTxt.color = new Color(0.38f, 0.28f, 0.17f, 0.55f);
        lTxt.alignment = TextAnchor.MiddleCenter;
        var lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = lRT.anchorMax = new Vector2(0.24f, 0.32f);
        lRT.pivot = new Vector2(0.5f, 0f); lRT.anchoredPosition = new Vector2(0, 50f);
        lRT.sizeDelta = new Vector2(140, 17f);

        // REST button
        var rGO  = NewGO("RestBtn", root);
        var rImg = rGO.AddComponent<Image>();
        rImg.color = new Color(0.13f, 0.19f, 0.13f, 0.92f);
        var rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = rRT.anchorMax = new Vector2(0.24f, 0.32f);
        rRT.pivot = new Vector2(0.5f, 0f); rRT.anchoredPosition = new Vector2(0, -55f);
        rRT.sizeDelta = new Vector2(145, 36f);
        var rBtn = rGO.AddComponent<Button>();
        var rc = rBtn.colors;
        rc.highlightedColor = new Color(0.21f, 0.32f, 0.21f);
        rc.pressedColor     = new Color(0.08f, 0.13f, 0.08f);
        rBtn.colors = rc;
        rBtn.onClick.AddListener(OnRestClick);

        var rlGO  = NewGO("RestLabel", rGO.transform);
        _restBtnLabel = rlGO.AddComponent<Text>();
        _restBtnLabel.text      = "[ REST ]";
        _restBtnLabel.font      = F();
        _restBtnLabel.fontSize  = 18;
        _restBtnLabel.fontStyle = FontStyle.Bold;
        _restBtnLabel.color     = new Color(0.5f, 0.8f, 0.5f);
        _restBtnLabel.alignment = TextAnchor.MiddleCenter;
        Stretch(rlGO.GetComponent<RectTransform>());
    }

    void BuildCrate(Transform root)
    {
        var cGO  = NewGO("Crate", root);
        var cImg = cGO.AddComponent<Image>();
        cImg.color = new Color(0.20f, 0.14f, 0.07f); cImg.raycastTarget = false;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.76f, 0.26f);
        cRT.pivot = new Vector2(0.5f, 0f); cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(188, 148f);

        for (int i = 0; i < 3; i++)
        {
            float yf = 0.24f + i * 0.26f;
            var ln  = NewGO("Ln" + i, cGO.transform);
            var lnI = ln.AddComponent<Image>();
            lnI.color = new Color(0.13f, 0.09f, 0.04f, 0.55f); lnI.raycastTarget = false;
            var lRT = ln.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.05f, yf); lRT.anchorMax = new Vector2(0.95f, yf);
            lRT.pivot = new Vector2(0.5f, 0.5f); lRT.anchoredPosition = Vector2.zero;
            lRT.sizeDelta = new Vector2(0, 2f);
        }

        var lkGO  = NewGO("Lock", cGO.transform);
        var lkImg = lkGO.AddComponent<Image>();
        lkImg.color = new Color(0.52f, 0.42f, 0.14f); lkImg.raycastTarget = false;
        var lkRT = lkGO.GetComponent<RectTransform>();
        lkRT.anchorMin = lkRT.anchorMax = new Vector2(0.5f, 0.78f);
        lkRT.pivot = new Vector2(0.5f, 0.5f); lkRT.anchoredPosition = Vector2.zero;
        lkRT.sizeDelta = new Vector2(17, 21f);

        // Lid
        var lidGO  = NewGO("Lid", root);
        var lidImg = lidGO.AddComponent<Image>();
        lidImg.color = new Color(0.26f, 0.18f, 0.09f); lidImg.raycastTarget = false;
        _crateLid = lidGO.GetComponent<RectTransform>();
        _crateLid.anchorMin = _crateLid.anchorMax = new Vector2(0.76f, 0.26f);
        _crateLid.pivot = new Vector2(0.5f, 0f); _crateLid.anchoredPosition = new Vector2(0, 148f);
        _crateLid.sizeDelta = new Vector2(188, 30f);

        var slGO  = NewGO("CrateLabel", root);
        var slTxt = slGO.AddComponent<Text>();
        slTxt.text = "SUPPLY CRATE"; slTxt.font = F(); slTxt.fontSize = 13;
        slTxt.color = new Color(0.43f, 0.31f, 0.15f, 0.58f);
        slTxt.alignment = TextAnchor.MiddleCenter;
        var slRT = slGO.GetComponent<RectTransform>();
        slRT.anchorMin = slRT.anchorMax = new Vector2(0.76f, 0.26f);
        slRT.pivot = new Vector2(0.5f, 0f); slRT.anchoredPosition = new Vector2(0, 183f);
        slRT.sizeDelta = new Vector2(188, 17f);

        var tbGO  = NewGO("TradeBtn", root);
        var tbImg = tbGO.AddComponent<Image>();
        tbImg.color = new Color(0.22f, 0.16f, 0.07f, 0.92f);
        var tbRT = tbGO.GetComponent<RectTransform>();
        tbRT.anchorMin = tbRT.anchorMax = new Vector2(0.76f, 0.26f);
        tbRT.pivot = new Vector2(0.5f, 0f); tbRT.anchoredPosition = new Vector2(0, -46f);
        tbRT.sizeDelta = new Vector2(145, 36f);
        var tbBtn = tbGO.AddComponent<Button>();
        var tc = tbBtn.colors;
        tc.highlightedColor = new Color(0.36f, 0.26f, 0.10f);
        tc.pressedColor     = new Color(0.12f, 0.08f, 0.03f);
        tbBtn.colors = tc;
        tbBtn.onClick.AddListener(OnTradeClick);

        var tlGO  = NewGO("TradeLabel", tbGO.transform);
        var tlTxt = tlGO.AddComponent<Text>();
        tlTxt.text = "[ TRADE ]"; tlTxt.font = F(); tlTxt.fontSize = 18; tlTxt.fontStyle = FontStyle.Bold;
        tlTxt.color = new Color(0.90f, 0.70f, 0.26f); tlTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(tlGO.GetComponent<RectTransform>());
    }

    void BuildDoor(Transform root)
    {
        // Door frame — bottom LEFT, looks like a real door in the wall
        var dfGO  = NewGO("DoorFrame", root);
        var dfImg = dfGO.AddComponent<Image>();
        dfImg.color = new Color(0.11f, 0.08f, 0.055f);
        _doorRT = dfGO.GetComponent<RectTransform>();
        _doorRT.anchorMin = _doorRT.anchorMax = new Vector2(0.09f, 0.10f);
        _doorRT.pivot = new Vector2(0.5f, 0f); _doorRT.anchoredPosition = Vector2.zero;
        _doorRT.sizeDelta = new Vector2(120, 180f);

        // Door panel (child)
        var dpGO  = NewGO("DoorPanel", dfGO.transform);
        var dpImg = dpGO.AddComponent<Image>();
        dpImg.color = new Color(0.15f, 0.11f, 0.07f); dpImg.raycastTarget = false;
        PadStretch(dpGO.GetComponent<RectTransform>(), 8f, 6f);

        // Inset decoration (child of panel)
        var diGO  = NewGO("Inset", dpGO.transform);
        var diImg = diGO.AddComponent<Image>();
        diImg.color = new Color(0.12f, 0.09f, 0.055f); diImg.raycastTarget = false;
        PadStretch(diGO.GetComponent<RectTransform>(), 10f, 12f);

        // Handle (child of frame)
        var dhGO  = NewGO("Handle", dfGO.transform);
        var dhImg = dhGO.AddComponent<Image>();
        dhImg.color = new Color(0.48f, 0.37f, 0.12f); dhImg.raycastTarget = false;
        var dhRT = dhGO.GetComponent<RectTransform>();
        dhRT.anchorMin = dhRT.anchorMax = new Vector2(0.78f, 0.42f);
        dhRT.pivot = new Vector2(0.5f, 0.5f); dhRT.anchoredPosition = Vector2.zero;
        dhRT.sizeDelta = new Vector2(10, 10f);

        // [ LEAVE ] label above door
        var llGO  = NewGO("LeaveLabel", root);
        var llTxt = llGO.AddComponent<Text>();
        llTxt.text = "[ LEAVE ]"; llTxt.font = F(); llTxt.fontSize = 15;
        llTxt.color = new Color(0.48f, 0.36f, 0.20f, 0.78f);
        llTxt.alignment = TextAnchor.MiddleCenter;
        var llRT = llGO.GetComponent<RectTransform>();
        llRT.anchorMin = llRT.anchorMax = new Vector2(0.09f, 0.10f);
        llRT.pivot = new Vector2(0.5f, 0f); llRT.anchoredPosition = new Vector2(0, 185f);
        llRT.sizeDelta = new Vector2(120, 19f);

        // Button — frame Image is the target, gets amber tint on hover
        var dBtn = dfGO.AddComponent<Button>();
        var dc = dBtn.colors;
        dc.normalColor      = Color.white;
        dc.highlightedColor = new Color(1.35f, 1.10f, 0.65f, 1f);
        dc.pressedColor     = new Color(0.72f, 0.54f, 0.28f, 1f);
        dBtn.colors        = dc;
        dBtn.targetGraphic = dfImg;
        dBtn.onClick.AddListener(OnDoorClick);
    }

    void BuildTradePanel(Transform root)
    {
        _tradePanel = NewGO("TradePanel", root);
        var bg = _tradePanel.AddComponent<Image>();
        bg.color = new Color(0.050f, 0.040f, 0.030f, 0.97f);
        var panelRT = _tradePanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.18f, 0.07f);
        panelRT.anchorMax = new Vector2(0.82f, 0.91f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        Transform pt = _tradePanel.transform;

        PanelText(pt, "Title", "SUPPLY CRATE", 30, FontStyle.Bold,
            new Color(0.92f, 0.72f, 0.28f),
            new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,-14), new Vector2(0,42));

        PanelText(pt, "Sub", "trade scrap for supplies", 17, FontStyle.Italic,
            new Color(0.50f, 0.40f, 0.28f),
            new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), new Vector2(0,-58), new Vector2(0,26));

        var scGO = NewGO("ScrapCount", pt);
        _scrapDisplay = scGO.AddComponent<Text>();
        _scrapDisplay.font = F(); _scrapDisplay.fontSize = 21;
        _scrapDisplay.color = new Color(0.60f, 0.82f, 0.58f);
        _scrapDisplay.alignment = TextAnchor.MiddleRight;
        var scRT = scGO.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0.55f,1); scRT.anchorMax = new Vector2(0.97f,1);
        scRT.pivot = new Vector2(1,1); scRT.anchoredPosition = new Vector2(0,-14);
        scRT.sizeDelta = new Vector2(0,28);

        Divider(pt, "Div1", false, new Vector2(0,-90));

        (string id, string label, int cost, Color col)[] items = {
            ("pills",  "Pills              +15 HP", 1, new Color(0.72f, 0.92f, 0.72f)),
            ("food",   "Food               +10 HP", 1, new Color(0.92f, 0.77f, 0.52f)),
            ("meds",   "Medical Supplies   +20 HP", 2, new Color(0.72f, 0.86f, 1.00f)),
            ("ammo",   "Ammo  \u00d73",             3, new Color(1.00f, 0.82f, 0.32f)),
            ("medkit", "Medkit             +30 HP", 5, new Color(0.92f, 0.42f, 0.42f)),
        };
        for (int i = 0; i < items.Length; i++)
            TradeRow(pt, items[i].id, items[i].label, items[i].cost, items[i].col, -100f - i * 64f);

        Divider(pt, "Div2", true, new Vector2(0, 50));
        CloseBtn(pt);
    }

    void BuildLoadingOverlay(Transform root)
    {
        _loadingOverlay = NewGO("LoadingOverlay", root);
        var bg = _loadingOverlay.AddComponent<Image>();
        bg.color = Color.black; bg.raycastTarget = true;
        Stretch(_loadingOverlay.GetComponent<RectTransform>());
        _overlayCG = _loadingOverlay.AddComponent<CanvasGroup>();

        var tGO = NewGO("OverlayText", _loadingOverlay.transform);
        _overlayText = tGO.AddComponent<Text>();
        _overlayText.font = F(); _overlayText.fontSize = 32; _overlayText.fontStyle = FontStyle.Bold;
        _overlayText.color = new Color(0.72f, 0.56f, 0.26f, 1f);
        _overlayText.alignment = TextAnchor.MiddleCenter;
        AnchorsFull(tGO.GetComponent<RectTransform>(), new Vector2(0.25f, 0.42f), new Vector2(0.75f, 0.58f));
    }

    // ── Trade Panel Helpers ───────────────────────────────────────────────────

    void TradeRow(Transform parent, string id, string label, int cost, Color col, float yOff)
    {
        var rGO  = NewGO("Row_" + id, parent);
        var rImg = rGO.AddComponent<Image>();
        rImg.color = new Color(0.10f, 0.08f, 0.05f, 0.55f);
        var rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.04f,1); rRT.anchorMax = new Vector2(0.96f,1);
        rRT.pivot = new Vector2(0.5f,1); rRT.anchoredPosition = new Vector2(0,yOff);
        rRT.sizeDelta = new Vector2(0,54);
        Transform rp = rGO.transform;

        var iGO = NewGO("Item", rp); var iTxt = iGO.AddComponent<Text>();
        iTxt.text = label; iTxt.font = F(); iTxt.fontSize = 19;
        iTxt.color = col; iTxt.alignment = TextAnchor.MiddleLeft;
        var iRT = iGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0,0); iRT.anchorMax = new Vector2(0.62f,1);
        iRT.pivot = new Vector2(0,0.5f); iRT.anchoredPosition = new Vector2(14,0);
        iRT.sizeDelta = Vector2.zero;

        var cGO = NewGO("Cost", rp); var cTxt = cGO.AddComponent<Text>();
        cTxt.text = cost + " scrap"; cTxt.font = F(); cTxt.fontSize = 17;
        cTxt.color = new Color(0.60f,0.80f,0.55f); cTxt.alignment = TextAnchor.MiddleCenter;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.60f,0); cRT.anchorMax = new Vector2(0.80f,1);
        cRT.pivot = new Vector2(0.5f,0.5f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

        var bGO = NewGO("Buy", rp); var bImg = bGO.AddComponent<Image>();
        bImg.color = new Color(0.20f,0.28f,0.14f);
        var bRT = bGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.81f,0.10f); bRT.anchorMax = new Vector2(0.97f,0.90f);
        bRT.pivot = new Vector2(0.5f,0.5f); bRT.anchoredPosition = Vector2.zero; bRT.sizeDelta = Vector2.zero;
        var bBtn = bGO.AddComponent<Button>();
        var bc = bBtn.colors;
        bc.highlightedColor = new Color(0.32f,0.46f,0.20f);
        bc.pressedColor     = new Color(0.11f,0.17f,0.08f);
        bBtn.colors = bc;
        string cid = id; int ccost = cost; int cqty = id == "ammo" ? 3 : 1;
        bBtn.onClick.AddListener(() => OnBuy(cid, ccost, cqty));

        var blGO = NewGO("BuyLbl", bGO.transform); var blTxt = blGO.AddComponent<Text>();
        blTxt.text = "BUY"; blTxt.font = F(); blTxt.fontSize = 17; blTxt.fontStyle = FontStyle.Bold;
        blTxt.color = new Color(0.72f,0.92f,0.52f); blTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(blGO.GetComponent<RectTransform>());
    }

    void CloseBtn(Transform parent)
    {
        var cbGO  = NewGO("CloseBtn", parent);
        var cbImg = cbGO.AddComponent<Image>();
        cbImg.color = new Color(0.20f,0.14f,0.07f);
        var cbRT = cbGO.GetComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(0.30f,0); cbRT.anchorMax = new Vector2(0.70f,0);
        cbRT.pivot = new Vector2(0.5f,0); cbRT.anchoredPosition = new Vector2(0,8);
        cbRT.sizeDelta = new Vector2(0,36);
        var cbBtn = cbGO.AddComponent<Button>();
        var cc = cbBtn.colors;
        cc.highlightedColor = new Color(0.34f,0.24f,0.12f);
        cbBtn.colors = cc;
        cbBtn.onClick.AddListener(OnCloseTrade);

        var clGO = NewGO("CloseLbl", cbGO.transform); var clTxt = clGO.AddComponent<Text>();
        clTxt.text = "CLOSE"; clTxt.font = F(); clTxt.fontSize = 19; clTxt.fontStyle = FontStyle.Bold;
        clTxt.color = new Color(0.72f,0.55f,0.30f); clTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(clGO.GetComponent<RectTransform>());
    }

    // ── Interaction ───────────────────────────────────────────────────────────

    void OnWindowClick()
    {
        Feedback(WindowMessages[Random.Range(0, WindowMessages.Length)], new Color(0.62f,0.50f,0.72f));
        StartCoroutine(WindowShake());
    }

    void OnRestClick()
    {
        if (_hasRested)
        {
            Feedback("The cot looks wrong now.\nYou don't want to sleep again.", new Color(0.70f,0.50f,0.30f));
            return;
        }
        var ps = PlayerStats.Instance;
        if (ps == null) return;
        if (ps.hp >= ps.maxHp)
        {
            Feedback("You're not injured.\nRest feels wrong anyway.", new Color(0.60f,0.60f,0.40f));
            return;
        }
        _hasRested = true;
        ps.Heal(25);
        if (_restBtnLabel != null)
        {
            _restBtnLabel.text  = "[ RESTED ]";
            _restBtnLabel.color = new Color(0.38f,0.48f,0.38f,0.65f);
        }
        Feedback(RestMessages[Random.Range(0, RestMessages.Length)], new Color(0.65f,0.78f,0.65f));
        StartCoroutine(ZzzEffect());
    }

    void OnTradeClick()  { UpdateScrap(); _tradePanel.SetActive(true); StartCoroutine(OpenLid()); }
    void OnCloseTrade()  { _tradePanel.SetActive(false); StartCoroutine(CloseLid()); }
    void OnLampClick()   => StartCoroutine(LampBurst());
    void OnDoorClick()   => StartCoroutine(DoorLeave());

    void OnBuy(string id, int cost, int qty)
    {
        var inv = Inventory.Instance;
        if (inv == null) return;
        if (!inv.Has("scrap", cost))        { Feedback("Not enough scrap.",   new Color(0.82f,0.36f,0.30f)); return; }
        if (inv.IsFull() && !inv.Has(id,1)) { Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return; }
        inv.Remove("scrap", cost);
        if (!inv.Add(id, qty)) { inv.Add("scrap", cost); Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return; }
        UpdateScrap();
        Feedback("+" + (qty > 1 ? qty + "x " : "") + ItemName(id), new Color(0.60f,0.88f,0.60f));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator LampFlicker()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(Random.Range(0.9f, 4.0f));
            int n = Random.Range(1, 4);
            for (int i = 0; i < n; i++)
            {
                GlowMult(Random.Range(0.18f, 0.55f));
                yield return new WaitForSeconds(Random.Range(0.04f, 0.11f));
                GlowMult(1f);
                yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            }
        }
    }

    IEnumerator LampBurst()
    {
        for (int i = 0; i < 7; i++)
        {
            GlowMult(Random.Range(0.08f, 0.40f));
            yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
            GlowMult(1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        }
        Feedback("The lamp stutters.\nSomething outside noticed.", new Color(0.72f,0.62f,0.36f));
    }

    IEnumerator AmbientPulse()
    {
        float t = 0f;
        while (isActiveAndEnabled)
        {
            t += Time.deltaTime;
            GlowAlpha(_lampGlow1, 0.048f * (0.93f + Mathf.Sin(t * 0.38f) * 0.07f));
            yield return null;
        }
    }

    IEnumerator WindowShake()
    {
        if (_windowRT == null) yield break;
        Vector2 origin = _windowRT.anchoredPosition;
        float[] offsets = { 8f, -7f, 6f, -5f, 3f, -2f, 0f };
        foreach (float x in offsets)
        {
            _windowRT.anchoredPosition = origin + new Vector2(x, 0);
            yield return new WaitForSeconds(0.07f);
        }
        _windowRT.anchoredPosition = origin;
    }

    IEnumerator DoorLeave()
    {
        if (_doorRT != null)
        {
            Vector2 origin = _doorRT.anchoredPosition;
            float[] offsets = { -5f, 5f, -4f, 3f, 0f };
            foreach (float x in offsets)
            {
                _doorRT.anchoredPosition = origin + new Vector2(x, 0);
                yield return new WaitForSeconds(0.06f);
            }
            _doorRT.anchoredPosition = origin;
        }
        Feedback("You brace the door open...", new Color(0.60f,0.55f,0.38f));
        yield return new WaitForSeconds(0.65f);
        Close();
    }

    IEnumerator ZzzEffect()
    {
        var zGO  = NewGO("ZZZ", transform);
        var zTxt = zGO.AddComponent<Text>();
        zTxt.text = "z  z  z"; zTxt.font = F(); zTxt.fontSize = 21;
        zTxt.color = new Color(0.60f,0.72f,0.60f); zTxt.alignment = TextAnchor.MiddleCenter;
        var zRT = zGO.GetComponent<RectTransform>();
        zRT.anchorMin = zRT.anchorMax = new Vector2(0.24f, 0.32f);
        zRT.pivot = new Vector2(0.5f, 0f); zRT.anchoredPosition = new Vector2(0, 60f);
        zRT.sizeDelta = new Vector2(130, 28f);
        float e = 0f;
        while (e < 1.6f)
        {
            e += Time.deltaTime; float p = e / 1.6f;
            zRT.anchoredPosition = new Vector2(0, 60f + p * 65f);
            zTxt.color = new Color(0.60f, 0.72f, 0.60f, 1f - p);
            yield return null;
        }
        Destroy(zGO);
    }

    IEnumerator OpenLid()
    {
        if (_crateOpen || _crateLid == null) yield break;
        _crateOpen = true;
        float t = 0f;
        while (t < 0.35f) { t += Time.deltaTime; _crateLid.localEulerAngles = new Vector3(0,0,Mathf.Lerp(0,-65f,t/0.35f)); yield return null; }
        _crateLid.localEulerAngles = new Vector3(0,0,-65f);
    }

    IEnumerator CloseLid()
    {
        if (!_crateOpen || _crateLid == null) yield break;
        _crateOpen = false;
        float t = 0f;
        while (t < 0.22f) { t += Time.deltaTime; _crateLid.localEulerAngles = new Vector3(0,0,Mathf.Lerp(-65f,0,t/0.22f)); yield return null; }
        _crateLid.localEulerAngles = Vector3.zero;
    }

    IEnumerator Typewriter(Text target, string msg, float delay)
    {
        target.text = "";
        foreach (char c in msg) { target.text += c; yield return new WaitForSeconds(delay); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void Feedback(string msg, Color col)
    {
        if (_feedbackCo != null) StopCoroutine(_feedbackCo);
        _feedbackCo = StartCoroutine(FeedbackFade(msg, col));
    }

    IEnumerator FeedbackFade(string msg, Color col)
    {
        _feedbackText.text = msg; _feedbackText.color = col;
        yield return new WaitForSeconds(2.8f);
        float t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; _feedbackText.color = new Color(col.r,col.g,col.b,1f-t/0.5f); yield return null; }
        _feedbackText.color = new Color(col.r,col.g,col.b,0);
    }

    void GlowMult(float m)
    {
        GlowAlpha(_lampGlow1, 0.048f * m);
        GlowAlpha(_lampGlow2, 0.092f * m);
        GlowAlpha(_lampGlow3, 0.210f * m);
    }

    void GlowAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var img = go.GetComponent<Image>();
        if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, a);
    }

    void UpdateScrap()
    {
        if (_scrapDisplay == null) return;
        _scrapDisplay.text = "SCRAP:  " + (Inventory.Instance != null ? Inventory.Instance.Count("scrap") : 0);
    }

    string ItemName(string id) => id switch {
        "pills"  => "Pills",
        "food"   => "Food",
        "meds"   => "Medical Supplies",
        "ammo"   => "Ammo",
        "medkit" => "Medkit",
        _        => id,
    };

    // ── Static UI Utilities ───────────────────────────────────────────────────

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void StaticBg(string name, Transform parent, Color col, Vector2 ancMin, Vector2 ancMax)
    {
        var go  = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = col; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject GlowCircle(string name, Transform parent, Vector2 anchor, float size, Color col)
    {
        var go  = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = col; img.sprite = CircleSprite(128); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void PadStretch(RectTransform rt, float px, float py = -1f)
    {
        float fy = py < 0 ? px : py;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(px, fy); rt.offsetMax = new Vector2(-px, -fy);
    }

    static void AnchorsFull(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Divider(Transform parent, string name, bool fromBottom, Vector2 pos)
    {
        var go  = NewGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.30f,0.22f,0.10f,0.55f); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        float ay = fromBottom ? 0 : 1;
        rt.anchorMin = new Vector2(0.04f,ay); rt.anchorMax = new Vector2(0.96f,ay);
        rt.pivot = new Vector2(0.5f,ay); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(0,1);
    }

    static void PanelText(Transform parent, string name, string text, int size, FontStyle style,
        Color col, Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 pos, Vector2 sd)
    {
        var go  = NewGO(name, parent);
        var txt = go.AddComponent<Text>();
        txt.text = text; txt.font = F(); txt.fontSize = size;
        txt.fontStyle = style; txt.color = col; txt.alignment = TextAnchor.MiddleCenter;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = sd;
    }

    static Sprite CircleSprite(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz * sz];
        float r = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x - r + 0.5f, dy = y - r + 0.5f;
            px[y*sz+x] = new Color(1,1,1, Mathf.Clamp01((r - Mathf.Sqrt(dx*dx+dy*dy)) / (r*0.14f)));
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
    }

    static Sprite VignetteSprite(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz * sz];
        float r = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = (x-r)/r, dy = (y-r)/r;
            float a = Mathf.Clamp01((Mathf.Sqrt(dx*dx+dy*dy) - 0.38f) / 0.62f) * 0.88f;
            px[y*sz+x] = new Color(0,0,0,a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
    }

    static Font F() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
