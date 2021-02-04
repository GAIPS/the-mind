using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public GameObject CardsUI;
    public GameObject HandUI;
    public GameObject WrongCardsUI;
    public GameObject ConnectionUI;
    public Pile pile;
    public int ID;
    public string KeyToPlayer;

    public string Name;
    public bool IsConnected;
    private List<int> cards;
    public bool HasSignaledRefocus;
    public bool IsReadyForNextLevel;
    public bool IsReadyToContinue;
    private int cardBeingPlayed;
    public string WrongCards;

    // Start is called before the first frame update
    void Start()
    {
        IsConnected = false;
        HasSignaledRefocus = false;
        IsReadyForNextLevel = false;
        IsReadyToContinue = false;
        cards = new List<int>();
        cardBeingPlayed = -1;
        WrongCards = "[]";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyToPlayer) && HowManyCardsLeft() > 0)
        {
            int nextCard = cards[0];
            pile.PlayCard(ID, nextCard);
            cards.RemoveAt(0);
            UpdateCardsUI();
        }
        if (cardBeingPlayed != -1)
        {
            PlayCard();
        }

        UpdateConnectionUI();
        UpdateHandUI();
        UpdateCardsUI();
        UpdateWrongCardsUI();
    }

    public void ConnectionReceived(string name)
    {
        IsConnected = true;
        Name = name;
        string[] splitted = name.Split('-');
        if (splitted.Length > 1)
        {
            if (splitted[1] == "r")
            {
                GameManager.CONDITION = 0;
            }
            else if (splitted[1] == "m")
            {
                GameManager.CONDITION = 1;
            }
            else if (splitted[1] == "mj")
            {
                GameManager.CONDITION = 2;
            }
            else if (splitted[1] == "p")
            {
                GameManager.CONDITION = 3;
            }
        }
    }

    public void RefocusSignal()
    {
        if (GameManager.GameState == GameState.Syncing || GameManager.GameState == GameState.Game)
        {
            HasSignaledRefocus = true;
        }
        else
        {
            Debug.Log("----- Received a refocus signal and the GameState is not GAME or SYNCING!!!");
        }
    }

    public void ReadyForNextLevel()
    {
        if (GameManager.GameState == GameState.NextLevel)
        {
            IsReadyForNextLevel = true;
        }
        else
        {
            Debug.Log("----- Received a ReadyForNextLevel and the GameState is not NEXT_LEVEL!!!");
        }
    }

    public void ContinueAfterMistake()
    {
        if (GameManager.GameState == GameState.Mistake)
        {
            IsReadyToContinue = true;
        }
        else
        {
            Debug.Log("----- Received a ContinueAfterMistake and the GameState is not MISTAKE!!!");
        }
    }

    public void CardPlayed(int card)
    {
        cardBeingPlayed = card;
    }

    private void PlayCard()
    {
        if (GameManager.GameState == GameState.Game)
        {
            if (cards.Count > 0 && cardBeingPlayed == cards[0])
            {
                pile.PlayCard(ID, cardBeingPlayed);
                cards.RemoveAt(0);
            }
            else
            {
                Debug.Log("----- the card being played -" + cardBeingPlayed + "- is not of this player.");
            }
            cardBeingPlayed = -1;
        }
        else
        {
            cardBeingPlayed = -1;
            Debug.Log("----- Received a card and the GameState is not GAME!!!");
        }
    }

    public List<int> GetWrongCards(int topOfThePile)
    {
        List<int> wrongCardsList = new List<int>();
        WrongCards = "[";
        while (cards.Count > 0 && cards[0] < topOfThePile)
        {
            if (WrongCards != "[")
            {
                WrongCards += ",";
            }
            wrongCardsList.Add(cards[0]);
            WrongCards += cards[0];
            cards.RemoveAt(0);
        }
        WrongCards += "]";
        WrongCardsUI.GetComponent<Text>().text = WrongCards;
        return wrongCardsList;
    }

    public void UpdateWrongCardsUI()
    {
        if (GameManager.GameState == GameState.Mistake)
        {
            WrongCardsUI.SetActive(true);
            WrongCardsUI.GetComponent<Text>().text = WrongCards;
        }
        else
        {
            WrongCardsUI.SetActive(false);
        }
    }

    public void UpdateCardsUI()
    {
        if (GameManager.DebugMode)
        {
            string text = "[";
            for (int i = 0; i < cards.Count; i++)
            {
                text += cards[i];
                if (i != cards.Count - 1)
                {
                    text += ",";
                }
            }
            text += "]";
            CardsUI.GetComponent<Text>().text = text;
        }
        else
        {
            string text = "" + cards.Count + " cards left";
            CardsUI.GetComponent<Text>().text = text;
        }
    }

    private void UpdateConnectionUI()
    {
        if (GameManager.GameState == GameState.Connection)
        {
            ConnectionUI.SetActive(true);
            if (IsConnected)
            {
                Text connectionText = ConnectionUI.GetComponent<Text>();
                connectionText.text = "P" + ID + " is connected";
                connectionText.color = new Color(1, 1, 1);
            }
            else
            {
                Text connectionText = ConnectionUI.GetComponent<Text>();
                connectionText.text = "Waiting for P" + ID;
                connectionText.color = new Color(1, 0, 0);
            }
        }
        else
        {
            ConnectionUI.SetActive(false);
        }
    }

    private void UpdateHandUI()
    {
        if (HasSignaledRefocus)
        {
            HandUI.SetActive(true);
        }
        else
        {
            HandUI.SetActive(false);
        }
    }

    public int HowManyCardsLeft()
    {
        return cards.Count;
    }

    public void ReceiveCards(List<int> hand)
    {
        cards = hand;
        cards.Sort();
        UpdateCardsUI();
    }

    public bool IsReady()
    {
        return ((IsReadyToContinue && (cards.Count > 0 || WrongCards != "[]")) || (cards.Count == 0 && WrongCards == "[]"));
    }
}
