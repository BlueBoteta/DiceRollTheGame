using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LootVendingScreen : MonoBehaviour
{
    public static LootVendingScreen Instance { get; private set; }

    Canvas        _canvas;
    RectTransform _machineRT;
    RectTransform _fallingItem;
    RectTransform _progressFill;
    Text          _messageText;
    Button        _continueBtn;
    bool          _done;
    bool          _holding;
    bool          _activated;

    static readonly Vector2 MachineBase = new Vector2(-195f, 0f);
    static readonly Vector2 ItemStart   = new Vector2(-42f, -80f);  // local to machine
    static readonly Vector2 ItemEnd     = new Vector2(  0f, -140f); // local to machine (tray)

    static readonly string[] VendingLootPool =
    {
        "food","food","food","pills","pills","meds","meds","ammo","ammo","battery"
    };

    static readonly string[] Messages =
    {
        "B4 drops.\n\nDiet Cola Zero. You haven't had electricity in days.\nThe can is somehow still cold.\n\nYou drink it in one go. Worth it.",
        "Protein bar. 2019 vintage.\nWrapper reads: 'GAINS OR GRAVE'\n\nBoth seem equally likely right now.\nYou eat it anyway.",
        "A small plush toy wobbles out of slot C3.\nYou stare at it. It stares back.\nYou put it in your pocket.\n\nMorale +1. Survival odds: unchanged.",
        "Beef jerky. Expired 2021.\nYou compare it against your own expiration date.\n\nBeef jerky wins.\nYou eat it without hesitation.",
        "Hot Cheetos. Somehow still crispy.\nYou didn't survive the outbreak for this.\nYou absolutely survived the outbreak for this.\n\nNo regrets.",
        "A banana. Fresh. In a vending machine. In the apocalypse.\n\nYou don't question it.\nSome miracles are sacred.",
    };

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open()
    {
        _done = _activated = _holding = false;
        _messageText.text = "";
        _fallingItem.anchoredPosition = ItemStart;
        _fallingItem.gameObject.SetActive(false);
        _progressFill.anchorMax = new Vector2(0f, 1f);
        _machineRT.anchoredPosition = MachineBase;
        _continueBtn.gameObject.SetActive(false);
        _canvas.gameObject.SetActive(true);

        yield return StartCoroutine(HoldRoutine());

        while (!_done)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    IEnumerator HoldRoutine()
    {
        float held = 0f;
        while (!_activated)
        {
            if (_holding)
            {
                held += Time.deltaTime;
                float shake = Mathf.Sin(Time.time * 30f) * Mathf.Lerp(0f, 9f, held / 2f);
                _machineRT.anchoredPosition = new Vector2(MachineBase.x + shake, MachineBase.y);
                _progressFill.anchorMax     = new Vector2(Mathf.Clamp01(held / 2f), 1f);

                if (held >= 2f)
                {
                    _activated = true;
                    _machineRT.anchoredPosition = MachineBase;
                    StartCoroutine(DropSequence());
                }
            }
            else if (held > 0f)
            {
                held = Mathf.Max(0f, held - Time.deltaTime * 3f);
                _progressFill.anchorMax     = new Vector2(held / 2f, 1f);
                _machineRT.anchoredPosition = MachineBase;
            }
            yield return null;
        }
    }

    IEnumerator DropSequence()
    {
        _fallingItem.gameObject.SetActive(true);

        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            _fallingItem.anchoredPosition = Vector2.Lerp(ItemStart, ItemEnd, p * p); // ease-in
            yield return null;
        }
        _fallingItem.anchoredPosition = ItemEnd;

        // Small bounce
        float bt = 0f, bdur = 0.3f;
        while (bt < bdur)
        {
            bt += Time.deltaTime;
            float bounce = Mathf.Abs(Mathf.Sin(bt / bdur * Mathf.PI * 2.5f)) * 10f * (1f - bt / bdur);
            _fallingItem.anchoredPosition = ItemEnd + new Vector2(0, bounce);
            yield return null;
        }
        _fallingItem.anchoredPosition = ItemEnd;

        yield return new WaitForSeconds(0.3f);

        // Pick and give loot
        string lootId   = VendingLootPool[Random.Range(0, VendingLootPool.Length)];
        int    qty      = lootId == "ammo" ? Random.Range(1, 4) : 1;

        string msg = Messages[Random.Range(0, Messages.Length)];
        foreach (char c in msg)
        {
            _messageText.text += c;
            yield return new WaitForSeconds(c == '\n' ? 0.09f : 0.028f);
        }

        yield return new WaitForSeconds(0.3f);
        if (LootPickupPrompt.Instance != null)
            yield return StartCoroutine(LootPickupPrompt.Instance.Show(lootId, qty));

        _continueBtn.gameObject.SetActive(true);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("LootVendingCanvas");
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

        var panel = Ctr("Panel", canvasGO.transform, Vector2.zero, new Vector2(920, 540));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.98f);

        var brd = Rt("Border", panel);
        brd.anchorMin = new Vector2(0, 1); brd.anchorMax = new Vector2(1, 1);
        brd.pivot = new Vector2(0.5f, 1); brd.sizeDelta = new Vector2(0, 3);
        brd.anchoredPosition = Vector2.zero;
        brd.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.55f, 0.22f);

        Txt(Ctr("Title", panel, new Vector2(0, 242), new Vector2(860, 46)),
            "YOU FOUND A VENDING MACHINE!", 27, FontStyle.Bold,
            new Color(0.35f, 0.80f, 0.40f), TextAnchor.MiddleCenter);

        _machineRT = BuildMachine(panel);

        // Falling item — child of machine so it shakes with it, then drops in local space
        var fi = Ctr("FallingItem", _machineRT, ItemStart, new Vector2(28f, 44f));
        fi.gameObject.AddComponent<Image>().color = new Color(0.65f, 0.15f, 0.15f);
        Ctr("CanLine1", fi, new Vector2(0,  8), new Vector2(22f, 3f))
            .gameObject.AddComponent<Image>().color = new Color(0.85f, 0.25f, 0.25f);
        Ctr("CanLine2", fi, new Vector2(0, -4), new Vector2(22f, 3f))
            .gameObject.AddComponent<Image>().color = new Color(0.45f, 0.06f, 0.06f);
        _fallingItem = fi;
        fi.gameObject.SetActive(false);

        // Hold button + progress bar (below machine)
        var holdRT = Ctr("HoldBtn", panel, new Vector2(MachineBase.x, -252f), new Vector2(220f, 50f));
        holdRT.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.28f);
        Txt(Ctr("HoldLbl", holdRT, Vector2.zero, holdRT.sizeDelta),
            "HOLD TO SHAKE", 21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var et = holdRT.gameObject.AddComponent<EventTrigger>();
        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => _holding = true);
        et.triggers.Add(down);
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => _holding = false);
        et.triggers.Add(up);

        var progBg = Ctr("ProgBg", panel, new Vector2(MachineBase.x, -222f), new Vector2(220f, 9f));
        progBg.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f);
        var fill = Rt("Fill", progBg);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
        fill.sizeDelta = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.88f, 0.45f);
        _progressFill = fill;
        _progressFill.anchorMax = new Vector2(0f, 1f);

        // Message panel (right side)
        var msgBg = Ctr("MsgBg", panel, new Vector2(230f, -20f), new Vector2(400f, 390f));
        msgBg.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.07f);
        var msgTR = Ctr("MsgText", msgBg, Vector2.zero, new Vector2(370f, 368f));
        _messageText = msgTR.gameObject.AddComponent<Text>();
        _messageText.fontSize           = 21;
        _messageText.lineSpacing        = 1.45f;
        _messageText.color              = new Color(0.88f, 0.80f, 0.65f);
        _messageText.alignment          = TextAnchor.UpperLeft;
        _messageText.font               = DefaultFont();
        _messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _messageText.verticalOverflow   = VerticalWrapMode.Overflow;

        var contRT = Ctr("ContBtn", panel, new Vector2(320f, -238f), new Vector2(160f, 44f));
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

    RectTransform BuildMachine(RectTransform panel)
    {
        var body = Ctr("VendingMachine", panel, MachineBase, new Vector2(185f, 340f));
        body.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.18f, 0.22f);

        // Header
        Ctr("Header", body, new Vector2(0, 149), new Vector2(185f, 44f))
            .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);
        Txt(Ctr("HeaderTxt", body, new Vector2(0, 149), new Vector2(175f, 38f)),
            "VEND-O-MATIC", 14, FontStyle.Bold, new Color(0.35f, 0.88f, 0.42f), TextAnchor.MiddleCenter);

        // Screen
        Ctr("Screen", body, new Vector2(0, 105), new Vector2(155f, 48f))
            .gameObject.AddComponent<Image>().color = new Color(0.05f, 0.18f, 0.10f);
        Txt(Ctr("ScreenTxt", body, new Vector2(0, 105), new Vector2(148f, 42f)),
            "INSERT COIN\n> SELECT ITEM", 11, FontStyle.Normal,
            new Color(0.25f, 0.90f, 0.38f), TextAnchor.MiddleCenter);

        // Item slots 2x3
        Color[] colors =
        {
            new Color(0.65f, 0.15f, 0.15f), new Color(0.65f, 0.15f, 0.15f),
            new Color(0.55f, 0.45f, 0.10f), new Color(0.55f, 0.45f, 0.10f),
            new Color(0.28f, 0.22f, 0.52f), new Color(0.55f, 0.35f, 0.14f),
        };
        Vector2[] pos =
        {
            new Vector2(-44, 52), new Vector2(44, 52),
            new Vector2(-44,-18), new Vector2(44,-18),
            new Vector2(-44,-88), new Vector2(44,-88),
        };
        for (int i = 0; i < 6; i++)
        {
            var slot = Ctr("Slot" + i, body, pos[i], new Vector2(70f, 62f));
            slot.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);
            Ctr("Item" + i, slot, Vector2.zero, new Vector2(26f, 42f))
                .gameObject.AddComponent<Image>().color = colors[i];
        }

        // Separators
        Ctr("SepH1", body, new Vector2(0, 20),   new Vector2(162f, 2f))
            .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);
        Ctr("SepH2", body, new Vector2(0, -50),  new Vector2(162f, 2f))
            .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);
        Ctr("SepV",  body, new Vector2(0, -18),  new Vector2(2f, 186f))
            .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);

        // Coin slot
        Ctr("CoinSlot", body, new Vector2(62f, -118f), new Vector2(18f, 8f))
            .gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f);

        // Delivery tray
        Ctr("Tray", body, new Vector2(0, -150f), new Vector2(162f, 24f))
            .gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f);
        Ctr("TrayOpen", body, new Vector2(0, -140f), new Vector2(115f, 12f))
            .gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f);

        // Glass sheen
        Ctr("Glass", body, new Vector2(0, -18f), new Vector2(162f, 186f))
            .gameObject.AddComponent<Image>().color = new Color(0.6f, 0.85f, 1f, 0.05f);

        return body;
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
