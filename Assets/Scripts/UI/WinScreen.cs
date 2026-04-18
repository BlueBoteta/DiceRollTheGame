using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinScreen : MonoBehaviour
{
    public static WinScreen Instance { get; private set; }

    Canvas      _canvas;
    CanvasGroup _cg;
    Text        _titleText;
    Text        _subText;
    Text        _statsText;
    Text        _quoteText;

    static readonly string[] EndQuotes =
    {
        "The silence is almost worse.",
        "You survived. Don't ask how.",
        "It cost everything. Worth it.",
        "The city holds its breath.\nFor now.",
        "One less nightmare in the dark.",
    };

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public void Show()
    {
        _canvas.gameObject.SetActive(true);
        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        // Populate stats
        int loops = GameManager.Instance != null ? GameManager.Instance.LoopCount : 0;
        int hp    = PlayerStats.Instance   != null ? PlayerStats.Instance.hp       : 0;
        _statsText.text = "Laps survived:  " + loops + "       HP remaining:  " + hp;
        _quoteText.text = EndQuotes[Random.Range(0, EndQuotes.Length)];

        // Fade in overlay
        _cg.alpha = 0f;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.SmoothStep(0f, 1f, t / 0.6f);
            yield return null;
        }
        _cg.alpha = 1f;

        // Title bounce in
        _titleText.gameObject.SetActive(true);
        _titleText.transform.localScale = Vector3.zero;
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(0f, 1f, t / 0.25f);
            _titleText.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.12f, s);
            yield return null;
        }
        t = 0f;
        while (t < 0.1f)
        {
            t += Time.unscaledDeltaTime;
            _titleText.transform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, t / 0.1f);
            yield return null;
        }
        _titleText.transform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(0.2f);

        // Subtitle fade in
        yield return StartCoroutine(FadeInText(_subText, 0.35f));

        yield return new WaitForSecondsRealtime(0.15f);

        // Quote fade in
        yield return StartCoroutine(FadeInText(_quoteText, 0.35f));

        yield return new WaitForSecondsRealtime(0.15f);

        // Stats fade in
        yield return StartCoroutine(FadeInText(_statsText, 0.3f));

        // Start slow title pulse
        StartCoroutine(PulseTitle());
    }

    IEnumerator FadeInText(Text txt, float dur)
    {
        Color base_ = txt.color;
        txt.color = new Color(base_.r, base_.g, base_.b, 0f);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            txt.color = new Color(base_.r, base_.g, base_.b, t / dur);
            yield return null;
        }
        txt.color = base_;
    }

    IEnumerator PulseTitle()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + 0.03f * Mathf.Sin(t * 1.8f);
            _titleText.transform.localScale = Vector3.one * s;
            yield return null;
        }
    }

    // ── UI Construction ──────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("WinCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        _cg = canvasGO.AddComponent<CanvasGroup>();

        // Near-black overlay with cold blue tint
        var dim = Rt("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.97f);

        // Thin gold top bar
        var bar = Rt("Bar", canvasGO.transform);
        bar.anchorMin = new Vector2(0,1); bar.anchorMax = new Vector2(1,1);
        bar.pivot = new Vector2(0.5f,1); bar.sizeDelta = new Vector2(0,4);
        bar.anchoredPosition = Vector2.zero;
        bar.gameObject.AddComponent<Image>().color = new Color(0.75f, 0.62f, 0.18f);

        // "YOU SURVIVED" — starts hidden, revealed in coroutine
        var titleRT = Ctr("Title", canvasGO.transform, new Vector2(0, 130), new Vector2(900, 140));
        _titleText = Txt(titleRT, "YOU  SURVIVED", 96, FontStyle.Bold,
                         new Color(0.95f, 0.82f, 0.28f), TextAnchor.MiddleCenter);
        _titleText.gameObject.SetActive(false);

        // Separator
        var sep = Rt("Sep", canvasGO.transform);
        sep.anchorMin = new Vector2(0.2f,0.5f); sep.anchorMax = new Vector2(0.8f,0.5f);
        sep.pivot = new Vector2(0.5f,0.5f); sep.sizeDelta = new Vector2(0,1);
        sep.anchoredPosition = new Vector2(0, 42);
        sep.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.38f, 0.12f);

        // Subtitle (enemy defeated line)
        var subRT = Ctr("Sub", canvasGO.transform, new Vector2(0, 0), new Vector2(720, 50));
        _subText = Txt(subRT, "The Horde Boss falls.  The city holds its breath.", 28,
                       FontStyle.Italic, new Color(0.72f, 0.68f, 0.58f), TextAnchor.MiddleCenter);
        _subText.color = new Color(_subText.color.r, _subText.color.g, _subText.color.b, 0f);

        // Random dark quote
        var quoteRT = Ctr("Quote", canvasGO.transform, new Vector2(0, -58), new Vector2(640, 60));
        _quoteText = Txt(quoteRT, "", 22, FontStyle.Italic,
                         new Color(0.45f, 0.45f, 0.50f), TextAnchor.MiddleCenter);
        _quoteText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _quoteText.verticalOverflow   = VerticalWrapMode.Overflow;
        _quoteText.color = new Color(_quoteText.color.r, _quoteText.color.g, _quoteText.color.b, 0f);

        // Stats line
        var statsRT = Ctr("Stats", canvasGO.transform, new Vector2(0, -130), new Vector2(640, 36));
        _statsText = Txt(statsRT, "", 22, FontStyle.Normal,
                         new Color(0.55f, 0.55f, 0.62f), TextAnchor.MiddleCenter);
        _statsText.color = new Color(_statsText.color.r, _statsText.color.g, _statsText.color.b, 0f);

        // Play Again button
        var btnRT = Ctr("PlayAgain", canvasGO.transform, new Vector2(0, -230), new Vector2(260, 62));
        btnRT.gameObject.AddComponent<Image>().color = new Color(0.52f, 0.42f, 0.08f);
        var btn = btnRT.gameObject.AddComponent<Button>();
        var bc  = btn.colors;
        bc.highlightedColor = new Color(0.72f, 0.60f, 0.14f);
        bc.pressedColor     = new Color(0.35f, 0.28f, 0.05f);
        btn.colors = bc;
        btn.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        var lblRT = Rt("Lbl", btnRT);
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one; lblRT.sizeDelta = Vector2.zero;
        Txt(lblRT, "PLAY AGAIN", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform Rt(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Ctr(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Rt(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Text Txt(RectTransform rt, string val, int size, FontStyle style,
                    Color color, TextAnchor align)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.text      = val;
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = align;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }
}
