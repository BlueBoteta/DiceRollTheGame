using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatScreen : MonoBehaviour
{
    public static CombatScreen Instance { get; private set; }

    Canvas _canvas;

    RectTransform _playerIcon;
    RectTransform _playerHpFill;
    Text          _playerHpText;

    RectTransform _enemyIcon;
    RectTransform _enemyHpFill;
    Text          _enemyHpText;
    Text          _enemyNameTxt;

    Text   _logText;
    Text   _actionText;
    Text   _lootFoundText;
    Button _exitBtn;

    bool      _exitPressed;
    int       _enemyHp, _enemyMaxHp;
    Image     _playerHpFillImg;
    Image     _enemyHpFillImg;
    Coroutine _playerBarCo;
    Coroutine _enemyBarCo;

    // ── Attack Move Definition ────────────────────────────────────────────────

    struct AttackMove
    {
        public string flash, log;
        public int minDmg, maxDmg, ammoCost;
        public Color color;
        public AttackMove(string f, string l, int mn, int mx, Color c, int ammo = 0)
        { flash = f; log = l; minDmg = mn; maxDmg = mx; color = c; ammoCost = ammo; }
    }

    static readonly Color ColGold      = new Color(1f,   0.85f, 0.15f);
    static readonly Color ColOrange    = new Color(1f,   0.55f, 0.10f);
    static readonly Color ColRed       = new Color(1f,   0.22f, 0.22f);
    static readonly Color ColDeepRed   = new Color(0.9f, 0.08f, 0.08f);
    static readonly Color ColPurple    = new Color(0.75f,0.15f, 0.85f);
    static readonly Color ColGreen     = new Color(0.28f,1f,    0.38f);

    // Pistol moves
    static readonly AttackMove[] PistolMoves =
    {
        new AttackMove("BANG!",    "fire at",    3,  7, ColGold,   ammo: 1),
        new AttackMove("FIRE!",    "shoot",      4,  8, ColGold,   ammo: 1),
        new AttackMove("UNLOAD!",  "unload on",  5, 10, ColGold,   ammo: 2),
    };
    // Shotgun moves — higher damage, more ammo
    static readonly AttackMove[] ShotgunMoves =
    {
        new AttackMove("PUMP!",    "pump-shot",  7, 12, ColGold,   ammo: 1),
        new AttackMove("BLAST!",   "blast",      9, 15, ColGold,   ammo: 2),
    };
    // Knife moves — free
    static readonly AttackMove[] KnifeMoves =
    {
        new AttackMove("STAB!",    "stab",       4,  9, ColOrange),
        new AttackMove("SLASH!",   "slash",      3,  8, ColOrange),
    };
    // No secondary equipped — bare fists
    static readonly AttackMove[] FistMoves =
    {
        new AttackMove("PUNCH!",   "punch",      1,  3, ColOrange),
        new AttackMove("SHOVE!",   "shove",      1,  4, ColOrange),
    };
    // Headshot — 8% chance with any primary, costs 1 ammo
    static readonly AttackMove HeadshotMove =
        new AttackMove("HEADSHOT!", "headshot", 12, 18, new Color(1f, 0.98f, 0.35f), ammo: 1);
    // Shotgun point-blank — 6% chance, costs 2 ammo
    static readonly AttackMove PointBlankMove =
        new AttackMove("POINT BLANK!", "fires point-blank at", 15, 22, new Color(1f, 0.95f, 0.2f), ammo: 2);

    // Enemy tiers — chosen by lap count
    static readonly AttackMove[] EnemyTier1 =   // laps 1–2: weak, slow
    {
        new AttackMove("SCRATCH",  "scratches you",   1, 3, ColRed),
        new AttackMove("SCRATCH",  "scratches you",   1, 3, ColRed),  // doubled weight
        new AttackMove("BITE",     "bites you",       2, 4, ColRed),
        new AttackMove("BITE",     "bites you",       2, 4, ColRed),
        new AttackMove("LUNGE",    "lunges at you",   1, 3, ColRed),
    };
    static readonly AttackMove[] EnemyTier2 =   // laps 3–5: medium
    {
        new AttackMove("HIT",      "hits you",        3, 6, ColRed),
        new AttackMove("SLASH",    "slashes you",     3, 7, ColRed),
        new AttackMove("GRAB",     "grabs and tears", 4, 8, ColDeepRed),
        new AttackMove("BITE",     "bites down hard", 3, 6, ColRed),
        new AttackMove("SMASH",    "smashes you",     5, 9, ColDeepRed),
    };
    static readonly AttackMove[] EnemyTier3 =   // laps 6+: brutal
    {
        new AttackMove("SMASH",    "smashes you",     5, 11, ColDeepRed),
        new AttackMove("TACKLE",   "tackles you",     6, 12, ColDeepRed),
        new AttackMove("TEAR",     "tears into you",  5, 10, ColDeepRed),
        new AttackMove("CRUSH",    "crushes you",     7, 13, ColDeepRed),
        new AttackMove("DEVOUR",   "tries to devour", 8, 14, ColDeepRed),
    };
    static readonly AttackMove[] BossMoves =
    {
        new AttackMove("FURY",     "unleashes fury on you",  8,  14, ColPurple),
        new AttackMove("SLAM",     "slams you down",         10, 16, ColPurple),
        new AttackMove("CRUSH",    "crushes you",            9,  15, ColPurple),
        new AttackMove("DEVOUR",   "tries to devour you",    7,  13, ColPurple),
        new AttackMove("RAMPAGE",  "rampages into you",      11, 17, ColPurple),
    };

    // Kill flash lines
    static readonly string[] KillLines = { "GOES DOWN.", "STAYS DOWN.", "NEUTRALIZED.", "ONE LESS." };

    // Loot pools
    static readonly string[] CombatLootPool = { "ammo","ammo","ammo","scrap","scrap","food","meds","pills","knife","vest","helmet" };
    static readonly string[] BossLootPool   = { "ammo","ammo","scrap","medkit","meds","pills","lockpick","battery","pistol","vest","riot_gear" };

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open(string enemyName = "Zombie", int enemyMaxHp = 0, int enemyAttack = 0)
    {
        int lap = GameManager.Instance != null ? GameManager.Instance.LoopCount : 0;

        // Scale enemy HP by lap — boss uses explicit value
        if (enemyMaxHp > 0)
        {
            _enemyMaxHp = enemyMaxHp;
        }
        else
        {
            int baseMin = Mathf.Min(6  + lap * 3, 20);
            int baseMax = Mathf.Min(13 + lap * 3, 28);
            _enemyMaxHp = Random.Range(baseMin, baseMax + 1);
        }

        _enemyHp        = _enemyMaxHp;
        _exitPressed    = false;
        _logText.text       = "";
        _actionText.text    = "";
        _lootFoundText.text = "";
        _exitBtn.interactable = false;
        _enemyNameTxt.text  = enemyName;

        RefreshBars();
        _canvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.45f);
        yield return StartCoroutine(RunCombat(enemyName));

        if (PlayerStats.Instance != null && PlayerStats.Instance.hp <= 0)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            GameOverScreen.Instance?.Show();
            yield break;
        }

        while (!_exitPressed)
            yield return null;

        _canvas.gameObject.SetActive(false);
    }

    IEnumerator RunCombat(string enemyName)
    {
        int lap    = GameManager.Instance != null ? GameManager.Instance.LoopCount : 0;
        bool isBoss = _enemyMaxHp >= 30;

        AttackMove[] enemyMoves = isBoss       ? BossMoves
                                : lap >= 6     ? EnemyTier3
                                : lap >= 3     ? EnemyTier2
                                :                EnemyTier1;

        Log(enemyName + " appears.  It wants your blood.");
        yield return new WaitForSeconds(0.7f);

        while (_enemyHp > 0 && PlayerStats.Instance != null && PlayerStats.Instance.hp > 0)
        {
            // ── Player attacks ──────────────────────────────────────────────
            AttackMove pm = PickPlayerMove();
            int pdmg = Random.Range(pm.minDmg, pm.maxDmg + 1);
            _enemyHp = Mathf.Max(0, _enemyHp - pdmg);
            Log("You " + pm.log + " for " + pdmg + ".");
            if (pm.ammoCost > 0 && Inventory.Instance != null)
            {
                Inventory.Instance.Remove("ammo", pm.ammoCost);
                if (Inventory.Instance.Count("ammo") == 0)
                    Log("-- Ammo depleted.  Switching to melee. --");
            }
            yield return StartCoroutine(FlashAction(pm.flash, pm.color));
            yield return StartCoroutine(Punch(_playerIcon, new Vector2(60, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.2f);

            if (_enemyHp <= 0) break;

            // ── Enemy attacks ───────────────────────────────────────────────
            AttackMove em = enemyMoves[Random.Range(0, enemyMoves.Length)];
            int rawDmg  = Random.Range(em.minDmg, em.maxDmg + 1);
            int defense = PlayerEquipment.Instance?.Get(EquipSlot.Defense)?.defense ?? 0;
            int blocked = Mathf.Min(defense, rawDmg - 1); // always deal at least 1
            int edmg    = rawDmg - blocked;
            PlayerStats.Instance.TakeDamage(edmg);
            string dmgLine = blocked > 0
                ? "  (-" + edmg + " HP, " + blocked + " blocked)."
                : "  (-" + edmg + " HP).";
            Log(enemyName + " " + em.log + dmgLine);
            string reaction = PlayerReaction(edmg);
            yield return StartCoroutine(FlashAction(em.flash, em.color));
            yield return StartCoroutine(FlashAction(reaction, ReactionColor(edmg)));
            yield return StartCoroutine(Punch(_enemyIcon, new Vector2(-60, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.2f);
        }

        if (_enemyHp <= 0)
        {
            yield return StartCoroutine(FlashAction(KillLines[Random.Range(0, KillLines.Length)], ColGreen));
            string loot = GiveCombatLoot(isBoss);
            Log("Dropped: " + loot);
            _lootFoundText.text = "LOOT:   " + loot;
            yield return new WaitForSeconds(0.3f);
            _exitBtn.interactable = true;
        }
        else
        {
            Log("You were overwhelmed.");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static AttackMove PickPlayerMove()
    {
        int ammo = Inventory.Instance?.Count("ammo") ?? 0;
        var equip     = PlayerEquipment.Instance;
        var primary   = equip?.Get(EquipSlot.Primary);
        var secondary = equip?.Get(EquipSlot.Secondary);

        AttackMove[] meleeMoves = secondary?.id == "knife" ? KnifeMoves : FistMoves;

        // No primary or out of ammo — melee only
        if (primary == null || ammo <= 0)
            return meleeMoves[Random.Range(0, meleeMoves.Length)];

        // Rare specials
        if (primary.id == "shotgun")
        {
            if (ammo >= 2 && Random.value < 0.06f) return PointBlankMove;
        }
        else if (Random.value < 0.08f) return HeadshotMove;

        // Normal round: 65% gun / 35% melee
        if (Random.value < 0.65f)
        {
            var gunMoves = primary.id == "shotgun" ? ShotgunMoves : PistolMoves;
            // Filter moves the player has ammo for
            var valid = new System.Collections.Generic.List<AttackMove>();
            foreach (var m in gunMoves)
                if (ammo >= m.ammoCost) valid.Add(m);
            if (valid.Count > 0) return valid[Random.Range(0, valid.Count)];
        }
        return meleeMoves[Random.Range(0, meleeMoves.Length)];
    }

    static string PlayerReaction(int dmg)
    {
        if (dmg <= 3)  return "SCRAPED.";
        if (dmg <= 6)  return "ARGH!";
        if (dmg <= 10) return "THAT HURT.";
        return "DAMN IT!";
    }

    static Color ReactionColor(int dmg)
    {
        if (dmg <= 3)  return new Color(1f, 0.65f, 0.3f);
        if (dmg <= 6)  return new Color(1f, 0.35f, 0.25f);
        if (dmg <= 10) return new Color(1f, 0.15f, 0.15f);
        return new Color(0.9f, 0.05f, 0.05f);
    }

    string GiveCombatLoot(bool isBoss)
    {
        if (Inventory.Instance == null) return "nothing.";
        string[] pool  = isBoss ? BossLootPool : CombatLootPool;
        int      drops = isBoss ? 2 : 1;
        var      parts = new List<string>();
        for (int i = 0; i < drops; i++)
        {
            string id  = pool[Random.Range(0, pool.Length)];
            int    qty = id == "ammo" ? Random.Range(1, 4) : 1;
            if (Inventory.Instance.Add(id, qty))
            {
                string name = ItemFactory.Create(id)?.displayName ?? id;
                parts.Add(qty > 1 ? qty + "x " + name : name);
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "nothing.  Inventory full.";
    }

    // ── Animations ───────────────────────────────────────────────────────────

    IEnumerator FlashAction(string text, Color color)
    {
        _actionText.text  = text;
        _actionText.color = color;
        _actionText.rectTransform.localScale = Vector3.one * 0.28f;

        float t = 0f;
        while (t < 0.13f)
        {
            t += Time.deltaTime;
            _actionText.rectTransform.localScale =
                Vector3.one * Mathf.Lerp(0.28f, 1.22f, Mathf.SmoothStep(0f, 1f, t / 0.13f));
            yield return null;
        }
        t = 0f;
        while (t < 0.07f)
        {
            t += Time.deltaTime;
            _actionText.rectTransform.localScale =
                Vector3.one * Mathf.Lerp(1.22f, 1f, t / 0.07f);
            yield return null;
        }
        _actionText.rectTransform.localScale = Vector3.one;
        yield return new WaitForSeconds(0.22f);

        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            _actionText.color = new Color(color.r, color.g, color.b, 1f - t / 0.15f);
            yield return null;
        }
        _actionText.text = "";
    }

    IEnumerator Punch(RectTransform rt, Vector2 dir)
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f, dur = 0.20f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = origin + dir * Mathf.Sin(t / dur * Mathf.PI);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    void RefreshBars()
    {
        float eRatio = _enemyMaxHp > 0 ? (float)_enemyHp / _enemyMaxHp : 0f;
        if (_enemyBarCo != null) StopCoroutine(_enemyBarCo);
        _enemyBarCo = StartCoroutine(AnimBar(_enemyHpFill, _enemyHpFillImg, eRatio, false));
        _enemyHpText.text = _enemyHp + " / " + _enemyMaxHp;

        if (PlayerStats.Instance != null)
        {
            float pRatio = PlayerStats.Instance.maxHp > 0
                ? (float)PlayerStats.Instance.hp / PlayerStats.Instance.maxHp : 0f;
            if (_playerBarCo != null) StopCoroutine(_playerBarCo);
            _playerBarCo = StartCoroutine(AnimBar(_playerHpFill, _playerHpFillImg, pRatio, true));
            _playerHpText.text = PlayerStats.Instance.hp + " / " + PlayerStats.Instance.maxHp;
        }
    }

    IEnumerator AnimBar(RectTransform fill, Image img, float target, bool isPlayer)
    {
        float from = fill.anchorMax.x;
        float t = 0f, dur = 0.28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float v = Mathf.Clamp01(Mathf.Lerp(from, target, Mathf.SmoothStep(0f,1f,t/dur)));
            fill.anchorMax = new Vector2(v, 1f);
            if (isPlayer && img != null)
                img.color = v > 0.5f
                    ? Color.Lerp(new Color(0.9f,0.78f,0.1f), new Color(0.2f,0.75f,0.3f), (v-0.5f)*2f)
                    : Color.Lerp(new Color(0.85f,0.12f,0.12f), new Color(0.9f,0.78f,0.1f), v*2f);
            yield return null;
        }
        fill.anchorMax = new Vector2(target, 1f);
    }

    void Log(string msg) => _logText.text += msg + "\n";

    // ── UI Construction ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("CombatCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 20;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var dim = Make("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        var panel = CenterRect("Panel", canvasGO.transform, Vector2.zero, new Vector2(900, 560));
        panel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.97f);

        var topBar = Make("TopBar", panel);
        topBar.anchorMin = new Vector2(0,1); topBar.anchorMax = new Vector2(1,1);
        topBar.pivot = new Vector2(0.5f,1); topBar.sizeDelta = new Vector2(0, 4);
        topBar.anchoredPosition = Vector2.zero;
        topBar.gameObject.AddComponent<Image>().color = new Color(0.72f, 0.08f, 0.08f);

        var titleTxt = AddText(Make("Title", panel), "COMBAT", 44, FontStyle.Bold,
                               new Color(1f, 0.18f, 0.18f), TextAnchor.MiddleCenter);
        Pos(titleTxt.rectTransform, new Vector2(0, 250), new Vector2(820, 54));

        BuildCombatant(panel, "You",    new Color(1f, 0.85f, 0.1f),    new Vector2(-230, 42),
                       out _playerIcon, out _playerHpFill, out _playerHpText, out _);
        BuildCombatant(panel, "Zombie", new Color(0.85f, 0.15f, 0.15f), new Vector2(230, 42),
                       out _enemyIcon,  out _enemyHpFill,  out _enemyHpText,  out _enemyNameTxt);
        _playerHpFillImg = _playerHpFill.GetComponent<Image>();
        _enemyHpFillImg  = _enemyHpFill.GetComponent<Image>();

        // VS — dim, sits behind flash
        AddText(Make("VS", panel), "VS", 30, FontStyle.Bold,
                new Color(0.22f, 0.22f, 0.28f), TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0, 62);

        // Action flash text
        var actionRT = Make("ActionText", panel);
        Pos(actionRT, new Vector2(0, 62), new Vector2(380, 88));
        _actionText           = actionRT.gameObject.AddComponent<Text>();
        _actionText.text      = "";
        _actionText.fontSize  = 56;
        _actionText.fontStyle = FontStyle.Bold;
        _actionText.alignment = TextAnchor.MiddleCenter;
        _actionText.color     = Color.clear;
        _actionText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Divider
        var div = Make("Div", panel);
        div.anchorMin = new Vector2(0.03f,0.5f); div.anchorMax = new Vector2(0.97f,0.5f);
        div.pivot = new Vector2(0.5f,0.5f); div.sizeDelta = new Vector2(0,1);
        div.anchoredPosition = new Vector2(0,-108);
        div.gameObject.AddComponent<Image>().color = new Color(0.22f, 0.08f, 0.08f);

        var logTxt = AddText(Make("Log", panel), "", 18, FontStyle.Normal,
                             new Color(0.70f, 0.68f, 0.65f), TextAnchor.LowerLeft);
        Pos(logTxt.rectTransform, new Vector2(0, -165), new Vector2(840, 95));
        _logText = logTxt;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow   = VerticalWrapMode.Overflow;

        var lootRT = Make("LootFound", panel);
        Pos(lootRT, new Vector2(-60, -238), new Vector2(600, 32));
        _lootFoundText           = lootRT.gameObject.AddComponent<Text>();
        _lootFoundText.text      = "";
        _lootFoundText.fontSize  = 19;
        _lootFoundText.fontStyle = FontStyle.Bold;
        _lootFoundText.alignment = TextAnchor.MiddleLeft;
        _lootFoundText.color     = new Color(0.92f, 0.80f, 0.22f);
        _lootFoundText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var exitGO = Make("ExitBtn", panel);
        Pos(exitGO, new Vector2(360, -238), new Vector2(130, 44));
        exitGO.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.5f, 0.22f);
        _exitBtn = exitGO.gameObject.AddComponent<Button>();
        var ec = _exitBtn.colors;
        ec.highlightedColor = new Color(0.18f, 0.65f, 0.3f);
        ec.pressedColor     = new Color(0.08f, 0.35f, 0.15f);
        ec.disabledColor    = new Color(0.12f, 0.12f, 0.16f);
        _exitBtn.colors = ec;
        _exitBtn.onClick.AddListener(() => _exitPressed = true);
        AddText(Make("Lbl", exitGO), "EXIT", 24, FontStyle.Bold, Color.white,
                TextAnchor.MiddleCenter).rectTransform.anchoredPosition = Vector2.zero;
    }

    void BuildCombatant(RectTransform parent, string label, Color color, Vector2 pos,
                        out RectTransform icon, out RectTransform hpFill,
                        out Text hpText, out Text nameText)
    {
        var root = CenterRect(label + "Side", parent, pos, new Vector2(180, 280));

        var iconRT = Make("Icon", root);
        Pos(iconRT, new Vector2(0, 65), new Vector2(110, 110));
        iconRT.gameObject.AddComponent<Image>().sprite = CircleSprite(color);
        icon = iconRT;

        var hpBg = Make("HpBg", root);
        Pos(hpBg, new Vector2(0, -25), new Vector2(160, 16));
        hpBg.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.04f, 0.04f);

        var fill = Make("Fill", hpBg);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.sizeDelta = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = label == "You"
            ? new Color(0.2f, 0.75f, 0.3f) : new Color(0.82f, 0.18f, 0.18f);
        hpFill = fill;

        hpText = AddText(Make("HpTxt", root), "-- / --", 17, FontStyle.Normal,
                         new Color(0.70f, 0.70f, 0.70f), TextAnchor.MiddleCenter);
        Pos(hpText.rectTransform, new Vector2(0, -48), new Vector2(160, 22));

        nameText = AddText(Make("Name", root), label, 19, FontStyle.Bold,
                           new Color(0.65f, 0.65f, 0.65f), TextAnchor.MiddleCenter);
        nameText.rectTransform.anchoredPosition = new Vector2(0, -72);
    }

    static RectTransform Make(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform CenterRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Make(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Pos(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Text AddText(RectTransform rt, string val, int size, FontStyle style,
                        Color color, TextAnchor align)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = val; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return t;
    }

    static Sprite CircleSprite(Color color)
    {
        int sz = 128, r = sz / 2;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[sz * sz];
        float cx = r - 0.5f, cy = r - 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float d = Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
            px[y*sz+x] = d <= r-2f ? color
                       : d <= r    ? Color.Lerp(color, Color.clear, (d-(r-2f))/2f)
                       : Color.clear;
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,sz,sz), Vector2.one*0.5f, 100f);
    }
}
