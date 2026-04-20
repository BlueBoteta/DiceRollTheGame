using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PinTumblerMinigame : MonoBehaviour
{
    public static PinTumblerMinigame Instance { get; private set; }

    Canvas          _canvas;
    RectTransform[] _driverRTs  = new RectTransform[Pins];
    Image[]         _driverImgs = new Image[Pins];
    Image[]         _shaftBgs   = new Image[Pins];
    RectTransform[] _zoneRTs    = new RectTransform[Pins];
    RectTransform   _timerFill;
    Text            _pinCountText;
    Text            _feedbackText;
    Text            _resultText;
    bool            _holdPressed;
    float           _driverY;

    public bool Result { get; private set; }

    const int   Pins        = 4;
    const float ShaftHeight = 168f;
    const float TotalTime   = 16f;
    const float PushSpeed   = 0.52f;
    const float DropSpeed   = 1.75f;
    const float ZoneSize    = 0.20f;

    static readonly Color ColActive   = new Color(0.24f, 0.24f, 0.32f);
    static readonly Color ColInactive = new Color(0.12f, 0.12f, 0.16f);
    static readonly Color ColSet      = new Color(0.08f, 0.22f, 0.10f);

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
        _holdPressed       = false;
        _timerFill.anchorMax = new Vector2(1f, 1f);

        float[] zoneMin = new float[Pins];
        for (int i = 0; i < Pins; i++)
        {
            zoneMin[i] = Random.Range(0.28f, 0.62f);
            _shaftBgs[i].color = ColInactive;
            _driverImgs[i].color = Color.white;
            _driverRTs[i].anchoredPosition = new Vector2(0f, -ShaftHeight * 0.5f);
            float zoneY = (zoneMin[i] + ZoneSize * 0.5f) * ShaftHeight - ShaftHeight * 0.5f;
            _zoneRTs[i].anchoredPosition = new Vector2(0f, zoneY);
            _zoneRTs[i].sizeDelta = new Vector2(38f, ZoneSize * ShaftHeight);
        }

        _canvas.gameObject.SetActive(true);

        float timeLeft = TotalTime;
        int   allSet   = 0;
        _driverY = 0f;

        for (int pin = 0; pin < Pins && timeLeft > 0f; pin++)
        {
            _driverY = 0f;
            _pinCountText.text   = "PIN  " + (pin + 1) + "  /  " + Pins;
            _shaftBgs[pin].color = ColActive;
            _driverRTs[pin].anchoredPosition = new Vector2(0f, -ShaftHeight * 0.5f);

            bool wasHolding = false;
            bool pinSet     = false;

            while (!pinSet && timeLeft > 0f)
            {
                bool holding = _holdPressed ||
                               UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;

                if (holding)
                    _driverY = Mathf.Min(0.97f, _driverY + PushSpeed * Time.deltaTime);
                else
                    _driverY = Mathf.Max(0f, _driverY - DropSpeed * Time.deltaTime);

                timeLeft -= Time.deltaTime;
                _timerFill.anchorMax = new Vector2(Mathf.Max(0f, timeLeft / TotalTime), 1f);

                // Hit top of shaft → spring back
                if (_driverY >= 0.95f)
                {
                    yield return StartCoroutine(SpringBack(pin));
                    wasHolding = false;
                    yield return null;
                    continue;
                }

                // Player just released → check if in zone
                float zMax = zoneMin[pin] + ZoneSize;
                if (wasHolding && !holding && _driverY >= zoneMin[pin] && _driverY <= zMax)
                {
                    pinSet = true;
                    allSet++;
                    _driverImgs[pin].color = new Color(0.22f, 0.85f, 0.32f);
                    _shaftBgs[pin].color   = ColSet;
                    yield return StartCoroutine(FlashFeedback("CLICK!", true));
                    break;
                }

                wasHolding = holding;
                _driverRTs[pin].anchoredPosition = new Vector2(0f, _driverY * ShaftHeight - ShaftHeight * 0.5f);
                yield return null;
            }
        }

        Result = allSet == Pins;
        _resultText.text  = Result ? "CRACKED!" : "TIME'S UP.";
        _resultText.color = Result
            ? new Color(0.28f, 1f,    0.38f)
            : new Color(1f,    0.22f, 0.22f);

        yield return new WaitForSeconds(1.4f);
        _canvas.gameObject.SetActive(false);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator SpringBack(int pin)
    {
        float startY = _driverY;
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            _driverY = Mathf.Lerp(startY, 0f, p);
            _driverRTs[pin].anchoredPosition = new Vector2(0f, _driverY * ShaftHeight - ShaftHeight * 0.5f);
            _driverImgs[pin].color = Color.Lerp(new Color(1f, 0.20f, 0.20f), Color.white, p);
            yield return null;
        }
        _driverY = 0f;
        _driverImgs[pin].color = Color.white;
        yield return StartCoroutine(FlashFeedback("SLIPPED!", false));
    }

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
        var cGO = new GameObject("PinTumblerCanvas");
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

        var panel = C("Panel", cGO.transform, Vector2.zero, new Vector2(580f, 440f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.09f, 0.98f);

        var top = Mk("Top", panel);
        top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1);
        top.pivot = new Vector2(0.5f, 1); top.sizeDelta = new Vector2(0, 3);
        top.anchoredPosition = Vector2.zero;
        top.gameObject.AddComponent<Image>().color = new Color(0.20f, 0.45f, 0.20f);

        T(C("Title", panel, new Vector2(0, 188), new Vector2(520f, 44f)),
          "CRACK THE SAFE", 30, FontStyle.Bold,
          new Color(0.40f, 0.85f, 0.42f), TextAnchor.MiddleCenter);

        // Timer bar
        var timerBg = C("TimerBg", panel, new Vector2(0, 155), new Vector2(520f, 8f));
        timerBg.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f);
        var fill = Mk("Fill", timerBg);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.sizeDelta = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.78f, 0.28f);
        _timerFill = fill;

        // 4 pin shafts
        float[] xPins = { -90f, -30f, 30f, 90f };
        for (int i = 0; i < Pins; i++)
        {
            var shaft = C("Shaft" + i, panel, new Vector2(xPins[i], 22f), new Vector2(42f, ShaftHeight));
            _shaftBgs[i] = shaft.gameObject.AddComponent<Image>();
            _shaftBgs[i].color = ColInactive;

            var zone = C("Zone" + i, shaft, Vector2.zero, new Vector2(38f, ZoneSize * ShaftHeight));
            zone.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.72f, 0.22f, 0.65f);
            _zoneRTs[i] = zone;

            var drv = C("Driver" + i, shaft, new Vector2(0f, -ShaftHeight * 0.5f), new Vector2(38f, 16f));
            _driverImgs[i] = drv.gameObject.AddComponent<Image>();
            _driverImgs[i].color = Color.white;
            _driverRTs[i] = drv;

            // Pin number label
            T(C("Num" + i, panel, new Vector2(xPins[i], 22f - ShaftHeight * 0.5f - 14f), new Vector2(42f, 20f)),
              (i + 1).ToString(), 13, FontStyle.Bold,
              new Color(0.38f, 0.38f, 0.48f), TextAnchor.MiddleCenter);
        }

        // Feedback + result (overlaid on shafts area)
        _feedbackText = T(C("Feedback", panel, new Vector2(0, 22), new Vector2(480f, 80f)),
                          "", 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        _resultText = T(C("Result", panel, new Vector2(0, 22), new Vector2(480f, 60f)),
                        "", 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        // Pin counter
        _pinCountText = T(C("PinCount", panel, new Vector2(0, -116), new Vector2(400f, 28f)),
                          "PIN  1  /  4", 18, FontStyle.Bold,
                          new Color(0.50f, 0.50f, 0.60f), TextAnchor.MiddleCenter);

        // Instruction
        T(C("Hint", panel, new Vector2(0, -146), new Vector2(520f, 22f)),
          "HOLD  SPACE  TO  PUSH  —  RELEASE  IN  THE  GREEN  ZONE", 13,
          FontStyle.Normal, new Color(0.38f, 0.38f, 0.48f), TextAnchor.MiddleCenter);

        // Hold button
        var holdRT = C("HoldBtn", panel, new Vector2(0, -183), new Vector2(200f, 48f));
        holdRT.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.36f, 0.16f);
        T(C("HoldLbl", holdRT, Vector2.zero, holdRT.sizeDelta),
          "HOLD", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        var et = holdRT.gameObject.AddComponent<EventTrigger>();
        var dn = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        dn.callback.AddListener(_ => _holdPressed = true);
        et.triggers.Add(dn);
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => _holdPressed = false);
        et.triggers.Add(up);
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
