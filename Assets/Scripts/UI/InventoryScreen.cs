using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InventoryScreen : MonoBehaviour
{
    public static InventoryScreen Instance { get; private set; }

    Canvas        _canvas;
    Image[]       _slotBg      = new Image[8];
    Text[]        _slotNameTxt = new Text[8];
    Text[]        _slotQtyTxt  = new Text[8];
    Image[]       _slotHighlight= new Image[8];

    Text   _infoName;
    Text   _infoDesc;
    Button _useBtn;
    Button _dropBtn;

    int _selectedSlot = -1;

    static readonly Color ColResource    = new Color(1f,   0.85f, 0.1f);
    static readonly Color ColConsumable  = new Color(0.2f, 0.85f, 0.35f);
    static readonly Color ColUtility     = new Color(0.2f, 0.6f,  1f);
    static readonly Color ColWeapon      = new Color(1f,   0.25f, 0.25f);
    static readonly Color ColEmpty       = new Color(0.12f,0.12f, 0.18f);
    static readonly Color ColHighlight   = new Color(1f,   0.82f, 0.1f,  0.28f);

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
        if (!UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame) return;

        if (_canvas.gameObject.activeSelf)
        {
            Close();
            return;
        }

        // Only open if no higher-priority screen is active
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c != _canvas && c.sortingOrder > 15 && c.gameObject.activeSelf) return;

        Open();
    }

    // ── Public ───────────────────────────────────────────────────────────────

    public void Open()
    {
        RefreshSlots();
        SelectSlot(-1);
        _canvas.gameObject.SetActive(true);
    }

    public void Close()
    {
        _canvas.gameObject.SetActive(false);
    }

    // ── Slot Logic ───────────────────────────────────────────────────────────

    void SelectSlot(int idx)
    {
        _selectedSlot = idx;

        for (int i = 0; i < 8; i++)
            _slotHighlight[i].gameObject.SetActive(i == idx);

        if (idx < 0 || Inventory.Instance == null)
        {
            SetInfoStrip(null);
            return;
        }

        SetInfoStrip(Inventory.Instance.GetSlot(idx));
    }

    void RefreshSlots()
    {
        if (Inventory.Instance == null) return;

        for (int i = 0; i < 8; i++)
        {
            var item = Inventory.Instance.GetSlot(i);
            if (item == null)
            {
                _slotBg[i].color      = ColEmpty;
                _slotNameTxt[i].text  = "";
                _slotQtyTxt[i].text   = "";
            }
            else
            {
                _slotBg[i].color      = CategoryColor(item.category) * 0.28f + ColEmpty * 0.72f;
                _slotNameTxt[i].text  = item.displayName;
                _slotQtyTxt[i].text   = item.stackable && item.quantity > 1 ? item.quantity.ToString() : "";
            }
        }

        // Refresh info strip if a slot is selected
        if (_selectedSlot >= 0)
            SetInfoStrip(Inventory.Instance.GetSlot(_selectedSlot));
    }

    void SetInfoStrip(InventoryItem item)
    {
        if (item == null)
        {
            _infoName.text        = "Select a slot";
            _infoDesc.text        = "";
            _useBtn.interactable  = false;
            _dropBtn.interactable = false;
            return;
        }

        _infoName.text        = item.displayName;
        _infoDesc.text        = ItemDescription(item.id);
        _dropBtn.interactable = true;

        bool canUse = item.category == ItemCategory.Consumable ||
                      item.category == ItemCategory.UtilityTool;
        _useBtn.interactable = canUse;
    }

    void UseSelected()
    {
        if (_selectedSlot < 0 || Inventory.Instance == null) return;
        var item = Inventory.Instance.GetSlot(_selectedSlot);
        if (item == null) return;

        bool consumed = ApplyUseEffect(item);
        if (consumed)
        {
            Inventory.Instance.Remove(item.id, 1);
            SelectSlot(-1);
        }
    }

    void DropSelected()
    {
        if (_selectedSlot < 0 || Inventory.Instance == null) return;
        Inventory.Instance.DropSlot(_selectedSlot);
        SelectSlot(-1);
    }

    static bool ApplyUseEffect(InventoryItem item)
    {
        if (PlayerStats.Instance == null) return false;

        switch (item.id)
        {
            case "medkit": PlayerStats.Instance.Heal(30); return true;
            case "food":   PlayerStats.Instance.Heal(10); return true;
            case "pills":  PlayerStats.Instance.Heal(15); return true;
            case "meds":   PlayerStats.Instance.Heal(20); return true;
            default: return false;
        }
    }

    static string ItemDescription(string id) => id switch
    {
        "ammo"     => "Firearm ammunition. Keep well-stocked.",
        "scrap"    => "Salvaged metal. Useful for crafting.",
        "meds"     => "Medical supplies. Restores 20 HP.",
        "battery"  => "Powers flashlights and devices.",
        "medkit"   => "Full med kit. Restores 30 HP.",
        "food"     => "Rations. Restores 10 HP.",
        "pills"    => "Pain pills. Restores 15 HP.",
        "lockpick" => "Pick a locked door or container.",
        "pistol"   => "Reliable sidearm. 9mm.",
        "shotgun"  => "Heavy firepower. Close range.",
        "knife"    => "Silent and brutal.",
        _          => ""
    };

    static Color CategoryColor(ItemCategory cat) => cat switch
    {
        ItemCategory.Resource    => ColResource,
        ItemCategory.Consumable  => ColConsumable,
        ItemCategory.UtilityTool => ColUtility,
        ItemCategory.Weapon      => ColWeapon,
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
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        // Main panel
        var panel = CenterRect("Panel", canvasGO.transform, Vector2.zero, new Vector2(780, 440));
        panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.97f);

        // Title
        var titleRT = MakeRect("Title", panel);
        CenterAnchor(titleRT, new Vector2(0, 185), new Vector2(740, 42));
        var titleTxt = titleRT.gameObject.AddComponent<Text>();
        titleTxt.text      = "INVENTORY";
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.fontSize  = 32;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color     = new Color(0.9f, 0.82f, 0.5f);
        titleTxt.font      = DefaultFont();

        // Tab hint
        var hintRT = MakeRect("Hint", panel);
        CenterAnchor(hintRT, new Vector2(330, 185), new Vector2(100, 30));
        var hintTxt = hintRT.gameObject.AddComponent<Text>();
        hintTxt.text      = "[TAB]";
        hintTxt.alignment = TextAnchor.MiddleRight;
        hintTxt.fontSize  = 18;
        hintTxt.color     = new Color(0.5f, 0.5f, 0.5f);
        hintTxt.font      = DefaultFont();

        // Slot grid: 4 cols × 2 rows, slot 160×90, gap 12
        float slotW = 160f, slotH = 90f, gapX = 12f, gapY = 12f;
        float gridW = 4 * slotW + 3 * gapX;
        float startX = -gridW / 2f + slotW / 2f;
        float startY = 80f;

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            float x = startX + col * (slotW + gapX);
            float y = startY - row * (slotH + gapY);

            BuildSlot(panel, i, new Vector2(x, y), new Vector2(slotW, slotH));
        }

        // Info strip
        BuildInfoStrip(panel);
    }

    void BuildSlot(RectTransform parent, int idx, Vector2 pos, Vector2 size)
    {
        var slotRT = CenterRect("Slot" + idx, parent, pos, size);
        var bg     = slotRT.gameObject.AddComponent<Image>();
        bg.color   = ColEmpty;
        _slotBg[idx] = bg;

        // Highlight overlay (separate GO — Image would conflict)
        var hlRT = MakeRect("Highlight", slotRT);
        hlRT.anchorMin = Vector2.zero;
        hlRT.anchorMax = Vector2.one;
        hlRT.sizeDelta = Vector2.zero;
        var hlImg      = hlRT.gameObject.AddComponent<Image>();
        hlImg.color    = ColHighlight;
        hlRT.gameObject.SetActive(false);
        _slotHighlight[idx] = hlImg;

        // Item name text
        var nameRT  = MakeRect("Name", slotRT);
        CenterAnchor(nameRT, new Vector2(0, 8), new Vector2(size.x - 12, 28));
        var nameTxt = nameRT.gameObject.AddComponent<Text>();
        nameTxt.text      = "";
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.fontSize  = 17;
        nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.color     = Color.white;
        nameTxt.font      = DefaultFont();
        _slotNameTxt[idx] = nameTxt;

        // Quantity badge (bottom-right)
        var qtyRT  = MakeRect("Qty", slotRT);
        CenterAnchor(qtyRT, new Vector2(size.x / 2f - 16f, -size.y / 2f + 12f), new Vector2(36, 22));
        var qtyTxt = qtyRT.gameObject.AddComponent<Text>();
        qtyTxt.text      = "";
        qtyTxt.alignment = TextAnchor.MiddleCenter;
        qtyTxt.fontSize  = 15;
        qtyTxt.color     = new Color(1f, 0.85f, 0.2f);
        qtyTxt.font      = DefaultFont();
        _slotQtyTxt[idx] = qtyTxt;

        // Click button
        var btn = slotRT.gameObject.AddComponent<Button>();
        var bc  = btn.colors;
        bc.highlightedColor = new Color(0.22f, 0.22f, 0.30f);
        bc.pressedColor     = new Color(0.30f, 0.28f, 0.12f);
        btn.colors = bc;
        int capture = idx;
        btn.onClick.AddListener(() => SelectSlot(capture));
    }

    void BuildInfoStrip(RectTransform parent)
    {
        var strip    = CenterRect("InfoStrip", parent, new Vector2(0, -158), new Vector2(740, 100));
        var stripImg = strip.gameObject.AddComponent<Image>();
        stripImg.color = new Vector4(0.09f, 0.09f, 0.14f, 1f);

        // Item name
        var nameRT = MakeRect("ItemName", strip);
        CenterAnchor(nameRT, new Vector2(-160, 26), new Vector2(380, 30));
        _infoName      = nameRT.gameObject.AddComponent<Text>();
        _infoName.text = "Select a slot";
        _infoName.alignment = TextAnchor.MiddleLeft;
        _infoName.fontSize  = 22;
        _infoName.fontStyle = FontStyle.Bold;
        _infoName.color     = new Color(0.9f, 0.88f, 0.7f);
        _infoName.font      = DefaultFont();

        // Description
        var descRT = MakeRect("ItemDesc", strip);
        CenterAnchor(descRT, new Vector2(-160, -10), new Vector2(380, 30));
        _infoDesc      = descRT.gameObject.AddComponent<Text>();
        _infoDesc.text = "";
        _infoDesc.alignment = TextAnchor.MiddleLeft;
        _infoDesc.fontSize  = 16;
        _infoDesc.color     = new Color(0.65f, 0.65f, 0.65f);
        _infoDesc.font      = DefaultFont();

        // USE button
        var useGO = CenterRect("UseBtn", strip, new Vector2(270, 22), new Vector2(110, 38));
        useGO.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.45f, 0.20f);
        _useBtn = useGO.gameObject.AddComponent<Button>();
        var uc  = _useBtn.colors;
        uc.highlightedColor = new Color(0.18f, 0.6f, 0.28f);
        uc.pressedColor     = new Color(0.08f, 0.3f, 0.13f);
        uc.disabledColor    = new Color(0.18f, 0.18f, 0.22f);
        _useBtn.colors      = uc;
        _useBtn.interactable = false;
        _useBtn.onClick.AddListener(UseSelected);
        var useLbl = MakeRect("Lbl", useGO);
        useLbl.anchorMin = Vector2.zero; useLbl.anchorMax = Vector2.one; useLbl.sizeDelta = Vector2.zero;
        var useTxt = useLbl.gameObject.AddComponent<Text>();
        useTxt.text = "USE"; useTxt.alignment = TextAnchor.MiddleCenter;
        useTxt.fontSize = 20; useTxt.fontStyle = FontStyle.Bold;
        useTxt.color = Color.white; useTxt.font = DefaultFont();

        // DROP button
        var dropGO = CenterRect("DropBtn", strip, new Vector2(270, -22), new Vector2(110, 38));
        dropGO.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.12f, 0.12f);
        _dropBtn = dropGO.gameObject.AddComponent<Button>();
        var dc   = _dropBtn.colors;
        dc.highlightedColor = new Color(0.6f, 0.18f, 0.18f);
        dc.pressedColor     = new Color(0.3f, 0.08f, 0.08f);
        dc.disabledColor    = new Color(0.18f, 0.18f, 0.22f);
        _dropBtn.colors     = dc;
        _dropBtn.interactable = false;
        _dropBtn.onClick.AddListener(DropSelected);
        var dropLbl = MakeRect("Lbl", dropGO);
        dropLbl.anchorMin = Vector2.zero; dropLbl.anchorMax = Vector2.one; dropLbl.sizeDelta = Vector2.zero;
        var dropTxt = dropLbl.gameObject.AddComponent<Text>();
        dropTxt.text = "DROP"; dropTxt.alignment = TextAnchor.MiddleCenter;
        dropTxt.fontSize = 20; dropTxt.fontStyle = FontStyle.Bold;
        dropTxt.color = Color.white; dropTxt.font = DefaultFont();
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
