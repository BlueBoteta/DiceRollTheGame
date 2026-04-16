using UnityEngine;
using UnityEngine.UI;

public class DiceRoller : MonoBehaviour
{
    public Text resultText;

    void Start()
    {
        if (resultText == null)
        {
            var go = GameObject.Find("ResultText");
            if (go != null) resultText = go.GetComponent<Text>();
        }
    }

    public void Roll()
    {
        int die1 = Random.Range(1, 7);
        int die2 = Random.Range(1, 7);
        int total = die1 + die2;

        if (resultText != null)
            resultText.text = die1 + "  +  " + die2 + "  =  " + total;

        if (PlayerToken.Instance != null)
        {
            int tileCount = BoardGenerator.Instance.Tiles.Count;
            int next = PlayerToken.Instance.CurrentTile + total;
            if (next >= tileCount)
            {
                PlayerToken.Instance.AddLoop();
                next -= tileCount;
            }
            PlayerToken.Instance.PlaceOnTile(next);
        }
    }
}
