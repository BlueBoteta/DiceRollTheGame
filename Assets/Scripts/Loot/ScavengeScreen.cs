using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScavengeScreen : MonoBehaviour
{
    public static ScavengeScreen Instance { get; private set; }

    enum ContainerType { Open, Locked, Safe }

    Canvas          _canvas;
    RectTransform[] _containerRTs  = new RectTransform[3];
    Button[]        _containerBtns = new Button[3];
    Image[]         _containerBg   = new Image[3];
    Text[]          _containerLabel = new Text[3];
    Text[]          _containerSub   = new Text[3];
    ContainerType[] _types          = new ContainerType[3];
    bool[]          _searched       = new bool[3];

    Text       _titleText;
    Text       _actionText;
    GameObject _choicePanel;
    Text       _choiceBodyText;
    Button     _choiceYesBtn;
    Text       _choiceYesLabel;
    int        _choiceResult;

    Button _leaveBtn;
    bool   _done;
    bool   _interacting;

    // ── Loot pools ────────────────────────────────────────────────────────────

    static readonly string[] OpenPool   = { "food","food","pills","meds","ammo","ammo","scrap","battery" };
    static readonly string[] LockedPool = { "medkit","meds","ammo","ammo","ammo","lockpick","knife","battery","vest" };
    static readonly string[] SafePool   = { "pistol","vest","riot_gear","medkit","medkit","ammo","ammo","battery","lockpick","shotgun" };

    // ── Flavor text ───────────────────────────────────────────────────────────

    static readonly (string title, string desc)[] Rooms =
    {
        ("HOSPITAL SUPPLY ROOM",  "Fluorescent lights buzz overhead. Something drips in the corner."),
        ("ABANDONED OFFICE",      "Papers everywhere. Half a cup of coffee, still on the desk."),
        ("GROCERY BACKROOM",      "Shelves half-stripped. A smell you decide not to investigate."),
        ("MOTEL ROOM",            "TV on. Static. You don't look at the bed."),
        ("SCHOOL CLOSET",         "Paint fumes. Old trophies. The door closed behind you."),
        ("APARTMENT KITCHEN",     "Cabinets half-open. Someone left mid-meal and never came back."),
    };

    static readonly string[] PreOpen   =
    {
        "Not locked.\nSomething shifts inside.",
        "The latch gives without resistance.",
        "Comes open easily.\nSomeone left in a hurry.",
    };
    static readonly string[] PreLocked =
    {
        "Padlocked.\nSomeone didn't want this found.",
        "The clasp won't budge.\nYou'll need a pick.",
        "Locked tight.\nCould be worth the effort.",
    };
    static readonly string[] PreSafe   =
    {
        "Cold steel.\nThe dial doesn't move. Solid.",
        "A combination lock.\nSomeone hid something important here.",
        "Heavy gauge.\nThis one was sealed on purpose.",
    };

    static readonly string[] SuccessLocked = { "The lock gives.\nYou're in.", "Click.\nIt opens." };
    static readonly string[] FailLocked    = { "The pick snaps.\nThe lock holds.", "You lose the tension.\nNothing." };
    static readonly string[] SuccessSafe   = { "The dial clicks home.\nThe door swings open.", "All four pins.\nThe bolt slides back." };
    static readonly string[] FailSafe      = { "Time runs out.\nThe steel holds.", "Not this time.\nThe safe stays shut." };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public IEnumerator Open()
    {
        _done        = false;
        _interacting = false;
        _actionText.text = "";
        _choicePanel.SetActive(false);

        var room = Rooms[Random.Range(0, Rooms.Length)];
        _titleText.text = room.title;

        for (int i = 0; i < 3; i++)
        {
            _searched[i] = false;
            int roll = Random.Range(0, 100);
            _types[i] = roll < 40 ? ContainerType.Open
                      : roll < 72 ? ContainerType.Locked
                      :             ContainerType.Safe;
            RefreshContainer(i);
            _containerBtns[i].interactable = false;
            _containerRTs[i].localScale    = Vector3.zero;
        }

        _canvas.gameObject.SetActive(true);

        // Typewrite room description into action area
        yield return StartCoroutine(TypeAction(room.desc, new Color(0.48f, 0.42f, 0.34f), 14, FontStyle.Italic));
        yield return new WaitForSeconds(0.4f);
        _actionText.text = "";

        // Bounce containers in with stagger
        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(BounceIn(_containerRTs[i]));
            yield return new WaitForSeconds(0.10f);
        }
        yield return new WaitForSeconds(0.28f);

        // Enable buttons and show hint
        SetAllBtns(true);
        _leaveBtn.interactable = true;
        yield return StartCoroutine(TypeAction("Click a container to investigate.",
            new Color(0.40f, 0.36f, 0.30f), 14, FontStyle.Italic));

        while (!_done)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    // ── Container Interaction ─────────────────────────────────────────────────

    void OnContainerClicked(int idx)
    {
        if (_searched[idx] || _interacting || _done) return;
        StartCoroutine(HandleContainer(idx));
    }

    IEnumerator HandleContainer(int idx)
    {
        _interacting = true;
        SetAllBtns(false);
        _leaveBtn.interactable = false;
        _actionText.text       = "";

        var type = _types[idx];

        // Shake the container on approach
        yield return StartCoroutine(ShakeContainer(_containerRTs[idx]));

        if (type == ContainerType.Open)
        {
            yield return StartCoroutine(TypeAction(PreOpen[Random.Range(0, PreOpen.Length)],
                new Color(0.78f, 0.72f, 0.55f), 16, FontStyle.Italic));
            yield return new WaitForSeconds(0.35f);

            _searched[idx] = true;
            RefreshContainer(idx);
            yield return StartCoroutine(BounceIn(_containerRTs[idx], 0.18f));

            _actionText.text = "";
            string id  = OpenPool[Random.Range(0, OpenPool.Length)];
            int    qty = id == "ammo" ? Random.Range(1, 4) : 1;
            if (LootPickupPrompt.Instance != null)
                yield return StartCoroutine(LootPickupPrompt.Instance.Show(id, qty));
        }
        else
        {
            // Show pre-flavor text
            string[] prePool = type == ContainerType.Safe ? PreSafe : PreLocked;
            yield return StartCoroutine(TypeAction(prePool[Random.Range(0, prePool.Length)],
                new Color(0.82f, 0.65f, 0.28f), 16, FontStyle.Italic));
            yield return new WaitForSeconds(0.45f);
            _actionText.text = "";

            // Build choice panel text
            bool hasLockpick = Inventory.Instance?.Has("lockpick") ?? false;
            int  pickCount   = Inventory.Instance?.Count("lockpick") ?? 0;
            if (type == ContainerType.Safe)
            {
                _choiceBodyText.text  = hasLockpick
                    ? $"Use a lockpick to crack the safe?\n({pickCount} left)"
                    : "No lockpick.\nYou can't crack this.";
                _choiceYesLabel.text  = "CRACK IT";
            }
            else
            {
                _choiceBodyText.text  = hasLockpick
                    ? $"Use a lockpick to pick the lock?\n({pickCount} left)"
                    : "No lockpick.\nYou can't open this.";
                _choiceYesLabel.text  = "PICK IT";
            }
            _choiceYesBtn.interactable = hasLockpick;
            _choiceResult = -1;
            _choicePanel.SetActive(true);

            while (_choiceResult == -1)
                yield return null;

            _choicePanel.SetActive(false);

            if (_choiceResult == 0) // use lockpick
            {
                Inventory.Instance?.Remove("lockpick", 1);

                // Approach text
                string approachMsg = type == ContainerType.Safe
                    ? "You work the tumbler pins one by one.\nYour hands are steady. For now."
                    : "You slide the pick in and feel for the pins.\nSteady.";
                yield return StartCoroutine(TypeAction(approachMsg,
                    new Color(0.72f, 0.62f, 0.42f), 15, FontStyle.Italic));
                yield return new WaitForSeconds(0.3f);
                _actionText.text = "";

                bool success = false;
                if (type == ContainerType.Safe && PinTumblerMinigame.Instance != null)
                {
                    yield return StartCoroutine(PinTumblerMinigame.Instance.Play());
                    success = PinTumblerMinigame.Instance.Result;
                }
                else if (LockpickMinigame.Instance != null)
                {
                    yield return StartCoroutine(LockpickMinigame.Instance.Play());
                    success = LockpickMinigame.Instance.Result;
                }

                _searched[idx] = true;
                RefreshContainer(idx);

                if (success)
                {
                    string[] sl = type == ContainerType.Safe ? SuccessSafe : SuccessLocked;
                    yield return StartCoroutine(TypeAction(sl[Random.Range(0, sl.Length)],
                        new Color(0.42f, 0.90f, 0.42f), 17, FontStyle.Bold));
                    yield return new WaitForSeconds(0.55f);
                    _actionText.text = "";

                    string[] pool = type == ContainerType.Safe ? SafePool : LockedPool;
                    string id  = pool[Random.Range(0, pool.Length)];
                    int    qty = id == "ammo" ? Random.Range(3, 7) : 1;
                    if (LootPickupPrompt.Instance != null)
                        yield return StartCoroutine(LootPickupPrompt.Instance.Show(id, qty));
                }
                else
                {
                    string[] fl = type == ContainerType.Safe ? FailSafe : FailLocked;
                    yield return StartCoroutine(TypeAction(fl[Random.Range(0, fl.Length)],
                        new Color(0.90f, 0.32f, 0.22f), 17, FontStyle.Bold));
                    yield return new WaitForSeconds(0.80f);
                    _actionText.text = "";
                }
            }
            else // skip
            {
                _searched[idx] = false; // leave it available
                yield return StartCoroutine(TypeAction("You leave it for now.",
                    new Color(0.40f, 0.38f, 0.35f), 14, FontStyle.Italic));
                yield return new WaitForSeconds(0.35f);
                _actionText.text = "";
            }
        }

        _interacting = false;
        SetAllBtns(true);
        _leaveBtn.interactable = true;
    }

    void RefreshContainer(int idx)
    {
        bool searched = _searched[idx];
        var  type     = _types[idx];

        Color bgCol = type switch
        {
            ContainerType.Open   => new Color(0.12f, 0.15f, 0.09f),
            ContainerType.Locked => new Color(0.16f, 0.12f, 0.05f),
            ContainerType.Safe   => new Color(0.09f, 0.10f, 0.17f),
            _                    => new Color(0.10f, 0.10f, 0.12f),
        };
        _containerBg[idx].color = searched
            ? new Color(bgCol.r * 0.3f, bgCol.g * 0.3f, bgCol.b * 0.3f)
            : bgCol;

        if (searched)
        {
            _containerLabel[idx].text  = "SEARCHED";
            _containerLabel[idx].color = new Color(0.28f, 0.28f, 0.34f);
            _containerSub[idx].text    = "";
        }
        else
        {
            _containerLabel[idx].text = type switch
            {
                ContainerType.Open   => "UNLOCKED",
                ContainerType.Locked => "LOCKED",
                ContainerType.Safe   => "SAFE",
                _                    => "?"
            };
            _containerLabel[idx].color = type switch
            {
                ContainerType.Open   => new Color(0.42f, 0.85f, 0.30f),
                ContainerType.Locked => new Color(0.95f, 0.70f, 0.18f),
                ContainerType.Safe   => new Color(0.40f, 0.65f, 0.95f),
                _                    => Color.white,
            };
            _containerSub[idx].text = type switch
            {
                ContainerType.Open   => "click to search",
                ContainerType.Locked => "lockpick required",
                ContainerType.Safe   => "lockpick + skill",
                _                    => ""
            };
        }
    }

    void SetAllBtns(bool on)
    {
        for (int i = 0; i < 3; i++)
            _containerBtns[i].interactable = on && !_searched[i];
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator BounceIn(RectTransform rt, float dur = 0.25f)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = p < 0.65f
                ? Mathf.Lerp(0f, 1.12f, p / 0.65f)
                : Mathf.Lerp(1.12f, 1f, (p - 0.65f) / 0.35f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator ShakeContainer(RectTransform rt)
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f, dur = 0.20f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 48f) * 6f * (1f - t / dur);
            rt.anchoredPosition = origin + new Vector2(shake, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    IEnumerator TypeAction(string msg, Color col, int fontSize, FontStyle style)
    {
        _actionText.color     = col;
        _actionText.fontSize  = fontSize;
        _actionText.fontStyle = style;
        _actionText.text      = "";
        foreach (char c in msg)
        {
            _actionText.text += c;
            yield return new WaitForSeconds(c == '\n' ? 0.06f : 0.028f);
        }
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("ScavengeCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 25;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        // Dark overlay
        var dim = Mk("Dim", cGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

        var panel = C("Panel", cGO.transform, Vector2.zero, new Vector2(820f, 555f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.08f, 0.98f);

        // Top accent
        var top = Mk("Top", panel);
        top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
        top.pivot = new Vector2(0.5f, 1); top.sizeDelta = new Vector2(0, 3);
        top.anchoredPosition = Vector2.zero;
        top.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.22f, 0.10f);

        // "YOU SEARCH" label
        T(C("YouSearch", panel, new Vector2(-88f, 240f), new Vector2(360f, 30f)),
          "YOU SEARCH", 15, FontStyle.Normal,
          new Color(0.45f, 0.40f, 0.32f), TextAnchor.MiddleRight);

        // Dynamic room title
        _titleText = T(C("RoomTitle", panel, new Vector2(100f, 240f), new Vector2(440f, 30f)),
          "THE AREA", 20, FontStyle.Bold,
          new Color(0.80f, 0.62f, 0.28f), TextAnchor.MiddleLeft);

        // Separator under title
        var sep = Mk("TitleSep", panel);
        sep.anchorMin = new Vector2(0.05f, 1f); sep.anchorMax = new Vector2(0.95f, 1f);
        sep.pivot = new Vector2(0.5f, 1f);
        sep.anchoredPosition = new Vector2(0f, -28f);
        sep.sizeDelta = new Vector2(0f, 1f);
        sep.gameObject.AddComponent<Image>().color = new Color(0.28f, 0.22f, 0.10f, 0.60f);

        // 3 containers
        float[] xPos    = { -248f, 0f, 248f };
        string[] objLbl = { "FILING CABINET", "FOOTLOCKER", "WALL SAFE" };

        for (int i = 0; i < 3; i++)
        {
            var ctr = C("Container" + i, panel, new Vector2(xPos[i], 35f), new Vector2(196f, 246f));
            _containerBg[i]  = ctr.gameObject.AddComponent<Image>();
            _containerRTs[i] = ctr;

            BuildContainerGraphic(i, ctr);

            _containerLabel[i] = T(C("TypeLbl", ctr, new Vector2(0, -82), new Vector2(176f, 24f)),
                "", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            _containerSub[i] = T(C("SubLbl", ctr, new Vector2(0, -104), new Vector2(176f, 18f)),
                "", 11, FontStyle.Italic, new Color(0.40f, 0.38f, 0.34f), TextAnchor.MiddleCenter);

            T(C("ObjLbl", ctr, new Vector2(0, -117), new Vector2(176f, 16f)),
              objLbl[i], 10, FontStyle.Normal,
              new Color(0.28f, 0.26f, 0.24f), TextAnchor.MiddleCenter);

            // Hover overlay (proper targetGraphic so bg color isn't affected)
            var ov = C("Overlay", ctr, Vector2.zero, new Vector2(196f, 246f));
            var ovImg = ov.gameObject.AddComponent<Image>();
            ovImg.color = Color.clear;

            int cap = i;
            var btn = ctr.gameObject.AddComponent<Button>();
            btn.targetGraphic = ovImg;
            var bc = btn.colors;
            bc.normalColor      = Color.clear;
            bc.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
            bc.pressedColor     = new Color(1f, 1f, 1f, 0.04f);
            bc.disabledColor    = Color.clear;
            btn.colors = bc;
            btn.onClick.AddListener(() => OnContainerClicked(cap));
            _containerBtns[i] = btn;
        }

        // Action text
        var actRT = C("ActionText", panel, new Vector2(0, -110), new Vector2(700f, 52f));
        _actionText = actRT.gameObject.AddComponent<Text>();
        _actionText.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _actionText.fontSize         = 15;
        _actionText.fontStyle        = FontStyle.Italic;
        _actionText.alignment        = TextAnchor.MiddleCenter;
        _actionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _actionText.verticalOverflow   = VerticalWrapMode.Overflow;
        _actionText.color            = new Color(0.62f, 0.55f, 0.40f);

        // Choice panel
        var cpRT = C("ChoicePanel", panel, new Vector2(0, -182), new Vector2(580f, 90f));
        cpRT.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.08f, 0.11f, 0.98f);
        var cpBrd = Mk("CpBorder", cpRT);
        cpBrd.anchorMin = new Vector2(0,1); cpBrd.anchorMax = new Vector2(1,1);
        cpBrd.pivot = new Vector2(0.5f,1); cpBrd.sizeDelta = new Vector2(0,2);
        cpBrd.anchoredPosition = Vector2.zero;
        cpBrd.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.32f, 0.10f, 0.80f);
        _choicePanel = cpRT.gameObject;

        _choiceBodyText = T(C("BodyTxt", cpRT, new Vector2(-120f, 5f), new Vector2(260f, 62f)),
            "", 13, FontStyle.Normal, new Color(0.80f, 0.72f, 0.55f), TextAnchor.MiddleLeft);
        _choiceBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;

        var yesRT = C("YesBtn", cpRT, new Vector2(120f, 8f), new Vector2(130f, 38f));
        yesRT.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.34f, 0.14f);
        _choiceYesBtn = yesRT.gameObject.AddComponent<Button>();
        var yc = _choiceYesBtn.colors;
        yc.highlightedColor = new Color(0.20f, 0.50f, 0.20f);
        yc.pressedColor     = new Color(0.09f, 0.22f, 0.09f);
        yc.disabledColor    = new Color(0.14f, 0.14f, 0.17f);
        _choiceYesBtn.colors = yc;
        _choiceYesBtn.onClick.AddListener(() => _choiceResult = 0);
        _choiceYesLabel = T(C("Lbl", yesRT, Vector2.zero, yesRT.sizeDelta),
            "PICK IT", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var noRT = C("NoBtn", cpRT, new Vector2(245f, 8f), new Vector2(90f, 38f));
        noRT.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.18f, 0.22f);
        var noBtn = noRT.gameObject.AddComponent<Button>();
        var nc = noBtn.colors;
        nc.highlightedColor = new Color(0.28f, 0.26f, 0.30f);
        noBtn.colors = nc;
        noBtn.onClick.AddListener(() => _choiceResult = 1);
        T(C("Lbl", noRT, Vector2.zero, noRT.sizeDelta),
          "SKIP", 15, FontStyle.Bold, new Color(0.50f, 0.48f, 0.55f), TextAnchor.MiddleCenter);

        _choicePanel.SetActive(false);

        // Leave button
        var leaveRT = C("LeaveBtn", panel, new Vector2(315f, -248f), new Vector2(150f, 44f));
        leaveRT.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.19f);
        _leaveBtn = leaveRT.gameObject.AddComponent<Button>();
        var lc = _leaveBtn.colors;
        lc.highlightedColor = new Color(0.22f, 0.22f, 0.28f);
        _leaveBtn.colors = lc;
        _leaveBtn.onClick.AddListener(() => _done = true);
        T(C("Lbl", leaveRT, Vector2.zero, leaveRT.sizeDelta),
          "LEAVE AREA", 15, FontStyle.Bold, new Color(0.50f, 0.48f, 0.55f), TextAnchor.MiddleCenter);
    }

    void BuildContainerGraphic(int idx, RectTransform p)
    {
        switch (idx)
        {
            case 0: // Filing cabinet — tall metal cabinet, 3 drawers
            {
                // Main body
                C("Body", p, new Vector2(0, 36f), new Vector2(92f, 138f))
                    .gameObject.AddComponent<Image>().color = new Color(0.24f, 0.21f, 0.17f);
                // Top ridge
                C("TopRidge", p, new Vector2(0, 103f), new Vector2(92f, 6f))
                    .gameObject.AddComponent<Image>().color = new Color(0.32f, 0.28f, 0.22f);
                // Bottom base
                C("Base", p, new Vector2(0, -31f), new Vector2(96f, 8f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.12f);
                // 3 drawers
                float[] drwY = { 76f, 38f, 0f };
                for (int d = 0; d < 3; d++)
                {
                    C("Drw" + d, p, new Vector2(0, drwY[d]), new Vector2(82f, 28f))
                        .gameObject.AddComponent<Image>().color = new Color(0.29f, 0.26f, 0.21f);
                    // Drawer gap (darker line above each drawer)
                    C("DGap" + d, p, new Vector2(0, drwY[d] + 14f), new Vector2(82f, 2f))
                        .gameObject.AddComponent<Image>().color = new Color(0.14f, 0.12f, 0.09f);
                    // Handle bar
                    C("Hdl" + d, p, new Vector2(0, drwY[d]), new Vector2(32f, 5f))
                        .gameObject.AddComponent<Image>().color = new Color(0.56f, 0.50f, 0.38f);
                    // Handle mounts (small dots)
                    C("HdlL" + d, p, new Vector2(-14f, drwY[d]), new Vector2(4f, 7f))
                        .gameObject.AddComponent<Image>().color = new Color(0.42f, 0.38f, 0.30f);
                    C("HdlR" + d, p, new Vector2(14f, drwY[d]), new Vector2(4f, 7f))
                        .gameObject.AddComponent<Image>().color = new Color(0.42f, 0.38f, 0.30f);
                }
                // Side panel lines (vertical indents)
                C("SideL", p, new Vector2(-40f, 36f), new Vector2(3f, 130f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.12f);
                C("SideR", p, new Vector2(40f, 36f), new Vector2(3f, 130f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.12f);
                break;
            }
            case 1: // Footlocker — military-style crate
            {
                // Main box body
                C("Box", p, new Vector2(0, 22f), new Vector2(144f, 70f))
                    .gameObject.AddComponent<Image>().color = new Color(0.25f, 0.20f, 0.12f);
                // Lid
                C("Lid", p, new Vector2(0, 56f), new Vector2(144f, 12f))
                    .gameObject.AddComponent<Image>().color = new Color(0.32f, 0.26f, 0.16f);
                // Lid highlight edge
                C("LidEdge", p, new Vector2(0, 61f), new Vector2(144f, 2f))
                    .gameObject.AddComponent<Image>().color = new Color(0.42f, 0.34f, 0.20f);
                // Plank lines on body
                for (int pl = 0; pl < 3; pl++)
                    C("Plank" + pl, p, new Vector2(0, 10f - pl * 16f), new Vector2(136f, 1f))
                        .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.14f, 0.08f);
                // Corner brackets — 4 corners (two rects each making an L)
                float[,] brk = { { -62f, 50f }, { 62f, 50f }, { -62f, -6f }, { 62f, -6f } };
                for (int b = 0; b < 4; b++)
                {
                    float bx = brk[b, 0], by = brk[b, 1];
                    int   sx  = bx < 0 ? 1 : -1;
                    C("BrkH" + b, p, new Vector2(bx + sx * 8, by), new Vector2(18f, 4f))
                        .gameObject.AddComponent<Image>().color = new Color(0.50f, 0.44f, 0.30f);
                    C("BrkV" + b, p, new Vector2(bx, by - 8), new Vector2(4f, 16f))
                        .gameObject.AddComponent<Image>().color = new Color(0.50f, 0.44f, 0.30f);
                }
                // Center clasp
                C("Clasp", p, new Vector2(0, 28f), new Vector2(22f, 16f))
                    .gameObject.AddComponent<Image>().color = new Color(0.58f, 0.50f, 0.34f);
                C("ClaspSlot", p, new Vector2(0, 28f), new Vector2(8f, 6f))
                    .gameObject.AddComponent<Image>().color = new Color(0.16f, 0.12f, 0.08f);
                // Leather straps
                C("StrapL", p, new Vector2(-50f, 22f), new Vector2(7f, 60f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.13f, 0.08f);
                C("StrapR", p, new Vector2(50f, 22f), new Vector2(7f, 60f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.13f, 0.08f);
                break;
            }
            case 2: // Wall safe — embedded in a wall section
            {
                // Wall section background (simulates embedded)
                C("Wall", p, new Vector2(0, 28f), new Vector2(148f, 128f))
                    .gameObject.AddComponent<Image>().color = new Color(0.14f, 0.13f, 0.16f);
                // Wall texture lines
                C("WallLine1", p, new Vector2(0, 82f), new Vector2(140f, 1f))
                    .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f);
                C("WallLine2", p, new Vector2(0, -12f), new Vector2(140f, 1f))
                    .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f);
                // Torn wallpaper flap (reveals the safe)
                var wp = C("Wallpaper", p, new Vector2(-30f, 72f), new Vector2(60f, 20f));
                wp.eulerAngles = new Vector3(0, 0, -8f);
                wp.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.18f, 0.14f);
                // Safe door frame (outer)
                C("DoorFrame", p, new Vector2(0, 28f), new Vector2(106f, 100f))
                    .gameObject.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f);
                // Safe door
                C("Door", p, new Vector2(0, 28f), new Vector2(98f, 92f))
                    .gameObject.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.23f);
                // Dial ring (outer)
                C("DialRing", p, new Vector2(-8f, 32f), new Vector2(60f, 60f))
                    .gameObject.AddComponent<Image>().color = new Color(0.38f, 0.34f, 0.26f);
                // Dial face
                C("DialFace", p, new Vector2(-8f, 32f), new Vector2(48f, 48f))
                    .gameObject.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.18f);
                // Dial center pip
                C("DialPip", p, new Vector2(-8f, 32f), new Vector2(8f, 8f))
                    .gameObject.AddComponent<Image>().color = new Color(0.50f, 0.45f, 0.34f);
                // Dial notch (indicator at top of ring)
                C("DialNotch", p, new Vector2(-8f, 60f), new Vector2(6f, 8f))
                    .gameObject.AddComponent<Image>().color = new Color(0.55f, 0.50f, 0.38f);
                // Handle bar (right side, L-shape)
                C("HandleH", p, new Vector2(38f, 34f), new Vector2(18f, 6f))
                    .gameObject.AddComponent<Image>().color = new Color(0.48f, 0.44f, 0.34f);
                C("HandleV", p, new Vector2(44f, 28f), new Vector2(6f, 18f))
                    .gameObject.AddComponent<Image>().color = new Color(0.48f, 0.44f, 0.34f);
                // Bolt indicators (left edge dots)
                for (int b = 0; b < 3; b++)
                    C("Bolt" + b, p, new Vector2(-46f, 44f - b * 16f), new Vector2(5f, 5f))
                        .gameObject.AddComponent<Image>().color = new Color(0.32f, 0.30f, 0.26f);
                // Status LED
                C("LED", p, new Vector2(38f, 58f), new Vector2(7f, 7f))
                    .gameObject.AddComponent<Image>().color = new Color(0.80f, 0.12f, 0.08f);
                break;
            }
        }
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
