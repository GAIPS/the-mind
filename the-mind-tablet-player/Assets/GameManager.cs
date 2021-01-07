using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum GameState
{
    Connection,
    Syncing,
    Game,
    Mistake,
    NextLevel,
    GameFinished
}


public class GameManager : MonoBehaviour
{

    public GameObject ConfigsScreen;
    public GameObject IpInputField;
    public GameObject PortInputField;
    public GameObject PlayerIDInputField;
    public GameObject ConnectButton;
    public GameObject GameScreen;
    public GameObject PlayButtonUI;
    public GameObject RefocusButtonUI;
    public GameObject ReadyButtonUI;
    public GameObject CardsUI;
    private int ID;
    private List<int> cards;
    private bool HasSignalledRefocus;
    private bool IsReady;
    private bool ShouldAckMistake;
    public int MaxLevel;
    private TabletThalamusConnector _thalamusConnector;

    public static GameState GameState;

    // Start is called before the first frame update
    void Start()
    {
        GameState = GameState.Connection;
        HasSignalledRefocus = false;
        IsReady = false;
        ShouldAckMistake = false;
        ConfigsScreen.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameState != GameState.Connection)
        {
            GameScreen.SetActive(true);

            UpdateCardsUI();
            UpdatePlayButtonUI();
            UpdateRefocusButtonUI();
            UpdateReadyButtonUI();
        }
    }

    void UpdateReadyButtonUI()
    {

        if (GameState == GameState.NextLevel && !IsReady)
        {
            ReadyButtonUI.GetComponent<Button>().interactable = true;
            ReadyButtonUI.GetComponentInChildren<Text>().text = "Ready";
        }
        else if (GameState == GameState.Mistake && (cards.Count > 0 || ShouldAckMistake) && !IsReady)
        {
            ReadyButtonUI.GetComponent<Button>().interactable = true;
            ReadyButtonUI.GetComponentInChildren<Text>().text = "Continue";
        }
        else
        {
            ReadyButtonUI.GetComponent<Button>().interactable = false;
        }
    }

    void UpdatePlayButtonUI()
    {

        if (GameState == GameState.Game && cards.Count > 0)
        {
            PlayButtonUI.GetComponent<Button>().interactable = true;
        }
        else
        {
            PlayButtonUI.GetComponent<Button>().interactable = false;
        }
    }

    void UpdateRefocusButtonUI()
    {
        if (GameState == GameState.NextLevel || GameState == GameState.Mistake)
        {
            RefocusButtonUI.GetComponent<Button>().interactable = false;
        }
        else if (cards.Count == 0 || (GameState == GameState.Syncing && HasSignalledRefocus))
        {
            RefocusButtonUI.GetComponent<Button>().interactable = false;
        }
        else
        {
            RefocusButtonUI.GetComponent<Button>().interactable = true;
        }
    }

    void UpdateCardsUI()
    {
        if (GameState == GameState.Syncing || GameState == GameState.Game || GameState == GameState.Mistake)
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
    }

    public void AllPlayersRefocused()
    {
        GameState = GameState.Game;
        HasSignalledRefocus = false;
        IsReady = false;
        ShouldAckMistake = false;
    }

    public void ConnectOnClick()
    {
        ConfigsScreen.SetActive(false);
        ID = int.Parse(PlayerIDInputField.GetComponent<InputField>().text);
        string IP = IpInputField.GetComponent<InputField>().text;
        int port = int.Parse(PortInputField.GetComponent<InputField>().text);
        _thalamusConnector = new TabletThalamusConnector(this, IP, port);
        _thalamusConnector.ConnectToGM(ID, "Tablet" + (ID + 1));
    }

    public void WaitForNewLevel()
    {
        IsReady = false;
        ShouldAckMistake = false;
        GameState = GameState.NextLevel;
    }

    public void GameFinished()
    {
        GameState = GameState.GameFinished;
    }

    public void NewLevelHasStarted(int[] p0Hand, int[] p1Hand, int[] p2Hand)
    {
        cards = new List<int>();
        if (ID == 0)
        {
            foreach (int card in p0Hand)
            {
                cards.Add(card);
            }
        }
        else if (ID == 1)
        {
            foreach (int card in p1Hand)
            {
                cards.Add(card);
            }
        }
        else if (ID == 1)
        {
            foreach (int card in p2Hand)
            {
                cards.Add(card);
            }
        }
        GameState = GameState.Syncing;
    }

    public void PlayerRequestedRefocus(int playerID)
    {
        if (playerID == -1)
        {
            GameState = GameState.Game;
        }
        else
        {
            GameState = GameState.Syncing;
        }
    }

    public void MistakeOccurred(int playerID, int card, int[] p0WrongCards, int[] p1WrongCards, int[] p2WrongCards)
    {
        GameState = GameState.Mistake;
        //mistake by another player
        if (playerID != ID)
        {
            if (ID == 0)
            {
                foreach (int wrongCard in p0WrongCards)
                {
                    ShouldAckMistake = true;
                    cards.Remove(wrongCard);
                }
            }
            else if (ID == 1)
            {
                foreach (int wrongCard in p1WrongCards)
                {
                    ShouldAckMistake = true;
                    cards.Remove(wrongCard);
                }
            }
            else if (ID == 2)
            {
                foreach (int wrongCard in p2WrongCards)
                {
                    ShouldAckMistake = true;
                    cards.Remove(wrongCard);
                }
            }
        }
    }

    public void PlayButton()
    {
        int cardToPlay = cards[0];
        cards.RemoveAt(0);
        _thalamusConnector.PlayCard(ID, cardToPlay);
    }

    public void RefocusButton()
    {
        HasSignalledRefocus = true;
        _thalamusConnector.RefocusSignal(ID);
    }

    public void ReadyButton()
    {
        IsReady = true;
        if (GameState == GameState.NextLevel)
        {
            _thalamusConnector.ReadyForNextLevel(ID);
        }
        else if (GameState == GameState.Mistake)
        {
            _thalamusConnector.ContinueAfterMistake(ID);
        }
    }
}
