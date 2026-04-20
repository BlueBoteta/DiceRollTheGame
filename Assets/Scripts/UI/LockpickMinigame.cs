using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LockpickMinigame : MonoBehaviour
{
    public static LockpickMinigame Instance { get; private set; }

    Canvas        _canvas;
    RectTransform _needle;
    RectTransform _greenZone;
    RectTransform _timerFill;
    Text          _roundText;
    Text          _feedbackText;
    Text          _resultText;
    bool          _clicked;

    public bool Result { get; private set; }

    const int   Rounds     = 3;
    const float TotalTime  = 12f;
    const float BarWidth   = 480f;
    const float StartZoneW = 110f;
    const float ZoneShrink = 22f;

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public IEnumerator Play()
    {
        Result             = false;
        _feedbackText.text = "";
        _resultText.text   = "";
        _timerFill.anchorMax = new Vector2(1f, 1f);
        _canvas.gameObject.SetActive(true);

        float timeLeft = TotalTime;
        float speed    = 1.6f;
        int   passed   = 0;

        for (int round = 0; round < Rounds; round++)
        {
            float zoneW      = (StartZoneW - round * ZoneShrink) / BarWidth;
            float zoneCenter = Random.Range(0.15f + zoneW * 0.5f, 0.85f - zoneW * 0.5f);
            speed += 0.5f;

            _greenZone.anchorMin = new Vector2(zoneCenter - zoneW * 0.5f, 0f);
            _greenZone.anchorMax = new Vector2(zoneCenter + zoneW * 0.5f, 1f);
            _greenZone.sizeDelta = Vector2.zero;
            _roundText.text      = (round + 1) + " / " + Rounds;
            _clicked             = false;

            float needleNorm = 0.5f;
            while (timeLeft > 0f)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                    _clicked = true;

                timeLeft -= Time.deltaTime;
                _timerFill.anchorMax = new Vector2(Mathf.Max(0f, timeLeft / TotalTime), 1f);
                needleNorm = Mathf.Sin(Time.time * speed) * 0.5f + 0.5f;
                _needle.anchoredPosition = new Vector2((needleNorm - 0.5f) * BarWidth, 0f);

                if (_clicked) break;
                yield return null;
            }

            if (timeLeft <= 0f) break;

            bool hit = needleNorm >= zoneCenter - zoneW * 0.5f &&
                       needleNorm <= zoneCenter + zoneW * 0.5f;

            yield return StartCoroutine(FlashFeedback(hit ? "CLICK!" : "SLIP!", hit));

            if (hit) passed++;
            else     break;
        }

        Result = passed == Rounds;
        _resultText.text  = Result ? "UNLOCKED!" : "FAILED.";
        _resultText.color = Result
            ? new Color(0.28f, 1f,    0.38f)
            : new Color(1f,    0.22f, 0.22f);

        yield return new WaitForSeconds(1.2f);
        _canvas.gameObject.SetActive(false);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator FlashFeedback(string msg, bool success)
    {
        _feedbackText.text  = msg;
        _feedbackText.color = success
            ? new Color(0.28f, 1f,    0.38f)
            : new Color(1f,    0.30f, 0.22f);
        _feedbackText.rectTransform.localScale = Vector3.one * 0.3f;

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            _feedbackText.rectTransform.localScale =
                Vector3.one * Mathf.Lerp(0.3f, 1.2f, Mathf.SmoothStep(0f, 1f, t / 0.12f));
            yield return null;
        }
        _feedbackText.rectTransform.localScale = Vector3.one;
        yield return new WaitForSeconds(0.25f);

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            var c = _feedbackText.color;
            _feedbackText.color = new Color(c.r, c.g, c.b, 1f - t / 0.18f);
            yield return null;
        }
        _feedbackText.text = "";
    }

    // ── UI Construction ───────────────────────────────────────────────────────

    void BuildUI()
    {
        var cGO = new GameObject("LockpickCanvas");
        _canvas = cGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 26;
        var sc = cGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGO.AddComponent<GraphicRaycaster>();

        var dim = Mk("Dim", cGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        var panel = C("Panel", cGO.transform, Vector2.zero, new Vector2(620f, 360f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.09f, 0.98f);

        // Top accent
        var top = Mk("Top", panel);
        top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
        top.pivot = new Vector2(0.5f, 1); top.sizeDelta = new Vector2(0, 3);
        top.anchoredPosition = Vector2.zero;
        top.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.45f, 0.12f);

        T(C("Title", panel, new Vector2(0, 148), new Vector2(560, 44)),
          "PICK THE LOCK", 30, FontStyle.Bold,
          new Color(0.88f, 0.72f, 0.28f), TextAnchor.MiddleCenter);

        BuildLockIcon(panel);

        // Timer bar
        var timerBg = C("TimerBg", panel, new Vector2(0, 28), new Vector2(BarWidth, 8f));
        timerBg.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.10f);
        var fill = Mk("Fill", timerBg);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.sizeDelta = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(0.88f, 0.62f, 0.18f);
        _timerFill = fill;

        // Meter bar
        var meterBg = C("MeterBg", panel, new Vector2(0, -10), new Vector2(BarWidth, 30f));
        meterBg.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f);

        var gzRT = Mk("GreenZone", meterBg);
        gzRT.anchorMin = new Vector2(0.3f, 0f); gzRT.anchorMax = new Vector2(0.5f, 1f);
        gzRT.sizeDelta = Vector2.zero;
        gzRT.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.72f, 0.22f, 0.75f);
        _greenZone = gzRT;

        var ndRT = C("Needle", meterBg, Vector2.zero, new Vector2(3f, 44f));
        ndRT.gameObject.AddComponent<Image>().color = Color.white;
        _needle = ndRT;

        // Round counter
        _roundText = T(C("RoundText", panel, new Vector2(0, -54), new Vector2(400, 28)),
                       "1 / 3", 18, FontStyle.Bold,
                       new Color(0.55f, 0.55f, 0.65f), TextAnchor.MiddleCenter);

        // Instruction
        T(C("Hint", panel, new Vector2(0, -86), new Vector2(520, 22)),
          "SPACE  OR  CLICK  WHEN  THE  NEEDLE  IS  IN  THE  GREEN", 13,
          FontStyle.Normal, new Color(0.38f, 0.38f, 0.48f), TextAnchor.MiddleCenter);

        // Click button
        var btnRT = C("ClickBtn", panel, new Vector2(0, -128), new Vector2(200f, 50f));
        btnRT.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.38f, 0.15f);
        var btn = btnRT.gameObject.AddComponent<Button>();
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.22f, 0.55f, 0.22f);
        bc.pressedColor     = new Color(0.10f, 0.25f, 0.10f);
        btn.colors = bc;
        btn.onClick.AddListener(() => _clicked = true);
        T(C("Lbl", btnRT, Vector2.zero, btnRT.sizeDelta),
          "CLICK!", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // Feedback flash (over lock icon)
        _feedbackText = T(C("Feedback", panel, new Vector2(0, 82), new Vector2(400, 80)),
                          "", 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // Result text (same area, shown after game ends)
        _resultText = T(C("Result", panel, new Vector2(0, 82), new Vector2(500, 60)),
                        "", 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    void BuildLockIcon(RectTransform panel)
    {
        // Padlock body
        C("LockBody", panel, new Vector2(0, 78), new Vector2(52f, 44f))
            .gameObject.AddComponent<Image>().color = new Color(0.35f, 0.30f, 0.20f);

        // Shackle legs
        C("ShackleL", panel, new Vector2(-13f, 110f), new Vector2(9f, 34f))
            .gameObject.AddComponent<Image>().color = new Color(0.45f, 0.40f, 0.25f);
        C("ShackleR", panel, new Vector2(13f, 110f), new Vector2(9f, 34f))
            .gameObject.AddComponent<Image>().color = new Color(0.45f, 0.40f, 0.25f);

        // Shackle top
        C("ShackleTop", panel, new Vector2(0, 126f), new Vector2(35f, 9f))
            .gameObject.AddComponent<Image>().color = new Color(0.45f, 0.40f, 0.25f);

        // Keyhole
        C("Keyhole", panel, new Vector2(0, 80f), new Vector2(12f, 18f))
            .gameObject.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f);

        // Lockpick (angled)
        var pick = C("Lockpick", panel, new Vector2(14f, 72f), new Vector2(38f, 4f));
        pick.eulerAngles = new Vector3(0f, 0f, 22f);
        pick.gameObject.AddComponent<Image>().color = new Color(0.68f, 0.65f, 0.55f);
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
