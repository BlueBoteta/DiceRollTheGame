using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    Text _die1Text;
    Text _die2Text;
    Button _rollButton;
    bool _rolling;

    void Awake()
    {
        BuildUI();
    }

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("DiceCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel background (bottom-right)
        var panel = MakeRect("Panel", canvasGO.transform);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        Anchor(panel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
               new Vector2(-20, 20), new Vector2(340, 180));

        // Dice display row
        var die1GO = MakeRect("Die1", panel);
        _die1Text = die1GO.gameObject.AddComponent<Text>();
        StyleDieText(_die1Text, "?");
        Anchor(die1GO, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
               new Vector2(30, 20), new Vector2(100, 100));

        var plusGO = MakeRect("Plus", panel);
        var plusText = plusGO.gameObject.AddComponent<Text>();
        StyleDieText(plusText, "+");
        plusText.fontSize = 40;
        plusText.color = new Color(0.7f, 0.7f, 0.7f);
        Anchor(plusGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
               new Vector2(0, 20), new Vector2(40, 80));

        var die2GO = MakeRect("Die2", panel);
        _die2Text = die2GO.gameObject.AddComponent<Text>();
        StyleDieText(_die2Text, "?");
        Anchor(die2GO, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
               new Vector2(-30, 20), new Vector2(100, 100));

        // Roll button
        var btnGO = MakeRect("RollButton", panel);
        var btnImg = btnGO.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.7f, 0.12f, 0.12f, 1f);
        _rollButton = btnGO.gameObject.AddComponent<Button>();
        var colors = _rollButton.colors;
        colors.highlightedColor = new Color(0.85f, 0.2f, 0.2f);
        colors.pressedColor = new Color(0.5f, 0.08f, 0.08f);
        _rollButton.colors = colors;
        Anchor(btnGO, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
               new Vector2(0, 8), new Vector2(0, 55));

        var labelGO = MakeRect("Label", btnGO);
        var labelText = labelGO.gameObject.AddComponent<Text>();
        labelText.text = "ROLL";
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontSize = 28;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Anchor(labelGO, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
               Vector2.zero, Vector2.zero);

        _rollButton.onClick.AddListener(() => { if (!_rolling) StartCoroutine(RollDice()); });
    }

    IEnumerator RollDice()
    {
        _rolling = true;
        _rollButton.interactable = false;

        // Spin animation — flash random numbers for 1 second
        float elapsed = 0f;
        float duration = 1f;
        float interval = 0.07f;
        float nextFlip = 0f;

        while (elapsed < duration)
        {
            if (elapsed >= nextFlip)
            {
                _die1Text.text = Random.Range(1, 7).ToString();
                _die2Text.text = Random.Range(1, 7).ToString();
                nextFlip = elapsed + interval;
                interval = Mathf.Lerp(0.07f, 0.22f, elapsed / duration); // slow down
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final values
        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        _die1Text.text = die1.ToString();
        _die2Text.text = die2.ToString();
        _die1Text.color = new Color(1f, 0.85f, 0.1f);
        _die2Text.color = new Color(1f, 0.85f, 0.1f);

        yield return new WaitForSeconds(0.3f);

        // Move player
        if (PlayerToken.Instance != null)
        {
            int total = die1 + die2;
            int tileCount = BoardGenerator.Instance.Tiles.Count;
            int next = PlayerToken.Instance.CurrentTile + total;
            if (next >= tileCount)
            {
                PlayerToken.Instance.AddLoop();
                next -= tileCount;
            }
            PlayerToken.Instance.PlaceOnTile(next);
        }

        yield return new WaitForSeconds(0.5f);

        _die1Text.color = Color.white;
        _die2Text.color = Color.white;
        _rollButton.interactable = true;
        _rolling = false;
    }

    void StyleDieText(Text t, string val)
    {
        t.text = val;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = 64;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    void Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
