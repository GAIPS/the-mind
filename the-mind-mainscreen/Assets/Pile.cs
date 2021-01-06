using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pile : MonoBehaviour
{
    public GameObject PileUI;
    private List<int> pile;
    public int LastPlayer;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdatePileUI();
    }

    public bool IsEmpty()
    {
        return pile.Count == 0;
    }

    public int GetTopCard()
    {
        int topOfThePile;
        if (pile.Count == 0)
        {
            topOfThePile = - 1;
        }
        else
        {
            topOfThePile = pile[pile.Count - 1];
        }
        return topOfThePile;
    }

    public void PlayCard(int playerID, int card)
    {
        LastPlayer = playerID;
        pile.Add(card);
    }

    public void UpdatePileUI()
    {
        if (GameManager.GameState == GameState.Game || GameManager.GameState == GameState.Mistake || GameManager.GameState == GameState.Syncing)
        {
            if (pile.Count > 0)
            {
                PileUI.GetComponent<Text>().text = "" + pile[pile.Count - 1];
            }
            else
            {
                PileUI.GetComponent<Text>().text = "-";
            }

            if (GameManager.GameState == GameState.Mistake)
            {
                PileUI.GetComponent<Text>().color = new Color(1, 0, 0);
            }
            else
            {
                PileUI.GetComponent<Text>().color = new Color(0, 0, 0);
            }
        }
    }

    public void StartNewLevel()
    {
        pile = new List<int>();
    }
}
