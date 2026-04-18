using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlackjackScreen : MonoBehaviour
{
    public static BlackjackScreen Instance { get; private set; }

    // ── Card constants ────────────────────────────────────────────────────────
    const float CW = 78f, CH = 112f, CGap = 9f;

    static readonly string[] RankStr = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
    static readonly string[] SuitStr = { "♠", "♥", "♦", "♣" };
    static readonly Color[]  SuitCol =
    {
        new Color(0.10f, 0.10f, 0.14f),  // ♠
        new Color(0.82f, 0.12f, 0.14f),  // ♥
        new Color(0.82f, 0.12f, 0.14f),  // ♦
        new Color(0.10f, 0.10f, 0.14f),  // ♣
    };

    struct Card { public int rank, suit; public bool faceDown; }

    // ── State ─────────────────────────────────────────────────────────────────
    Canvas _canvas;
    bool   _done, _playerDone, _earlyExit;

    GameObject    _bettingPanel, _gamePanel, _resultPanel;
    RectTransform _dealerRow, _playerRow, _npcRow;
    Text _dealerTotal, _playerTotal, _npcTotal, _statusText, _npcDialogueText;
    Button _hitBtn, _standBtn, _doubleBtn, _leaveBtn;

    int   _betType  = 0;
    int   _betValue = 1;
    Text  _betValueText;
    Image[] _betTypeBgs = new Image[3];

    // Resources shown in each panel
    Text _bAmmo, _bScrap, _bMeds;   // betting panel
    Text _gAmmo, _gScrap, _gMeds;   // game panel

    Text _resultTitle, _resultSub, _resultNpcLine;
    CanvasGroup _resultCG;

    readonly List<Card> _deck   = new List<Card>();
    readonly List<Card> _player = new List<Card>();
    readonly List<Card> _dealer = new List<Card>();
    readonly List<Card> _npc    = new List<Card>();
    bool _doubled;

    static readonly string[] ResourceNames = { "AMMO", "SCRAP", "MEDS" };

    static readonly string[] NpcWin =
    {
        "Survivor: \"Heh. Luck's on my side today.\"",
        "Survivor: \"Better luck next time... if there is one.\"",
        "Survivor: \"Ammo well spent. On me.\"",
        "Survivor: \"Don't feel bad. Everyone loses to me.\"",
        "Survivor: \"You just funded my next meal. Thanks.\"",
        "Survivor: \"Saw that coming. Your face gave it away.\"",
        "Survivor: \"Should've stayed in the bunker.\"",
        "Survivor: \"More scrap for the pile. Appreciated.\"",
        "Survivor: \"Bold play. Stupid, but bold.\"",
        "Survivor: \"The wasteland giveth to me. Not you.\"",
    };
    static readonly string[] NpcLose =
    {
        "Survivor: \"Kid's good. I'll admit it.\"",
        "Survivor: \"Damn. Where'd you learn to play?\"",
        "Survivor: \"Next round I'll get you back.\"",
        "Survivor: \"Lucky draw. Enjoy it while it lasts.\"",
        "Survivor: \"Fine. Take it. You'll need it out there.\"",
        "Survivor: \"I swear this deck is rigged.\"",
        "Survivor: \"Never losing to a newbie again.\"",
        "Survivor: \"You're lucky I need both hands to eat.\"",
        "Survivor: \"...Don't let it go to your head.\"",
        "Survivor: \"Ugh. My own fault for playing sober.\"",
    };
    static readonly string[] DealerWin =
    {
        "Dealer: \"House always wins. Nothing personal.\"",
        "Dealer: \"Should've stood. Classic rookie mistake.\"",
        "Dealer: \"Thanks for the generous donation.\"",
        "Dealer: \"Come back when you have more to lose.\"",
        "Dealer: \"I've seen better bluffs from a corpse.\"",
        "Dealer: \"You played right into it. Beautiful.\"",
        "Dealer: \"The dead don't gamble for a reason.\"",
        "Dealer: \"Bold of you to assume you'd win.\"",
    };
    static readonly string[] DealerLose =
    {
        "Dealer: \"...Impressive. For a scavenger.\"",
        "Dealer: \"The apocalypse really does make gamblers of us all.\"",
        "Dealer: \"Don't get used to it.\"",
        "Dealer: \"I'll be taking that back next hand.\"",
        "Dealer: \"Lucky. Disgustingly lucky.\"",
        "Dealer: \"Enjoy it. The wasteland will take it soon enough.\"",
        "Dealer: \"You read me perfectly. Unsettling.\"",
        "Dealer: \"First time I've lost all week. Congratulations, I suppose.\"",
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
        _done = false;
        SwitchTo("betting");
        _betValue = 1;
        RefreshBetUI();
        RefreshResources();
        _canvas.gameObject.SetActive(true);
        while (!_done) yield return null;
        _canvas.gameObject.SetActive(false);
    }

    // ── Betting ───────────────────────────────────────────────────────────────

    void RefreshBetUI()
    {
        if (_betValueText != null) _betValueText.text = _betValue.ToString();
        for (int i = 0; i < 3; i++)
            if (_betTypeBgs[i] != null)
                _betTypeBgs[i].color = i == _betType
                    ? new Color(0.38f, 0.52f, 0.28f)
                    : new Color(0.14f, 0.20f, 0.14f);
    }

    void RefreshResources()
    {
        var r = MiniGameResources.Instance;
        if (r == null) return;
        void Set(Text t, string prefix, int v) { if (t != null) t.text = prefix + "  " + v; }
        Set(_bAmmo,  "AMMO",  r.ammo);  Set(_bScrap, "SCRAP", r.scrap); Set(_bMeds, "MEDS", r.meds);
        Set(_gAmmo,  "AMMO",  r.ammo);  Set(_gScrap, "SCRAP", r.scrap); Set(_gMeds, "MEDS", r.meds);
    }

    int GetResource(int t)
    {
        var r = MiniGameResources.Instance;
        if (r == null) return 0;
        return t == 0 ? r.ammo : t == 1 ? r.scrap : r.meds;
    }

    void AddResource(int t, int d)
    {
        var r = MiniGameResources.Instance;
        if (r == null) return;
        if (t == 0) r.ammo  = Mathf.Max(0, r.ammo  + d);
        if (t == 1) r.scrap = Mathf.Max(0, r.scrap + d);
        if (t == 2) r.meds  = Mathf.Max(0, r.meds  + d);
    }

    void OnDeal()
    {
        if (_betValue <= 0 || _betValue > GetResource(_betType))
        { StartCoroutine(FlashStatus("Not enough " + ResourceNames[_betType] + "!")); return; }
        AddResource(_betType, -_betValue);
        RefreshResources();
        StartCoroutine(DealRoutine());
    }

    // ── Main game flow ────────────────────────────────────────────────────────

    IEnumerator DealRoutine()
    {
        SwitchTo("game");
        _doubled = _earlyExit = _playerDone = false;
        _statusText.text      = "";
        _npcDialogueText.text = "";
        BuildDeck();
        _player.Clear(); _dealer.Clear(); _npc.Clear();
        ClearRow(_dealerRow); ClearRow(_playerRow); ClearRow(_npcRow);
        RefreshTotals();

        yield return Deal(_player, _playerRow, false, 0.15f);
        yield return Deal(_npc,    _npcRow,    false, 0.15f);
        yield return Deal(_dealer, _dealerRow, false, 0.15f);
        yield return Deal(_player, _playerRow, false, 0.15f);
        yield return Deal(_npc,    _npcRow,    false, 0.15f);
        yield return Deal(_dealer, _dealerRow, true,  0.15f);
        RefreshTotals();

        if (Value(_player) == 21)
        {
            _statusText.text = "★  BLACKJACK!  ★";
            yield return new WaitForSeconds(1.8f);
            yield return StartCoroutine(FlipDealerCard());
            yield return new WaitForSeconds(0.7f);
            yield return StartCoroutine(ResolveRound());
            yield break;
        }

        _statusText.text = "Your turn.";
        SetButtons(true);
        while (!_playerDone) yield return null;
        SetButtons(false);
        if (_earlyExit) yield break;

        yield return StartCoroutine(NpcTurn());
        yield return StartCoroutine(DealerTurn());
        yield return StartCoroutine(ResolveRound());
    }

    IEnumerator NpcTurn()
    {
        _statusText.text = "Survivor thinks";
        yield return StartCoroutine(ThinkingDots(_statusText, "Survivor thinks", 0.8f));
        while (Value(_npc) < 16 && Value(_npc) <= 21)
            yield return Deal(_npc, _npcRow, false, 0.22f);
        RefreshTotals();
    }

    IEnumerator DealerTurn()
    {
        _statusText.text = "Dealer reveals...";
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(FlipDealerCard());
        yield return new WaitForSeconds(0.3f);
        while (Value(_dealer) < 17)
        {
            yield return new WaitForSeconds(0.25f);
            yield return Deal(_dealer, _dealerRow, false, 0.18f);
        }
    }

    IEnumerator FlipDealerCard()
    {
        int idx = -1;
        for (int i = 0; i < _dealer.Count; i++)
            if (_dealer[i].faceDown) { idx = i; break; }
        if (idx < 0) yield break;

        var cardRT = _dealerRow.GetChild(idx) as RectTransform;
        if (cardRT == null) yield break;

        // Flip: scale X → 0
        float t = 0f;
        while (t < 0.13f)
        {
            t += Time.deltaTime;
            cardRT.localScale = new Vector3(Mathf.Lerp(1f, 0f, t / 0.13f), 1f, 1f);
            yield return null;
        }

        // Update data + rebuild card at same position
        var c = _dealer[idx]; c.faceDown = false; _dealer[idx] = c;
        Vector2 savedPos = cardRT.anchoredPosition;
        Destroy(cardRT.gameObject);
        var newCard = MakeCard(_dealerRow, _dealer[idx]);
        newCard.SetSiblingIndex(idx);
        newCard.anchoredPosition = savedPos;
        newCard.localScale       = new Vector3(0f, 1f, 1f);

        // Flip: scale X → 1
        t = 0f;
        while (t < 0.13f)
        {
            t += Time.deltaTime;
            newCard.localScale = new Vector3(Mathf.Lerp(0f, 1f, t / 0.13f), 1f, 1f);
            yield return null;
        }
        newCard.localScale = Vector3.one;
        RefreshTotals();
    }

    // ── Player actions ────────────────────────────────────────────────────────

    void OnHit()   { StartCoroutine(HitRoutine()); }
    void OnStand() { _statusText.text = "Standing."; _playerDone = true; }

    void OnDouble()
    {
        if (GetResource(_betType) < _betValue) { StartCoroutine(FlashStatus("Can't afford double!")); return; }
        AddResource(_betType, -_betValue);
        _betValue *= 2;
        _doubled = true;
        RefreshResources();
        StartCoroutine(DoubleRoutine());
    }

    void OnLeave()
    {
        SetButtons(false);
        _earlyExit = _playerDone = true;
        StartCoroutine(ShowResultDelayed("WALKED AWAY", false, "Bet forfeited.", "\"Running already? Smart.\""));
    }

    IEnumerator HitRoutine()
    {
        SetButtons(false);
        yield return Deal(_player, _playerRow, false, 0.12f);
        int v = Value(_player);
        if      (v > 21) { _statusText.text = "Bust!"; yield return new WaitForSeconds(0.4f); _playerDone = true; }
        else if (v == 21){ _statusText.text = "21!";   yield return new WaitForSeconds(0.3f); _playerDone = true; }
        else               SetButtons(true);
    }

    IEnumerator DoubleRoutine()
    {
        SetButtons(false);
        yield return Deal(_player, _playerRow, false, 0.12f);
        _statusText.text = "Doubled — standing.";
        yield return new WaitForSeconds(0.4f);
        _playerDone = true;
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    IEnumerator ResolveRound()
    {
        int pv = Value(_player), dv = Value(_dealer);
        bool bust   = pv > 21, dBust = dv > 21;
        bool bjack  = pv == 21 && _player.Count == 2;
        bool win    = !bust && (dBust || pv > dv);
        bool push   = !bust && !dBust && pv == dv;

        string title, sub, npc;
        bool   isWin;

        if (bjack && !(_dealer.Count == 2 && dv == 21))
        {
            int pay = _betValue * 3; AddResource(_betType, pay);
            title = "BLACKJACK!"; sub = "+" + pay + " " + ResourceNames[_betType] + "  (3×)";
            npc = Random.value < 0.5f ? DealerLose[Random.Range(0, DealerLose.Length)]
                                      : NpcLose[Random.Range(0, NpcLose.Length)];
            isWin = true;
        }
        else if (win)
        {
            int pay = _betValue * 2; AddResource(_betType, pay);
            title = "YOU WIN"; sub = "+" + pay + " " + ResourceNames[_betType] + "  (2×)";
            npc = Random.value < 0.5f ? DealerLose[Random.Range(0, DealerLose.Length)]
                                      : NpcLose[Random.Range(0, NpcLose.Length)];
            isWin = true;
        }
        else if (push)
        {
            AddResource(_betType, _betValue);
            title = "PUSH"; sub = "Bet returned.";
            npc = Random.value < 0.5f ? "Dealer: \"Too close. I'll let you have that one.\""
                                      : "Survivor: \"A tie. How anticlimactic.\"";
            isWin = false;
        }
        else
        {
            title = bust ? "BUST!" : "DEALER WINS";
            sub = "-" + _betValue + " " + ResourceNames[_betType];
            npc = Random.value < 0.5f ? DealerWin[Random.Range(0, DealerWin.Length)]
                                      : NpcWin[Random.Range(0, NpcWin.Length)];
            isWin = false;
        }

        RefreshResources();

        // Flash outcome in the game panel status text so player sees it first
        _statusText.text  = title;
        _statusText.color = isWin  ? new Color(0.28f, 1f, 0.42f)
                          : push   ? new Color(0.90f, 0.85f, 0.35f)
                          :          new Color(1f, 0.28f, 0.28f);
        yield return new WaitForSeconds(1.6f);

        // Fade result panel in
        SwitchTo("result");
        _resultTitle.text   = title;
        _resultTitle.color  = isWin ? new Color(0.28f, 1f, 0.42f) : new Color(1f, 0.26f, 0.26f);
        _resultSub.text     = sub;
        _resultNpcLine.text = npc;
        _resultCG.alpha     = 0f;
        float ft = 0f;
        while (ft < 0.5f) { ft += Time.deltaTime; _resultCG.alpha = ft / 0.5f; yield return null; }
        _resultCG.alpha = 1f;

        yield return StartCoroutine(PulseResult());
    }

    IEnumerator ShowResultDelayed(string title, bool win, string sub, string npc)
    {
        yield return new WaitForSeconds(0.8f);
        SwitchTo("result");
        _resultTitle.text   = title;
        _resultTitle.color  = win ? new Color(0.28f, 1f, 0.42f) : new Color(1f, 0.26f, 0.26f);
        _resultSub.text     = sub;
        _resultNpcLine.text = npc;
        _resultCG.alpha     = 0f;
        float ft = 0f;
        while (ft < 0.5f) { ft += Time.deltaTime; _resultCG.alpha = ft / 0.5f; yield return null; }
        _resultCG.alpha = 1f;
        yield return StartCoroutine(PulseResult());
    }

    IEnumerator PulseResult()
    {
        _resultTitle.transform.localScale = Vector3.one;
        float t = 0f;
        while (t < 0.55f)
        {
            t += Time.deltaTime;
            float s = 1f + 0.14f * Mathf.Sin(t / 0.55f * Mathf.PI * 2.5f);
            _resultTitle.transform.localScale = Vector3.one * s;
            yield return null;
        }
        _resultTitle.transform.localScale = Vector3.one;
    }

    IEnumerator ThinkingDots(Text t, string prefix, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            int dots = Mathf.FloorToInt(elapsed / 0.25f) % 4;
            t.text = prefix + new string('.', dots);
            yield return null;
        }
    }

    IEnumerator FlashStatus(string msg)
    {
        _statusText.text = msg;
        yield return new WaitForSeconds(1.2f);
        _statusText.text = "";
    }

    void SwitchTo(string p)
    {
        if (_bettingPanel != null) _bettingPanel.SetActive(p == "betting");
        if (_gamePanel    != null) _gamePanel.SetActive(p == "game");
        if (_resultPanel  != null) _resultPanel.SetActive(p == "result");
    }

    void SetButtons(bool on)
    {
        if (_hitBtn    != null) _hitBtn.interactable    = on;
        if (_standBtn  != null) _standBtn.interactable  = on;
        if (_doubleBtn != null) _doubleBtn.interactable = on && _player.Count == 2;
        if (_leaveBtn  != null) _leaveBtn.interactable  = on;
    }

    // ── Card logic ────────────────────────────────────────────────────────────

    void BuildDeck()
    {
        _deck.Clear();
        for (int s = 0; s < 4; s++)
        for (int r = 1; r <= 13; r++)
            _deck.Add(new Card { rank = r, suit = s });
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = _deck[i]; _deck[i] = _deck[j]; _deck[j] = tmp;
        }
    }

    Card Draw(bool fd = false)
    {
        if (_deck.Count == 0) BuildDeck();
        var c = _deck[0]; _deck.RemoveAt(0);
        c.faceDown = fd; return c;
    }

    static int Value(List<Card> hand)
    {
        int total = 0, aces = 0;
        foreach (var c in hand)
        {
            if (c.faceDown) continue;
            if (c.rank == 1) { aces++; total += 11; }
            else total += Mathf.Min(c.rank, 10);
        }
        while (total > 21 && aces > 0) { total -= 10; aces--; }
        return total;
    }

    void RefreshTotals()
    {
        if (_playerTotal != null)
            _playerTotal.text = "YOU: " + (Value(_player) > 0 ? Value(_player).ToString() : "—");

        if (_npcTotal != null)
            _npcTotal.text = Value(_npc) > 0 ? Value(_npc).ToString() : "—";

        var vis = new List<Card>();
        foreach (var c in _dealer) if (!c.faceDown) vis.Add(c);
        if (_dealerTotal != null)
            _dealerTotal.text = "DEALER: " + (Value(vis) > 0 ? Value(vis).ToString() : "—");
    }

    // ── Card visuals ──────────────────────────────────────────────────────────

    IEnumerator Deal(List<Card> hand, RectTransform row, bool fd, float delay)
    {
        var card = Draw(fd);
        hand.Add(card);
        var rt = MakeCard(row, card);

        rt.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.deltaTime;
            float p = t / 0.16f;
            float s = p < 0.65f ? Mathf.Lerp(0f, 1.15f, p / 0.65f)
                                 : Mathf.Lerp(1.15f, 1f, (p - 0.65f) / 0.35f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
        Reposition(row, hand.Count);
        RefreshTotals();
        yield return new WaitForSeconds(delay);
    }

    RectTransform MakeCard(RectTransform row, Card card)
    {
        var rt = Ctr("Card", row, Vector2.zero, new Vector2(CW, CH));

        if (card.faceDown)
        {
            rt.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.06f, 0.06f);
            // Inner border
            Ctr("Inn", rt, Vector2.zero, new Vector2(CW - 8f, CH - 8f))
                .gameObject.AddComponent<Image>().color = new Color(0.26f, 0.10f, 0.10f);
            // Repeating ✕ symbol
            Txt(Ctr("Sym", rt, Vector2.zero, new Vector2(CW, CH)),
                "✕", 30, FontStyle.Bold, new Color(0.40f, 0.14f, 0.14f), TextAnchor.MiddleCenter);
        }
        else
        {
            rt.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.93f, 0.90f);
            Color sc  = SuitCol[card.suit];
            string rs = RankStr[card.rank];
            string ss = SuitStr[card.suit];

            // Top-left: rank
            Txt(Ctr("TLR", rt,
                    new Vector2(-(CW * 0.5f - 12f), CH * 0.5f - 13f),
                    new Vector2(26f, 22f)),
                rs, 17, FontStyle.Bold, sc, TextAnchor.MiddleCenter);

            // Top-left: suit (below rank)
            Txt(Ctr("TLS", rt,
                    new Vector2(-(CW * 0.5f - 12f), CH * 0.5f - 32f),
                    new Vector2(22f, 18f)),
                ss, 13, FontStyle.Normal, sc, TextAnchor.MiddleCenter);

            // Center watermark suit (large, faint)
            Txt(Ctr("CS", rt, new Vector2(0f, 4f), new Vector2(CW - 6f, CH - 20f)),
                ss, 48, FontStyle.Bold,
                new Color(sc.r, sc.g, sc.b, 0.14f), TextAnchor.MiddleCenter);

            // Center rank (prominent)
            Txt(Ctr("CR", rt, new Vector2(0f, 4f), new Vector2(CW - 6f, CH - 20f)),
                rs, 32, FontStyle.Bold, sc, TextAnchor.MiddleCenter);

            // Bottom-right rank (rotated)
            var brR = Ctr("BRR", rt,
                new Vector2(CW * 0.5f - 12f, -(CH * 0.5f - 13f)),
                new Vector2(26f, 22f));
            brR.localEulerAngles = new Vector3(0f, 0f, 180f);
            Txt(brR, rs, 17, FontStyle.Bold, sc, TextAnchor.MiddleCenter);

            // Bottom-right suit (rotated)
            var brS = Ctr("BRS", rt,
                new Vector2(CW * 0.5f - 12f, -(CH * 0.5f - 32f)),
                new Vector2(22f, 18f));
            brS.localEulerAngles = new Vector3(0f, 0f, 180f);
            Txt(brS, ss, 13, FontStyle.Normal, sc, TextAnchor.MiddleCenter);

            // Thin horizontal dividers (top and bottom strip, matches card feel)
            var tDiv = Rt("TDiv", rt);
            tDiv.anchorMin = new Vector2(0f, 1f); tDiv.anchorMax = new Vector2(1f, 1f);
            tDiv.pivot = new Vector2(0.5f, 1f); tDiv.sizeDelta = new Vector2(0f, 1.5f);
            tDiv.anchoredPosition = new Vector2(0f, -44f);
            tDiv.gameObject.AddComponent<Image>().color = new Color(sc.r, sc.g, sc.b, 0.10f);

            var bDiv = Rt("BDiv", rt);
            bDiv.anchorMin = new Vector2(0f, 0f); bDiv.anchorMax = new Vector2(1f, 0f);
            bDiv.pivot = new Vector2(0.5f, 0f); bDiv.sizeDelta = new Vector2(0f, 1.5f);
            bDiv.anchoredPosition = new Vector2(0f, 44f);
            bDiv.gameObject.AddComponent<Image>().color = new Color(sc.r, sc.g, sc.b, 0.10f);
        }
        return rt;
    }

    void Reposition(RectTransform row, int count)
    {
        if (count == 0) return;
        float total = count * CW + (count - 1) * CGap;
        float x0    = -total * 0.5f + CW * 0.5f;
        for (int i = 0; i < row.childCount; i++)
        {
            var c = row.GetChild(i) as RectTransform;
            if (c != null) c.anchoredPosition = new Vector2(x0 + i * (CW + CGap), 0f);
        }
    }

    void ClearRow(RectTransform row)
    {
        for (int i = row.childCount - 1; i >= 0; i--)
            Destroy(row.GetChild(i).gameObject);
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("BlackjackCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 26;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        var dim = Rt("Dim", cGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

        // Main felt panel
        var panel = Ctr("Panel", cGO.transform, Vector2.zero, new Vector2(1120f, 700f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.11f, 0.07f, 0.98f);

        // Gold border
        Border("BrdT", panel, new Vector2(0,1), new Vector2(1,1), new Vector2(.5f,1), new Vector2(0,3));
        Border("BrdB", panel, new Vector2(0,0), new Vector2(1,0), new Vector2(.5f,0), new Vector2(0,3));
        Border("BrdL", panel, new Vector2(0,0), new Vector2(0,1), new Vector2(0,.5f), new Vector2(3,0));
        Border("BrdR", panel, new Vector2(1,0), new Vector2(1,1), new Vector2(1,.5f), new Vector2(3,0));

        // Title — centered at the top, no resource strip overlapping it
        Txt(Ctr("Title", panel, new Vector2(0f, 320f), new Vector2(1060f, 48f)),
            "♠  BLACKJACK — SURVIVORS TABLE  ♠", 27, FontStyle.Bold,
            new Color(0.68f, 0.52f, 0.20f), TextAnchor.MiddleCenter);

        BuildBettingPanel(panel);
        BuildGamePanel(panel);
        BuildResultPanel(panel);

        _bettingPanel.SetActive(false);
        _gamePanel.SetActive(false);
        _resultPanel.SetActive(false);
    }

    void Border(string n, RectTransform p, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 sz)
    {
        var b = Rt(n, p);
        b.anchorMin = aMin; b.anchorMax = aMax; b.pivot = piv;
        b.sizeDelta = sz; b.anchoredPosition = Vector2.zero;
        b.gameObject.AddComponent<Image>().color = new Color(0.52f, 0.38f, 0.12f);
    }

    // ── Betting Panel ─────────────────────────────────────────────────────────

    void BuildBettingPanel(RectTransform panel)
    {
        var bp = Ctr("BettingPanel", panel, new Vector2(0f, -10f), new Vector2(700f, 570f));
        bp.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.05f, 0.96f);
        _bettingPanel = bp.gameObject;

        // Inner gold border
        var ib = Rt("IB", bp);
        ib.anchorMin = Vector2.zero; ib.anchorMax = Vector2.one;
        ib.sizeDelta = new Vector2(-6f, -6f); ib.anchoredPosition = Vector2.zero;
        ib.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.26f, 0.08f, 0.30f);

        Txt(Ctr("H1", bp, new Vector2(0f, 220f), new Vector2(620f, 44f)),
            "PLACE YOUR BET", 30, FontStyle.Bold,
            new Color(0.65f, 0.48f, 0.15f), TextAnchor.MiddleCenter);

        // ── Resource bar ──────────────────────────────────────────────────────
        var resBar = Ctr("ResBar", bp, new Vector2(0f, 170f), new Vector2(620f, 38f));
        resBar.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.04f);

        Color[] rCol = { new Color(0.95f, 0.78f, 0.30f), new Color(0.55f, 0.78f, 0.95f), new Color(0.40f, 0.92f, 0.52f) };
        float[] rxs = { -185f, 0f, 185f };
        var bResTexts = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var cell = Ctr("RC" + i, resBar, new Vector2(rxs[i], 0f), new Vector2(180f, 34f));
            bResTexts[i] = Txt(cell, ResourceNames[i] + "  2", 16, FontStyle.Bold, rCol[i], TextAnchor.MiddleCenter);
        }
        _bAmmo = bResTexts[0]; _bScrap = bResTexts[1]; _bMeds = bResTexts[2];

        Txt(Ctr("Sub", bp, new Vector2(0f, 132f), new Vector2(580f, 26f)),
            "Survivors are watching. Choose wisely.", 16, FontStyle.Italic,
            new Color(0.42f, 0.58f, 0.42f), TextAnchor.MiddleCenter);

        // ── Bet type selection ─────────────────────────────────────────────────
        Txt(Ctr("WageLbl", bp, new Vector2(0f, 96f), new Vector2(580f, 24f)),
            "WAGER TYPE", 14, FontStyle.Bold,
            new Color(0.40f, 0.52f, 0.40f), TextAnchor.MiddleCenter);

        string[] rNames = { "AMMO", "SCRAP", "MEDS" };
        float[]  bxs    = { -175f, 0f, 175f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var bRT = Ctr("BT" + i, bp, new Vector2(bxs[i], 55f), new Vector2(148f, 52f));
            _betTypeBgs[i] = bRT.gameObject.AddComponent<Image>();
            _betTypeBgs[i].color = new Color(0.14f, 0.20f, 0.14f);
            var btn = bRT.gameObject.AddComponent<Button>();
            var bc = btn.colors;
            bc.highlightedColor = new Color(0.28f, 0.42f, 0.22f);
            bc.pressedColor     = new Color(0.08f, 0.14f, 0.08f);
            btn.colors = bc;
            btn.onClick.AddListener(() => { _betType = idx; RefreshBetUI(); });
            Txt(Ctr("L", bRT, Vector2.zero, bRT.sizeDelta),
                rNames[i], 18, FontStyle.Bold, rCol[i], TextAnchor.MiddleCenter);
        }

        // ── Amount control ─────────────────────────────────────────────────────
        Txt(Ctr("AmtLbl", bp, new Vector2(0f, 5f), new Vector2(300f, 24f)),
            "AMOUNT", 14, FontStyle.Bold,
            new Color(0.40f, 0.52f, 0.40f), TextAnchor.MiddleCenter);

        var minRT = Ctr("Min", bp, new Vector2(-70f, -36f), new Vector2(54f, 54f));
        minRT.gameObject.AddComponent<Image>().color = new Color(0.26f, 0.16f, 0.08f);
        var minBtn = minRT.gameObject.AddComponent<Button>();
        var mc = minBtn.colors; mc.highlightedColor = new Color(0.40f, 0.26f, 0.12f); minBtn.colors = mc;
        minBtn.onClick.AddListener(() => { if (_betValue > 1) _betValue--; RefreshBetUI(); });
        Txt(Ctr("L", minRT, Vector2.zero, minRT.sizeDelta), "−", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var valRT = Ctr("Val", bp, new Vector2(0f, -36f), new Vector2(66f, 54f));
        valRT.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.12f, 0.07f);
        _betValueText = Txt(Ctr("VT", valRT, Vector2.zero, valRT.sizeDelta),
            "1", 26, FontStyle.Bold, new Color(1f, 0.90f, 0.30f), TextAnchor.MiddleCenter);

        var plusRT = Ctr("Plus", bp, new Vector2(70f, -36f), new Vector2(54f, 54f));
        plusRT.gameObject.AddComponent<Image>().color = new Color(0.26f, 0.16f, 0.08f);
        var plusBtn = plusRT.gameObject.AddComponent<Button>();
        var pc = plusBtn.colors; pc.highlightedColor = new Color(0.40f, 0.26f, 0.12f); plusBtn.colors = pc;
        plusBtn.onClick.AddListener(() => { if (_betValue < GetResource(_betType)) _betValue++; RefreshBetUI(); });
        Txt(Ctr("L", plusRT, Vector2.zero, plusRT.sizeDelta), "+", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // ── Deal button ────────────────────────────────────────────────────────
        var dealRT = Ctr("Deal", bp, new Vector2(0f, -118f), new Vector2(235f, 60f));
        dealRT.gameObject.AddComponent<Image>().color = new Color(0.44f, 0.30f, 0.08f);
        var dealBtn = dealRT.gameObject.AddComponent<Button>();
        var dc = dealBtn.colors;
        dc.highlightedColor = new Color(0.62f, 0.44f, 0.14f);
        dc.pressedColor     = new Color(0.28f, 0.18f, 0.05f);
        dealBtn.colors = dc;
        dealBtn.onClick.AddListener(OnDeal);
        Txt(Ctr("L", dealRT, Vector2.zero, dealRT.sizeDelta),
            "DEAL CARDS", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // ── Instructions ──────────────────────────────────────────────────────
        var infoBox = Ctr("Rules", bp, new Vector2(0f, -176f), new Vector2(640f, 72f));
        infoBox.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.14f, 0.08f, 0.98f);
        var ib2 = Rt("RB2", infoBox); ib2.anchorMin = Vector2.zero; ib2.anchorMax = Vector2.one;
        ib2.sizeDelta = new Vector2(-4f, -4f); ib2.anchoredPosition = Vector2.zero;
        ib2.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.52f, 0.30f, 0.18f);
        Txt(Ctr("RL1", infoBox, new Vector2(0f, 14f), new Vector2(620f, 26f)),
            "Get to 21 without busting. Beat the dealer. Blackjack (Ace + 10 / J / Q / K) pays 3x!",
            14, FontStyle.Bold, new Color(0.72f, 0.92f, 0.72f), TextAnchor.MiddleCenter);
        Txt(Ctr("RL2", infoBox, new Vector2(0f, -14f), new Vector2(620f, 26f)),
            "Dealer hits to 16, stands on 17+.    HIT: draw    STAND: end turn    DOUBLE: 2x bet + 1 card",
            13, FontStyle.Normal, new Color(0.55f, 0.76f, 0.55f), TextAnchor.MiddleCenter);

        // Walk away
        var walkRT = Ctr("Walk", bp, new Vector2(0f, -243f), new Vector2(160f, 36f));
        walkRT.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.10f, 0.10f);
        var walkBtn = walkRT.gameObject.AddComponent<Button>();
        var wc = walkBtn.colors; wc.highlightedColor = new Color(0.32f, 0.16f, 0.16f); walkBtn.colors = wc;
        walkBtn.onClick.AddListener(() => _done = true);
        Txt(Ctr("L", walkRT, Vector2.zero, walkRT.sizeDelta),
            "WALK AWAY", 15, FontStyle.Italic,
            new Color(0.60f, 0.40f, 0.40f), TextAnchor.MiddleCenter);
    }

    // ── Game Panel ────────────────────────────────────────────────────────────

    void BuildGamePanel(RectTransform panel)
    {
        var gp = Ctr("GamePanel", panel, new Vector2(0f, -8f), new Vector2(1100f, 720f));
        _gamePanel = gp.gameObject;

        // ── Resource strip — right side, below title ──────────────────────────
        var resBox = Ctr("ResBox", gp, new Vector2(390f, 268f), new Vector2(270f, 54f));
        resBox.gameObject.AddComponent<Image>().color = new Color(0.03f, 0.07f, 0.04f, 0.90f);

        Color[] rc   = { new Color(0.90f,0.74f,0.28f), new Color(0.55f,0.70f,0.82f), new Color(0.38f,0.84f,0.48f) };
        float[] ry   = { 17f, 0f, -17f };
        string[] rn  = { "AMMO", "SCRAP", "MEDS" };
        var gRes = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var cell = Ctr("GR" + i, resBox, new Vector2(-10f, ry[i]), new Vector2(260f, 16f));
            gRes[i] = Txt(cell, rn[i] + "  2", 13, FontStyle.Bold, rc[i], TextAnchor.MiddleCenter);
        }
        _gAmmo = gRes[0]; _gScrap = gRes[1]; _gMeds = gRes[2];

        // ── Dealer area ───────────────────────────────────────────────────────
        // Push dealer and player sections right to leave room for NPC box on left
        var dSec = Ctr("DSec", gp, new Vector2(90f, 210f), new Vector2(870f, 140f));

        var dHeader = Ctr("DHdr", dSec, new Vector2(-400f, 52f), new Vector2(300f, 36f));
        Txt(Ctr("DLbl", dHeader, new Vector2(-88f, 0f), new Vector2(110f, 34f)),
            "DEALER", 18, FontStyle.Bold, new Color(0.52f, 0.38f, 0.12f), TextAnchor.MiddleLeft);
        _dealerTotal = Txt(Ctr("DTot", dHeader, new Vector2(62f, 0f), new Vector2(130f, 34f)),
            "—", 22, FontStyle.Bold, new Color(0.70f, 0.88f, 0.70f), TextAnchor.MiddleLeft);

        _dealerRow = Ctr("DRow", dSec, new Vector2(0f, -14f), new Vector2(860f, 128f));

        // ── NPC box — left sidebar ────────────────────────────────────────────
        var npcBox = Ctr("NpcBox", gp, new Vector2(-435f, 52f), new Vector2(190f, 240f));
        npcBox.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.05f, 0.85f);
        var nb = Rt("NBrd", npcBox); nb.anchorMin = Vector2.zero; nb.anchorMax = Vector2.one;
        nb.sizeDelta = new Vector2(-4f, -4f); nb.anchoredPosition = Vector2.zero;
        nb.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.32f, 0.20f, 0.20f);

        Txt(Ctr("NLbl", npcBox, new Vector2(0f, 100f), new Vector2(175f, 22f)),
            "★  SURVIVOR", 13, FontStyle.Bold, new Color(0.50f, 0.78f, 0.50f), TextAnchor.MiddleCenter);
        _npcTotal = Txt(Ctr("NTot", npcBox, new Vector2(0f, 76f), new Vector2(175f, 28f)),
            "—", 17, FontStyle.Bold, new Color(0.68f, 0.88f, 0.68f), TextAnchor.MiddleCenter);
        _npcRow = Ctr("NRow", npcBox, new Vector2(0f, -30f), new Vector2(180f, 128f));

        // ── Divider ───────────────────────────────────────────────────────────
        var div = Rt("Div", gp);
        div.anchorMin = new Vector2(0.02f, 0.5f); div.anchorMax = new Vector2(0.98f, 0.5f);
        div.pivot = new Vector2(0.5f, 0.5f); div.sizeDelta = new Vector2(0f, 2f);
        div.anchoredPosition = new Vector2(0f, -56f);
        div.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.30f, 0.18f, 0.5f);

        // ── Player area ───────────────────────────────────────────────────────
        var pSec = Ctr("PSec", gp, new Vector2(90f, -175f), new Vector2(870f, 140f));

        var pHeader = Ctr("PHdr", pSec, new Vector2(-400f, 52f), new Vector2(300f, 36f));
        Txt(Ctr("PLbl", pHeader, new Vector2(-88f, 0f), new Vector2(70f, 34f)),
            "YOU", 18, FontStyle.Bold, new Color(0.90f, 0.74f, 0.28f), TextAnchor.MiddleLeft);
        _playerTotal = Txt(Ctr("PTot", pHeader, new Vector2(50f, 0f), new Vector2(150f, 34f)),
            "—", 22, FontStyle.Bold, new Color(1f, 0.92f, 0.50f), TextAnchor.MiddleLeft);

        _playerRow = Ctr("PRow", pSec, new Vector2(0f, -14f), new Vector2(860f, 128f));

        // ── Status + NPC dialogue ─────────────────────────────────────────────
        _statusText = Txt(Ctr("Status", gp, new Vector2(90f, -56f), new Vector2(500f, 34f)),
            "", 20, FontStyle.Bold, new Color(0.62f, 0.88f, 0.62f), TextAnchor.MiddleCenter);

        _npcDialogueText = Txt(Ctr("NpcDlg", gp, new Vector2(-435f, -88f), new Vector2(188f, 22f)),
            "", 11, FontStyle.Italic, new Color(0.45f, 0.60f, 0.45f), TextAnchor.MiddleCenter);

        // ── Action buttons ─────────────────────────────────────────────────────
        float by = -316f;
        _hitBtn    = ActionBtn(gp, "HIT",    new Vector2(-258f, by), new Color(0.20f, 0.46f, 0.18f), OnHit);
        _standBtn  = ActionBtn(gp, "STAND",  new Vector2( -86f, by), new Color(0.44f, 0.34f, 0.07f), OnStand);
        _doubleBtn = ActionBtn(gp, "DOUBLE", new Vector2(  86f, by), new Color(0.16f, 0.28f, 0.52f), OnDouble);
        _leaveBtn  = ActionBtn(gp, "LEAVE",  new Vector2( 258f, by), new Color(0.40f, 0.10f, 0.10f), OnLeave);
    }

    Button ActionBtn(RectTransform p, string label, Vector2 pos, Color col,
                     UnityEngine.Events.UnityAction action)
    {
        var rt = Ctr(label + "Btn", p, pos, new Vector2(150f, 58f));
        rt.gameObject.AddComponent<Image>().color = col;
        var btn = rt.gameObject.AddComponent<Button>();
        var bc = btn.colors;
        bc.highlightedColor = Color.Lerp(col, Color.white, 0.22f);
        bc.pressedColor     = Color.Lerp(col, Color.black, 0.32f);
        bc.disabledColor    = new Color(0.18f, 0.18f, 0.18f, 0.50f);
        btn.colors = bc;
        btn.onClick.AddListener(action);
        Txt(Ctr("L", rt, Vector2.zero, rt.sizeDelta), label, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        return btn;
    }

    // ── Result Panel ──────────────────────────────────────────────────────────

    void BuildResultPanel(RectTransform panel)
    {
        var rp = Ctr("ResultPanel", panel, new Vector2(0f, -18f), new Vector2(600f, 360f));
        rp.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.05f, 0.97f);
        _resultPanel = rp.gameObject;
        _resultCG = rp.gameObject.AddComponent<CanvasGroup>();

        var rb = Rt("RB", rp); rb.anchorMin = Vector2.zero; rb.anchorMax = Vector2.one;
        rb.sizeDelta = new Vector2(-6f, -6f); rb.anchoredPosition = Vector2.zero;
        rb.gameObject.AddComponent<Image>().color = new Color(0.32f, 0.24f, 0.07f, 0.28f);

        _resultTitle = Txt(Ctr("RT", rp, new Vector2(0f, 122f), new Vector2(540f, 72f)),
            "", 44, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        _resultSub = Txt(Ctr("RS", rp, new Vector2(0f, 66f), new Vector2(540f, 34f)),
            "", 21, FontStyle.Normal, new Color(0.70f, 0.80f, 0.70f), TextAnchor.MiddleCenter);
        _resultNpcLine = Txt(Ctr("RN", rp, new Vector2(0f, 22f), new Vector2(540f, 28f)),
            "", 15, FontStyle.Italic, new Color(0.48f, 0.62f, 0.48f), TextAnchor.MiddleCenter);

        // Thin divider
        var rdiv = Rt("RDiv", rp);
        rdiv.anchorMin = new Vector2(0.08f, 0.5f); rdiv.anchorMax = new Vector2(0.92f, 0.5f);
        rdiv.pivot = new Vector2(0.5f, 0.5f); rdiv.sizeDelta = new Vector2(0f, 1f);
        rdiv.anchoredPosition = new Vector2(0f, -8f);
        rdiv.gameObject.AddComponent<Image>().color = new Color(0.30f, 0.22f, 0.07f, 0.40f);

        // Buttons
        var leaveRT = Ctr("LvBtn", rp, new Vector2(-85f, -96f), new Vector2(195f, 52f));
        leaveRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.42f, 0.18f);
        var lv = leaveRT.gameObject.AddComponent<Button>();
        var lc = lv.colors; lc.highlightedColor = new Color(0.26f, 0.58f, 0.26f); lv.colors = lc;
        lv.onClick.AddListener(() => _done = true);
        Txt(Ctr("L", leaveRT, Vector2.zero, leaveRT.sizeDelta),
            "LEAVE TABLE", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        var agRT = Ctr("AgBtn", rp, new Vector2(95f, -96f), new Vector2(195f, 52f));
        agRT.gameObject.AddComponent<Image>().color = new Color(0.38f, 0.28f, 0.08f);
        var ag = agRT.gameObject.AddComponent<Button>();
        var ac = ag.colors; ac.highlightedColor = new Color(0.54f, 0.40f, 0.12f); ag.colors = ac;
        ag.onClick.AddListener(() =>
        {
            _betValue = 1;
            RefreshBetUI();
            RefreshResources();
            SwitchTo("betting");
        });
        Txt(Ctr("L", agRT, Vector2.zero, agRT.sizeDelta),
            "PLAY AGAIN", 19, FontStyle.Bold, new Color(1f, 0.88f, 0.30f), TextAnchor.MiddleCenter);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform Rt(string n, Transform p)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Ctr(string n, Transform p, Vector2 pos, Vector2 sz)
    {
        var rt = Rt(n, p);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        return rt;
    }

    static Text Txt(RectTransform rt, string val, int sz, FontStyle fs, Color col, TextAnchor a)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text = val; t.fontSize = sz; t.fontStyle = fs; t.color = col; t.alignment = a;
        t.font = DefaultFont(); return t;
    }

    static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
}
