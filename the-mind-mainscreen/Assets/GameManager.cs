using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int NumPlayers;
    public int Level;
    public GameObject LevelUI;
    public int Lives;
    public GameObject LivesUI;
    public GameObject OverlayNextLevelUI;
    public GameObject P0cardsUI;
    public GameObject P1cardsUI;
    public GameObject P2cardsUI;
    public GameObject PileUI;
    private List<int> pile;
    public GameObject P0handUI;
    public GameObject P1handUI;
    public GameObject P2handUI;
    public GameObject OverlaySyncingUI;
    private bool p0Hand;
    private bool p1Hand;
    private bool p2Hand;
    private bool syncing;
    private List<List<int>> players;
    public GameObject OverlayMistakeUI;
    public GameObject ContinueButtonUI;
    public GameObject GameOverTextUI;
    public GameObject P0wrongCardsUI;
    public GameObject P1wrongCardsUI;
    public GameObject P2wrongCardsUI;

    private GameMasterThalamusConnector _thalamusConnector;

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

        _thalamusConnector = new GameMasterThalamusConnector();
    }

    // Update is called once per frame
    void Update()
    {
        if (!OverlayNextLevelUI.activeSelf && !OverlayMistakeUI.activeSelf)
        {
            //initiate syncing
            if (Input.GetKeyDown("1") && !p0Hand)
            {
                p0Hand = true;
                OverlaySyncingUI.SetActive(true);
                P0handUI.SetActive(true);
                CheckEndOfSync();
            }
            else if (Input.GetKeyDown("2") && !p1Hand)
            {
                p1Hand = true;
                OverlaySyncingUI.SetActive(true);
                P1handUI.SetActive(true);
                CheckEndOfSync();
            }
            else if (Input.GetKeyDown("3") && !p2Hand)
            {
                p2Hand = true;
                OverlaySyncingUI.SetActive(true);
                P2handUI.SetActive(true);
                CheckEndOfSync();
            }

            //normal play
            if (!OverlaySyncingUI.activeSelf && Input.GetKeyDown("a") && players[0].Count > 0)
            {
                int nextCard = players[0][0];
                pile.Add(nextCard);
                players[0].RemoveAt(0);
                UpdateCardsUI();
                UpdatePileUI();
                ValidateMove();
            }
            else if (!OverlaySyncingUI.activeSelf && Input.GetKeyDown("l") && players[1].Count > 0)
            {
                int nextCard = players[1][0];
                pile.Add(nextCard);
                players[1].RemoveAt(0);
                UpdateCardsUI();
                UpdatePileUI();
                ValidateMove();
            }
            else if (!OverlaySyncingUI.activeSelf && Input.GetKeyDown("space") && players[2].Count > 0)
            {
                int nextCard = players[2][0];
                pile.Add(nextCard);
                players[2].RemoveAt(0);
                UpdateCardsUI();
                UpdatePileUI();
                ValidateMove();
            }
        }
        if (!OverlayNextLevelUI.activeSelf && !OverlayMistakeUI.activeSelf && !OverlaySyncingUI.activeSelf)
        {
            UpdateCardsUI();
            UpdatePileUI();
            CheckEndOfLevel();
        }
    }

    void ValidateMove()
    {
        int lastCard = pile[pile.Count - 1];
        bool mistake = false;
        List<string> wrongCardsUI = new List<string>();

        for (int i = 0; i < NumPlayers; i++)
        {
            string wrongCards = "[";
            while (players[i].Count > 0 && players[i][0] < lastCard)
            {
                mistake = true;
                if (wrongCards != "[")
                {
                    wrongCards += ",";
                }
                wrongCards += players[i][0];
                players[i].RemoveAt(0);
            }
            wrongCards += "]";
            wrongCardsUI.Add(wrongCards);
        }
        if (mistake)
        {
            P0wrongCardsUI.GetComponent<Text>().text = wrongCardsUI[0];
            P1wrongCardsUI.GetComponent<Text>().text = wrongCardsUI[1];
            P2wrongCardsUI.GetComponent<Text>().text = wrongCardsUI[2];
            Lives--;
            LivesUI.GetComponent<Text>().color = new Color(1, 0, 0);
            PileUI.GetComponent<Text>().color = new Color(1, 0, 0);
            UpdateLivesUI();
            OverlayMistakeUI.SetActive(true);
            if (Lives == 0)
            {
                ContinueButtonUI.SetActive(false);
                GameOverTextUI.SetActive(true);
            }
        }
    }

    public void ContinueAfterMistake()
    {
        LivesUI.GetComponent<Text>().color = new Color(0, 0, 0);
        PileUI.GetComponent<Text>().color = new Color(0, 0, 0);
        UpdateLivesUI();
        OverlayMistakeUI.SetActive(false);
    }

    public void NextLevel()
    {
        UpdateLevelUI();
        DealCards();
        UpdateCardsUI();
        UpdatePileUI();
        OverlayNextLevelUI.SetActive(false);
    }

    void CheckEndOfLevel()
    {
        if (players[0].Count == 0 && players[1].Count == 0 && players[2].Count == 0)
        {
            OverlayNextLevelUI.SetActive(true);
        }
    }

    void CheckEndOfSync()
    {
        if (p0Hand && p1Hand && p2Hand)
        {
            p0Hand = false;
            p1Hand = false;
            p2Hand = false;
        }
    }

    void ShrinkUntilDeactive()
    {
        if (OverlaySyncingUI.activeSelf)
        {
            Vector3 scaleChange = new Vector3(-0.01f, -0.01f, 0.00f);
            OverlaySyncingUI.transform.localScale += scaleChange;
            if (OverlaySyncingUI.transform.localScale.x <= 0.05 || OverlaySyncingUI.transform.localScale.y <= 0.05)
            {
                P0handUI.SetActive(false);
                P1handUI.SetActive(false);
                P2handUI.SetActive(false);
                OverlaySyncingUI.SetActive(false);
                OverlaySyncingUI.transform.localScale = new Vector3(1.0f, 1.0f, 0.00f);
                CancelInvoke();
            }
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
        P0cardsUI.GetComponent<Text>().text = text;
        
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
        P1cardsUI.GetComponent<Text>().text = text;

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
        P2cardsUI.GetComponent<Text>().text = text;
    }

    void UpdatePileUI()
    {
        if (pile.Count > 0)
        {
            PileUI.GetComponent<Text>().text = "" + pile[pile.Count - 1];
        }
        else
        {
            PileUI.GetComponent<Text>().text = "-";
        }
    }

    void UpdateLevelUI()
    {
        Level++;
        pile = new List<int>();
        LevelUI.GetComponent<Text>().text = "Level: " + Level;

    }

    void UpdateLivesUI()
    {
        LivesUI.GetComponent<Text>().text = "Lives: " + Lives;
    }
}
