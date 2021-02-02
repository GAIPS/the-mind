using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    private string IPadress;
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
    public GameObject IPinputField;
    public static bool DebugMode = true;
    public static GameState GameState;
    public AudioClip playingCardSound;
    public AudioClip errorSound;
    private AudioSource audioSource;

    public Player[] players;
    public Pile pile;
    private int topOfThePile;
    public static int CONDITION;
    private int[][][] cardsPerLevelPerCondition = new int[][][] {
        new int[][] {
            new int[] { 11, 43, 12 },
            new int[] { 26, 49, 24, 40, 25, 39 },
            new int[] { 9, 18, 34, 8, 19, 35, 10, 25, 33 }
        },
        new int[][] {
            new int[] { 12, 44, 13 },
            new int[] { 27, 50, 25, 41, 26, 40 },
            new int[] { 10, 19, 35, 9, 20, 36, 11, 26, 34 }
        },
        new int[][] {
            new int[] { 10, 42, 11 },
            new int[] { 25, 48, 23, 39, 24, 38 },
            new int[] { 8, 17, 33, 7, 18, 34, 9, 24, 32 }
        }
    };

    private GameMasterThalamusConnector _thalamusConnector;

    // Start is called before the first frame update
    void Start()
    {
        IPadress = "";
        topOfThePile = -1;
        _thalamusConnector = null;
        GameState = GameState.Connection;
        audioSource = GetComponent<AudioSource>();
        CONDITION = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (_thalamusConnector == null && IPadress != "")
        {
            IPinputField.GetComponent<InputField>().interactable = false;
            _thalamusConnector = new GameMasterThalamusConnector(this, IPadress);
        }
        UpdateNumLevelsSetupUI();
        if (GameState == GameState.Connection)
        {
            if (players[0].IsConnected && players[1].IsConnected && players[2].IsConnected)
            {
                IPinputField.SetActive(false);
                _thalamusConnector.AllConnected(MaxLevels, players[0].ID, players[0].Name, players[1].ID, players[1].Name, players[2].ID, players[2].Name);
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
            if (players[0].IsReady() && players[1].IsReady() && players[2].IsReady())
            {
                if (Lives == 0)
                {
                    GameFinishedTextUI.GetComponent<Text>().text = "Game Over";
                    GameFinishedTextUI.SetActive(true);
                    GameState = GameState.GameFinished;
                    _thalamusConnector.GameOver(Level);
                }
                else
                {
                    ContinueAfterMistake();
                    for (int i = 0; i < players.Length; i++)
                    {
                        players[i].IsReadyToContinue = false;
                    }
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
            audioSource.PlayOneShot(errorSound);
            Lives--;
            LivesUI.GetComponent<Text>().color = new Color(1, 0, 0);
            UpdateLivesUI();
            OverlayMistakeUI.SetActive(true);
            GameState = GameState.Mistake;
        }
        else
        {
            _thalamusConnector.CardPlayed(pile.LastPlayer, topOfThePile);
            audioSource.PlayOneShot(playingCardSound);
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
            if (HowManyPlayersLeft() == 1)
            {
                _thalamusConnector.RefocusRequest(-1);
            }
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
        List<int> cards;
        if (CONDITION == -1)
        {
            cards = new List<int>();
            while (cards.Count < NumPlayers * Level)
            {
                int nextCard = Random.Range(1, 100);
                if (!cards.Contains(nextCard))
                {
                    cards.Add(nextCard);
                }
            }
        }
        else
        {
            cards = new List<int>(cardsPerLevelPerCondition[CONDITION][Level - 1]);
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

    public void ChangeThalamusClientIP()
    {
        IPadress = IPinputField.GetComponent<InputField>().text;
    }

    public void ChangeMaxLevel()
    {
        int max = int.Parse(MaxLevelsInputFieldUI.GetComponent<InputField>().text);
        MaxLevels = max;
    }

    public void ChangeDebugMode()
    {
        DebugMode = MaxLevelsInputFieldUI.GetComponentInChildren<Toggle>().isOn;
    }

    public void StartFromLevelOne()
    {
        Level = 1;
        Lives = 5;
        OverlayMistakeUI.SetActive(false);
        GameFinishedTextUI.SetActive(false);
        _thalamusConnector.AllConnected(MaxLevels, players[0].ID, players[0].Name, players[1].ID, players[1].Name, players[2].ID, players[2].Name);
        OverlayNextLevelUI.SetActive(true);
        UpdateLivesUI();
        GameState = GameState.NextLevel;
    }
}
