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

    // Tile notification
    Text _notifText;
    Coroutine _notifRoutine;

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        new GameObject("CombatScreen").AddComponent<CombatScreen>();
        new GameObject("GameOverScreen").AddComponent<GameOverScreen>();
        new GameObject("StoryScreen").AddComponent<StoryScreen>();
        new GameObject("LootCarScreen").AddComponent<LootCarScreen>();
        new GameObject("LootVendingScreen").AddComponent<LootVendingScreen>();
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
        BuildHUD(canvasGO.transform);
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

    void BuildHUD(Transform canvas)
    {
        var hud    = MakeRect("HUD", canvas);
        var hudImg = hud.gameObject.AddComponent<Image>();
        hudImg.color = new Color(0.08f,0.08f,0.12f,0.85f);
        Anchor(hud, new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
               new Vector2(20,-20), new Vector2(160,80));

        var hpGO = MakeRect("HP", hud);
        _hpText  = hpGO.gameObject.AddComponent<Text>();
        _hpText.text      = "HP   10 / 10";
        _hpText.alignment = TextAnchor.MiddleLeft;
        _hpText.fontSize  = 22;
        _hpText.color     = new Color(0.9f,0.25f,0.25f);
        _hpText.font      = DefaultFont();
        Anchor(hpGO, new Vector2(0,1), new Vector2(1,1), new Vector2(0,1),
               new Vector2(14,-8), new Vector2(-14,32));

        var ammoGO = MakeRect("Ammo", hud);
        _ammoText  = ammoGO.gameObject.AddComponent<Text>();
        _ammoText.text      = "Ammo  6";
        _ammoText.alignment = TextAnchor.MiddleLeft;
        _ammoText.fontSize  = 22;
        _ammoText.color     = new Color(0.9f,0.75f,0.3f);
        _ammoText.font      = DefaultFont();
        Anchor(ammoGO, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0),
               new Vector2(14,8), new Vector2(-14,32));
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
        if (PlayerStats.Instance == null) return;
        var s = PlayerStats.Instance;
        if (_hpText   != null) _hpText.text   = "HP   " + s.hp + " / " + s.maxHp;
        if (_ammoText != null) _ammoText.text  = "Ammo  " + s.ammo;
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
            TileType.Combat => "COMBAT!",
            TileType.Loot   => "LOOT!",
            TileType.Boss   => "BOSS FIGHT!",
            _               => ""
        };

        Color col = type switch
        {
            TileType.Combat => new Color(1f, 0.25f, 0.25f),
            TileType.Loot   => new Color(1f, 0.85f, 0.2f),
            TileType.Boss   => new Color(0.8f, 0.3f, 1f),
            _               => Color.clear
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
            else if (landed.tileType == TileType.Loot)
            {
                yield return new WaitForSeconds(0.3f);
                if (Random.value < 0.5f && LootCarScreen.Instance != null)
                    yield return StartCoroutine(LootCarScreen.Instance.Open());
                else if (LootVendingScreen.Instance != null)
                    yield return StartCoroutine(LootVendingScreen.Instance.Open());
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
