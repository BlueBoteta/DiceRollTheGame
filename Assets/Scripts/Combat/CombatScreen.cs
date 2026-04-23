using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatScreen : MonoBehaviour
{
    public static CombatScreen Instance { get; private set; }

    Canvas        _canvas;

    // Intro
    RectTransform _introOverlay;
    Image         _introOverlayImg;
    Text          _introText;

    // Combat panel (no border, no tilt — just the background art panel)
    RectTransform _combatPanel;

    // Combat UI refs
    RectTransform _playerIcon;
    RectTransform _enemyIcon;
    RectTransform _playerHpFill;   // top-left mini bar fill
    Text          _playerHpText;   // overlaid on player HP bar
    RectTransform _enemyHpFill;    // top-right mini bar fill
    Text          _enemyHpText;    // overlaid on enemy HP bar
    Text          _enemyNameTxt;
    Text          _logText;
    Text          _actionText;
    Text          _lootFoundText;
    Button        _exitBtn;

    bool      _exitPressed;
    int       _enemyHp, _enemyMaxHp;
    Image     _playerHpFillImg;
    Image     _enemyHpFillImg;
    Coroutine _playerBarCo;
    Coroutine _enemyBarCo;

    static readonly string[] IntroLines =
    {
        "THE DEAD DON'T SLEEP",
        "SURVIVE.  OR JOIN THEM.",
        "NOWHERE TO RUN",
        "THEY SMELL YOUR FEAR",
        "FIGHT.  OR DIE TRYING.",
        "NO ONE HEARS YOU SCREAM",
    };

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color ColGold    = new Color(1f,    0.85f, 0.15f);
    static readonly Color ColOrange  = new Color(1f,    0.55f, 0.10f);
    static readonly Color ColRed     = new Color(1f,    0.22f, 0.22f);
    static readonly Color ColDeepRed = new Color(0.9f,  0.08f, 0.08f);
    static readonly Color ColPurple  = new Color(0.75f, 0.15f, 0.85f);
    static readonly Color ColGreen   = new Color(0.28f, 1f,    0.38f);

    // ── Attack moves ──────────────────────────────────────────────────────────
    struct AttackMove
    {
        public string flash, log;
        public int minDmg, maxDmg, ammoCost;
        public Color color;
        public AttackMove(string f, string l, int mn, int mx, Color c, int ammo = 0)
        { flash = f; log = l; minDmg = mn; maxDmg = mx; color = c; ammoCost = ammo; }
    }

    static readonly AttackMove[] PistolMoves =
    {
        new AttackMove("BANG!",   "fire at",   3,  7, ColGold, ammo: 1),
        new AttackMove("FIRE!",   "shoot",     4,  8, ColGold, ammo: 1),
        new AttackMove("UNLOAD!", "unload on", 5, 10, ColGold, ammo: 2),
    };
    static readonly AttackMove[] ShotgunMoves =
    {
        new AttackMove("PUMP!",  "pump-shot", 7, 12, ColGold, ammo: 1),
        new AttackMove("BLAST!", "blast",     9, 15, ColGold, ammo: 2),
    };
    static readonly AttackMove[] KnifeMoves =
    {
        new AttackMove("STAB!",  "stab",  4, 9, ColOrange),
        new AttackMove("SLASH!", "slash", 3, 8, ColOrange),
    };
    static readonly AttackMove[] FistMoves =
    {
        new AttackMove("PUNCH!", "punch", 1, 3, ColOrange),
        new AttackMove("SHOVE!", "shove", 1, 4, ColOrange),
    };
    static readonly AttackMove HeadshotMove =
        new AttackMove("HEADSHOT!", "headshot", 12, 18, new Color(1f, 0.98f, 0.35f), ammo: 1);
    static readonly AttackMove PointBlankMove =
        new AttackMove("POINT BLANK!", "fires point-blank at", 15, 22, new Color(1f, 0.95f, 0.2f), ammo: 2);

    static readonly AttackMove[] EnemyTier1 =
    {
        new AttackMove("SCRATCH", "scratches you",   1, 3, ColRed),
        new AttackMove("SCRATCH", "scratches you",   1, 3, ColRed),
        new AttackMove("BITE",    "bites you",       2, 4, ColRed),
        new AttackMove("BITE",    "bites you",       2, 4, ColRed),
        new AttackMove("LUNGE",   "lunges at you",   1, 3, ColRed),
    };
    static readonly AttackMove[] EnemyTier2 =
    {
        new AttackMove("HIT",   "hits you",        3, 6, ColRed),
        new AttackMove("SLASH", "slashes you",     3, 7, ColRed),
        new AttackMove("GRAB",  "grabs and tears", 4, 8, ColDeepRed),
        new AttackMove("BITE",  "bites down hard", 3, 6, ColRed),
        new AttackMove("SMASH", "smashes you",     5, 9, ColDeepRed),
    };
    static readonly AttackMove[] EnemyTier3 =
    {
        new AttackMove("SMASH",  "smashes you",     5, 11, ColDeepRed),
        new AttackMove("TACKLE", "tackles you",     6, 12, ColDeepRed),
        new AttackMove("TEAR",   "tears into you",  5, 10, ColDeepRed),
        new AttackMove("CRUSH",  "crushes you",     7, 13, ColDeepRed),
        new AttackMove("DEVOUR", "tries to devour", 8, 14, ColDeepRed),
    };
    static readonly AttackMove[] BossMoves =
    {
        new AttackMove("FURY",    "unleashes fury on you",  8,  14, ColPurple),
        new AttackMove("SLAM",    "slams you down",         10, 16, ColPurple),
        new AttackMove("CRUSH",   "crushes you",            9,  15, ColPurple),
        new AttackMove("DEVOUR",  "tries to devour you",    7,  13, ColPurple),
        new AttackMove("RAMPAGE", "rampages into you",      11, 17, ColPurple),
    };

    static readonly string[] KillLines      = { "GOES DOWN.", "STAYS DOWN.", "NEUTRALIZED.", "ONE LESS." };
    static readonly string[] CombatLootPool = { "ammo","ammo","ammo","scrap","scrap","food","meds","pills","knife","vest","helmet" };
    static readonly string[] BossLootPool   = { "ammo","ammo","scrap","medkit","meds","pills","lockpick","battery","pistol","vest","riot_gear" };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public IEnumerator Open(string enemyName = "Zombie", int enemyMaxHp = 0, int enemyAttack = 0)
    {
        int lap = GameManager.Instance != null ? GameManager.Instance.LoopCount : 0;

        if (enemyMaxHp > 0)
            _enemyMaxHp = enemyMaxHp;
        else
        {
            int baseMin = Mathf.Min(6 + lap * 3, 20);
            int baseMax = Mathf.Min(13 + lap * 3, 28);
            _enemyMaxHp = Random.Range(baseMin, baseMax + 1);
        }

        _enemyHp              = _enemyMaxHp;
        _exitPressed          = false;
        _logText.text         = "";
        _actionText.text      = "";
        _lootFoundText.text   = "";
        _exitBtn.interactable = false;
        _enemyNameTxt.text    = enemyName;
        RefreshBars();

        _canvas.gameObject.SetActive(true);
        _combatPanel.gameObject.SetActive(false);
        _introOverlay.gameObject.SetActive(true);
        _introOverlayImg.color              = new Color(0f, 0f, 0f, 1f);
        _introText.color                    = new Color(0.88f, 0.15f, 0.15f, 0f);
        _introText.rectTransform.localScale = Vector3.one;
        _introText.text                     = IntroLines[Random.Range(0, IntroLines.Length)];

        yield return StartCoroutine(FadeAlpha(_introText, 0f, 1f, 0.55f));
        yield return new WaitForSeconds(1.1f);

        _combatPanel.gameObject.SetActive(true);
        _combatPanel.localScale = Vector3.one * 0.90f;
        yield return StartCoroutine(CrossFade());
        _introOverlay.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(RunCombat(enemyName));

        if (PlayerStats.Instance != null && PlayerStats.Instance.hp <= 0)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            GameOverScreen.Instance?.Show();
            yield break;
        }

        while (!_exitPressed) yield return null;
        _canvas.gameObject.SetActive(false);
    }

    IEnumerator CrossFade()
    {
        float dur = 0.50f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float n = Mathf.SmoothStep(0f, 1f, t / dur);
            _introOverlayImg.color              = new Color(0f, 0f, 0f, 1f - n);
            _introText.color                    = new Color(0.88f, 0.15f, 0.15f, 1f - n);
            _introText.rectTransform.localScale = Vector3.one * (1f + n * 0.12f);
            _combatPanel.localScale             = Vector3.one * Mathf.Lerp(0.90f, 1f, n);
            yield return null;
        }
        _introOverlayImg.color   = Color.clear;
        _introText.color         = Color.clear;
        _combatPanel.localScale  = Vector3.one;
    }

    IEnumerator FadeAlpha(Graphic g, float from, float to, float dur)
    {
        Color c = g.color;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            c.a     = Mathf.Lerp(from, to, t / dur);
            g.color = c;
            yield return null;
        }
        c.a     = to;
        g.color = c;
    }

    // ── Combat logic ──────────────────────────────────────────────────────────

    IEnumerator RunCombat(string enemyName)
    {
        int  lap    = GameManager.Instance != null ? GameManager.Instance.LoopCount : 0;
        bool isBoss = _enemyMaxHp >= 30;

        AttackMove[] enemyMoves = isBoss   ? BossMoves
                                : lap >= 6 ? EnemyTier3
                                : lap >= 3 ? EnemyTier2
                                :            EnemyTier1;

        bool flashAdvantage = PlayerEquipment.Instance?.Get(EquipSlot.Utility)?.id == "flashlight";

        Log(enemyName + " appears.  It wants your blood.");
        if (flashAdvantage) Log("Flashlight — you spot it first.");
        yield return new WaitForSeconds(0.7f);

        while (_enemyHp > 0 && PlayerStats.Instance != null && PlayerStats.Instance.hp > 0)
        {
            AttackMove pm   = PickPlayerMove();
            int        pdmg = Random.Range(pm.minDmg, pm.maxDmg + 1);
            _enemyHp = Mathf.Max(0, _enemyHp - pdmg);
            Log("You " + pm.log + " for " + pdmg + ".");
            if (pm.ammoCost > 0 && Inventory.Instance != null)
            {
                Inventory.Instance.Remove("ammo", pm.ammoCost);
                if (Inventory.Instance.Count("ammo") == 0)
                    Log("-- Ammo depleted.  Switching to melee. --");
            }
            yield return StartCoroutine(FlashAction(pm.flash, pm.color));
            yield return StartCoroutine(Punch(_playerIcon, new Vector2(55, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.2f);

            if (_enemyHp <= 0) break;

            if (flashAdvantage)
            {
                flashAdvantage = false;
                yield return StartCoroutine(FlashAction("FREE ROUND!", new Color(1f, 0.95f, 0.35f)));
                Log("It didn't see you coming — enemy misses.");
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            AttackMove em      = enemyMoves[Random.Range(0, enemyMoves.Length)];
            int        rawDmg  = Random.Range(em.minDmg, em.maxDmg + 1);
            int        defense = PlayerEquipment.Instance?.Get(EquipSlot.Defense)?.defense ?? 0;
            int        blocked = Mathf.Min(defense, rawDmg - 1);
            int        edmg    = rawDmg - blocked;
            PlayerStats.Instance.TakeDamage(edmg);
            string dmgLine = blocked > 0
                ? "  (-" + edmg + " HP, " + blocked + " blocked)."
                : "  (-" + edmg + " HP).";
            Log(enemyName + " " + em.log + dmgLine);
            yield return StartCoroutine(FlashAction(em.flash, em.color));
            yield return StartCoroutine(FlashAction(PlayerReaction(edmg), ReactionColor(edmg)));
            yield return StartCoroutine(Punch(_enemyIcon, new Vector2(-55, 0)));
            RefreshBars();
            yield return new WaitForSeconds(0.2f);
        }

        if (_enemyHp <= 0)
        {
            yield return StartCoroutine(FlashAction(KillLines[Random.Range(0, KillLines.Length)], ColGreen));
            yield return StartCoroutine(GiveCombatLoot(isBoss));
            yield return new WaitForSeconds(0.3f);
            _exitBtn.interactable = true;
        }
        else
        {
            Log("You were overwhelmed.");
        }
    }

    static AttackMove PickPlayerMove()
    {
        int ammo      = Inventory.Instance?.Count("ammo") ?? 0;
        var equip     = PlayerEquipment.Instance;
        var primary   = equip?.Get(EquipSlot.Primary);
        var secondary = equip?.Get(EquipSlot.Secondary);

        AttackMove[] meleeMoves = secondary?.id == "knife" ? KnifeMoves : FistMoves;

        if (primary == null || ammo <= 0)
            return meleeMoves[Random.Range(0, meleeMoves.Length)];

        if (primary.id == "shotgun")
        {
            if (ammo >= 2 && Random.value < 0.06f) return PointBlankMove;
        }
        else if (Random.value < 0.08f) return HeadshotMove;

        if (Random.value < 0.65f)
        {
            var gunMoves = primary.id == "shotgun" ? ShotgunMoves : PistolMoves;
            var valid    = new List<AttackMove>();
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
        if (dmg <= 3)  return new Color(1f,  0.65f, 0.3f);
        if (dmg <= 6)  return new Color(1f,  0.35f, 0.25f);
        if (dmg <= 10) return new Color(1f,  0.15f, 0.15f);
        return new Color(0.9f, 0.05f, 0.05f);
    }

    IEnumerator GiveCombatLoot(bool isBoss)
    {
        string[] pool  = isBoss ? BossLootPool : CombatLootPool;
        int      drops = isBoss ? 2 : 1;
        var      parts = new List<string>();

        for (int i = 0; i < drops; i++)
        {
            string id   = pool[Random.Range(0, pool.Length)];
            int    qty  = id == "ammo" ? Random.Range(1, 4) : 1;
            string name = ItemFactory.Create(id)?.displayName ?? id;
            parts.Add(qty > 1 ? qty + "x " + name : name);
            if (LootPickupPrompt.Instance != null)
                yield return StartCoroutine(LootPickupPrompt.Instance.Show(id, qty));
        }

        string lootStr = string.Join(", ", parts);
        Log("Dropped: " + lootStr);
        _lootFoundText.text = "LOOT: " + lootStr;
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

    // ── Animations ────────────────────────────────────────────────────────────

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
        float   t      = 0f;
        while (t < 0.20f)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = origin + dir * Mathf.Sin(t / 0.20f * Mathf.PI);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    IEnumerator AnimBar(RectTransform fill, Image img, float target, bool isPlayer)
    {
        float from = fill.anchorMax.x;
        float t    = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float v = Mathf.Clamp01(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / 0.28f)));
            fill.anchorMax = new Vector2(v, 1f);
            if (isPlayer && img != null)
                img.color = v > 0.5f
                    ? Color.Lerp(new Color(0.9f, 0.78f, 0.1f), new Color(0.2f, 0.75f, 0.3f), (v - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.85f, 0.12f, 0.12f), new Color(0.9f, 0.78f, 0.1f), v * 2f);
            yield return null;
        }
        fill.anchorMax = new Vector2(target, 1f);
    }

    void Log(string msg) => _logText.text += msg + "\n";

    // ── UI Construction ───────────────────────────────────────────────────────

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

        // World dimmer — darkens board behind the combat panel
        var dimRT = Make("WorldDimmer", canvasGO.transform);
        dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one; dimRT.sizeDelta = Vector2.zero;
        dimRT.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

        // Combat panel — 16:9 (1280×720), centered, no border, no tilt
        // Leaves 320px breathing room each side on 1920-wide canvas
        _combatPanel = CenterRect("CombatPanel", canvasGO.transform, Vector2.zero, new Vector2(1280f, 720f));
        var bgImg = _combatPanel.gameObject.AddComponent<Image>();
        var bgTex = Resources.Load<Texture2D>("Combat/CombatBackGround2");
        if (bgTex != null)
        {
            bgImg.sprite         = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.preserveAspect = false;
        }
        else bgImg.color = new Color(0.04f, 0.07f, 0.10f);

        BuildCombatUI(_combatPanel);

        // Intro overlay — full black screen, rendered on top
        _introOverlay = Make("IntroOverlay", canvasGO.transform);
        _introOverlay.anchorMin = Vector2.zero; _introOverlay.anchorMax = Vector2.one; _introOverlay.sizeDelta = Vector2.zero;
        _introOverlayImg = _introOverlay.gameObject.AddComponent<Image>();
        _introOverlayImg.color = new Color(0f, 0f, 0f, 1f);

        var introTxtRT = CenterRect("IntroText", _introOverlay, Vector2.zero, new Vector2(1400f, 130f));
        _introText                    = introTxtRT.gameObject.AddComponent<Text>();
        _introText.text               = "";
        _introText.fontSize           = 64;
        _introText.fontStyle          = FontStyle.Bold;
        _introText.alignment          = TextAnchor.MiddleCenter;
        _introText.color              = Color.clear;
        _introText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _introText.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    // Combat panel: 1280×720. Center=(0,0). x: ±640  y: ±360
    void BuildCombatUI(RectTransform p)
    {
        // ══════════════════════════════════════════════════════════════════════
        //  TOP STRIP  —  Player HP (LEFT)  |  VS (CENTER)  |  Enemy (RIGHT)
        //  y: 295–325
        // ══════════════════════════════════════════════════════════════════════

        // "YOU" label — top left
        AddText(Make("YouLabel", p), "YOU", 20, FontStyle.Bold,
                new Color(0.30f, 0.92f, 0.38f), TextAnchor.MiddleLeft)
            .rectTransform.anchoredPosition = new Vector2(-590f, 322f);

        // Player HP bar — top left, green
        var pHpBg = CenterRect("PlayerHpBg", p, new Vector2(-325f, 295f), new Vector2(580f, 18f));
        pHpBg.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.16f, 0.05f, 0.85f);
        var pFill = Make("PlayerFill", pHpBg);
        pFill.anchorMin = Vector2.zero; pFill.anchorMax = Vector2.one; pFill.sizeDelta = Vector2.zero;
        _playerHpFillImg = pFill.gameObject.AddComponent<Image>();
        _playerHpFillImg.color = new Color(0.22f, 0.78f, 0.32f);
        _playerHpFill = pFill;
        // HP numbers overlaid on bar
        var phpGO = new GameObject("PlayerHpTxt");
        phpGO.transform.SetParent(pHpBg.transform, false);
        var phpRT = phpGO.AddComponent<RectTransform>();
        phpRT.anchorMin = Vector2.zero; phpRT.anchorMax = Vector2.one; phpRT.sizeDelta = Vector2.zero;
        _playerHpText           = phpGO.AddComponent<Text>();
        _playerHpText.text      = "-- / --";
        _playerHpText.fontSize  = 12;
        _playerHpText.fontStyle = FontStyle.Bold;
        _playerHpText.alignment = TextAnchor.MiddleCenter;
        _playerHpText.color     = Color.white;
        _playerHpText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // VS — center top
        AddText(Make("VS", p), "VS", 26, FontStyle.Bold,
                new Color(1f, 1f, 1f, 0.40f), TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = new Vector2(0f, 308f);

        // Enemy name — top right
        _enemyNameTxt = AddText(Make("EnemyName", p), "Zombie", 20, FontStyle.Bold,
                new Color(0.95f, 0.22f, 0.22f), TextAnchor.MiddleRight);
        Pos(_enemyNameTxt.rectTransform, new Vector2(495f, 322f), new Vector2(260f, 26f));

        // Enemy HP bar — top right, red
        var eHpBg = CenterRect("EnemyHpBg", p, new Vector2(325f, 295f), new Vector2(580f, 18f));
        eHpBg.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.04f, 0.04f, 0.85f);
        var eFill = Make("EnemyFill", eHpBg);
        eFill.anchorMin = Vector2.zero; eFill.anchorMax = Vector2.one; eFill.sizeDelta = Vector2.zero;
        _enemyHpFillImg = eFill.gameObject.AddComponent<Image>();
        _enemyHpFillImg.color = new Color(0.82f, 0.14f, 0.14f);
        _enemyHpFill = eFill;
        // HP numbers overlaid on bar
        var ehpGO = new GameObject("EnemyHpTxt");
        ehpGO.transform.SetParent(eHpBg.transform, false);
        var ehpRT = ehpGO.AddComponent<RectTransform>();
        ehpRT.anchorMin = Vector2.zero; ehpRT.anchorMax = Vector2.one; ehpRT.sizeDelta = Vector2.zero;
        _enemyHpText           = ehpGO.AddComponent<Text>();
        _enemyHpText.text      = "-- / --";
        _enemyHpText.fontSize  = 12;
        _enemyHpText.fontStyle = FontStyle.Bold;
        _enemyHpText.alignment = TextAnchor.MiddleCenter;
        _enemyHpText.color     = Color.white;
        _enemyHpText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ══════════════════════════════════════════════════════════════════════
        //  BATTLEFIELD  —  player icon (left) | flash (center) | enemy (right)
        //  y: 40–260
        // ══════════════════════════════════════════════════════════════════════

        // Action flash — center stage
        var actionRT = CenterRect("ActionText", p, new Vector2(0f, 180f), new Vector2(680f, 96f));
        _actionText           = actionRT.gameObject.AddComponent<Text>();
        _actionText.text      = "";
        _actionText.fontSize  = 56;
        _actionText.fontStyle = FontStyle.Bold;
        _actionText.alignment = TextAnchor.MiddleCenter;
        _actionText.color     = Color.clear;
        _actionText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Player icon — left
        var pIconRT = CenterRect("PlayerIcon", p, new Vector2(-450f, 110f), new Vector2(130f, 130f));
        pIconRT.gameObject.AddComponent<Image>().color = new Color(1f, 0.85f, 0.12f, 0.78f);
        _playerIcon = pIconRT;

        // Enemy icon — right
        var eIconRT = CenterRect("EnemyIcon", p, new Vector2(450f, 110f), new Vector2(130f, 130f));
        eIconRT.gameObject.AddComponent<Image>().color = new Color(0.82f, 0.15f, 0.15f, 0.78f);
        _enemyIcon = eIconRT;

        // ══════════════════════════════════════════════════════════════════════
        //  DIVIDER
        // ══════════════════════════════════════════════════════════════════════
        MakeDivider(p, new Vector2(0f, -18f), 1220f, new Color(0.50f, 0.08f, 0.08f, 0.70f));

        // ══════════════════════════════════════════════════════════════════════
        //  COMBAT LOG
        //  y: -20 to -190
        // ══════════════════════════════════════════════════════════════════════
        var logBg = CenterRect("LogBg", p, new Vector2(0f, -118f), new Vector2(1220f, 160f));
        logBg.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.50f);

        var logGO = new GameObject("Log");
        logGO.transform.SetParent(logBg.transform, false);
        var logRT = logGO.AddComponent<RectTransform>();
        logRT.anchorMin = Vector2.zero; logRT.anchorMax = Vector2.one;
        logRT.offsetMin = new Vector2(14f, 8f); logRT.offsetMax = new Vector2(-8f, -8f);
        _logText                    = logGO.AddComponent<Text>();
        _logText.text               = "";
        _logText.fontSize           = 17;
        _logText.fontStyle          = FontStyle.Normal;
        _logText.alignment          = TextAnchor.LowerLeft;
        _logText.color              = new Color(0.80f, 0.78f, 0.74f);
        _logText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow   = VerticalWrapMode.Overflow;

        // ══════════════════════════════════════════════════════════════════════
        //  DIVIDER
        // ══════════════════════════════════════════════════════════════════════
        MakeDivider(p, new Vector2(0f, -202f), 1220f, new Color(0.20f, 0.20f, 0.26f, 0.65f));

        // ══════════════════════════════════════════════════════════════════════
        //  BOTTOM ROW  —  loot text (left)  |  exit button (right)
        //  y: -248
        // ══════════════════════════════════════════════════════════════════════
        var lootGO = new GameObject("LootFound");
        lootGO.transform.SetParent(p.transform, false);
        var lootRT = lootGO.AddComponent<RectTransform>();
        lootRT.anchorMin = lootRT.anchorMax = lootRT.pivot = new Vector2(0.5f, 0.5f);
        lootRT.anchoredPosition = new Vector2(-370f, -248f);
        lootRT.sizeDelta        = new Vector2(680f, 32f);
        _lootFoundText                    = lootGO.AddComponent<Text>();
        _lootFoundText.text               = "";
        _lootFoundText.fontSize           = 17;
        _lootFoundText.fontStyle          = FontStyle.Bold;
        _lootFoundText.alignment          = TextAnchor.MiddleLeft;
        _lootFoundText.color              = new Color(0.92f, 0.80f, 0.22f);
        _lootFoundText.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _lootFoundText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var exitRT = CenterRect("ExitBtn", p, new Vector2(570f, -248f), new Vector2(120f, 42f));
        exitRT.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.50f, 0.22f);
        _exitBtn = exitRT.gameObject.AddComponent<Button>();
        var ec = _exitBtn.colors;
        ec.highlightedColor = new Color(0.18f, 0.65f, 0.30f);
        ec.pressedColor     = new Color(0.08f, 0.35f, 0.15f);
        ec.disabledColor    = new Color(0.12f, 0.12f, 0.16f);
        _exitBtn.colors = ec;
        _exitBtn.onClick.AddListener(() => _exitPressed = true);
        AddText(Make("ExitLbl", exitRT), "EXIT", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter)
            .rectTransform.anchoredPosition = Vector2.zero;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void MakeDivider(RectTransform parent, Vector2 pos, float width, Color color)
    {
        var rt = Make("Div", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(width, 1f);
        rt.gameObject.AddComponent<Image>().color = color;
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
        rt.sizeDelta        = size;
        return rt;
    }

    static void Pos(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    static Text AddText(RectTransform rt, string val, int size, FontStyle style, Color color, TextAnchor align)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text      = val;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = align;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return t;
    }
}
