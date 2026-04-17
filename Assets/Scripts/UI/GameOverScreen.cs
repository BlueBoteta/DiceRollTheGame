using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    Canvas _canvas;
    Text   _titleText;

    void Awake()
    {
        Instance = this;
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public void Show()
    {
        Time.timeScale = 0f;
        _canvas.gameObject.SetActive(true);
        StartCoroutine(PulseTitle());
    }

    IEnumerator PulseTitle()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            float scale = 1f + 0.05f * Mathf.Sin(t * 2.8f);
            _titleText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("GameOverCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full dark overlay
        var dim = Rect("Dim", canvasGO.transform);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.sizeDelta = Vector2.zero;
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.93f);

        // GAME OVER title
        var titleRT = Center("Title", canvasGO.transform, new Vector2(0, 100), new Vector2(900, 130));
        _titleText = AddText(titleRT, "GAME  OVER", 100, FontStyle.Bold,
                             new Color(0.88f, 0.08f, 0.08f), TextAnchor.MiddleCenter);

        // Subtitle
        var subRT = Center("Sub", canvasGO.transform, new Vector2(0, -20), new Vector2(700, 50));
        AddText(subRT, "You didn't survive the outbreak...", 30, FontStyle.Italic,
                new Color(0.65f, 0.65f, 0.65f), TextAnchor.MiddleCenter);

        // Restart button
        var btnRT = Center("Restart", canvasGO.transform, new Vector2(0, -130), new Vector2(240, 62));
        btnRT.gameObject.AddComponent<Image>().color = new Color(0.68f, 0.1f, 0.1f);
        var btn = btnRT.gameObject.AddComponent<Button>();
        var col = btn.colors;
        col.highlightedColor = new Color(0.85f, 0.18f, 0.18f);
        col.pressedColor     = new Color(0.45f, 0.06f, 0.06f);
        btn.colors = col;
        btn.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        var lblRT = Rect("Lbl", btnRT);
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.sizeDelta = Vector2.zero;
        AddText(lblRT, "RESTART", 32, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
    }

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform Center(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Text AddText(RectTransform rt, string val, int size, FontStyle style,
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
