using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardLogic : MonoBehaviour
{
    #region PhaseText
    const string PlayText = "Play";
    const string TradeText = "Trade";
    const string DiscardText = "Discard";
    #endregion

    #region ErrorMessages
    float _errorMessageDuration = 3.0f;
    const string InvalidSelectionByType = "Your selection is invalid.";
    const string InvalidSelectionByNumber = "Wrong number of cards selected";
    #endregion

    #region Scores
    public bool[] UseMultiplier;
    public bool[] UseTimer;
    public int[] MatchesObjective;
    public float[] MatchesTime;
    public int CurrentMatchObjective
    {
        get
        {
            return MatchesObjective[_gameBoard.GetActiveTable()];
        }
    }
    public float CurrentMatchTime
    {
        get
        {
            return MatchesTime[_gameBoard.GetActiveTable()];
        }
    }
    #endregion

    #region Timer
    float currentTime;
    #endregion

    #region UI Elements
    [SerializeField] TextMeshProUGUI _turnPhaseText;
    [SerializeField] Slider _pointsSlider;
    [SerializeField] TextMeshProUGUI _objectivePoints;
    [SerializeField] TextMeshProUGUI _pointsText;
    [SerializeField] TextMeshProUGUI _timerText;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] TextMeshProUGUI _errorText;

    [SerializeField] Canvas cardGameCanvas;
    [SerializeField] Button _nextPhaseButton;
    [SerializeField] Button _confirmButton;
    #endregion

    public static Action<TurnPhase> ChangeTurnPhase;
    private TurnPhase currentTurnPhase;

    [SerializeField] Board _gameBoard;
    [SerializeField] CoinScript _coinScript;
    private List<Card> cards;

    private List<CardPositionAndDirection> playCardPositions;

    private bool _gotSetThisTurn = false;
    private int _boardPointsCollected;
    private int _lastAddedPoints = 0;

    private bool tavernCardSelectedBuyPhase = false;
    private bool handCardSelectedBuyPhase = false;

    public Animator _animator;
    private bool _hasSeenTutorial;

    private void Awake()
    {
        cards = new List<Card> ();
        playCardPositions = new List<CardPositionAndDirection>();

        currentTurnPhase = TurnPhase.Discard;
    }

    private void OnEnable()
    {
        _nextPhaseButton.onClick.AddListener(OnChangeTurnPhase);
        _confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnDisable()
    {
        _nextPhaseButton.onClick.RemoveListener(OnChangeTurnPhase);
        _confirmButton.onClick.RemoveListener(OnConfirm);
    }

    public void Reposition()
    {
        this.transform.position = _gameBoard.GetActiveTableObject().transform.position;
        this.transform.position += Vector3.up * 0.81f;
        UnselectAllCards();
    }

    public void SelectCard(Card card) 
    {
        cards.Add(card);
    }

    public void UnselectCard(Card card) 
    {
        cards.Remove(card);
    }

    public void ShowTutorial()
    {
        if (_hasSeenTutorial) return;

        if (currentTurnPhase == TurnPhase.Trade)
        {
            CardGameState.ChangeGamePhase?.Invoke(GamePhase.Tutorial);
        }
        else if (currentTurnPhase == TurnPhase.Play)
        {
            CardGameState.ChangeGamePhase?.Invoke(GamePhase.Tutorial);
            _hasSeenTutorial = true;
        }
    }

    public void EnterDiscard()
    {
        _coinScript.FlipTheCoin("discard");

        currentTurnPhase = TurnPhase.Discard;
        _turnPhaseText.text = DiscardText;

        _nextPhaseButton.interactable = false;
        _gotSetThisTurn = false;

        if (playCardPositions.Count > 0)
        {
            _gameBoard.FinishTurn(playCardPositions);
            playCardPositions.Clear();
        }
    }

    public void EnterTrade()
    {
        _coinScript.FlipTheCoin("sell");

        currentTurnPhase = TurnPhase.Trade;
        _turnPhaseText.text = TradeText;

    }

    public void EnterPlay()
    {
        _coinScript.FlipTheCoin("buy");

        currentTurnPhase = TurnPhase.Play;
        _turnPhaseText.text = PlayText;
    }

    public void OnChangeTurnPhase() 
    {
        if (currentTurnPhase == TurnPhase.Discard)
        {
            ChangeTurnPhase?.Invoke(TurnPhase.Trade);
            EnterTrade();
            ShowTutorial();
        }
        else if (currentTurnPhase == TurnPhase.Trade)
        {
            ChangeTurnPhase?.Invoke(TurnPhase.Play);
            EnterPlay();
            ShowTutorial();
        }
        else // Play
        {
            ChangeTurnPhase?.Invoke(TurnPhase.Discard);
            EnterDiscard();
        }

        UnselectAllCards();
    }

    public void OnConfirm() 
    {
        if (currentTurnPhase == TurnPhase.Discard)
        {
            DiscardCards();
        }
        else if (currentTurnPhase == TurnPhase.Trade)
        {
            TradeCards();
        }
        else // Play
        {
            PlayCards();
        }

        UnSelectTavernCardBuyPhase();
        UnSelectHandCardBuyPhase();
        UnselectAllCards();
    }

    private void DiscardCards()
    {
        if (!CheckSelectionNumber(1)) return;

        _nextPhaseButton.interactable = true;

        _gameBoard.DiscardCard(cards[0]);

        OnChangeTurnPhase();
    }

    private void TradeCards()
    {
        if (!CheckSelectionNumber(2)) return;

        if (cards[0].CardData.Position == Position.Tavern)
        {
            _gameBoard.ExchangeTavernCard(cards[1], cards[0]);
        }
        else
        {
            _gameBoard.ExchangeTavernCard(cards[0], cards[1]);
        }

        OnChangeTurnPhase();
    }

    private void PlayCards()
    {
        if (!CheckSelectionNumber(3)) return;

        #region Calculating Values
        var card1 = cards[0].CardData;
        var card2 = cards[1].CardData;
        var card3 = cards[2].CardData;
        bool equalSuit = card1.Suit == card2.Suit && card2.Suit == card3.Suit;
        bool differentSuit = card1.Suit != card2.Suit && card2.Suit != card3.Suit;
        bool equalValue = card1.Value == card2.Value && card2.Value == card3.Value;
        bool runValue = AreTheyInOrder(card1.Value, card2.Value, card3.Value);
        bool set = equalValue;
        bool run = equalSuit && runValue;

        int collectedPoints = 0;
        float multiplier;
        string score = "";
        #endregion

        if (!set && !run)
        {
            StartCoroutine(ErrorAppeared(InvalidSelectionByType, _errorMessageDuration));
            return;
        }

        if (set || run)
        {
            #region Calculating Multiplers
            if (set && _gotSetThisTurn)
            {
                multiplier = 2;
                _boardPointsCollected = _boardPointsCollected + _lastAddedPoints;
            }
            else if (set && !_gotSetThisTurn)
            {
                multiplier = 1;
                _gotSetThisTurn = true;
            }
            else
            {
                multiplier = 1.5f;
            }
            #endregion

            foreach (Card card in cards)
            {
                collectedPoints += card.CardData.Score;
                playCardPositions.Add(new CardPositionAndDirection(card.transform.position, card.transform.up));
            }

            _gameBoard.CollectPoints(cards);

            // Collect Points
            if (UseMultiplier[_gameBoard.GetActiveTable()])
            {
                _boardPointsCollected += (int)(collectedPoints * multiplier);
                _lastAddedPoints = (int)(collectedPoints * multiplier);
            }
            else
            {
                _boardPointsCollected += collectedPoints;
                _lastAddedPoints = 0;
            }

            score += multiplier.ToString("0.0") + " x " + collectedPoints.ToString();
            AnimateScore(score);

            _pointsText.text = _boardPointsCollected.ToString();
            _pointsSlider.value = _boardPointsCollected;

            if (_boardPointsCollected >= CurrentMatchObjective) CardGameState.ChangeGamePhase(GamePhase.Win);
        }
    }

    private void AnimateScore(string s)
    {
        _scoreText.text = s;
        _animator.SetTrigger("score");
    }

    private bool AreTheyInOrder(params int[] values)
    {
        values = BubbleSort(values);

        if (NextToEachOther(values[0], values[1], values[2]))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private int[] BubbleSort(int[] values)
    {
        for (int i = 0; i < values.Length - 1; i++)
        {
            for (int j = 0; j < values.Length - i - 1; j++)
            {
                if (values[j] > values[j + 1])
                {
                    var v = values[j];
                    values[j] = values[j + 1];
                    values[j + 1] = v;
                }
            }
        }
        return values;
    }

    private bool NextToEachOther(int a, int b, int c)
    {
        return MathF.Abs(a - b) == 1 && MathF.Abs(b - c) == 1;
    }

    public bool CheckSelectionNumber(int checkTo)
    {
        if (cards.Count == checkTo)
        {
            return true;
        }
        else
        {
            StartCoroutine(ErrorAppeared(InvalidSelectionByNumber, _errorMessageDuration));
            return false;
        }
    }

    IEnumerator ErrorAppeared (string message, float displayTime)
    {
        var timeElapsed = 0f;

        foreach (var card in cards)
        {
            card.DenyAnimation();
        }

        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
        while (timeElapsed < displayTime)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        _errorText.gameObject.SetActive(false);
    }

    public IEnumerator StartTimer(float t)
    {
        currentTime = t;

        while (currentTime > 0)
        {
            _timerText.text = currentTime.ToString("000");
            currentTime -= Time.deltaTime;
            yield return null;
        }

        CardGameState.ChangeGamePhase(GamePhase.Lose);
    }

    public void GameWon() 
    {
        StopCoroutine("StartTimer");
        ResetGame();
        _gameBoard.SetNextActiveTable();
    }

    public void GameOver() 
    {
        ResetGame();
    }

    public void ResetGame()
    {
        UnselectAllCards();
        _gameBoard.ResetDeck();
        StartNewBoard();
    }

    public int SelectedCardsLength()
    {
        return cards.Count;
    }

    public void UnselectAllCards()
    {
        var tmpsCards = new List<Card>(cards);
        foreach (Card c in tmpsCards)
        {
            c.UnselectCard();
        }

        UnSelectTavernCardBuyPhase();
        UnSelectHandCardBuyPhase();
    }

    public void StartNewBoard()
    {
        _boardPointsCollected = 0;

        _pointsText.text = _boardPointsCollected.ToString();
        _pointsSlider.value = _boardPointsCollected;

        currentTurnPhase = TurnPhase.Discard;
        _turnPhaseText.text = DiscardText;

        _objectivePoints.text = CurrentMatchObjective.ToString();
        _pointsSlider.maxValue = CurrentMatchObjective;

        _turnPhaseText.text = DiscardText;
        _nextPhaseButton.interactable = false;

        _timerText.text = "0.00";

        playCardPositions.Clear();
    }

    public void SelectHandCardBuyPhase()
    {
        handCardSelectedBuyPhase = true;
    }

    public void UnSelectHandCardBuyPhase()
    {
        handCardSelectedBuyPhase = false;
    }

    public void SelectTavernCardBuyPhase()
    {
        tavernCardSelectedBuyPhase = true; 
    }

    public void UnSelectTavernCardBuyPhase()
    {
        tavernCardSelectedBuyPhase = false;
    }

    public bool IsHandCardSelectedBuyPhase()
    {
        return handCardSelectedBuyPhase;
    }

    public bool IsTavernCardSelectedBuyPhase()
    {
        return tavernCardSelectedBuyPhase;
    }
}

public struct CardPositionAndDirection
{
    public CardPositionAndDirection(Vector3 position, Vector3 normal)
    {
        cardPosition = position;
        cardNormal = normal;
    }

    public Vector3 cardPosition;
    public Vector3 cardNormal;
}