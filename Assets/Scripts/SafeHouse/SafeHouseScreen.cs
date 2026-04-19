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

    // Room container — all room visuals live here; pivoted at door for zoom exit
    Transform     _roomContainer;
    GameObject    _lampGlow1, _lampGlow2, _lampGlow3;
    RectTransform _windowRT;
    RectTransform _doorRT;
    RectTransform _crateLid;

    GameObject _tradePanel;
    GameObject _craftPanel;

    Text _wallMsgText;
    Text _feedbackText;
    Text _restBtnLabel;
    Text _scrapDisplay;
    Text _craftResourcesText;

    RawImage _playerRaw;

    // Craft detail pane
    CanvasGroup   _detailCG;
    Text          _detailTitle;
    Image[]       _detailIngBgs   = new Image[2];
    Image[]       _detailIngChips = new Image[2];
    Text[]        _detailIngNames = new Text[2];
    Text[]        _detailIngHave  = new Text[2];
    Text          _detailMissing;
    Image         _detailResultBg;
    Image         _detailResultChip;
    Text          _detailResultName;
    Text          _detailResultDesc;
    Button        _detailCraftBtn;
    Image         _detailCraftBtnImg;
    Text          _detailCraftBtnLbl;
    RectTransform _detailResultRT;
    GameObject    _detailSelectHint;

    bool      _hasRested;
    bool      _crateOpen;
    int       _selectedRecipe = -1;
    Coroutine _feedbackCo;
    Coroutine _craftBtnPulseCo;
    Coroutine _detailFadeCo;

    class CraftRowUI { public Image rowBg; public Image indicator; public Text nameText; }
    CraftRowUI[] _craftRows;

    struct Ingredient { public string id; public int qty; }
    struct Recipe {
        public string       name;
        public Ingredient[] ingredients;
        public string       resultId;
        public int          resultQty;
    }

    static readonly Recipe[] Recipes = {
        new Recipe { name="Field Medkit",
            ingredients=new Ingredient[]{new Ingredient{id="meds",   qty=2},new Ingredient{id="scrap",  qty=1}},
            resultId="medkit",    resultQty=1 },
        new Recipe { name="Salvage Meds",
            ingredients=new Ingredient[]{new Ingredient{id="pills",  qty=1},new Ingredient{id="food",   qty=1}},
            resultId="meds",      resultQty=2 },
        new Recipe { name="Crude Rounds",
            ingredients=new Ingredient[]{new Ingredient{id="scrap",  qty=3}},
            resultId="ammo",      resultQty=4 },
        new Recipe { name="Hot Load",
            ingredients=new Ingredient[]{new Ingredient{id="scrap",  qty=2},new Ingredient{id="battery",qty=1}},
            resultId="ammo",      resultQty=7 },
        new Recipe { name="Jury Pick",
            ingredients=new Ingredient[]{new Ingredient{id="scrap",  qty=3},new Ingredient{id="battery",qty=1}},
            resultId="lockpick",  resultQty=1 },
        new Recipe { name="Flashlight Kit",
            ingredients=new Ingredient[]{new Ingredient{id="battery",qty=1},new Ingredient{id="scrap",  qty=4}},
            resultId="flashlight",resultQty=1 },
        new Recipe { name="Scrap Vest",
            ingredients=new Ingredient[]{new Ingredient{id="scrap",  qty=5}},
            resultId="vest",      resultQty=1 },
    };

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
        "IF YOU CAN READ THIS\nYOU'RE DOING BETTER THAN MOST.",
        "DON'T EAT THE RED ONE.",
        "RULE 1: DON'T DIE.\nRULE 2: SEE RULE 1.",
        "THIS WAS SOMEONE'S HOME.",
        "I STOPPED COUNTING DAYS.\nIT DIDN'T HELP.",
        "THE SCRATCHING GETS LOUDER\nAFTER MIDNIGHT.",
        "I LEFT SOMETHING\nUNDER THE FLOORBOARDS.",
        "THEY USED TO BE PEOPLE.",
        "WHOEVER TOOK MY MEDKIT:\nI HOPE IT WAS WORTH IT.",
        "MY SCRAP FOR YOURS.\n- DAVE.  DAVE IS GONE.",
        "THE COT SMELLS LIKE\nFINAL DECISIONS.",
        "THERE'S A SECOND SAFE HOUSE.\nI FORGET WHERE.",
        "CONSERVE AMMO.\nAIM FOR THE HEAD.",
        "IT'S NOT OVER.\nIT'S NEVER OVER.",
        "STILL HERE.\nSTILL BREATHING.\nSOMEHOW.",
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
        _hasRested      = false;
        _crateOpen      = false;
        _selectedRecipe = -1;
        if (_roomContainer != null) _roomContainer.localScale = Vector3.one;
        if (_restBtnLabel  != null) { _restBtnLabel.text = "[ REST ]"; _restBtnLabel.color = new Color(0.5f,0.8f,0.5f); }
        if (_crateLid      != null) _crateLid.localEulerAngles = Vector3.zero;
        if (_tradePanel    != null) _tradePanel.SetActive(false);
        if (_craftPanel    != null) _craftPanel.SetActive(false);
        if (_feedbackText  != null) _feedbackText.color = Color.clear;
        if (_wallMsgText   != null) _wallMsgText.text   = "";
        if (_detailCG      != null) _detailCG.alpha     = 0f;
        if (_detailSelectHint != null) _detailSelectHint.SetActive(true);

        _loadingOverlay.SetActive(true);
        _overlayCG.alpha  = 1f;
        _overlayText.text = "";
        _canvas.gameObject.SetActive(true);

        yield return StartCoroutine(EnterTransition());
        StartCoroutine(LampFlicker());
        StartCoroutine(AmbientPulse());
        if (_playerRaw != null) StartCoroutine(PlayerIdleAnim());
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
        while (t < 0.65f) { t += Time.deltaTime; _overlayCG.alpha = 1f - Mathf.Clamp01(t/0.65f); yield return null; }
        _overlayCG.alpha = 0f;
        _loadingOverlay.SetActive(false);
    }

    IEnumerator CloseRoutine()
    {
        _overlayText.text = "";
        _loadingOverlay.SetActive(true);
        _overlayCG.alpha = 0f;
        float t = 0f;
        while (t < 0.40f) { t += Time.deltaTime; _overlayCG.alpha = Mathf.Clamp01(t/0.40f); yield return null; }
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

        // Room container — pivot set at door anchor so ZoomToDoor scales toward it
        var rcGO = new GameObject("RoomContainer");
        rcGO.transform.SetParent(root, false);
        var rcRT = rcGO.AddComponent<RectTransform>();
        rcRT.anchorMin = Vector2.zero; rcRT.anchorMax = Vector2.one;
        rcRT.pivot     = new Vector2(0.09f, 0.10f);   // door position
        rcRT.offsetMin = rcRT.offsetMax = Vector2.zero;
        _roomContainer = rcGO.transform;

        Transform rc = _roomContainer;

        StaticBg("BG",    rc, new Color(0.028f,0.022f,0.018f), Vector2.zero,          Vector2.one);
        StaticBg("Wall",  rc, new Color(0.075f,0.060f,0.048f), new Vector2(0,0.35f),  Vector2.one);
        StaticBg("Floor", rc, new Color(0.058f,0.046f,0.036f), Vector2.zero,          new Vector2(1,0.39f));

        BuildLampGlow(rc, new Vector2(0.70f, 0.54f));
        BuildVignette(rc);
        BuildWallGraffiti(rc);
        BuildWindow(rc);
        BuildTableAndLamp(rc);
        BuildCot(rc);
        BuildPlayerSprite(rc);
        BuildWorkbench(rc);
        BuildCrate(rc);
        BuildDoor(rc);

        // Wall message (inside room container — zooms with room)
        var wmGO = NewGO("WallMsg", rc);
        _wallMsgText = wmGO.AddComponent<Text>();
        _wallMsgText.font = F(); _wallMsgText.fontSize = 22; _wallMsgText.fontStyle = FontStyle.Bold;
        _wallMsgText.color = new Color(0.36f,0.12f,0.12f,0.92f); _wallMsgText.alignment = TextAnchor.UpperLeft;
        AnchorsFull(wmGO.GetComponent<RectTransform>(), new Vector2(0.40f,0.78f), new Vector2(0.84f,0.95f));

        BuildTradePanel(root);
        BuildCraftPanel(root);

        // Feedback — rendered after panels, stays fixed during zoom
        var fbGO = NewGO("Feedback", root);
        _feedbackText = fbGO.AddComponent<Text>();
        _feedbackText.font = F(); _feedbackText.fontSize = 26; _feedbackText.fontStyle = FontStyle.Italic;
        _feedbackText.color = Color.clear; _feedbackText.alignment = TextAnchor.MiddleCenter;
        AnchorsFull(fbGO.GetComponent<RectTransform>(), new Vector2(0.20f,0.43f), new Vector2(0.80f,0.60f));

        var rlGO  = NewGO("RoomLabel", root); var rlTxt = rlGO.AddComponent<Text>();
        rlTxt.text = "SAFE HOUSE"; rlTxt.font = F(); rlTxt.fontSize = 16; rlTxt.fontStyle = FontStyle.Bold;
        rlTxt.color = new Color(0.34f,0.26f,0.15f,0.40f); rlTxt.alignment = TextAnchor.MiddleCenter;
        var rlRT = rlGO.GetComponent<RectTransform>();
        rlRT.anchorMin = Vector2.zero; rlRT.anchorMax = new Vector2(1,0);
        rlRT.pivot = new Vector2(0.5f,0); rlRT.anchoredPosition = new Vector2(0,5); rlRT.sizeDelta = new Vector2(0,26);

        BuildLoadingOverlay(root);
    }

    // ── Room Pieces ───────────────────────────────────────────────────────────

    void BuildLampGlow(Transform rc, Vector2 center)
    {
        _lampGlow1 = GlowCircle("Glow1", rc, center, 700, new Color(0.88f,0.58f,0.12f,0.048f));
        _lampGlow2 = GlowCircle("Glow2", rc, center, 390, new Color(0.94f,0.70f,0.18f,0.092f));
        _lampGlow3 = GlowCircle("Glow3", rc, center, 165, new Color(1.00f,0.84f,0.35f,0.210f));
    }

    void BuildVignette(Transform rc)
    {
        var go  = NewGO("Vignette", rc); var img = go.AddComponent<Image>();
        img.sprite = VignetteSprite(256); img.color = Color.white; img.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
    }

    void BuildWallGraffiti(Transform rc)
    {
        (string txt,float ax,float ay,int sz,float a)[] scrawls = {
            ("DON'T OPEN THE DOOR",   0.52f,0.90f,14,0.22f),
            ("no no no no no no no",  0.62f,0.85f,11,0.16f),
            ("\u2020",                0.38f,0.84f,18,0.14f),
            ("still breathing",       0.44f,0.88f,12,0.18f),
            ("day ???",               0.76f,0.91f,13,0.15f),
        };
        foreach (var s in scrawls)
        {
            var go  = NewGO("Scrawl", rc); var txt = go.AddComponent<Text>();
            txt.text = s.txt; txt.font = F(); txt.fontSize = s.sz;
            txt.color = new Color(0.30f,0.10f,0.10f,s.a); txt.alignment = TextAnchor.MiddleLeft;
            txt.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(s.ax,s.ay);
            rt.pivot = new Vector2(0,0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(260,20);
        }
    }

    void BuildWindow(Transform rc)
    {
        var fGO  = NewGO("Window", rc); var fImg = fGO.AddComponent<Image>();
        fImg.color = new Color(0.09f,0.07f,0.055f); fImg.raycastTarget = false;
        _windowRT = fGO.GetComponent<RectTransform>();
        _windowRT.anchorMin = _windowRT.anchorMax = new Vector2(0.18f,0.70f);
        _windowRT.pivot = new Vector2(0.5f,0.5f); _windowRT.anchoredPosition = Vector2.zero;
        _windowRT.sizeDelta = new Vector2(162,194f);

        var glGO  = NewGO("Glass", fGO.transform); var glImg = glGO.AddComponent<Image>();
        glImg.color = new Color(0.008f,0.006f,0.010f); glImg.raycastTarget = false;
        PadStretch(glGO.GetComponent<RectTransform>(), 10f);

        (float a,float x,float y,float w,float h)[] planks = {
            (-7f,0f,60f,186f,14f),(32f,5f,12f,222f,12f),(-4f,0f,-32f,186f,14f),(-28f,12f,10f,208f,11f),
        };
        foreach (var p in planks)
        {
            var plGO  = NewGO("Plank", fGO.transform); var plImg = plGO.AddComponent<Image>();
            plImg.color = new Color(0.30f,0.19f,0.08f); plImg.raycastTarget = false;
            var pRT = plGO.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f,0.5f); pRT.pivot = new Vector2(0.5f,0.5f);
            pRT.anchoredPosition = new Vector2(p.x,p.y); pRT.sizeDelta = new Vector2(p.w,p.h);
            pRT.localEulerAngles = new Vector3(0,0,p.a);
        }
        var lbGO  = NewGO("ListenLbl", fGO.transform); var lbTxt = lbGO.AddComponent<Text>();
        lbTxt.text = "[ LISTEN ]"; lbTxt.font = F(); lbTxt.fontSize = 14;
        lbTxt.color = new Color(0.38f,0.27f,0.16f,0.75f); lbTxt.alignment = TextAnchor.MiddleCenter;
        var lbRT = lbGO.GetComponent<RectTransform>();
        lbRT.anchorMin = new Vector2(0,0); lbRT.anchorMax = new Vector2(1,0);
        lbRT.pivot = new Vector2(0.5f,1f); lbRT.anchoredPosition = new Vector2(0,-5f); lbRT.sizeDelta = new Vector2(0,20f);

        var wlGO  = NewGO("WindowLabel", rc); var wlTxt = wlGO.AddComponent<Text>();
        wlTxt.text = "WINDOW"; wlTxt.font = F(); wlTxt.fontSize = 13;
        wlTxt.color = new Color(0.34f,0.25f,0.14f,0.52f); wlTxt.alignment = TextAnchor.MiddleCenter;
        var wlRT = wlGO.GetComponent<RectTransform>();
        wlRT.anchorMin = wlRT.anchorMax = new Vector2(0.18f,0.70f);
        wlRT.pivot = new Vector2(0.5f,0f); wlRT.anchoredPosition = new Vector2(0,102f); wlRT.sizeDelta = new Vector2(162,17f);

        var cGO  = NewGO("WindowBtn", rc); var cImg = cGO.AddComponent<Image>();
        cImg.color = Color.clear;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.18f,0.70f);
        cRT.pivot = new Vector2(0.5f,0.5f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = new Vector2(170,228f);
        var cBtn = cGO.AddComponent<Button>(); cBtn.transition = Selectable.Transition.None;
        cBtn.onClick.AddListener(OnWindowClick);
    }

    void BuildTableAndLamp(Transform rc)
    {
        var tGO  = NewGO("Table", rc); var tImg = tGO.AddComponent<Image>();
        tImg.color = new Color(0.19f,0.13f,0.065f); tImg.raycastTarget = false;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = new Vector2(0.70f,0.48f);
        tRT.pivot = new Vector2(0.5f,1f); tRT.anchoredPosition = Vector2.zero; tRT.sizeDelta = new Vector2(238,15f);
        TableLeg("LegL", tGO.transform, 0.04f, 0.10f);
        TableLeg("LegR", tGO.transform, 0.90f, 0.96f);
        var bkGO  = NewGO("Book", tGO.transform); var bkImg = bkGO.AddComponent<Image>();
        bkImg.color = new Color(0.11f,0.08f,0.054f); bkImg.raycastTarget = false;
        var bkRT = bkGO.GetComponent<RectTransform>();
        bkRT.anchorMin = bkRT.anchorMax = new Vector2(0.20f,1f);
        bkRT.pivot = new Vector2(0.5f,0f); bkRT.anchoredPosition = Vector2.zero; bkRT.sizeDelta = new Vector2(44,7f);

        var lbGO  = NewGO("LampBody", rc); var lbImg = lbGO.AddComponent<Image>();
        lbImg.color = new Color(0.19f,0.14f,0.07f); lbImg.raycastTarget = false;
        var lbRT = lbGO.GetComponent<RectTransform>();
        lbRT.anchorMin = lbRT.anchorMax = new Vector2(0.70f,0.48f);
        lbRT.pivot = new Vector2(0.5f,0f); lbRT.anchoredPosition = Vector2.zero; lbRT.sizeDelta = new Vector2(20,44f);
        var shGO  = NewGO("Shade", lbGO.transform); var shImg = shGO.AddComponent<Image>();
        shImg.color = new Color(0.33f,0.21f,0.07f); shImg.raycastTarget = false;
        var shRT = shGO.GetComponent<RectTransform>();
        shRT.anchorMin = shRT.anchorMax = new Vector2(0.5f,1f);
        shRT.pivot = new Vector2(0.5f,0f); shRT.anchoredPosition = Vector2.zero; shRT.sizeDelta = new Vector2(36,17f);
        var flGO  = NewGO("Flame", lbGO.transform); var flImg = flGO.AddComponent<Image>();
        flImg.color = new Color(1f,0.60f,0.12f,0.92f); flImg.sprite = CircleSprite(32); flImg.raycastTarget = false;
        var flRT = flGO.GetComponent<RectTransform>();
        flRT.anchorMin = flRT.anchorMax = new Vector2(0.5f,1f);
        flRT.pivot = new Vector2(0.5f,0f); flRT.anchoredPosition = new Vector2(0,17f); flRT.sizeDelta = new Vector2(12,16f);

        var lcGO  = NewGO("LampClick", rc); var lcImg = lcGO.AddComponent<Image>();
        lcImg.color = Color.clear;
        var lcRT = lcGO.GetComponent<RectTransform>();
        lcRT.anchorMin = lcRT.anchorMax = new Vector2(0.70f,0.48f);
        lcRT.pivot = new Vector2(0.5f,0f); lcRT.anchoredPosition = Vector2.zero; lcRT.sizeDelta = new Vector2(62,88f);
        var lcBtn = lcGO.AddComponent<Button>(); lcBtn.transition = Selectable.Transition.None;
        lcBtn.onClick.AddListener(OnLampClick);
    }

    void TableLeg(string name, Transform parent, float x0, float x1)
    {
        var go  = NewGO(name, parent); var img = go.AddComponent<Image>();
        img.color = new Color(0.14f,0.10f,0.048f); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0,0); rt.anchorMax = new Vector2(x1,0);
        rt.pivot = new Vector2(0.5f,1f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0,90f);
    }

    void BuildCot(Transform rc)
    {
        var cGO  = NewGO("Cot", rc); var cImg = cGO.AddComponent<Image>();
        cImg.color = new Color(0.16f,0.12f,0.08f); cImg.raycastTarget = false;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.24f,0.32f);
        cRT.pivot = new Vector2(0.5f,0.5f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = new Vector2(298,88f);
        var mGO  = NewGO("Mattress", cGO.transform); var mImg = mGO.AddComponent<Image>();
        mImg.color = new Color(0.20f,0.16f,0.12f); mImg.raycastTarget = false;
        PadStretch(mGO.GetComponent<RectTransform>(), 5f, 13f);
        var pGO  = NewGO("Pillow", cGO.transform); var pImg = pGO.AddComponent<Image>();
        pImg.color = new Color(0.26f,0.20f,0.15f); pImg.raycastTarget = false;
        var pRT = pGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.74f,0.16f); pRT.anchorMax = new Vector2(0.93f,0.84f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;

        var lGO  = NewGO("CotLabel", rc); var lTxt = lGO.AddComponent<Text>();
        lTxt.text = "COT"; lTxt.font = F(); lTxt.fontSize = 13;
        lTxt.color = new Color(0.38f,0.28f,0.17f,0.55f); lTxt.alignment = TextAnchor.MiddleCenter;
        var lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = lRT.anchorMax = new Vector2(0.24f,0.32f);
        lRT.pivot = new Vector2(0.5f,0f); lRT.anchoredPosition = new Vector2(0,50f); lRT.sizeDelta = new Vector2(140,17f);

        var rGO  = NewGO("RestBtn", rc); var rImg = rGO.AddComponent<Image>();
        rImg.color = new Color(0.10f,0.17f,0.10f,0.95f);
        var rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = rRT.anchorMax = new Vector2(0.24f,0.32f);
        rRT.pivot = new Vector2(0.5f,0f); rRT.anchoredPosition = new Vector2(0,-62f); rRT.sizeDelta = new Vector2(155,48f);
        var rBtn = rGO.AddComponent<Button>();
        var rc2 = rBtn.colors;
        rc2.normalColor = Color.white; rc2.highlightedColor = new Color(1.25f,1.55f,1.25f,1f);
        rc2.pressedColor = new Color(0.65f,0.85f,0.65f,1f);
        rBtn.colors = rc2; rBtn.targetGraphic = rImg; rBtn.onClick.AddListener(OnRestClick);

        var rlGO  = NewGO("RestMain", rGO.transform);
        _restBtnLabel = rlGO.AddComponent<Text>();
        _restBtnLabel.text = "[ REST ]"; _restBtnLabel.font = F();
        _restBtnLabel.fontSize = 19; _restBtnLabel.fontStyle = FontStyle.Bold;
        _restBtnLabel.color = new Color(0.52f,0.86f,0.52f); _restBtnLabel.alignment = TextAnchor.MiddleCenter;
        var rlRT = rlGO.GetComponent<RectTransform>();
        rlRT.anchorMin = new Vector2(0,0.45f); rlRT.anchorMax = Vector2.one;
        rlRT.offsetMin = rlRT.offsetMax = Vector2.zero;

        var rSubGO  = NewGO("RestSub", rGO.transform); var rSubTxt = rSubGO.AddComponent<Text>();
        rSubTxt.text = "+25 HP  \u00b7  once"; rSubTxt.font = F(); rSubTxt.fontSize = 12;
        rSubTxt.color = new Color(0.42f,0.62f,0.42f,0.80f); rSubTxt.alignment = TextAnchor.MiddleCenter;
        var rSubRT = rSubGO.GetComponent<RectTransform>();
        rSubRT.anchorMin = Vector2.zero; rSubRT.anchorMax = new Vector2(1,0.48f);
        rSubRT.offsetMin = rSubRT.offsetMax = Vector2.zero;
    }

    void BuildPlayerSprite(Transform rc)
    {
        var tex = Resources.Load<Texture2D>("Character/IDLECharacter");
        if (tex == null) return;
        var pGO = NewGO("PlayerSprite", rc);
        _playerRaw = pGO.AddComponent<RawImage>();
        _playerRaw.texture = tex;
        _playerRaw.uvRect  = new Rect(0f, 6f/7f, 1f/8f, 1f/7f);
        _playerRaw.color   = new Color(1f,1f,1f,0.85f); _playerRaw.raycastTarget = false;
        var pRT = pGO.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = new Vector2(0.285f, 0.350f);
        pRT.pivot = new Vector2(0.5f,0f); pRT.anchoredPosition = Vector2.zero;
        pRT.sizeDelta = new Vector2(110f, 165f);
    }

    void BuildWorkbench(Transform rc)
    {
        var wGO  = NewGO("Workbench", rc); var wImg = wGO.AddComponent<Image>();
        wImg.color = new Color(0.10f,0.12f,0.17f); wImg.raycastTarget = false;
        var wRT = wGO.GetComponent<RectTransform>();
        wRT.anchorMin = wRT.anchorMax = new Vector2(0.50f,0.27f);
        wRT.pivot = new Vector2(0.5f,1f); wRT.anchoredPosition = Vector2.zero; wRT.sizeDelta = new Vector2(220,16f);
        WorkbenchLeg("WBLegL", wGO.transform, 0.05f, 0.12f);
        WorkbenchLeg("WBLegR", wGO.transform, 0.84f, 0.95f);
        float[] toolX = {0.22f,0.46f,0.70f};
        foreach (float tx in toolX)
        {
            var dn  = NewGO("Mark", wGO.transform); var dnI = dn.AddComponent<Image>();
            dnI.color = new Color(0.18f,0.22f,0.32f,0.80f); dnI.raycastTarget = false;
            var dRT = dn.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(tx,0.15f); dRT.anchorMax = new Vector2(tx+0.05f,0.85f);
            dRT.offsetMin = dRT.offsetMax = Vector2.zero;
        }
        var wlGO  = NewGO("WBLabel", rc); var wlTxt = wlGO.AddComponent<Text>();
        wlTxt.text = "WORKBENCH"; wlTxt.font = F(); wlTxt.fontSize = 13;
        wlTxt.color = new Color(0.30f,0.42f,0.60f,0.60f); wlTxt.alignment = TextAnchor.MiddleCenter;
        var wlRT = wlGO.GetComponent<RectTransform>();
        wlRT.anchorMin = wlRT.anchorMax = new Vector2(0.50f,0.27f);
        wlRT.pivot = new Vector2(0.5f,0f); wlRT.anchoredPosition = new Vector2(0,17f); wlRT.sizeDelta = new Vector2(200,17f);

        var cbGO  = NewGO("CraftBtn", rc); var cbImg = cbGO.AddComponent<Image>();
        cbImg.color = new Color(0.07f,0.12f,0.20f,0.95f);
        var cbRT = cbGO.GetComponent<RectTransform>();
        cbRT.anchorMin = cbRT.anchorMax = new Vector2(0.50f,0.27f);
        cbRT.pivot = new Vector2(0.5f,0f); cbRT.anchoredPosition = new Vector2(0,-46f); cbRT.sizeDelta = new Vector2(155,48f);
        var cbBtn = cbGO.AddComponent<Button>();
        var cc = cbBtn.colors;
        cc.normalColor = Color.white; cc.highlightedColor = new Color(1.3f,1.5f,1.8f,1f);
        cc.pressedColor = new Color(0.65f,0.75f,0.90f,1f);
        cbBtn.colors = cc; cbBtn.targetGraphic = cbImg; cbBtn.onClick.AddListener(OnCraftClick);

        var clGO  = NewGO("CraftMain", cbGO.transform); var clTxt = clGO.AddComponent<Text>();
        clTxt.text = "[ CRAFT ]"; clTxt.font = F(); clTxt.fontSize = 19; clTxt.fontStyle = FontStyle.Bold;
        clTxt.color = new Color(0.40f,0.72f,1.00f); clTxt.alignment = TextAnchor.MiddleCenter;
        var clRT = clGO.GetComponent<RectTransform>();
        clRT.anchorMin = new Vector2(0,0.44f); clRT.anchorMax = Vector2.one;
        clRT.offsetMin = clRT.offsetMax = Vector2.zero;
        var cSubGO  = NewGO("CraftSub", cbGO.transform); var cSubTxt = cSubGO.AddComponent<Text>();
        cSubTxt.text = "combine materials"; cSubTxt.font = F(); cSubTxt.fontSize = 12;
        cSubTxt.color = new Color(0.32f,0.54f,0.76f,0.80f); cSubTxt.alignment = TextAnchor.MiddleCenter;
        var cSubRT = cSubGO.GetComponent<RectTransform>();
        cSubRT.anchorMin = Vector2.zero; cSubRT.anchorMax = new Vector2(1,0.47f);
        cSubRT.offsetMin = cSubRT.offsetMax = Vector2.zero;
    }

    void WorkbenchLeg(string name, Transform parent, float x0, float x1)
    {
        var go  = NewGO(name, parent); var img = go.AddComponent<Image>();
        img.color = new Color(0.07f,0.08f,0.11f); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0,0); rt.anchorMax = new Vector2(x1,0);
        rt.pivot = new Vector2(0.5f,1f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0,82f);
    }

    void BuildCrate(Transform rc)
    {
        var cGO  = NewGO("Crate", rc); var cImg = cGO.AddComponent<Image>();
        cImg.color = new Color(0.20f,0.14f,0.07f); cImg.raycastTarget = false;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = cRT.anchorMax = new Vector2(0.76f,0.26f);
        cRT.pivot = new Vector2(0.5f,0f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = new Vector2(188,148f);
        for (int i = 0; i < 3; i++)
        {
            float yf = 0.24f + i*0.26f;
            var ln  = NewGO("Ln"+i, cGO.transform); var lnI = ln.AddComponent<Image>();
            lnI.color = new Color(0.13f,0.09f,0.04f,0.55f); lnI.raycastTarget = false;
            var lRT = ln.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.05f,yf); lRT.anchorMax = new Vector2(0.95f,yf);
            lRT.pivot = new Vector2(0.5f,0.5f); lRT.anchoredPosition = Vector2.zero; lRT.sizeDelta = new Vector2(0,2f);
        }
        var lkGO  = NewGO("Lock", cGO.transform); var lkImg = lkGO.AddComponent<Image>();
        lkImg.color = new Color(0.52f,0.42f,0.14f); lkImg.raycastTarget = false;
        var lkRT = lkGO.GetComponent<RectTransform>();
        lkRT.anchorMin = lkRT.anchorMax = new Vector2(0.5f,0.78f);
        lkRT.pivot = new Vector2(0.5f,0.5f); lkRT.anchoredPosition = Vector2.zero; lkRT.sizeDelta = new Vector2(17,21f);

        var lidGO  = NewGO("Lid", rc); var lidImg = lidGO.AddComponent<Image>();
        lidImg.color = new Color(0.26f,0.18f,0.09f); lidImg.raycastTarget = false;
        _crateLid = lidGO.GetComponent<RectTransform>();
        _crateLid.anchorMin = _crateLid.anchorMax = new Vector2(0.76f,0.26f);
        _crateLid.pivot = new Vector2(0.5f,0f); _crateLid.anchoredPosition = new Vector2(0,148f);
        _crateLid.sizeDelta = new Vector2(188,30f);

        var slGO  = NewGO("CrateLabel", rc); var slTxt = slGO.AddComponent<Text>();
        slTxt.text = "SUPPLY CRATE"; slTxt.font = F(); slTxt.fontSize = 13;
        slTxt.color = new Color(0.43f,0.31f,0.15f,0.58f); slTxt.alignment = TextAnchor.MiddleCenter;
        var slRT = slGO.GetComponent<RectTransform>();
        slRT.anchorMin = slRT.anchorMax = new Vector2(0.76f,0.26f);
        slRT.pivot = new Vector2(0.5f,0f); slRT.anchoredPosition = new Vector2(0,183f); slRT.sizeDelta = new Vector2(188,17f);

        var tbGO  = NewGO("TradeBtn", rc); var tbImg = tbGO.AddComponent<Image>();
        tbImg.color = new Color(0.18f,0.13f,0.06f,0.95f);
        var tbRT = tbGO.GetComponent<RectTransform>();
        tbRT.anchorMin = tbRT.anchorMax = new Vector2(0.76f,0.26f);
        tbRT.pivot = new Vector2(0.5f,0f); tbRT.anchoredPosition = new Vector2(0,-62f); tbRT.sizeDelta = new Vector2(155,48f);
        var tbBtn = tbGO.AddComponent<Button>();
        var tc = tbBtn.colors;
        tc.normalColor = Color.white; tc.highlightedColor = new Color(1.45f,1.20f,0.80f,1f);
        tc.pressedColor = new Color(0.75f,0.58f,0.30f,1f);
        tbBtn.colors = tc; tbBtn.targetGraphic = tbImg; tbBtn.onClick.AddListener(OnTradeClick);

        var tlGO  = NewGO("TradeMain", tbGO.transform); var tlTxt = tlGO.AddComponent<Text>();
        tlTxt.text = "[ TRADE ]"; tlTxt.font = F(); tlTxt.fontSize = 19; tlTxt.fontStyle = FontStyle.Bold;
        tlTxt.color = new Color(0.90f,0.70f,0.26f); tlTxt.alignment = TextAnchor.MiddleCenter;
        var tlRT = tlGO.GetComponent<RectTransform>();
        tlRT.anchorMin = new Vector2(0,0.44f); tlRT.anchorMax = Vector2.one;
        tlRT.offsetMin = tlRT.offsetMax = Vector2.zero;
        var tSubGO  = NewGO("TradeSub", tbGO.transform); var tSubTxt = tSubGO.AddComponent<Text>();
        tSubTxt.text = "spend scrap"; tSubTxt.font = F(); tSubTxt.fontSize = 12;
        tSubTxt.color = new Color(0.65f,0.50f,0.20f,0.80f); tSubTxt.alignment = TextAnchor.MiddleCenter;
        var tSubRT = tSubGO.GetComponent<RectTransform>();
        tSubRT.anchorMin = Vector2.zero; tSubRT.anchorMax = new Vector2(1,0.47f);
        tSubRT.offsetMin = tSubRT.offsetMax = Vector2.zero;
    }

    void BuildDoor(Transform rc)
    {
        var dfGO  = NewGO("DoorFrame", rc); var dfImg = dfGO.AddComponent<Image>();
        dfImg.color = new Color(0.11f,0.08f,0.055f);
        _doorRT = dfGO.GetComponent<RectTransform>();
        _doorRT.anchorMin = _doorRT.anchorMax = new Vector2(0.09f,0.10f);
        _doorRT.pivot = new Vector2(0.5f,0f); _doorRT.anchoredPosition = Vector2.zero; _doorRT.sizeDelta = new Vector2(120,180f);

        var dpGO  = NewGO("DoorPanel", dfGO.transform); var dpImg = dpGO.AddComponent<Image>();
        dpImg.color = new Color(0.15f,0.11f,0.07f); dpImg.raycastTarget = false;
        PadStretch(dpGO.GetComponent<RectTransform>(), 8f, 6f);
        var diGO  = NewGO("Inset", dpGO.transform); var diImg = diGO.AddComponent<Image>();
        diImg.color = new Color(0.12f,0.09f,0.055f); diImg.raycastTarget = false;
        PadStretch(diGO.GetComponent<RectTransform>(), 10f, 12f);
        var dhGO  = NewGO("Handle", dfGO.transform); var dhImg = dhGO.AddComponent<Image>();
        dhImg.color = new Color(0.48f,0.37f,0.12f); dhImg.raycastTarget = false;
        var dhRT = dhGO.GetComponent<RectTransform>();
        dhRT.anchorMin = dhRT.anchorMax = new Vector2(0.78f,0.42f);
        dhRT.pivot = new Vector2(0.5f,0.5f); dhRT.anchoredPosition = Vector2.zero; dhRT.sizeDelta = new Vector2(10,10f);

        var llGO  = NewGO("LeaveLabel", rc); var llTxt = llGO.AddComponent<Text>();
        llTxt.text = "[ LEAVE ]"; llTxt.font = F(); llTxt.fontSize = 15;
        llTxt.color = new Color(0.48f,0.36f,0.20f,0.78f); llTxt.alignment = TextAnchor.MiddleCenter;
        var llRT = llGO.GetComponent<RectTransform>();
        llRT.anchorMin = llRT.anchorMax = new Vector2(0.09f,0.10f);
        llRT.pivot = new Vector2(0.5f,0f); llRT.anchoredPosition = new Vector2(0,185f); llRT.sizeDelta = new Vector2(120,19f);

        var dBtn = dfGO.AddComponent<Button>();
        var dc = dBtn.colors;
        dc.normalColor = Color.white; dc.highlightedColor = new Color(1.35f,1.10f,0.65f,1f);
        dc.pressedColor = new Color(0.72f,0.54f,0.28f,1f);
        dBtn.colors = dc; dBtn.targetGraphic = dfImg; dBtn.onClick.AddListener(OnDoorClick);
    }

    // ── Trade Panel ───────────────────────────────────────────────────────────

    void BuildTradePanel(Transform root)
    {
        _tradePanel = NewGO("TradePanel", root); var bg = _tradePanel.AddComponent<Image>();
        bg.color = new Color(0.050f,0.040f,0.030f,0.97f);
        var panelRT = _tradePanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.18f,0.07f); panelRT.anchorMax = new Vector2(0.82f,0.91f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        Transform pt = _tradePanel.transform;

        PanelText(pt,"Title","SUPPLY CRATE",30,FontStyle.Bold,new Color(0.92f,0.72f,0.28f),
            new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-14),new Vector2(0,42));
        PanelText(pt,"Sub","trade scrap for supplies",17,FontStyle.Italic,new Color(0.50f,0.40f,0.28f),
            new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-58),new Vector2(0,26));

        var scGO = NewGO("ScrapCount", pt); _scrapDisplay = scGO.AddComponent<Text>();
        _scrapDisplay.font = F(); _scrapDisplay.fontSize = 21;
        _scrapDisplay.color = new Color(0.60f,0.82f,0.58f); _scrapDisplay.alignment = TextAnchor.MiddleRight;
        var scRT = scGO.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0.55f,1); scRT.anchorMax = new Vector2(0.97f,1);
        scRT.pivot = new Vector2(1,1); scRT.anchoredPosition = new Vector2(0,-14); scRT.sizeDelta = new Vector2(0,28);

        Divider(pt,"Div1",false,new Vector2(0,-90));

        // Rebalanced prices: Food cheapest (fewest HP), Medkit most expensive
        (string id,string label,int cost,Color col)[] items = {
            ("food",   "Food               +10 HP", 1, new Color(0.92f,0.77f,0.52f)),
            ("pills",  "Pills              +15 HP", 2, new Color(0.72f,0.92f,0.72f)),
            ("meds",   "Medical Supplies   +20 HP", 3, new Color(0.72f,0.86f,1.00f)),
            ("ammo",   "Ammo  \u00d73",             2, new Color(1.00f,0.82f,0.32f)),
            ("medkit", "Medkit             +30 HP", 5, new Color(0.92f,0.42f,0.42f)),
        };
        for (int i = 0; i < items.Length; i++)
            TradeRow(pt,items[i].id,items[i].label,items[i].cost,items[i].col,-100f-i*64f);
        Divider(pt,"Div2",true,new Vector2(0,50));
        SimpleCloseBtn(pt,"CloseBtn",OnCloseTrade,new Color(0.20f,0.14f,0.07f),new Color(0.72f,0.55f,0.30f));
    }

    // ── Craft Panel (black/white, list + detail) ──────────────────────────────

    void BuildCraftPanel(Transform root)
    {
        _craftPanel = NewGO("CraftPanel", root); var bg = _craftPanel.AddComponent<Image>();
        bg.color = new Color(0.04f,0.04f,0.04f,0.97f);  // near-black
        var panelRT = _craftPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.08f,0.04f); panelRT.anchorMax = new Vector2(0.92f,0.94f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
        Transform pt = _craftPanel.transform;

        PanelText(pt,"Title","WORKBENCH",30,FontStyle.Bold,new Color(0.92f,0.92f,0.92f),
            new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1),new Vector2(0,-12),new Vector2(0,42));

        var rdGO = NewGO("CraftRes", pt); _craftResourcesText = rdGO.AddComponent<Text>();
        _craftResourcesText.font = F(); _craftResourcesText.fontSize = 18;
        _craftResourcesText.color = new Color(0.52f,0.52f,0.52f); _craftResourcesText.alignment = TextAnchor.MiddleCenter;
        var rdRT = rdGO.GetComponent<RectTransform>();
        rdRT.anchorMin = new Vector2(0,1); rdRT.anchorMax = new Vector2(1,1);
        rdRT.pivot = new Vector2(0.5f,1); rdRT.anchoredPosition = new Vector2(0,-56); rdRT.sizeDelta = new Vector2(0,26);

        Divider(pt,"CDiv1",false,new Vector2(0,-86));

        var vdGO  = NewGO("PaneDivider", pt); var vdImg = vdGO.AddComponent<Image>();
        vdImg.color = new Color(0.22f,0.22f,0.22f,0.60f); vdImg.raycastTarget = false;
        var vdRT = vdGO.GetComponent<RectTransform>();
        vdRT.anchorMin = new Vector2(0.46f,0); vdRT.anchorMax = new Vector2(0.46f,1);
        vdRT.pivot = new Vector2(0.5f,0.5f); vdRT.offsetMin = new Vector2(0,52); vdRT.offsetMax = new Vector2(1,-46);

        var lpLblGO  = NewGO("RecipesLbl", pt); var lpLbl = lpLblGO.AddComponent<Text>();
        lpLbl.text = "RECIPES"; lpLbl.font = F(); lpLbl.fontSize = 14; lpLbl.fontStyle = FontStyle.Bold;
        lpLbl.color = new Color(0.40f,0.40f,0.40f,0.90f); lpLbl.alignment = TextAnchor.MiddleLeft;
        var lpLblRT = lpLblGO.GetComponent<RectTransform>();
        lpLblRT.anchorMin = new Vector2(0.01f,1); lpLblRT.anchorMax = new Vector2(0.45f,1);
        lpLblRT.pivot = new Vector2(0,1); lpLblRT.anchoredPosition = new Vector2(10,-90); lpLblRT.sizeDelta = new Vector2(0,20);

        _craftRows = new CraftRowUI[Recipes.Length];
        for (int i = 0; i < Recipes.Length; i++)
            _craftRows[i] = BuildCraftListRow(pt, i, Recipes[i], -114f - i * 52f);

        BuildCraftDetailPane(pt);
        Divider(pt,"CDiv2",true,new Vector2(0,50));
        SimpleCloseBtn(pt,"CraftCloseBtn",OnCloseCraft,new Color(0.08f,0.08f,0.08f),new Color(0.55f,0.55f,0.55f));
        _craftPanel.SetActive(false);
    }

    CraftRowUI BuildCraftListRow(Transform parent, int index, Recipe recipe, float yOff)
    {
        var row = new CraftRowUI();
        var rGO  = NewGO("CRow_"+index, parent); var rImg = rGO.AddComponent<Image>();
        rImg.color = new Color(0.07f,0.07f,0.07f,0.55f);
        var rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.01f,1); rRT.anchorMax = new Vector2(0.44f,1);
        rRT.pivot = new Vector2(0f,1); rRT.anchoredPosition = new Vector2(0,yOff); rRT.sizeDelta = new Vector2(0,50);
        row.rowBg = rImg;

        var dotGO  = NewGO("Dot", rGO.transform); var dotImg = dotGO.AddComponent<Image>();
        dotImg.sprite = CircleSprite(16); dotImg.color = new Color(0.25f,0.25f,0.25f,0.55f);
        var dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0,0.5f); dotRT.anchorMax = new Vector2(0,0.5f);
        dotRT.pivot = new Vector2(0,0.5f); dotRT.anchoredPosition = new Vector2(10,0); dotRT.sizeDelta = new Vector2(10,10);
        row.indicator = dotImg;

        var nGO  = NewGO("Name", rGO.transform); var nTxt = nGO.AddComponent<Text>();
        nTxt.text = recipe.name; nTxt.font = F(); nTxt.fontSize = 16;
        nTxt.color = new Color(0.38f,0.38f,0.38f,0.70f); nTxt.alignment = TextAnchor.MiddleLeft;
        var nRT = nGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0,0); nRT.anchorMax = Vector2.one;
        nRT.pivot = new Vector2(0,0.5f); nRT.anchoredPosition = new Vector2(26,0); nRT.sizeDelta = new Vector2(-30,0);
        row.nameText = nTxt;

        var bGO  = NewGO("RowBtn", rGO.transform); var bImg = bGO.AddComponent<Image>();
        bImg.color = Color.clear;
        Stretch(bGO.GetComponent<RectTransform>());
        var btn = bGO.AddComponent<Button>(); btn.transition = Selectable.Transition.None;
        int idx = index; btn.onClick.AddListener(() => SelectRecipe(idx));
        return row;
    }

    void BuildCraftDetailPane(Transform parent)
    {
        var hGO  = NewGO("SelectHint", parent); var hTxt = hGO.AddComponent<Text>();
        hTxt.text = "\u2190  select a recipe"; hTxt.font = F(); hTxt.fontSize = 18; hTxt.fontStyle = FontStyle.Italic;
        hTxt.color = new Color(0.28f,0.28f,0.28f,0.70f); hTxt.alignment = TextAnchor.MiddleCenter;
        AnchorsFull(hGO.GetComponent<RectTransform>(), new Vector2(0.47f,0.15f), new Vector2(0.99f,0.82f));
        _detailSelectHint = hGO;

        var detGO = NewGO("Detail", parent); detGO.AddComponent<Image>().color = Color.clear;
        _detailCG = detGO.AddComponent<CanvasGroup>(); _detailCG.alpha = 0f;
        AnchorsFull(detGO.GetComponent<RectTransform>(), new Vector2(0.47f,0), new Vector2(0.99f,1));
        Transform dp = detGO.transform;

        var dtGO  = NewGO("DTitle", dp); _detailTitle = dtGO.AddComponent<Text>();
        _detailTitle.font = F(); _detailTitle.fontSize = 22; _detailTitle.fontStyle = FontStyle.Bold;
        _detailTitle.color = new Color(1f,1f,1f); _detailTitle.alignment = TextAnchor.MiddleLeft;
        var dtRT = dtGO.GetComponent<RectTransform>();
        dtRT.anchorMin = new Vector2(0,1); dtRT.anchorMax = new Vector2(1,1);
        dtRT.pivot = new Vector2(0,1); dtRT.anchoredPosition = new Vector2(8,-92); dtRT.sizeDelta = new Vector2(-16,34);

        var tdGO  = NewGO("TDiv", dp); var tdImg = tdGO.AddComponent<Image>();
        tdImg.color = new Color(0.24f,0.24f,0.24f,0.60f); tdImg.raycastTarget = false;
        var tdRT = tdGO.GetComponent<RectTransform>();
        tdRT.anchorMin = new Vector2(0,1); tdRT.anchorMax = new Vector2(1,1);
        tdRT.pivot = new Vector2(0.5f,1); tdRT.anchoredPosition = new Vector2(0,-130); tdRT.sizeDelta = new Vector2(0,1);

        PanelText(dp,"IngLbl","INGREDIENTS",13,FontStyle.Bold,new Color(0.40f,0.40f,0.40f,0.90f),
            new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),new Vector2(8,-136),new Vector2(-16,18));

        for (int i = 0; i < 2; i++) BuildDetailIngCard(dp, i, -158f - i * 74f);

        var msGO  = NewGO("Missing", dp); _detailMissing = msGO.AddComponent<Text>();
        _detailMissing.font = F(); _detailMissing.fontSize = 14; _detailMissing.fontStyle = FontStyle.Italic;
        _detailMissing.color = new Color(0.88f,0.48f,0.26f,0.90f); _detailMissing.alignment = TextAnchor.MiddleLeft;
        var msRT = msGO.GetComponent<RectTransform>();
        msRT.anchorMin = new Vector2(0,1); msRT.anchorMax = new Vector2(1,1);
        msRT.pivot = new Vector2(0,1); msRT.anchoredPosition = new Vector2(8,-312); msRT.sizeDelta = new Vector2(-16,20);

        PanelText(dp,"ResLbl","\u2192  RESULT",13,FontStyle.Bold,new Color(0.40f,0.40f,0.40f,0.90f),
            new Vector2(0,1),new Vector2(1,1),new Vector2(0,1),new Vector2(8,-336),new Vector2(-16,18));

        var rcGO  = NewGO("ResultCard", dp); var rcImg = rcGO.AddComponent<Image>();
        rcImg.color = new Color(0.10f,0.10f,0.10f,0.90f); rcImg.raycastTarget = false; _detailResultBg = rcImg;
        _detailResultRT = rcGO.GetComponent<RectTransform>();
        _detailResultRT.anchorMin = new Vector2(0.02f,1); _detailResultRT.anchorMax = new Vector2(0.98f,1);
        _detailResultRT.pivot = new Vector2(0,1); _detailResultRT.anchoredPosition = new Vector2(0,-358);
        _detailResultRT.sizeDelta = new Vector2(0,70);
        Transform rp = rcGO.transform;

        var rChipGO  = NewGO("ResChip", rp); _detailResultChip = rChipGO.AddComponent<Image>();
        _detailResultChip.raycastTarget = false;
        var rChipRT = rChipGO.GetComponent<RectTransform>();
        rChipRT.anchorMin = new Vector2(0,0.12f); rChipRT.anchorMax = new Vector2(0,0.88f);
        rChipRT.pivot = new Vector2(0,0.5f); rChipRT.anchoredPosition = new Vector2(10,0); rChipRT.sizeDelta = new Vector2(44,0);

        var rNameGO  = NewGO("ResName", rp); _detailResultName = rNameGO.AddComponent<Text>();
        _detailResultName.font = F(); _detailResultName.fontSize = 18; _detailResultName.fontStyle = FontStyle.Bold;
        _detailResultName.color = new Color(0.94f,0.94f,0.94f); _detailResultName.alignment = TextAnchor.MiddleLeft;
        var rNameRT = rNameGO.GetComponent<RectTransform>();
        rNameRT.anchorMin = new Vector2(0,0.5f); rNameRT.anchorMax = new Vector2(1,1);
        rNameRT.pivot = new Vector2(0,0.5f); rNameRT.anchoredPosition = new Vector2(62,0); rNameRT.sizeDelta = new Vector2(-70,0);

        var rDescGO  = NewGO("ResDesc", rp); _detailResultDesc = rDescGO.AddComponent<Text>();
        _detailResultDesc.font = F(); _detailResultDesc.fontSize = 13; _detailResultDesc.fontStyle = FontStyle.Italic;
        _detailResultDesc.color = new Color(0.50f,0.50f,0.50f,0.85f); _detailResultDesc.alignment = TextAnchor.UpperLeft;
        var rDescRT = rDescGO.GetComponent<RectTransform>();
        rDescRT.anchorMin = Vector2.zero; rDescRT.anchorMax = new Vector2(1,0.52f);
        rDescRT.pivot = new Vector2(0,0); rDescRT.anchoredPosition = new Vector2(62,4); rDescRT.sizeDelta = new Vector2(-70,0);

        // CRAFT button — white when craftable (high contrast)
        var cbGO  = NewGO("CraftBtn", dp); _detailCraftBtnImg = cbGO.AddComponent<Image>();
        _detailCraftBtnImg.color = new Color(0.08f,0.08f,0.08f);
        var cbRT = cbGO.GetComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(0.04f,1); cbRT.anchorMax = new Vector2(0.96f,1);
        cbRT.pivot = new Vector2(0.5f,1); cbRT.anchoredPosition = new Vector2(0,-440); cbRT.sizeDelta = new Vector2(0,52);
        _detailCraftBtn = cbGO.AddComponent<Button>();
        var bc = _detailCraftBtn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(0.80f,0.80f,0.80f,1f);
        bc.pressedColor     = new Color(0.55f,0.55f,0.55f,1f);
        bc.disabledColor    = new Color(0.35f,0.35f,0.35f,0.55f);
        _detailCraftBtn.colors = bc; _detailCraftBtn.targetGraphic = _detailCraftBtnImg;
        _detailCraftBtn.onClick.AddListener(() => OnCraftRecipe(_selectedRecipe));

        var clGO  = NewGO("CraftBtnLbl", cbGO.transform);
        _detailCraftBtnLbl = clGO.AddComponent<Text>();
        _detailCraftBtnLbl.text = "\u25b6  CRAFT"; _detailCraftBtnLbl.font = F();
        _detailCraftBtnLbl.fontSize = 20; _detailCraftBtnLbl.fontStyle = FontStyle.Bold;
        _detailCraftBtnLbl.color = new Color(0.06f,0.06f,0.06f);  // dark text on bright button
        _detailCraftBtnLbl.alignment = TextAnchor.MiddleCenter;
        Stretch(clGO.GetComponent<RectTransform>());
    }

    void BuildDetailIngCard(Transform parent, int index, float yOff)
    {
        var cGO  = NewGO("IngCard"+index, parent); var cImg = cGO.AddComponent<Image>();
        cImg.color = new Color(0.09f,0.09f,0.09f,0.90f); cImg.raycastTarget = false; _detailIngBgs[index] = cImg;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.02f,1); cRT.anchorMax = new Vector2(0.98f,1);
        cRT.pivot = new Vector2(0,1); cRT.anchoredPosition = new Vector2(0,yOff); cRT.sizeDelta = new Vector2(0,64);
        Transform cp = cGO.transform;

        var chipGO  = NewGO("Chip", cp); _detailIngChips[index] = chipGO.AddComponent<Image>();
        _detailIngChips[index].raycastTarget = false;
        var chipRT = chipGO.GetComponent<RectTransform>();
        chipRT.anchorMin = new Vector2(0,0.12f); chipRT.anchorMax = new Vector2(0,0.88f);
        chipRT.pivot = new Vector2(0,0.5f); chipRT.anchoredPosition = new Vector2(10,0); chipRT.sizeDelta = new Vector2(44,0);

        var nmGO  = NewGO("IngName", cp); _detailIngNames[index] = nmGO.AddComponent<Text>();
        _detailIngNames[index].font = F(); _detailIngNames[index].fontSize = 17; _detailIngNames[index].fontStyle = FontStyle.Bold;
        _detailIngNames[index].color = new Color(0.88f,0.88f,0.88f); _detailIngNames[index].alignment = TextAnchor.MiddleLeft;
        var nmRT = nmGO.GetComponent<RectTransform>();
        nmRT.anchorMin = new Vector2(0,0.5f); nmRT.anchorMax = Vector2.one;
        nmRT.pivot = new Vector2(0,0.5f); nmRT.anchoredPosition = new Vector2(62,0); nmRT.sizeDelta = new Vector2(-70,0);

        var hvGO  = NewGO("IngHave", cp); _detailIngHave[index] = hvGO.AddComponent<Text>();
        _detailIngHave[index].font = F(); _detailIngHave[index].fontSize = 13;
        _detailIngHave[index].color = new Color(0.52f,0.52f,0.52f,0.90f); _detailIngHave[index].alignment = TextAnchor.MiddleLeft;
        var hvRT = hvGO.GetComponent<RectTransform>();
        hvRT.anchorMin = Vector2.zero; hvRT.anchorMax = new Vector2(1,0.52f);
        hvRT.pivot = new Vector2(0,0); hvRT.anchoredPosition = new Vector2(62,4); hvRT.sizeDelta = new Vector2(-70,0);
    }

    void BuildLoadingOverlay(Transform root)
    {
        _loadingOverlay = NewGO("LoadingOverlay", root); var bg = _loadingOverlay.AddComponent<Image>();
        bg.color = Color.black; bg.raycastTarget = true;
        Stretch(_loadingOverlay.GetComponent<RectTransform>());
        _overlayCG = _loadingOverlay.AddComponent<CanvasGroup>();
        var tGO = NewGO("OverlayText", _loadingOverlay.transform);
        _overlayText = tGO.AddComponent<Text>();
        _overlayText.font = F(); _overlayText.fontSize = 32; _overlayText.fontStyle = FontStyle.Bold;
        _overlayText.color = new Color(0.72f,0.56f,0.26f,1f); _overlayText.alignment = TextAnchor.MiddleCenter;
        AnchorsFull(tGO.GetComponent<RectTransform>(), new Vector2(0.25f,0.42f), new Vector2(0.75f,0.58f));
    }

    // ── Craft Selection & Refresh ─────────────────────────────────────────────

    void SelectRecipe(int index)
    {
        _selectedRecipe = index;
        var inv = Inventory.Instance;
        for (int i = 0; i < _craftRows.Length; i++)
        {
            if (_craftRows[i] == null) continue;
            bool sel = (i == index); bool can = CanCraftRecipe(i, inv);
            _craftRows[i].rowBg.color    = sel ? new Color(0.20f,0.20f,0.20f,0.85f)
                : (can ? new Color(0.10f,0.10f,0.10f,0.60f) : new Color(0.07f,0.07f,0.07f,0.50f));
            _craftRows[i].indicator.color = can ? new Color(0.90f,0.90f,0.90f,0.90f)
                : new Color(0.25f,0.25f,0.25f,0.55f);
            _craftRows[i].nameText.color = sel ? new Color(1f,1f,1f,1f)
                : (can ? new Color(0.72f,0.72f,0.72f,0.90f) : new Color(0.38f,0.38f,0.38f,0.70f));
        }
        if (_detailSelectHint != null) _detailSelectHint.SetActive(false);
        if (_detailFadeCo != null) StopCoroutine(_detailFadeCo);
        _detailFadeCo = StartCoroutine(FadeDetail(index));
    }

    IEnumerator FadeDetail(int index)
    {
        if (_detailCG.alpha > 0.05f)
        {
            float t = 0f;
            while (t < 0.08f) { t += Time.deltaTime; _detailCG.alpha = Mathf.Lerp(1f,0f,t/0.08f); yield return null; }
        }
        _detailCG.alpha = 0f;
        PopulateDetailPane(index);
        float t2 = 0f;
        while (t2 < 0.18f) { t2 += Time.deltaTime; _detailCG.alpha = Mathf.Clamp01(t2/0.18f); yield return null; }
        _detailCG.alpha = 1f;
    }

    void PopulateDetailPane(int index)
    {
        var recipe = Recipes[index];
        var inv    = Inventory.Instance;
        if (_detailTitle != null) _detailTitle.text = recipe.name.ToUpper();

        string missingMsg = "";
        for (int i = 0; i < 2; i++)
        {
            bool hasCard = i < recipe.ingredients.Length;
            _detailIngBgs[i].gameObject.SetActive(hasCard);
            if (!hasCard) continue;
            var ing  = recipe.ingredients[i];
            int have = inv?.Count(ing.id) ?? 0;
            bool ok  = have >= ing.qty;

            // B&W: light grey chip if have, dark grey if missing
            _detailIngChips[i].color = ok ? new Color(0.82f,0.82f,0.82f,0.90f) : new Color(0.22f,0.22f,0.22f,0.65f);
            _detailIngBgs[i].color   = new Color(0.09f,0.09f,0.09f,0.90f);
            _detailIngNames[i].text  = ChipName(ing.id) + "  \u00d7" + ing.qty;
            _detailIngNames[i].color = ok ? new Color(0.92f,0.92f,0.92f) : new Color(0.55f,0.42f,0.38f);
            _detailIngHave[i].text   = ok ? "you have: " + have + "  \u2713"
                                          : "you have: " + have + "  \u2014  need " + (ing.qty-have) + " more";
            _detailIngHave[i].color  = ok ? new Color(0.55f,0.55f,0.55f,0.90f)
                                          : new Color(0.88f,0.48f,0.26f,0.90f);
            if (!ok && missingMsg == "") missingMsg = "Need " + (ing.qty-have) + " more " + ChipName(ing.id);
        }
        if (_detailMissing != null) _detailMissing.text = missingMsg;

        if (_detailResultChip != null) _detailResultChip.color = new Color(0.75f,0.75f,0.75f,0.92f);
        if (_detailResultBg   != null) _detailResultBg.color   = new Color(0.10f,0.10f,0.10f,0.90f);
        if (_detailResultName != null) _detailResultName.text  =
            ChipName(recipe.resultId) + (recipe.resultQty > 1 ? "  \u00d7" + recipe.resultQty : "");
        if (_detailResultDesc != null) _detailResultDesc.text  = ItemDesc(recipe.resultId);

        bool canCraft = CanCraftRecipe(index, inv) && (inv == null || !inv.IsFull() || inv.Has(recipe.resultId,1));
        if (_detailCraftBtnImg != null)
            _detailCraftBtnImg.color = canCraft ? new Color(0.88f,0.88f,0.88f) : new Color(0.08f,0.08f,0.08f);
        if (_detailCraftBtnLbl != null)
            _detailCraftBtnLbl.color = canCraft ? new Color(0.06f,0.06f,0.06f) : new Color(0.28f,0.28f,0.28f,0.60f);
        if (_detailCraftBtn != null) _detailCraftBtn.interactable = canCraft;

        if (_craftBtnPulseCo != null) StopCoroutine(_craftBtnPulseCo);
        if (canCraft && _detailCraftBtn != null) _craftBtnPulseCo = StartCoroutine(CraftBtnPulse());
    }

    void RefreshCraftPanel()
    {
        var inv = Inventory.Instance;
        if (_craftResourcesText != null)
        {
            int sc = inv?.Count("scrap")   ?? 0;
            int md = inv?.Count("meds")    ?? 0;
            int bt = inv?.Count("battery") ?? 0;
            _craftResourcesText.text = "SCRAP: " + sc + "   \u00b7   MEDS: " + md + "   \u00b7   BATTERY: " + bt;
        }
        if (_craftRows == null) return;
        for (int i = 0; i < Recipes.Length; i++)
        {
            if (_craftRows[i] == null) continue;
            bool sel = (i == _selectedRecipe); bool can = CanCraftRecipe(i, inv);
            _craftRows[i].rowBg.color    = sel ? new Color(0.20f,0.20f,0.20f,0.85f)
                : (can ? new Color(0.10f,0.10f,0.10f,0.60f) : new Color(0.07f,0.07f,0.07f,0.50f));
            _craftRows[i].indicator.color = can ? new Color(0.90f,0.90f,0.90f,0.90f) : new Color(0.25f,0.25f,0.25f,0.55f);
            _craftRows[i].nameText.color = sel ? new Color(1f,1f,1f,1f)
                : (can ? new Color(0.72f,0.72f,0.72f,0.90f) : new Color(0.38f,0.38f,0.38f,0.70f));
        }
        if (_selectedRecipe >= 0) PopulateDetailPane(_selectedRecipe);
    }

    bool CanCraftRecipe(int index, Inventory inv)
    {
        if (inv == null) return false;
        foreach (var ing in Recipes[index].ingredients)
            if (!inv.Has(ing.id, ing.qty)) return false;
        return true;
    }

    // ── Interaction ───────────────────────────────────────────────────────────

    void OnWindowClick()
    {
        Feedback(WindowMessages[Random.Range(0, WindowMessages.Length)], new Color(0.62f,0.50f,0.72f));
        StartCoroutine(WindowShake());
    }

    void OnRestClick()
    {
        if (_hasRested) { Feedback("The cot looks wrong now.\nYou don't want to sleep again.", new Color(0.70f,0.50f,0.30f)); return; }
        var ps = PlayerStats.Instance;
        if (ps == null) return;
        if (ps.hp >= ps.maxHp) { Feedback("You're not injured.\nRest feels wrong anyway.", new Color(0.60f,0.60f,0.40f)); return; }
        _hasRested = true; ps.Heal(25);
        if (_restBtnLabel != null) { _restBtnLabel.text = "[ RESTED ]"; _restBtnLabel.color = new Color(0.38f,0.48f,0.38f,0.65f); }
        Feedback(RestMessages[Random.Range(0, RestMessages.Length)], new Color(0.65f,0.78f,0.65f));
        StartCoroutine(ZzzEffect());
    }

    void OnTradeClick()
    {
        if (_craftPanel != null) _craftPanel.SetActive(false);
        UpdateScrap(); _tradePanel.SetActive(true); StartCoroutine(OpenLid());
    }
    void OnCloseTrade()  { _tradePanel.SetActive(false); StartCoroutine(CloseLid()); }

    void OnCraftClick()
    {
        if (_tradePanel != null) _tradePanel.SetActive(false);
        if (_crateOpen) StartCoroutine(CloseLid());
        RefreshCraftPanel(); _craftPanel.SetActive(true);
    }
    void OnCloseCraft()
    {
        if (_craftBtnPulseCo != null) { StopCoroutine(_craftBtnPulseCo); _craftBtnPulseCo = null; }
        if (_detailCraftBtn != null) _detailCraftBtn.GetComponent<RectTransform>().localScale = Vector3.one;
        _craftPanel.SetActive(false);
    }

    void OnLampClick()  => StartCoroutine(LampBurst());
    void OnDoorClick()  => StartCoroutine(DoorLeave());

    void OnBuy(string id, int cost, int qty)
    {
        var inv = Inventory.Instance; if (inv == null) return;
        if (!inv.Has("scrap", cost))        { Feedback("Not enough scrap.",   new Color(0.82f,0.36f,0.30f)); return; }
        if (inv.IsFull() && !inv.Has(id,1)) { Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return; }
        inv.Remove("scrap", cost);
        if (!inv.Add(id, qty)) { inv.Add("scrap", cost); Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return; }
        UpdateScrap();
        Feedback("+" + (qty > 1 ? qty+"x " : "") + ItemName(id), new Color(0.60f,0.88f,0.60f));
    }

    void OnCraftRecipe(int index)
    {
        if (index < 0 || index >= Recipes.Length) return;
        var inv = Inventory.Instance; var recipe = Recipes[index]; if (inv == null) return;
        foreach (var ing in recipe.ingredients)
            if (!inv.Has(ing.id, ing.qty)) { Feedback("Missing materials.", new Color(0.82f,0.36f,0.30f)); return; }
        if (inv.IsFull() && !inv.Has(recipe.resultId, 1))
        { Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return; }
        foreach (var ing in recipe.ingredients) inv.Remove(ing.id, ing.qty);
        if (!inv.Add(recipe.resultId, recipe.resultQty))
        {
            foreach (var ing in recipe.ingredients) inv.Add(ing.id, ing.qty);
            Feedback("Inventory is full.", new Color(0.82f,0.36f,0.30f)); return;
        }
        RefreshCraftPanel();
        Feedback("Crafted: " + recipe.name, new Color(0.92f,0.96f,0.92f));
        StartCoroutine(ResultPulse());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator PlayerIdleAnim()
    {
        int frame = 0;
        while (isActiveAndEnabled && _playerRaw != null)
        {
            float col = (frame % 8) * (1f / 8f);
            _playerRaw.uvRect = new Rect(col, 6f/7f, 1f/8f, 1f/7f);
            frame = (frame + 1) % 8;
            yield return new WaitForSeconds(0.12f);   // ~8 fps
        }
    }

    IEnumerator CraftBtnPulse()
    {
        var rt = _detailCraftBtn?.GetComponent<RectTransform>();
        if (rt == null) yield break;
        float t = 0f;
        while (_detailCraftBtn != null && _detailCraftBtn.interactable)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 2.4f) * 0.022f;
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    IEnumerator ResultPulse()
    {
        if (_detailResultRT == null) yield break;
        float t = 0f, dur = 0.55f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t/dur * Mathf.PI) * 0.32f;
            _detailResultRT.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        _detailResultRT.localScale = Vector3.one;
    }

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
        float[] offsets = {8f,-7f,6f,-5f,3f,-2f,0f};
        foreach (float x in offsets) { _windowRT.anchoredPosition = origin + new Vector2(x,0); yield return new WaitForSeconds(0.07f); }
        _windowRT.anchoredPosition = origin;
    }

    IEnumerator DoorLeave()
    {
        // Close any open panels immediately
        if (_tradePanel != null && _tradePanel.activeSelf) _tradePanel.SetActive(false);
        if (_craftPanel != null && _craftPanel.activeSelf) OnCloseCraft();

        // Door shake
        if (_doorRT != null)
        {
            Vector2 origin = _doorRT.anchoredPosition;
            float[] offsets = {-5f,5f,-4f,3f,0f};
            foreach (float x in offsets) { _doorRT.anchoredPosition = origin + new Vector2(x,0); yield return new WaitForSeconds(0.06f); }
            _doorRT.anchoredPosition = origin;
        }
        Feedback("You brace the door open...", new Color(0.60f,0.55f,0.38f));
        yield return new WaitForSeconds(0.25f);

        // Camera zoom toward door
        yield return StartCoroutine(ZoomToDoor());

        // Loading screen
        Close();
    }

    IEnumerator ZoomToDoor()
    {
        if (_roomContainer == null) yield break;
        float t = 0f, dur = 0.72f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            float s = Mathf.Lerp(1f, 3.2f, p);
            _roomContainer.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        yield return new WaitForSeconds(0.06f);
    }

    IEnumerator ZzzEffect()
    {
        var zGO  = NewGO("ZZZ", transform); var zTxt = zGO.AddComponent<Text>();
        zTxt.text = "z  z  z"; zTxt.font = F(); zTxt.fontSize = 21;
        zTxt.color = new Color(0.60f,0.72f,0.60f); zTxt.alignment = TextAnchor.MiddleCenter;
        var zRT = zGO.GetComponent<RectTransform>();
        zRT.anchorMin = zRT.anchorMax = new Vector2(0.24f,0.32f);
        zRT.pivot = new Vector2(0.5f,0f); zRT.anchoredPosition = new Vector2(0,60f); zRT.sizeDelta = new Vector2(130,28f);
        float e = 0f;
        while (e < 1.6f)
        {
            e += Time.deltaTime; float p = e/1.6f;
            zRT.anchoredPosition = new Vector2(0, 60f + p*65f);
            zTxt.color = new Color(0.60f,0.72f,0.60f, 1f-p);
            yield return null;
        }
        Destroy(zGO);
    }

    IEnumerator OpenLid()
    {
        if (_crateOpen || _crateLid == null) yield break; _crateOpen = true;
        float t = 0f;
        while (t < 0.35f) { t += Time.deltaTime; _crateLid.localEulerAngles = new Vector3(0,0,Mathf.Lerp(0,-65f,t/0.35f)); yield return null; }
        _crateLid.localEulerAngles = new Vector3(0,0,-65f);
    }

    IEnumerator CloseLid()
    {
        if (!_crateOpen || _crateLid == null) yield break; _crateOpen = false;
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
    { GlowAlpha(_lampGlow1,0.048f*m); GlowAlpha(_lampGlow2,0.092f*m); GlowAlpha(_lampGlow3,0.210f*m); }

    void GlowAlpha(GameObject go, float a)
    {
        if (go == null) return;
        var img = go.GetComponent<Image>();
        if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, a);
    }

    void UpdateScrap()
    {
        if (_scrapDisplay == null) return;
        _scrapDisplay.text = "SCRAP:  " + (Inventory.Instance?.Count("scrap") ?? 0);
    }

    string ItemName(string id) => id switch {
        "pills"=>"Pills","food"=>"Food","meds"=>"Medical Supplies","ammo"=>"Ammo","medkit"=>"Medkit",_=>id };

    static string ChipName(string id) => id switch {
        "scrap"=>"Scrap","meds"=>"Meds","pills"=>"Pills","food"=>"Food","battery"=>"Battery",
        "ammo"=>"Ammo","medkit"=>"Medkit","lockpick"=>"Lockpick","flashlight"=>"Flashlight",_=>id };

    static string ItemDesc(string id) => id switch {
        "medkit"    => "Stops the bleeding.\nBuys time.",
        "meds"      => "Not just painkillers.\nWell. Also painkillers.",
        "ammo"      => "Every bullet is\na decision.",
        "lockpick"  => "Opens things that\nwant to stay closed.",
        "flashlight"=> "See what\nyou shouldn't.",
        _           => "" };

    // ── Static UI Utilities ───────────────────────────────────────────────────

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>(); return go;
    }

    static void StaticBg(string name, Transform parent, Color col, Vector2 ancMin, Vector2 ancMax)
    {
        var go  = NewGO(name, parent); var img = go.AddComponent<Image>();
        img.color = col; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject GlowCircle(string name, Transform parent, Vector2 anchor, float size, Color col)
    {
        var go  = NewGO(name, parent); var img = go.AddComponent<Image>();
        img.color = col; img.sprite = CircleSprite(128); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f,0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(size,size);
        return go;
    }

    static void Stretch(RectTransform rt)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    static void PadStretch(RectTransform rt, float px, float py = -1f)
    {
        float fy = py < 0 ? px : py;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(px,fy); rt.offsetMax = new Vector2(-px,-fy);
    }

    static void AnchorsFull(RectTransform rt, Vector2 min, Vector2 max)
    { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    static void Divider(Transform parent, string name, bool fromBottom, Vector2 pos)
    {
        var go  = NewGO(name, parent); var img = go.AddComponent<Image>();
        img.color = new Color(0.28f,0.20f,0.10f,0.55f); img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>(); float ay = fromBottom ? 0 : 1;
        rt.anchorMin = new Vector2(0.04f,ay); rt.anchorMax = new Vector2(0.96f,ay);
        rt.pivot = new Vector2(0.5f,ay); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(0,1);
    }

    static void PanelText(Transform parent, string name, string text, int size, FontStyle style,
        Color col, Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 pos, Vector2 sd)
    {
        var go  = NewGO(name, parent); var txt = go.AddComponent<Text>();
        txt.text = text; txt.font = F(); txt.fontSize = size; txt.fontStyle = style;
        txt.color = col; txt.alignment = TextAnchor.MiddleCenter;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = sd;
    }

    void TradeRow(Transform parent, string id, string label, int cost, Color col, float yOff)
    {
        var rGO  = NewGO("Row_"+id, parent); var rImg = rGO.AddComponent<Image>();
        rImg.color = new Color(0.10f,0.08f,0.05f,0.55f);
        var rRT = rGO.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(0.04f,1); rRT.anchorMax = new Vector2(0.96f,1);
        rRT.pivot = new Vector2(0.5f,1); rRT.anchoredPosition = new Vector2(0,yOff); rRT.sizeDelta = new Vector2(0,54);
        Transform rp = rGO.transform;

        var iGO = NewGO("Item",rp); var iTxt = iGO.AddComponent<Text>();
        iTxt.text = label; iTxt.font = F(); iTxt.fontSize = 19; iTxt.color = col; iTxt.alignment = TextAnchor.MiddleLeft;
        var iRT = iGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0,0); iRT.anchorMax = new Vector2(0.62f,1);
        iRT.pivot = new Vector2(0,0.5f); iRT.anchoredPosition = new Vector2(14,0); iRT.sizeDelta = Vector2.zero;

        var cGO = NewGO("Cost",rp); var cTxt = cGO.AddComponent<Text>();
        cTxt.text = cost+" scrap"; cTxt.font = F(); cTxt.fontSize = 17;
        cTxt.color = new Color(0.60f,0.80f,0.55f); cTxt.alignment = TextAnchor.MiddleCenter;
        var cRT = cGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.60f,0); cRT.anchorMax = new Vector2(0.80f,1);
        cRT.pivot = new Vector2(0.5f,0.5f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;

        var bGO = NewGO("Buy",rp); var bImg = bGO.AddComponent<Image>();
        bImg.color = new Color(0.20f,0.28f,0.14f);
        var bRT = bGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.81f,0.10f); bRT.anchorMax = new Vector2(0.97f,0.90f);
        bRT.pivot = new Vector2(0.5f,0.5f); bRT.anchoredPosition = Vector2.zero; bRT.sizeDelta = Vector2.zero;
        var bBtn = bGO.AddComponent<Button>();
        var bc = bBtn.colors;
        bc.highlightedColor = new Color(0.32f,0.46f,0.20f); bc.pressedColor = new Color(0.11f,0.17f,0.08f);
        bBtn.colors = bc;
        string cid = id; int ccost = cost; int cqty = id == "ammo" ? 3 : 1;
        bBtn.onClick.AddListener(() => OnBuy(cid, ccost, cqty));
        var blGO = NewGO("BuyLbl",bGO.transform); var blTxt = blGO.AddComponent<Text>();
        blTxt.text = "BUY"; blTxt.font = F(); blTxt.fontSize = 17; blTxt.fontStyle = FontStyle.Bold;
        blTxt.color = new Color(0.72f,0.92f,0.52f); blTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(blGO.GetComponent<RectTransform>());
    }

    static void SimpleCloseBtn(Transform parent, string name, UnityEngine.Events.UnityAction cb, Color bgCol, Color lblCol)
    {
        var cbGO  = NewGO(name, parent); var cbImg = cbGO.AddComponent<Image>(); cbImg.color = bgCol;
        var cbRT = cbGO.GetComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(0.30f,0); cbRT.anchorMax = new Vector2(0.70f,0);
        cbRT.pivot = new Vector2(0.5f,0); cbRT.anchoredPosition = new Vector2(0,8); cbRT.sizeDelta = new Vector2(0,36);
        var cbBtn = cbGO.AddComponent<Button>();
        var cc = cbBtn.colors;
        cc.highlightedColor = new Color(bgCol.r*1.8f,bgCol.g*1.8f,bgCol.b*1.8f,1f);
        cbBtn.colors = cc; cbBtn.onClick.AddListener(cb);
        var clGO = NewGO("Lbl",cbGO.transform); var clTxt = clGO.AddComponent<Text>();
        clTxt.text = "CLOSE"; clTxt.font = F(); clTxt.fontSize = 19; clTxt.fontStyle = FontStyle.Bold;
        clTxt.color = lblCol; clTxt.alignment = TextAnchor.MiddleCenter;
        Stretch(clGO.GetComponent<RectTransform>());
    }

    static Sprite CircleSprite(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz*sz]; float r = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float dx = x-r+0.5f, dy = y-r+0.5f;
            px[y*sz+x] = new Color(1,1,1,Mathf.Clamp01((r-Mathf.Sqrt(dx*dx+dy*dy))/(r*0.14f)));
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
    }

    static Sprite VignetteSprite(int sz)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz*sz]; float r = sz*0.5f;
        for (int y = 0; y < sz; y++) for (int x = 0; x < sz; x++)
        {
            float dx = (x-r)/r, dy = (y-r)/r;
            float a = Mathf.Clamp01((Mathf.Sqrt(dx*dx+dy*dy)-0.38f)/0.62f)*0.88f;
            px[y*sz+x] = new Color(0,0,0,a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, sz);
    }

    static Font F() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
