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
    private int cardBeingPlayed;
    private string wrongCards;

    // Start is called before the first frame update
    void Start()
    {
        IsConnected = false;
        HasSignaledRefocus = false;
        cards = new List<int>();
        cardBeingPlayed = -1;
        wrongCards = "[]";
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
        wrongCards = "[";
        while (cards.Count > 0 && cards[0] < topOfThePile)
        {
            if (wrongCards != "[")
            {
                wrongCards += ",";
            }
            wrongCardsList.Add(cards[0]);
            wrongCards += cards[0];
            cards.RemoveAt(0);
        }
        wrongCards += "]";
        WrongCardsUI.GetComponent<Text>().text = wrongCards;
        return wrongCardsList;
    }

    public void UpdateWrongCardsUI()
    {
        if (GameManager.GameState == GameState.Mistake || GameManager.GameState == GameState.GameFinished)
        {
            WrongCardsUI.SetActive(true);
            WrongCardsUI.GetComponent<Text>().text = wrongCards;
        }
        else
        {
            WrongCardsUI.SetActive(false);
        }
    }

    public void UpdateCardsUI()
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
}
