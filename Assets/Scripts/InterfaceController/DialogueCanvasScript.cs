using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class DialogueCanvasScript : MonoBehaviour
{
    public static Action<bool> ToggleVisibility;

    private Canvas _dialogueCanvas;
    public TMP_Text DialogueBox;

    [HideInInspector] public bool IsTyping;
    private Coroutine _typewritterCoroutine;
    private WaitForSeconds _typeWait;
    public float TypeSpeed;
    private string _currentText;


    private void Start()
    {
        _dialogueCanvas = GetComponent<Canvas>();
        _typeWait = new WaitForSeconds(TypeSpeed);

        ToggleVisibility += OnToggleVisibility;
    }

    public void OnToggleVisibility(bool isVisible)
    {
        DialogueBox.text = string.Empty;
        FullConversation = string.Empty;
        _dialogueCanvas.enabled = isVisible;
    }

    public void StartTypeWritterEffect(string text)
    {
        DialogueBox.text = string.Empty;
        _currentText = text;
        _typewritterCoroutine = StartCoroutine(TypewritterEffect(text));

        //_typewritterCoroutine = StartCoroutine(CharByCharTypewritter(_currentText));
    }

    public void EndTypeWritterEffect()
    {
        StopCoroutine(_typewritterCoroutine);
        DialogueBox.text = _currentText;
        IsTyping = false;

        //SavingDialogue(_currentText);
    }

    private string FullConversation;
    public IEnumerator CharByCharTypewritter(string text)
    {
        if (FullConversation != string.Empty) FullConversation += "\n";
        IsTyping = true;

        int charIndex = 0;
        int tagLength;

        while (charIndex < text.Length)
        {
            tagLength = CheckForTag(charIndex, text);
            charIndex += tagLength;

            DialogueBox.text = FullConversation + text.Substring(0, charIndex);
            charIndex++;
            yield return _typeWait;
        }

        SavingDialogue(text);
    }

    public IEnumerator TypewritterEffect(string text)
    {
        IsTyping = true;

        int charIndex = 0;
        int tagLength;

        while (charIndex < text.Length)
        {
            tagLength = CheckForTag(charIndex, text);
            charIndex += tagLength;

            DialogueBox.text = text.Substring(0, charIndex);
            charIndex++;

            yield return _typeWait;
        }

        IsTyping = false;
    }

    public void SavingDialogue(string text)
    {
        FullConversation += text;
        DialogueBox.text = FullConversation;
        IsTyping = false;
    }

    private int CheckForTag(int endIndex, string text)
    {
        if (text[endIndex] != '<') return 0;

        var length = 1;
        do
        {
            endIndex++;
            length++;
        }
        while (text[endIndex] != '>');
        return length;
    }
}
