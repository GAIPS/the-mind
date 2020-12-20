using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int NumPlayers;
    public int Level;
    public int Lives;
    public GameObject LevelTextUI;
    public GameObject Overlay;
    public GameObject P0cards;
    public GameObject P1cards;
    public GameObject P2cards;
    public GameObject Pile;
    private List<int> pile;
    private List<List<int>> players;

    // Start is called before the first frame update
    void Start()
    {
        players = new List<List<int>>();
        players.Add(new List<int>());
        players.Add(new List<int>());
        players.Add(new List<int>());
        pile = new List<int>();

        DealCards();
        UpdateCardsUI();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Overlay.activeSelf)
        {
            if (Input.GetKeyDown("a") && players[0].Count > 0)
            {
                int nextCard = players[0][0];
                pile.Add(nextCard);
                players[0].RemoveAt(0);
            }
            else if (Input.GetKeyDown("l") && players[1].Count > 0)
            {
                int nextCard = players[1][0];
                pile.Add(nextCard);
                players[1].RemoveAt(0);
            }
            else if (Input.GetKeyDown("space") && players[2].Count > 0)
            {
                int nextCard = players[2][0];
                pile.Add(nextCard);
                players[2].RemoveAt(0);
            }

            UpdateCardsUI();
            UpdatePileUI();
        }
        CheckEndOfLevel();
    }

    public void NextLevel()
    {
        UpdateLevelUI();
        DealCards();
        UpdateCardsUI();
        UpdatePileUI();
        Overlay.SetActive(false);
    }

    void CheckEndOfLevel()
    {
        if (players[0].Count == 0 && players[1].Count == 0 && players[2].Count == 0)
        {
            Overlay.SetActive(true);
        }
    }

    void DealCards()
    {
        List<int> cards = new List<int>();
        while (cards.Count < NumPlayers * Level)
        {
            int nextCard = Random.Range(1, 100);
            if (!cards.Contains(nextCard))
            {
                cards.Add(nextCard);
            }
        }
        for (int i = 0; i < NumPlayers; i++)
        {
            for (int j = 0; j < Level; j++)
            {
                int nextCard = cards[0];
                cards.RemoveAt(0);
                players[i].Add(nextCard);
            }
            players[i].Sort();
        }
    }

    void UpdateCardsUI()
    {
        string text = "[";
        for (int i = 0; i < players[0].Count; i++)
        {
            text += players[0][i];
            if (i != players[0].Count - 1)
            {
                text += ",";
            }
        }
        text += "]";
        P0cards.GetComponent<Text>().text = text;
        
        text = "[";
        for (int i = 0; i < players[1].Count; i++)
        {
            text += players[1][i];
            if (i != players[1].Count - 1)
            {
                text += ",";
            }
        }
        text += "]";
        P1cards.GetComponent<Text>().text = text;

        text = "[";
        for (int i = 0; i < players[2].Count; i++)
        {
            text += players[2][i];
            if (i != players[2].Count - 1)
            {
                text += ",";
            }
        }
        text += "]";
        P2cards.GetComponent<Text>().text = text;
    }

    void UpdatePileUI()
    {
        if (pile.Count > 0)
        {
            Pile.GetComponent<Text>().text = "" + pile[pile.Count - 1];
        }
        else
        {
            Pile.GetComponent<Text>().text = "-";
        }
    }

    void UpdateLevelUI()
    {
        Level++;
        pile = new List<int>();
        LevelTextUI.GetComponent<Text>().text = "Level: " + Level;

    }
}
