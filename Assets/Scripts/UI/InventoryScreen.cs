using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScreen : MonoBehaviour
{
    public static InventoryScreen Instance { get; private set; }

    Canvas        _canvas;
    CanvasGroup   _panelCG;
    RectTransform _panelRT;

    Image[]       _slotBg        = new Image[8];
    RectTransform[] _slotRT      = new RectTransform[8];
    Text[]        _slotNameTxt   = new Text[8];
    Text[]        _slotQtyTxt    = new Text[8];
    Text[]        _slotCatTxt    = new Text[8];
    Image[]       _slotHighlight = new Image[8];
    Image[]       _slotAccent    = new Image[8];

    Text   _infoName;
    Text   _infoDesc;
    Text   _infoCat;
    Text   _feedbackText;
    Button _useBtn;
    Button _dropBtn;
    Text   _useBtnTxt;

    // Drop quantity picker
    RectTransform _dropQtyPanel;
    Text          _dropQtyNum;
    int           _dropQty;
    int           _dropQtyMax;

    int  _selectedSlot = -1;
    bool _animating;

    static readonly Color ColResource   = new Color(1f,   0.82f, 0.1f);
    static readonly Color ColConsumable = new Color(0.22f,0.85f, 0.38f);
    static readonly Color ColUtility    = new Color(0.2f, 0.62f, 1f);
    static readonly Color ColWeapon     = new Color(1f,   0.22f, 0.22f);
    static readonly Color ColArmor      = new Color(0.4f, 0.72f, 0.88f);
    static readonly Color ColEmpty      = new Color(0.10f,0.10f, 0.15f);

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnChanged += RefreshSlots;
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnChanged -= RefreshSlots;
    }

    void Update()
    {
        if (_animating) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (_canvas.gameObject.activeSelf)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                StartCoroutine(CloseAnim());
            return;
        }

        if (!kb.tabKey.wasPressedThisFrame) return;
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c != _canvas && c.sortingOrder > 15 && c.gameObject.activeSelf) return;
        StartCoroutine(OpenAnim());
    }

    // ── Public ───────────────────────────────────────────────────────────────

    public void Open()  => StartCoroutine(OpenAnim());
    public void Close() => StartCoroutine(CloseAnim());

    // ── Animations ───────────────────────────────────────────────────────────

    IEnumerator OpenAnim()
    {
        _animating = true;
        RefreshSlots();
        SelectSlot(-1);
        ClearFeedback();
        _canvas.gameObject.SetActive(true);

        _panelCG.alpha = 0f;
        _panelRT.anchoredPosition = new Vector2(0, -24f);

        float t = 0f, dur = 0.16f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _panelCG.alpha = p;
            _panelRT.anchoredPosition = Vector2.Lerp(new Vector2(0, -24f), Vector2.zero, p);
            yield return null;
        }
        _panelCG.alpha = 1f;
        _panelRT.anchoredPosition = Vector2.zero;
        _animating = false;
    }

    IEnumerator CloseAnim()
    {
        _animating = true;
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            _panelCG.alpha = 1f - p;
            _panelRT.anchoredPosition = new Vector2(0, -16f * p);
            yield return null;
        }
        _canvas.gameObject.SetActive(false);
        _animating = false;
    }

    IEnumerator PulseSlot(int idx)
    {
        var rt = _slotRT[idx];
        Vector3 origin = Vector3.one;
        float t = 0f, dur = 0.14f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            float s = p < 0.5f ? Mathf.Lerp(1f, 1.06f, p / 0.5f)
                                : Mathf.Lerp(1.06f, 1f, (p - 0.5f) / 0.5f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator FeedbackFade(string msg, Color col)
    {
        _feedbackText.text  = msg;
        _feedbackText.color = col;
        yield return new WaitForSecondsRealtime(1.0f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            _feedbackText.color = new Color(col.r, col.g, col.b, 1f - t / 0.4f);
            yield return null;
        }
        _feedbackText.text = "";
    }

    // ── Slot Logic ───────────────────────────────────────────────────────────

    void SelectSlot(int idx)
    {
        _selectedSlot = idx;

        for (int i = 0; i < 8; i++)
        {
            _slotHighlight[i].gameObject.SetActive(i == idx);
            _slotAccent[i].gameObject.SetActive(i == idx);
        }

        if (idx >= 0) StartCoroutine(PulseSlot(idx));

        SetInfoStrip(idx >= 0 ? Inventory.Instance?.GetSlot(idx) : null);
    }

    void RefreshSlots()
    {
        if (Inventory.Instance == null) return;

        for (int i = 0; i < 8; i++)
        {
            var item = Inventory.Instance.GetSlot(i);
            if (item == null)
            {
                _slotBg[i].color     = ColEmpty;
                _slotNameTxt[i].text = "";
                _slotQtyTxt[i].text  = "";
                _slotCatTxt[i].text  = "";
            }
            else
            {
                Color cat = CategoryColor(item.category);
                _slotBg[i].color     = cat * 0.18f + ColEmpty * 0.82f;
                _slotNameTxt[i].text = item.displayName;
                _slotQtyTxt[i].text  = item.stackable ? item.quantity.ToString() : "";
                _slotCatTxt[i].text  = CategoryTag(item.category);
                _slotCatTxt[i].color = cat * 0.75f;
            }
        }

        if (_selectedSlot >= 0)
            SetInfoStrip(Inventory.Instance.GetSlot(_selectedSlot));
    }

    void SetInfoStrip(InventoryItem item)
    {
        if (item == null)
        {
            _infoName.text       = "NO ITEM SELECTED";
            _infoName.color      = new Color(0.4f, 0.4f, 0.5f);
            _infoDesc.text       = "Click a slot to inspect its contents.";
            _infoCat.text        = "";
            _useBtn.interactable  = false;
            _dropBtn.interactable = false;
            _useBtnTxt.text      = "USE";
            return;
        }

        Color catCol = CategoryColor(item.category);
        _infoName.text  = item.displayName.ToUpper();
        _infoName.color = catCol;
        _infoDesc.text  = ItemDescription(item.id);
        _infoCat.text   = CategoryTag(item.category);
        _infoCat.color  = catCol * 0.8f;

        _dropBtn.interactable = true;

        bool isHeal     = IsHealingItem(item.id);
        bool isEquipItem = item.equipSlot.HasValue;
        bool isEquipped  = isEquipItem &&
            PlayerEquipment.Instance?.Get(item.equipSlot.Value)?.id == item.id;

        bool canUse = isEquipItem ||
                      item.category == ItemCategory.Consumable ||
                      item.category == ItemCategory.UtilityTool || isHeal;
        _useBtn.interactable = canUse;

        if (isEquipItem)
        {
            _useBtnTxt.text  = isEquipped ? "UNEQUIP" : "EQUIP";
            _useBtnTxt.color = isEquipped ? new Color(0.7f,0.7f,0.7f) : new Color(0.45f,1f,0.55f);
        }
        else
        {
            _useBtnTxt.text  = "USE";
            _useBtnTxt.color = Color.white;
        }
    }

    void UseSelected()
    {
        if (_selectedSlot < 0 || Inventory.Instance == null) return;
        var item = Inventory.Instance.GetSlot(_selectedSlot);
        if (item == null) return;

        // Equipment items → equip or unequip
        if (item.equipSlot.HasValue) { HandleEquip(item); return; }

        var (consumed, feedback) = ApplyUseEffect(item);
        if (consumed)
        {
            Inventory.Instance.Remove(item.id, 1);
            SelectSlot(-1);
            ShowFeedback("+HP", new Color(0.3f, 1f, 0.4f));
        }
        else if (!string.IsNullOrEmpty(feedback))
            ShowFeedback(feedback, new Color(1f, 0.5f, 0.2f));
    }

    void HandleEquip(InventoryItem item)
    {
        var equip = PlayerEquipment.Instance;
        if (equip == null) return;
        var slot    = item.equipSlot.Value;
        var current = equip.Get(slot);

        if (current != null && current.id == item.id)
        {
            // Unequip: return item to inventory
            var unequipped = equip.Unequip(slot);
            if (unequipped != null) Inventory.Instance.Add(unequipped.id, unequipped.quantity);
            SelectSlot(-1);
            ShowFeedback("Unequipped " + item.displayName, new Color(0.7f, 0.7f, 0.7f));
        }
        else
        {
            // Equip: remove from inventory, displace current into inventory
            var equipped   = item.Clone();
            Inventory.Instance.Remove(item.id, 1);
            var displaced  = equip.Equip(slot, equipped);
            if (displaced != null) Inventory.Instance.Add(displaced.id, displaced.quantity);
            SelectSlot(-1);
            ShowFeedback("Equipped " + item.displayName, new Color(0.45f, 1f, 0.55f));
        }
    }

    void DropSelected()
    {
        if (_selectedSlot < 0 || Inventory.Instance == null) return;
        var item = Inventory.Instance.GetSlot(_selectedSlot);
        if (item == null) return;

        if (item.stackable && item.quantity > 1)
        {
            ShowDropQtyPanel(item.quantity);
            return;
        }
        ExecuteDrop(1);
    }

    void ShowDropQtyPanel(int max)
    {
        _dropQty    = 1;
        _dropQtyMax = max;
        UpdateDropQtyNum();
        _dropQtyPanel.gameObject.SetActive(true);
    }

    void UpdateDropQtyNum() =>
        _dropQtyNum.text = _dropQty.ToString();

    void ExecuteDrop(int qty)
    {
        if (_selectedSlot < 0 || Inventory.Instance == null) return;
        var item = Inventory.Instance.GetSlot(_selectedSlot);
        if (item == null) return;

        Inventory.Instance.Remove(item.id, qty);

        // If slot is now empty, deselect
        var remaining = Inventory.Instance.GetSlot(_selectedSlot);
        if (remaining == null) SelectSlot(-1);
        else                   SetInfoStrip(remaining);
    }

    void ShowFeedback(string msg, Color col)
    {
        StopCoroutine("FeedbackFade");
        StartCoroutine(FeedbackFade(msg, col));
    }

    void ClearFeedback() => _feedbackText.text = "";

    static bool IsHealingItem(string id) =>
        id == "medkit" || id == "food" || id == "pills" || id == "meds";

    static (bool consumed, string feedback) ApplyUseEffect(InventoryItem item)
    {
        if (PlayerStats.Instance == null) return (false, "");

        if (IsHealingItem(item.id))
        {
            if (PlayerStats.Instance.hp >= PlayerStats.Instance.maxHp)
                return (false, "Already at full HP.");
            int heal = item.id == "medkit" ? 30
                     : item.id == "meds"   ? 20
                     : item.id == "pills"  ? 15 : 10;
            PlayerStats.Instance.Heal(heal);
            return (true, "");
        }

        return (false, "");
    }

    static string ItemDescription(string id) => id switch
    {
        "ammo"     => "Count them. Every round is a decision.\nMake them count.",
        "scrap"    => "Bent metal, cracked plastic, broken things.\nThis world runs on broken things now.",
        "meds"     => "Gauze soaked brown. Needle and thread.\nRestores 20 HP. Hurts. Good.",
        "battery"  => "Still has juice. So do you.\nDon't waste either.",
        "medkit"   => "Red cross. White lie.\nRestores 30 HP. Hold together a little longer.",
        "food"     => "Expired rations. Tastes like cardboard\nand regret. Restores 10 HP. Eat it.",
        "pills"    => "No label. Could be painkillers.\nCould be anything. Restores 15 HP. Works.",
        "lockpick" => "Thin wire and a steady hand.\nSome doors don't want to open. Make them.",
        "pistol"   => "Cold. Reliable. Doesn't ask questions.\nNeither should you.",
        "shotgun"  => "Loud. Final.\nLeaves nothing behind.",
        "knife"    => "Up close. Personal.\nThe quietest way.",
        "vest"     => "Kevlar, duct tape, and prayer.\nBlocks 3 damage per hit. Better than nothing.",
        "helmet"   => "Dented. Someone wore this before you.\nBlocks 2 damage per hit. Don't ask what happened.",
        "riot_gear"=> "Heavy. Hot. Miserable.\nBlocks 5 damage per hit. Worth every ounce.",
        _          => ""
    };

    static string CategoryTag(ItemCategory cat) => cat switch
    {
        ItemCategory.Resource    => "RESOURCE",
        ItemCategory.Consumable  => "CONSUMABLE",
        ItemCategory.UtilityTool => "TOOL",
        ItemCategory.Weapon      => "WEAPON",
        ItemCategory.Armor       => "ARMOR",
        _                        => ""
    };

    static Color CategoryColor(ItemCategory cat) => cat switch
    {
        ItemCategory.Resource    => ColResource,
        ItemCategory.Consumable  => ColConsumable,
        ItemCategory.UtilityTool => ColUtility,
        ItemCategory.Weapon      => ColWeapon,
        ItemCategory.Armor       => ColArmor,
        _                        => Color.white
    };

    // ── UI Construction ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("InventoryCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 15;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dim overlay
        var dim = MakeRect("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // Panel
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        _panelRT = panelGO.AddComponent<RectTransform>();
        _panelRT.anchorMin = _panelRT.anchorMax = _panelRT.pivot = new Vector2(0.5f, 0.5f);
        _panelRT.anchoredPosition = Vector2.zero;
        _panelRT.sizeDelta = new Vector2(860, 500);
        _panelCG = panelGO.AddComponent<CanvasGroup>();
        panelGO.AddComponent<Image>().color = new Color(0.055f, 0.055f, 0.09f, 0.98f);

        // Top accent bar
        var topBar = MakeRect("TopBar", _panelRT);
        topBar.anchorMin = new Vector2(0,1); topBar.anchorMax = new Vector2(1,1);
        topBar.pivot = new Vector2(0.5f,1); topBar.sizeDelta = new Vector2(0,3);
        topBar.anchoredPosition = Vector2.zero;
        topBar.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.45f, 0.12f);

        // Title row
        var titleRT = MakeRect("Title", _panelRT);
        CenterAnchor(titleRT, new Vector2(-80, 220), new Vector2(500, 44));
        var titleTxt = titleRT.gameObject.AddComponent<Text>();
        titleTxt.text = "INVENTORY"; titleTxt.alignment = TextAnchor.MiddleLeft;
        titleTxt.fontSize = 34; titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(0.88f, 0.78f, 0.42f); titleTxt.font = DefaultFont();

        var hintRT = MakeRect("Hint", _panelRT);
        CenterAnchor(hintRT, new Vector2(300, 220), new Vector2(130, 30));
        var hintTxt = hintRT.gameObject.AddComponent<Text>();
        hintTxt.text = "[TAB] / [ESC]"; hintTxt.alignment = TextAnchor.MiddleRight;
        hintTxt.fontSize = 16; hintTxt.color = new Color(0.35f, 0.35f, 0.42f);
        hintTxt.font = DefaultFont();

        // X close button — far left of title row
        var xRT  = CenterRect("CloseBtn", _panelRT, new Vector2(-406, 218), new Vector2(38, 38));
        var xImg = xRT.gameObject.AddComponent<Image>();
        xImg.color = new Color(0.16f, 0.06f, 0.06f);
        var xBtn = xRT.gameObject.AddComponent<Button>();
        var xc   = xBtn.colors;
        xc.highlightedColor = new Color(0.30f, 0.10f, 0.10f);
        xc.pressedColor     = new Color(0.09f, 0.03f, 0.03f);
        xBtn.colors = xc;
        xBtn.onClick.AddListener(() => { if (!_animating) StartCoroutine(CloseAnim()); });
        var xLblRT  = MakeRect("Lbl", xRT);
        xLblRT.anchorMin = xLblRT.anchorMax = xLblRT.pivot = new Vector2(0.5f,0.5f);
        xLblRT.anchoredPosition = Vector2.zero; xLblRT.sizeDelta = new Vector2(38,38);
        var xLblT = xLblRT.gameObject.AddComponent<Text>();
        xLblT.text = "\u00d7"; xLblT.font = DefaultFont(); xLblT.fontSize = 26;
        xLblT.fontStyle = FontStyle.Bold; xLblT.alignment = TextAnchor.MiddleCenter;
        xLblT.color = new Color(0.72f, 0.28f, 0.28f);

        // Separator under title
        var sep = MakeRect("Sep", _panelRT);
        sep.anchorMin = new Vector2(0.03f,0.5f); sep.anchorMax = new Vector2(0.97f,0.5f);
        sep.pivot = new Vector2(0.5f,0.5f); sep.sizeDelta = new Vector2(0,1);
        sep.anchoredPosition = new Vector2(0, 193);
        sep.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.20f, 0.12f);

        // Slot grid  4 cols × 2 rows
        float slotW = 175f, slotH = 100f, gapX = 10f, gapY = 10f;
        float gridW = 4 * slotW + 3 * gapX;
        float startX = -gridW / 2f + slotW / 2f;

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4, row = i / 4;
            float x = startX + col * (slotW + gapX);
            float y = 110f   - row * (slotH + gapY);
            BuildSlot(_panelRT, i, new Vector2(x, y), new Vector2(slotW, slotH));
        }

        BuildInfoStrip(_panelRT);
        BuildDropQtyPanel(_panelRT);

        // Feedback text (above info strip)
        var fbRT = MakeRect("Feedback", _panelRT);
        CenterAnchor(fbRT, new Vector2(0, -128), new Vector2(820, 28));
        _feedbackText = fbRT.gameObject.AddComponent<Text>();
        _feedbackText.text = ""; _feedbackText.alignment = TextAnchor.MiddleCenter;
        _feedbackText.fontSize = 19; _feedbackText.fontStyle = FontStyle.Bold;
        _feedbackText.color = Color.clear; _feedbackText.font = DefaultFont();
    }

    void BuildSlot(RectTransform parent, int idx, Vector2 pos, Vector2 size)
    {
        var slotRT = CenterRect("Slot" + idx, parent, pos, size);
        _slotRT[idx] = slotRT;

        var bg     = slotRT.gameObject.AddComponent<Image>();
        bg.color   = ColEmpty;
        _slotBg[idx] = bg;

        // Left accent strip (colored by category when selected)
        var accentRT = MakeRect("Accent", slotRT);
        accentRT.anchorMin = new Vector2(0,0.1f); accentRT.anchorMax = new Vector2(0,0.9f);
        accentRT.pivot = new Vector2(0,0.5f); accentRT.sizeDelta = new Vector2(3,0);
        accentRT.anchoredPosition = new Vector2(3,0);
        var accentImg = accentRT.gameObject.AddComponent<Image>();
        accentImg.color = ColResource;
        accentRT.gameObject.SetActive(false);
        _slotAccent[idx] = accentImg;

        // Highlight overlay
        var hlRT = MakeRect("Highlight", slotRT);
        hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one; hlRT.sizeDelta = Vector2.zero;
        var hlImg = hlRT.gameObject.AddComponent<Image>();
        hlImg.color = new Color(1f, 0.85f, 0.1f, 0.12f);
        hlRT.gameObject.SetActive(false);
        _slotHighlight[idx] = hlImg;

        // Category tag (top-left)
        var catRT = MakeRect("Cat", slotRT);
        catRT.anchorMin = new Vector2(0,1); catRT.anchorMax = new Vector2(0,1);
        catRT.pivot = new Vector2(0,1); catRT.sizeDelta = new Vector2(90,18);
        catRT.anchoredPosition = new Vector2(6,-4);
        var catTxt = catRT.gameObject.AddComponent<Text>();
        catTxt.text = ""; catTxt.alignment = TextAnchor.MiddleLeft;
        catTxt.fontSize = 11; catTxt.fontStyle = FontStyle.Bold;
        catTxt.color = new Color(0.5f,0.5f,0.5f); catTxt.font = DefaultFont();
        _slotCatTxt[idx] = catTxt;

        // Item name
        var nameRT = MakeRect("Name", slotRT);
        CenterAnchor(nameRT, new Vector2(0, 10), new Vector2(size.x - 16, 30));
        var nameTxt = nameRT.gameObject.AddComponent<Text>();
        nameTxt.text = ""; nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.fontSize = 18; nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.color = Color.white; nameTxt.font = DefaultFont();
        _slotNameTxt[idx] = nameTxt;

        // Quantity badge (bottom-right)
        var qtyBgRT = MakeRect("QtyBg", slotRT);
        qtyBgRT.anchorMin = new Vector2(1,0); qtyBgRT.anchorMax = new Vector2(1,0);
        qtyBgRT.pivot = new Vector2(1,0); qtyBgRT.sizeDelta = new Vector2(32,20);
        qtyBgRT.anchoredPosition = new Vector2(-4,4);
        qtyBgRT.gameObject.AddComponent<Image>().color = new Color(0f,0f,0f,0.45f);
        var qtyRT = MakeRect("Qty", qtyBgRT);
        qtyRT.anchorMin = Vector2.zero; qtyRT.anchorMax = Vector2.one; qtyRT.sizeDelta = Vector2.zero;
        var qtyTxt = qtyRT.gameObject.AddComponent<Text>();
        qtyTxt.text = ""; qtyTxt.alignment = TextAnchor.MiddleCenter;
        qtyTxt.fontSize = 14; qtyTxt.fontStyle = FontStyle.Bold;
        qtyTxt.color = new Color(1f, 0.88f, 0.25f); qtyTxt.font = DefaultFont();
        _slotQtyTxt[idx] = qtyTxt;

        // Click button
        var btn = slotRT.gameObject.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = Color.white;
        bc.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        bc.pressedColor     = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = bc;
        int capture = idx;
        btn.onClick.AddListener(() => SelectSlot(capture));
    }

    void BuildInfoStrip(RectTransform parent)
    {
        var strip = CenterRect("InfoStrip", parent, new Vector2(0, -172), new Vector2(820, 130));
        strip.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.11f, 1f);

        // Left border accent
        var lb = MakeRect("LeftBorder", strip);
        lb.anchorMin = new Vector2(0,0.1f); lb.anchorMax = new Vector2(0,0.9f);
        lb.pivot = new Vector2(0,0.5f); lb.sizeDelta = new Vector2(3,0);
        lb.anchoredPosition = Vector2.zero;
        lb.gameObject.AddComponent<Image>().color = new Color(0.55f,0.45f,0.12f);

        // Category tag
        var catRT = MakeRect("Cat", strip);
        CenterAnchor(catRT, new Vector2(-210, 44), new Vector2(160, 20));
        _infoCat = catRT.gameObject.AddComponent<Text>();
        _infoCat.text = ""; _infoCat.alignment = TextAnchor.MiddleLeft;
        _infoCat.fontSize = 13; _infoCat.fontStyle = FontStyle.Bold;
        _infoCat.color = new Color(0.5f,0.5f,0.5f); _infoCat.font = DefaultFont();

        // Item name
        var nameRT = MakeRect("ItemName", strip);
        CenterAnchor(nameRT, new Vector2(-130, 18), new Vector2(460, 36));
        _infoName = nameRT.gameObject.AddComponent<Text>();
        _infoName.text = "NO ITEM SELECTED"; _infoName.alignment = TextAnchor.MiddleLeft;
        _infoName.fontSize = 26; _infoName.fontStyle = FontStyle.Bold;
        _infoName.color = new Color(0.4f,0.4f,0.5f); _infoName.font = DefaultFont();

        // Description (wrapping)
        var descRT = MakeRect("ItemDesc", strip);
        CenterAnchor(descRT, new Vector2(-120, -22), new Vector2(460, 52));
        _infoDesc = descRT.gameObject.AddComponent<Text>();
        _infoDesc.text = "Click a slot to inspect its contents.";
        _infoDesc.alignment = TextAnchor.UpperLeft;
        _infoDesc.fontSize = 15; _infoDesc.lineSpacing = 1.3f;
        _infoDesc.color = new Color(0.58f, 0.55f, 0.48f); _infoDesc.font = DefaultFont();
        _infoDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
        _infoDesc.verticalOverflow   = VerticalWrapMode.Overflow;

        // USE button
        var useGO = CenterRect("UseBtn", strip, new Vector2(335, 26), new Vector2(120, 44));
        useGO.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.40f, 0.18f);
        _useBtn = useGO.gameObject.AddComponent<Button>();
        var uc = _useBtn.colors;
        uc.highlightedColor = new Color(0.16f, 0.58f, 0.26f);
        uc.pressedColor     = new Color(0.07f, 0.28f, 0.12f);
        uc.disabledColor    = new Color(0.14f, 0.14f, 0.18f);
        _useBtn.colors = uc; _useBtn.interactable = false;
        _useBtn.onClick.AddListener(UseSelected);
        var useLbl = MakeRect("Lbl", useGO);
        useLbl.anchorMin = Vector2.zero; useLbl.anchorMax = Vector2.one; useLbl.sizeDelta = Vector2.zero;
        _useBtnTxt = useLbl.gameObject.AddComponent<Text>();
        _useBtnTxt.text = "USE"; _useBtnTxt.alignment = TextAnchor.MiddleCenter;
        _useBtnTxt.fontSize = 21; _useBtnTxt.fontStyle = FontStyle.Bold;
        _useBtnTxt.color = Color.white; _useBtnTxt.font = DefaultFont();

        // DROP button
        var dropGO = CenterRect("DropBtn", strip, new Vector2(335, -26), new Vector2(120, 44));
        dropGO.gameObject.AddComponent<Image>().color = new Color(0.40f, 0.10f, 0.10f);
        _dropBtn = dropGO.gameObject.AddComponent<Button>();
        var dc = _dropBtn.colors;
        dc.highlightedColor = new Color(0.58f, 0.16f, 0.16f);
        dc.pressedColor     = new Color(0.28f, 0.07f, 0.07f);
        dc.disabledColor    = new Color(0.14f, 0.14f, 0.18f);
        _dropBtn.colors = dc; _dropBtn.interactable = false;
        _dropBtn.onClick.AddListener(DropSelected);
        var dropLbl = MakeRect("Lbl", dropGO);
        dropLbl.anchorMin = Vector2.zero; dropLbl.anchorMax = Vector2.one; dropLbl.sizeDelta = Vector2.zero;
        var dropTxt = dropLbl.gameObject.AddComponent<Text>();
        dropTxt.text = "DROP"; dropTxt.alignment = TextAnchor.MiddleCenter;
        dropTxt.fontSize = 21; dropTxt.fontStyle = FontStyle.Bold;
        dropTxt.color = Color.white; dropTxt.font = DefaultFont();
    }

    void BuildDropQtyPanel(RectTransform parent)
    {
        // Sits exactly over the info strip — same position / size
        var panel = CenterRect("DropQtyPanel", parent, new Vector2(0, -172), new Vector2(820, 130));
        panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.98f);
        _dropQtyPanel = panel;

        // Left border accent
        var lb = MakeRect("LeftBorder", panel);
        lb.anchorMin = new Vector2(0,0.1f); lb.anchorMax = new Vector2(0,0.9f);
        lb.pivot = new Vector2(0,0.5f); lb.sizeDelta = new Vector2(3,0);
        lb.anchoredPosition = Vector2.zero;
        lb.gameObject.AddComponent<Image>().color = new Color(0.72f, 0.12f, 0.12f);

        // Title
        var titleRT = MakeRect("Title", panel);
        CenterAnchor(titleRT, new Vector2(0, 42), new Vector2(500, 28));
        var titleTxt = titleRT.gameObject.AddComponent<Text>();
        titleTxt.text = "DROP HOW MANY?"; titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.fontSize = 20; titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(0.88f, 0.35f, 0.35f); titleTxt.font = DefaultFont();

        // Minus button
        var minusGO = CenterRect("Minus", panel, new Vector2(-110, -5), new Vector2(48, 48));
        minusGO.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.10f, 0.10f);
        var minusBtn = minusGO.gameObject.AddComponent<Button>();
        var mc = minusBtn.colors; mc.highlightedColor = new Color(0.5f, 0.15f, 0.15f); minusBtn.colors = mc;
        minusBtn.onClick.AddListener(() => { _dropQty = Mathf.Max(1, _dropQty - 1); UpdateDropQtyNum(); });
        var minusLbl = MakeRect("Lbl", minusGO);
        minusLbl.anchorMin = Vector2.zero; minusLbl.anchorMax = Vector2.one; minusLbl.sizeDelta = Vector2.zero;
        var mt = minusLbl.gameObject.AddComponent<Text>();
        mt.text = "-"; mt.alignment = TextAnchor.MiddleCenter; mt.fontSize = 28;
        mt.fontStyle = FontStyle.Bold; mt.color = Color.white; mt.font = DefaultFont();

        // Quantity number display
        var numRT = CenterRect("Num", panel, new Vector2(0, -5), new Vector2(80, 48));
        numRT.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f);
        var numTxtRT = MakeRect("NumTxt", numRT);
        numTxtRT.anchorMin = Vector2.zero; numTxtRT.anchorMax = Vector2.one; numTxtRT.sizeDelta = Vector2.zero;
        _dropQtyNum = numTxtRT.gameObject.AddComponent<Text>();
        _dropQtyNum.text = "1"; _dropQtyNum.alignment = TextAnchor.MiddleCenter;
        _dropQtyNum.fontSize = 26; _dropQtyNum.fontStyle = FontStyle.Bold;
        _dropQtyNum.color = new Color(1f, 0.82f, 0.2f); _dropQtyNum.font = DefaultFont();

        // Plus button
        var plusGO = CenterRect("Plus", panel, new Vector2(110, -5), new Vector2(48, 48));
        plusGO.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.30f, 0.12f);
        var plusBtn = plusGO.gameObject.AddComponent<Button>();
        var pc = plusBtn.colors; pc.highlightedColor = new Color(0.15f, 0.50f, 0.18f); plusBtn.colors = pc;
        plusBtn.onClick.AddListener(() => { _dropQty = Mathf.Min(_dropQtyMax, _dropQty + 1); UpdateDropQtyNum(); });
        var plusLbl = MakeRect("Lbl", plusGO);
        plusLbl.anchorMin = Vector2.zero; plusLbl.anchorMax = Vector2.one; plusLbl.sizeDelta = Vector2.zero;
        var pt = plusLbl.gameObject.AddComponent<Text>();
        pt.text = "+"; pt.alignment = TextAnchor.MiddleCenter; pt.fontSize = 28;
        pt.fontStyle = FontStyle.Bold; pt.color = Color.white; pt.font = DefaultFont();

        // Confirm button
        var confirmGO = CenterRect("Confirm", panel, new Vector2(270, -5), new Vector2(130, 48));
        confirmGO.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.10f, 0.10f);
        var confirmBtn = confirmGO.gameObject.AddComponent<Button>();
        var cc = confirmBtn.colors; cc.highlightedColor = new Color(0.65f, 0.15f, 0.15f); confirmBtn.colors = cc;
        confirmBtn.onClick.AddListener(() =>
        {
            _dropQtyPanel.gameObject.SetActive(false);
            ExecuteDrop(_dropQty);
        });
        var confirmLbl = MakeRect("Lbl", confirmGO);
        confirmLbl.anchorMin = Vector2.zero; confirmLbl.anchorMax = Vector2.one; confirmLbl.sizeDelta = Vector2.zero;
        var ct = confirmLbl.gameObject.AddComponent<Text>();
        ct.text = "DROP"; ct.alignment = TextAnchor.MiddleCenter; ct.fontSize = 20;
        ct.fontStyle = FontStyle.Bold; ct.color = Color.white; ct.font = DefaultFont();

        // Cancel button
        var cancelGO = CenterRect("Cancel", panel, new Vector2(270, -42), new Vector2(130, 34));
        cancelGO.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var cancelBtn = cancelGO.gameObject.AddComponent<Button>();
        var xc = cancelBtn.colors; xc.highlightedColor = new Color(0.26f, 0.26f, 0.32f); cancelBtn.colors = xc;
        cancelBtn.onClick.AddListener(() => _dropQtyPanel.gameObject.SetActive(false));
        var cancelLbl = MakeRect("Lbl", cancelGO);
        cancelLbl.anchorMin = Vector2.zero; cancelLbl.anchorMax = Vector2.one; cancelLbl.sizeDelta = Vector2.zero;
        var xt = cancelLbl.gameObject.AddComponent<Text>();
        xt.text = "CANCEL"; xt.alignment = TextAnchor.MiddleCenter; xt.fontSize = 16;
        xt.fontStyle = FontStyle.Bold; xt.color = new Color(0.6f,0.6f,0.6f); xt.font = DefaultFont();

        panel.gameObject.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform CenterRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = MakeRect(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void CenterAnchor(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
