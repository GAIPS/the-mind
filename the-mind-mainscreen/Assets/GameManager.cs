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
    public int MaxLevels;
    public int NumPlayers;
    public int Level;
    public GameObject LevelUI;
    public int Lives;
    public GameObject LivesUI;
    public GameObject OverlayNextLevelUI;
    public GameObject OverlaySyncingUI;
    public GameObject OverlayMistakeUI;
    public GameObject GameFinishedTextUI;
    public GameObject MaxLevelsInputFieldUI;
    public static GameState GameState;

    public Player[] players;
    public Pile pile;
    private int topOfThePile;

    private GameMasterThalamusConnector _thalamusConnector;

    // Start is called before the first frame update
    void Start()
    {
        topOfThePile = -1;
        _thalamusConnector = new GameMasterThalamusConnector(this);
        GameState = GameState.Connection;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateNumLevelsSetupUI();
        if (GameState == GameState.Connection)
        {
            if (players[0].IsConnected && players[1].IsConnected && players[2].IsConnected)
            {
                _thalamusConnector.AllConnected(players[0].ID, players[0].Name, players[1].ID, players[1].Name, players[2].ID, players[2].Name);
                OverlayNextLevelUI.SetActive(true);
                GameState = GameState.NextLevel;
            }
        }

        if (GameState == GameState.Syncing)
        {
            OverlaySyncingUI.SetActive(true);
            if ((players[0].HasSignaledRefocus || players[0].HowManyCardsLeft() == 0) && (players[1].HasSignaledRefocus || players[1].HowManyCardsLeft() == 0) && (players[2].HasSignaledRefocus || players[2].HowManyCardsLeft() == 0))
            {
                _thalamusConnector.AllRefocused();
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].HasSignaledRefocus = false;
                }
                InvokeRepeating("ShrinkUntilDeactive", 0, 0.006f);
                GameState = GameState.Game;
            }
        }

        if (GameState == GameState.Game)
        {
            int updatedTopOfThePile = pile.GetTopCard();
            if (topOfThePile != updatedTopOfThePile)
            {
                topOfThePile = updatedTopOfThePile;
                ValidateMove();
            }
            else if (players[0].HowManyCardsLeft() == 0 && players[1].HowManyCardsLeft() == 0 && players[2].HowManyCardsLeft() == 0)
            {
                _thalamusConnector.FinishLevel(Level, Lives);
                if (Level == MaxLevels)
                {
                    OverlayMistakeUI.SetActive(true);
                    GameFinishedTextUI.SetActive(true);
                    GameFinishedTextUI.GetComponent<Text>().text = "Game Completed!";
                    GameState = GameState.GameFinished;
                    _thalamusConnector.GameCompleted();
                }
                else
                {
                    LevelUp();
                    OverlayNextLevelUI.SetActive(true);
                    GameState = GameState.NextLevel;
                }
            }
            
            if (players[0].HasSignaledRefocus || players[1].HasSignaledRefocus || players[2].HasSignaledRefocus)
            {
                GameState = GameState.Syncing;
                int requester = players[0].HasSignaledRefocus ? 0 : (players[1].HasSignaledRefocus ? 1 : 2);
                _thalamusConnector.RefocusRequest(requester);
            }

        }

        if (GameState == GameState.NextLevel)
        {
            if (players[0].IsReadyForNextLevel && players[1].IsReadyForNextLevel && players[2].IsReadyForNextLevel)
            {
                NextLevel();
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].IsReadyForNextLevel = false;
                }
            }
        }

        if (GameState == GameState.Mistake)
        {
            if ((players[0].IsReadyToContinue || players[0].HowManyCardsLeft() == 0) && (players[1].IsReadyToContinue || players[1].HowManyCardsLeft() == 0) && (players[2].IsReadyToContinue || players[2].HowManyCardsLeft() == 0))
            {
                ContinueAfterMistake();
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].IsReadyToContinue = false;
                }
            }
        }

    }

    void UpdateNumLevelsSetupUI()
    {
        if (GameState == GameState.Connection)
        {
            MaxLevelsInputFieldUI.SetActive(true);
            MaxLevelsInputFieldUI.GetComponentInChildren<Button>().interactable = false;
        }
        else if (GameState == GameState.GameFinished)
        {
            MaxLevelsInputFieldUI.SetActive(true);
            MaxLevelsInputFieldUI.GetComponentInChildren<Button>().interactable = true;
        }
        else
        {
            MaxLevelsInputFieldUI.SetActive(false);
        }
    }

    void ValidateMove()
    {
        bool mistake = false;
        List<List<int>> wrongCards = new List<List<int>>();

        foreach (Player p in players)
        {
            List<int> playerWrongCards = p.GetWrongCards(topOfThePile);
            wrongCards.Add(playerWrongCards);
            mistake = playerWrongCards.Count > 0 || mistake;
        }

        if (mistake)
        {
            _thalamusConnector.Mistake(pile.LastPlayer, topOfThePile, wrongCards[0].ToArray(), wrongCards[1].ToArray(), wrongCards[2].ToArray());
            Lives--;
            LivesUI.GetComponent<Text>().color = new Color(1, 0, 0);
            UpdateLivesUI();
            OverlayMistakeUI.SetActive(true);
            if (Lives == 0)
            {
                GameFinishedTextUI.GetComponent<Text>().text = "Game Over";
                GameFinishedTextUI.SetActive(true);
                GameState = GameState.GameFinished;
                _thalamusConnector.GameOver(Level);
            }
            else
            {
                GameState = GameState.Mistake;
            }
        }
        else
        {
            _thalamusConnector.CardPlayed(pile.LastPlayer, topOfThePile);
        }
    }

    private int HowManyPlayersLeft()
    {
        int countFinishedPlayers = 0;
        foreach (Player p in players)
        {
            if (p.HowManyCardsLeft() == 0)
            {
                countFinishedPlayers++;
            }
        }
        return NumPlayers - countFinishedPlayers;
    }

    private void ContinueAfterMistake()
    {
        if (HowManyPlayersLeft() > 1)
        {
            GameState = GameState.Syncing;
            _thalamusConnector.RefocusRequest(4);
        }
        else
        {
            GameState = GameState.Game;
            _thalamusConnector.RefocusRequest(-1);
        }
        LivesUI.GetComponent<Text>().color = new Color(0, 0, 0);
        UpdateLivesUI();
        OverlayMistakeUI.SetActive(false);
    }

    private void NextLevel()
    {
        StartNewLevel();
        topOfThePile = pile.GetTopCard();
        LevelUI.GetComponent<Text>().color = new Color(0, 0, 0);
        OverlayNextLevelUI.SetActive(false);
    }

    void ShrinkUntilDeactive()
    {
        Vector3 scaleChange = new Vector3(-0.01f, -0.01f, 0.00f);
        OverlaySyncingUI.transform.localScale += scaleChange;
        if (OverlaySyncingUI.transform.localScale.x <= 0.02 || OverlaySyncingUI.transform.localScale.y <= 0.02)
        {
            OverlaySyncingUI.SetActive(false);
            OverlaySyncingUI.transform.localScale = new Vector3(1.2f, 1.0f, 0.00f);
            CancelInvoke();
        }
    }

    List<List<int>> DealCards()
    {
        List<List<int>> hands = new List<List<int>>();
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
            List<int> hand = cards.GetRange(i * Level, Level);
            hands.Add(hand);
        }
        return hands;
    }

    void LevelUp()
    {
        Level++;
        LevelUI.GetComponent<Text>().text = "Level: " + Level;
        LevelUI.GetComponent<Text>().color = new Color(1, 1, 1);

    }

    void UpdateLivesUI()
    {
        LivesUI.GetComponent<Text>().text = "Lives: " + Lives;
    }

    void StartNewLevel()
    {
        List<List<int>> hands = DealCards();
        for (int i = 0; i < players.Length; i++)
        {
            players[i].ReceiveCards(hands[i]);
        }
        pile.StartNewLevel();
        _thalamusConnector.StartLevel(Level, Lives, hands[0].ToArray(), hands[1].ToArray(), hands[2].ToArray());
        GameState = GameState.Syncing;
    }

    public void ChangeMaxLevel()
    {
        int max = int.Parse(MaxLevelsInputFieldUI.GetComponent<InputField>().text);
        MaxLevels = max;
    }

    public void StartFromLevelOne()
    {
        Level = 1;
        OverlayMistakeUI.SetActive(false);
        GameFinishedTextUI.SetActive(false);
        _thalamusConnector.AllConnected(players[0].ID, players[0].Name, players[1].ID, players[1].Name, players[2].ID, players[2].Name);
        OverlayNextLevelUI.SetActive(true);
        GameState = GameState.NextLevel;
    }
}
