using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    // Dice panel
    Text _die1Text;
    Text _die2Text;
    Text _sumText;
    Text _loopText;
    Button _rollButton;
    bool _rolling;

    // HUD
    Text _hpText;
    Text _ammoText;

    // HUD equipment bar
    static readonly string[] EquipLabels = { "PRIMARY", "SECONDARY", "DEFENSE", "UTILITY" };
    static readonly Color[]  EquipSlotColors = {
        new Color(0.88f, 0.22f, 0.22f),
        new Color(0.90f, 0.55f, 0.15f),
        new Color(0.20f, 0.55f, 0.88f),
        new Color(0.15f, 0.75f, 0.68f),
    };
    Image[]       _equipSlotBg   = new Image[4];
    Text[]        _equipSlotName = new Text[4];
    Image[]       _equipDots     = new Image[4];
    RectTransform _hpBarFill;
    Image         _hpBarFillImg;
    Coroutine     _hpAnimCo;

    // Tile notification
    Text _notifText;
    Coroutine _notifRoutine;

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        new GameObject("PlayerEquipment").AddComponent<PlayerEquipment>();
        new GameObject("CombatScreen").AddComponent<CombatScreen>();
        new GameObject("GameOverScreen").AddComponent<GameOverScreen>();
        new GameObject("StoryScreen").AddComponent<StoryScreen>();
        new GameObject("LootCarScreen").AddComponent<LootCarScreen>();
        new GameObject("LootVendingScreen").AddComponent<LootVendingScreen>();
        new GameObject("BlackjackScreen").AddComponent<BlackjackScreen>();
        new GameObject("Inventory").AddComponent<Inventory>();
        new GameObject("InventoryScreen").AddComponent<InventoryScreen>();
        new GameObject("WinScreen").AddComponent<WinScreen>();
        new GameObject("SafeHouseScreen").AddComponent<SafeHouseScreen>();
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoopCompleted += UpdateLoopDisplay;
            GameManager.Instance.OnTileLanded    += ShowTileNotification;
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += RefreshHUD;
            RefreshHUD();
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnChanged += RefreshHUD;
            RefreshHUD();
        }

        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.OnChanged += RefreshEquipmentBar;

        if (PlayerToken.Instance != null)
            PlayerToken.Instance.OnLandedOnTile += tile =>
                GameManager.Instance?.HandleTileLanding(tile);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoopCompleted -= UpdateLoopDisplay;
            GameManager.Instance.OnTileLanded    -= ShowTileNotification;
        }
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= RefreshHUD;
        if (Inventory.Instance != null)
            Inventory.Instance.OnChanged -= RefreshHUD;
        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.OnChanged -= RefreshEquipmentBar;
    }

    // ── UI Construction ─────────────────────────────────────────────────────

    void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("DiceCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildDicePanel(canvasGO.transform);
        BuildHUDPanel(canvasGO.transform);
        BuildNotification(canvasGO.transform);
    }

    void BuildDicePanel(Transform canvas)
    {
        var panel    = MakeRect("DicePanel", canvas);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        Anchor(panel, new Vector2(1,0), new Vector2(1,0), new Vector2(1,0),
               new Vector2(-20,20), new Vector2(340,255));

        // Loop counter
        var loopGO = MakeRect("LoopCounter", panel);
        _loopText  = loopGO.gameObject.AddComponent<Text>();
        _loopText.text      = "Lap  0";
        _loopText.alignment = TextAnchor.MiddleCenter;
        _loopText.fontSize  = 22;
        _loopText.color     = new Color(0.6f, 0.9f, 1f);
        _loopText.font      = DefaultFont();
        Anchor(loopGO, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
               new Vector2(0,-4), new Vector2(0,30));

        // Die 1
        var die1GO = MakeRect("Die1", panel);
        _die1Text  = die1GO.gameObject.AddComponent<Text>();
        StyleDieText(_die1Text, "?");
        Anchor(die1GO, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0,0.5f),
               new Vector2(30,10), new Vector2(100,100));

        // Plus
        var plusGO   = MakeRect("Plus", panel);
        var plusText = plusGO.gameObject.AddComponent<Text>();
        StyleDieText(plusText, "+");
        plusText.fontSize = 40;
        plusText.color    = new Color(0.7f,0.7f,0.7f);
        Anchor(plusGO, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
               new Vector2(0,10), new Vector2(40,80));

        // Die 2
        var die2GO = MakeRect("Die2", panel);
        _die2Text  = die2GO.gameObject.AddComponent<Text>();
        StyleDieText(_die2Text, "?");
        Anchor(die2GO, new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(1,0.5f),
               new Vector2(-30,10), new Vector2(100,100));

        // Sum
        var sumGO = MakeRect("Sum", panel);
        _sumText  = sumGO.gameObject.AddComponent<Text>();
        _sumText.text      = "";
        _sumText.alignment = TextAnchor.MiddleCenter;
        _sumText.fontSize  = 26;
        _sumText.fontStyle = FontStyle.Bold;
        _sumText.color     = new Color(1f,0.85f,0.1f);
        _sumText.font      = DefaultFont();
        Anchor(sumGO, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0),
               new Vector2(0,68), new Vector2(0,36));

        // Roll button
        var btnGO  = MakeRect("RollButton", panel);
        var btnImg = btnGO.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.7f,0.12f,0.12f,1f);
        _rollButton  = btnGO.gameObject.AddComponent<Button>();
        var colors   = _rollButton.colors;
        colors.highlightedColor = new Color(0.85f,0.2f,0.2f);
        colors.pressedColor     = new Color(0.5f,0.08f,0.08f);
        _rollButton.colors = colors;
        Anchor(btnGO, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0),
               new Vector2(0,8), new Vector2(0,55));

        var labelGO  = MakeRect("Label", btnGO);
        var labelTxt = labelGO.gameObject.AddComponent<Text>();
        labelTxt.text      = "ROLL";
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.fontSize  = 28;
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.color     = Color.white;
        labelTxt.font      = DefaultFont();
        Anchor(labelGO, Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
               Vector2.zero, Vector2.zero);

        _rollButton.onClick.AddListener(() =>
        {
            if (!_rolling) StartCoroutine(RollDice());
        });
    }

    void BuildHUDPanel(Transform canvas)
    {
        const float pad   = 8f;
        const float panW  = 280f;
        const float hpH   = 22f;
        const float ammoH = 22f;
        const float slotH = 36f;

        float yHp    = 11f;
        float yAmmo  = yHp + hpH + 5f;
        float ySep   = yAmmo + ammoH + 4f;
        float ySlots = ySep + 1f + 6f;
        float panH   = ySlots + 4 * slotH + pad;

        var panel = MakeRect("HUDPanel", canvas);
        panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        Anchor(panel, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
               new Vector2(20,-20), new Vector2(panW, panH));

        // Top accent stripe
        var acc = MakeRect("Accent", panel);
        Anchor(acc, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
               Vector2.zero, new Vector2(0, 3));
        acc.gameObject.AddComponent<Image>().color = new Color(0.60f, 0.08f, 0.08f);

        // HP bar track
        var hpTrack = MakeRect("HpTrack", panel);
        Anchor(hpTrack, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
               new Vector2(pad, -yHp), new Vector2(-pad*2, hpH));
        hpTrack.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.04f, 0.04f);

        // HP fill (width animated via anchorMax.x)
        var fill = MakeRect("HpFill", hpTrack);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.sizeDelta = Vector2.zero;
        _hpBarFillImg = fill.gameObject.AddComponent<Image>();
        _hpBarFillImg.color = new Color(0.15f, 0.82f, 0.28f);
        _hpBarFill = fill;

        // "HP" label overlaid on bar left
        var hpLbl = MakeRect("HpLbl", hpTrack);
        hpLbl.anchorMin = new Vector2(0,0); hpLbl.anchorMax = new Vector2(0.4f,1);
        hpLbl.sizeDelta = Vector2.zero; hpLbl.anchoredPosition = new Vector2(5,0);
        var hpLblT = hpLbl.gameObject.AddComponent<Text>();
        hpLblT.text = "HP"; hpLblT.font = DefaultFont(); hpLblT.fontSize = 12;
        hpLblT.fontStyle = FontStyle.Bold; hpLblT.alignment = TextAnchor.MiddleLeft;
        hpLblT.color = new Color(1f,1f,1f,0.50f);

        // HP numbers overlaid on bar right
        var hpNum = MakeRect("HpNum", hpTrack);
        hpNum.anchorMin = new Vector2(0.55f,0); hpNum.anchorMax = new Vector2(1,1);
        hpNum.sizeDelta = new Vector2(-4,0); hpNum.anchoredPosition = Vector2.zero;
        _hpText = hpNum.gameObject.AddComponent<Text>();
        _hpText.text = "100 / 100"; _hpText.font = DefaultFont(); _hpText.fontSize = 12;
        _hpText.fontStyle = FontStyle.Bold; _hpText.alignment = TextAnchor.MiddleRight;
        _hpText.color = new Color(1f,1f,1f,0.80f);

        // Ammo row
        var ammo = MakeRect("Ammo", panel);
        Anchor(ammo, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
               new Vector2(pad, -yAmmo), new Vector2(-pad*2, ammoH));
        _ammoText = ammo.gameObject.AddComponent<Text>();
        _ammoText.text = "AMMO  0"; _ammoText.font = DefaultFont();
        _ammoText.fontSize = 15; _ammoText.alignment = TextAnchor.MiddleLeft;
        _ammoText.color = new Color(0.88f, 0.72f, 0.28f);

        // Separator
        var sep = MakeRect("Sep", panel);
        Anchor(sep, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
               new Vector2(0,-ySep), new Vector2(-pad*2, 1));
        sep.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.08f, 0.08f, 0.70f);

        // Equipment rows
        for (int i = 0; i < 4; i++)
        {
            float y = ySlots + i * slotH;
            var slot = MakeRect("Slot" + i, panel);
            _equipSlotBg[i] = slot.gameObject.AddComponent<Image>();
            _equipSlotBg[i].color = Color.clear;
            Anchor(slot, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
                   new Vector2(0,-y), new Vector2(0, slotH));

            // Colored indicator dot
            var dot = MakeRect("Dot", slot);
            Anchor(dot, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(0.5f,0.5f),
                   new Vector2(14,0), new Vector2(7,7));
            _equipDots[i] = dot.gameObject.AddComponent<Image>();
            _equipDots[i].color = new Color(0.18f, 0.18f, 0.22f);

            // Slot label ("PRIMARY" etc)
            var lbl = MakeRect("Lbl", slot);
            Anchor(lbl, new Vector2(0,0), new Vector2(0,1), new Vector2(0,0.5f),
                   new Vector2(25,0), new Vector2(82,0));
            var lblT = lbl.gameObject.AddComponent<Text>();
            lblT.text = EquipLabels[i]; lblT.font = DefaultFont();
            lblT.fontSize = 10; lblT.alignment = TextAnchor.MiddleLeft;
            lblT.color = new Color(0.30f, 0.30f, 0.38f);

            // Item name
            var name = MakeRect("Name", slot);
            Anchor(name, new Vector2(0,0), new Vector2(1,1), new Vector2(0,0.5f),
                   new Vector2(108,0), new Vector2(-116,0));
            _equipSlotName[i] = name.gameObject.AddComponent<Text>();
            _equipSlotName[i].text = "—"; _equipSlotName[i].font = DefaultFont();
            _equipSlotName[i].fontSize = 15; _equipSlotName[i].fontStyle = FontStyle.Bold;
            _equipSlotName[i].alignment = TextAnchor.MiddleLeft;
            _equipSlotName[i].color = new Color(0.22f, 0.22f, 0.28f);
        }
    }

    void RefreshEquipmentBar()
    {
        if (PlayerEquipment.Instance == null) return;
        var equip = PlayerEquipment.Instance;
        for (int i = 0; i < 4; i++)
        {
            var item = equip.Get((EquipSlot)i);
            bool  has    = item != null;
            Color accent = EquipSlotColors[i];
            _equipSlotName[i].text  = has ? item.displayName.ToUpper() : "—";
            _equipSlotName[i].color = has ? accent : new Color(0.22f, 0.22f, 0.28f);
            if (_equipDots[i] != null)
                _equipDots[i].color = has ? accent : new Color(0.18f, 0.18f, 0.22f);
            _equipSlotBg[i].color   = has
                ? new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.40f)
                : Color.clear;
        }
    }

    void BuildNotification(Transform canvas)
    {
        var go     = MakeRect("TileNotif", canvas);
        _notifText = go.gameObject.AddComponent<Text>();
        _notifText.text      = "";
        _notifText.alignment = TextAnchor.MiddleCenter;
        _notifText.fontSize  = 52;
        _notifText.fontStyle = FontStyle.Bold;
        _notifText.color     = new Color(1,1,1,0);
        _notifText.font      = DefaultFont();
        Anchor(go, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
               new Vector2(0,60), new Vector2(600,80));
    }

    // ── Callbacks ────────────────────────────────────────────────────────────

    void UpdateLoopDisplay(int count)
    {
        if (_loopText != null) _loopText.text = "Lap  " + count;
    }

    void RefreshHUD()
    {
        if (PlayerStats.Instance != null)
        {
            var s = PlayerStats.Instance;
            if (_hpText != null) _hpText.text = s.hp + " / " + s.maxHp;
            if (_hpBarFill != null)
            {
                float target = s.maxHp > 0 ? (float)s.hp / s.maxHp : 0f;
                if (_hpAnimCo != null) StopCoroutine(_hpAnimCo);
                _hpAnimCo = StartCoroutine(AnimateHpBar(target));
            }
        }
        if (_ammoText != null)
            _ammoText.text = "AMMO  " + (Inventory.Instance?.Count("ammo") ?? 0);
    }

    IEnumerator AnimateHpBar(float target)
    {
        float from = _hpBarFill.anchorMax.x;
        float t = 0f, dur = 0.38f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float v = Mathf.Clamp01(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / dur)));
            _hpBarFill.anchorMax = new Vector2(v, 1f);
            _hpBarFillImg.color  = v > 0.5f
                ? Color.Lerp(new Color(0.88f,0.78f,0.10f), new Color(0.15f,0.82f,0.28f), (v-0.5f)*2f)
                : Color.Lerp(new Color(0.85f,0.12f,0.12f), new Color(0.88f,0.78f,0.10f), v*2f);
            yield return null;
        }
        _hpBarFill.anchorMax = new Vector2(target, 1f);
    }

    void ShowTileNotification(TileType type)
    {
        if (_notifRoutine != null) StopCoroutine(_notifRoutine);
        _notifRoutine = StartCoroutine(NotifRoutine(type));
    }

    IEnumerator NotifRoutine(TileType type)
    {
        string label = type switch
        {
            TileType.Combat    => "COMBAT!",
            TileType.Loot      => "LOOT!",
            TileType.Boss      => "BOSS FIGHT!",
            TileType.SafeHouse => "SAFE HOUSE",
            _                  => ""
        };

        Color col = type switch
        {
            TileType.Combat    => new Color(1f,  0.25f, 0.25f),
            TileType.Loot      => new Color(1f,  0.85f, 0.2f),
            TileType.Boss      => new Color(0.8f,0.3f,  1f),
            TileType.SafeHouse => new Color(0.9f,0.75f, 0.35f),
            _                  => Color.clear
        };

        if (string.IsNullOrEmpty(label)) yield break;

        _notifText.text  = label;
        _notifText.color = col;

        // Hold
        yield return new WaitForSeconds(1.2f);

        // Fade out
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            _notifText.color = new Color(col.r, col.g, col.b, 1f - (t / 0.5f));
            yield return null;
        }
        _notifText.color = new Color(col.r, col.g, col.b, 0f);
    }

    // ── Dice Roll ────────────────────────────────────────────────────────────

    IEnumerator RollDice()
    {
        _rolling = true;
        _rollButton.interactable = false;
        _sumText.text = "";

        float elapsed  = 0f;
        float duration = 1f;
        float interval = 0.07f;
        float nextFlip = 0f;

        while (elapsed < duration)
        {
            if (elapsed >= nextFlip)
            {
                _die1Text.text = Random.Range(1,7).ToString();
                _die2Text.text = Random.Range(1,7).ToString();
                nextFlip = elapsed + interval;
                interval = Mathf.Lerp(0.07f, 0.22f, elapsed / duration);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        int die1 = Random.Range(1,7);
        int die2 = Random.Range(1,7);
        _die1Text.text  = die1.ToString();
        _die2Text.text  = die2.ToString();
        _die1Text.color = new Color(1f,0.85f,0.1f);
        _die2Text.color = new Color(1f,0.85f,0.1f);
        _sumText.text   = "Total: " + (die1 + die2);

        yield return new WaitForSeconds(0.3f);

        if (PlayerToken.Instance != null)
            yield return StartCoroutine(PlayerToken.Instance.MoveSteps(die1 + die2));

        if (GameManager.Instance != null && GameManager.Instance.BossFightPending)
        {
            GameManager.Instance.ClearBossFightPending();
            yield return new WaitForSeconds(0.5f);
            if (PlayerToken.Instance != null)
                yield return StartCoroutine(PlayerToken.Instance.MoveToBoss());
            if (CombatScreen.Instance != null)
            {
                yield return new WaitForSeconds(0.4f);
                yield return StartCoroutine(CombatScreen.Instance.Open("The Horde Boss", 45, 4));
            }

            // Boss fight resolved — player alive means boss is dead
            _rolling = false;
            if (PlayerStats.Instance != null && PlayerStats.Instance.hp > 0)
                WinScreen.Instance?.Show();
            yield break;
        }
        else if (PlayerToken.Instance != null && BoardGenerator.Instance != null)
        {
            var landed = BoardGenerator.Instance.Tiles[PlayerToken.Instance.CurrentTile];
            if (landed.tileType == TileType.Combat && CombatScreen.Instance != null)
            {
                landed.EnemyMarker?.SetActive(false);
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(CombatScreen.Instance.Open());
            }
            else if (landed.tileType == TileType.Story && StoryScreen.Instance != null)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(StoryScreen.Instance.Open());
            }
            else if (landed.tileType == TileType.Blackjack && BlackjackScreen.Instance != null)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(BlackjackScreen.Instance.Open());
            }
            else if (landed.tileType == TileType.Loot)
            {
                yield return new WaitForSeconds(0.3f);
                if (Random.value < 0.5f && LootCarScreen.Instance != null)
                    yield return StartCoroutine(LootCarScreen.Instance.Open());
                else if (LootVendingScreen.Instance != null)
                    yield return StartCoroutine(LootVendingScreen.Instance.Open());
            }
            else if (landed.tileType == TileType.SafeHouse && SafeHouseScreen.Instance != null)
            {
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(SafeHouseScreen.Instance.Open());
            }
        }

        // Don't re-enable roll if player is dead (game over screen took over)
        if (PlayerStats.Instance != null && PlayerStats.Instance.hp <= 0)
        {
            _rolling = false;
            yield break;
        }

        yield return new WaitForSeconds(0.3f);

        _die1Text.color = Color.white;
        _die2Text.color = Color.white;
        _rollButton.interactable = true;
        _rolling = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void StyleDieText(Text t, string val)
    {
        t.text      = val;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize  = 64;
        t.fontStyle = FontStyle.Bold;
        t.color     = Color.white;
        t.font      = DefaultFont();
    }

    RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta       = size;
    }

    Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
