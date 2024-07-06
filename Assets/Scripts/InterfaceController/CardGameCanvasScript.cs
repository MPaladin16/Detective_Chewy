using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum ErrorType
{
    WrongSelection,
    WrongNumber
}

public class CardGameCanvasScript : MonoBehaviour
{
    public CardLogic _logic;

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

    public static Action<bool> ToggleVisibility;
    private Canvas _cardGameCanvas;

    #region UI Elements
    [SerializeField] TextMeshProUGUI _turnPhaseText;

    [SerializeField] TextMeshProUGUI _currentPointsText;
    [SerializeField] TextMeshProUGUI _objectivePoints;
    [SerializeField] Slider _pointsSlider;
        
    [SerializeField] TextMeshProUGUI _errorText;
    
    [SerializeField] TextMeshProUGUI _timerText;
    [SerializeField] TextMeshProUGUI _baseScoreText;
    [SerializeField] TextMeshProUGUI _discardMultiplierText;
    [SerializeField] TextMeshProUGUI _setMultiplierText;
    [SerializeField] TextMeshProUGUI _runMultiplierText;

    [SerializeField] Button _nextPhaseButton;
    [SerializeField] Button _confirmButton;
    #endregion

    private void Start()
    {
        _cardGameCanvas = GetComponent<Canvas>();

        ToggleVisibility += OnToggleVisibility;
    }

    private void OnEnable()
    {
        _nextPhaseButton.onClick.AddListener(_logic.OnChangeTurnPhase);
        _confirmButton.onClick.AddListener(_logic.OnConfirm);
    }

    private void OnDisable()
    {
        _nextPhaseButton.onClick.RemoveListener(_logic.OnChangeTurnPhase);
        _confirmButton.onClick.RemoveListener(_logic.OnConfirm);
    }

    public void ResetCanvas()
    {
        _currentPointsText.text = _logic.BoardPointsCollected.ToString();
        _pointsSlider.value = _logic.BoardPointsCollected;

        _objectivePoints.text = _logic.CurrentMatchObjective.ToString();
        _pointsSlider.maxValue = _logic.CurrentMatchObjective;

        _turnPhaseText.text = DiscardText;
        _nextPhaseButton.interactable = false;

        _timerText.text = "00";

        ResetPointDisplay();
    }

    public void ResetPointDisplay()
    {
        _baseScoreText.text = "BP: ";
        _discardMultiplierText.text = "DP: ";
        _setMultiplierText.text = "SP: ";
        _runMultiplierText.text = "RP: ";
    }

    public void TickTimerText(string time)
    {
        _timerText.text = time;
    }

    public void UpdateTotalPoints(int points)
    {
        _pointsSlider.value = points;
        _currentPointsText.text = points.ToString();
    }

    public void UpdateBaseScore(float score)
    {
        _baseScoreText.text = "BP: " + score.ToString();
    }

    public void UpdateDiscardMultiplier(float score)
    {
        _discardMultiplierText.text = "DP:" + score.ToString();
    }

    public void UpdateSetMultiplier(float score)
    {
        _setMultiplierText.text = "SP: " + score.ToString();
    }

    public void UpdateRunMultiplier(float score)
    {
        _runMultiplierText.text = "RP: " + score.ToString();
    }

    public void OnToggleVisibility(bool isVisible)
    {
        _cardGameCanvas.enabled = isVisible;
    }

    public void ChangeTurn(TurnPhase phase)
    {
        switch(phase)
        {
            case TurnPhase.Discard:
                _turnPhaseText.text = DiscardText;
                _nextPhaseButton.interactable = false;
                break;
            case TurnPhase.Trade:
                _turnPhaseText.text = TradeText;
                _nextPhaseButton.interactable = true;
                break;
            case TurnPhase.Play:
                _turnPhaseText.text = PlayText;
                break;
        }
    }

    public void CallError(ErrorType type, List<Card> cards)
    {
        switch (type)
        {
            case ErrorType.WrongSelection:
                StartCoroutine(ErrorAppeared(InvalidSelectionByType, cards));
                break;
            case ErrorType.WrongNumber:
                StartCoroutine(ErrorAppeared(InvalidSelectionByNumber, cards));
                break;
        }
    }

    IEnumerator ErrorAppeared(string message, List<Card> cards)
    {
        var timeElapsed = 0f;

        foreach (var card in cards)
        {
            card.DenyAnimation();
        }

        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
        while (timeElapsed < _errorMessageDuration)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        _errorText.gameObject.SetActive(false);
    }
}
