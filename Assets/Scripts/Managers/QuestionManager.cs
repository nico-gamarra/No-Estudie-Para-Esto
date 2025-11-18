using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class QuestionManager : MonoBehaviour
{
    public static event Action<bool, Button> OnAnswer;
    
    [SerializeField] private Timer timer;

    private Dictionary<(QuestionData.Subject, QuestionData.Difficulty), List<QuestionData>> _questionBank;

    private QuestionData _selectedQuestion;
    private bool _playerHasAnswered;
    private bool _playerAnswersCorrectly;
    private bool _timeRanOut;
    private string language;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += ChangeLanguage;
    }
    
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= ChangeLanguage;
    }

    private void Start()
    {
        InitializeQuestionBank();
        LoadQuestions("es");
    }
    
    public IEnumerator StartQuestions()
    {
        yield return StartCoroutine(GoThroughCards()); //Espera a que termine la corrutina antes de seguir.
    }

    //Recorre todas las cartas jugadas en la fase de colocación.
    private IEnumerator GoThroughCards()
    {
        CardBoard[] boardCards = GameManager.Instance.GetUIManager().GetBoardCards();

        foreach (CardBoard card in boardCards)
        {
            GameManager.Instance.GetAudioManager().PlayMusicWithFade(AudioManager.MusicList.Questions);
            yield return ProcessCard(card);
        }
    }

    #region ProcessCard

    /*Se procesa la carta, esto conlleva muchas cosas:
    - Selecciona una pregunta aleatoria de la dificultad asociada.
    - Indica que la carta está seleccionada (la pinta de amarillo).
    - Inicia el timer.
    - Espera a que el jugador responda o el tiempo se acabe.
    - Verifica la respuesta seleccionada.*/
    private IEnumerator ProcessCard(CardBoard card)
    {
        if (!card) yield break;
        
        int difficulty = card.GetCardValue();

        SelectQuestion(difficulty - 1);
        
        card.ChangeColor(CardBoard.CardColor.Yellow);

        _playerHasAnswered = false;
        _timeRanOut = false;

        timer.StartTimer();
        
        yield return new WaitUntil(() => _playerHasAnswered || _timeRanOut);
        GameManager.Instance.GetAudioManager().StopMusic();

        HandleAnswerResult(card, difficulty);

        yield return new WaitForSeconds(2f);
        
        GameManager.Instance.GetUIManager().ResetAnswersColors();
    }
    
    //Selecciona una pregunta dada una dificultad y la elimina de la lista correspondiente para no repetirla.
    private void SelectQuestion(int difficulty)
    {
        QuestionData.Subject subject = GameManager.Instance.GetLevelsManager().GetActualSubject();

        if (subject == QuestionData.Subject.Principal)
        {
            List<QuestionData.Subject> values = Enum.GetValues(typeof(QuestionData.Subject)).Cast<QuestionData.Subject>().Where(s => s != QuestionData.Subject.Principal).ToList();
            subject = values[Random.Range(0, values.Count)];
        }

        QuestionData.Difficulty difficultyEnum = (QuestionData.Difficulty)difficulty;
        var key = (subject, difficultyEnum);

        if (_questionBank.TryGetValue(key, out var list) && list.Count > 0)
        {
            int index = Random.Range(0, list.Count);
            _selectedQuestion = list[index];
            list.RemoveAt(index);

            GameManager.Instance.GetUIManager().ShowQuestion(_selectedQuestion);
        }
        else
        {
            Debug.LogWarning($"No questions found for {subject} - {difficultyEnum}");
        }
    }
    
    //Indica que el jugador selecciono una respuesta.
    public void OnAnswerSelected(Button clickedButton)
    {
        string selectedText = clickedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        _playerAnswersCorrectly = CheckAnswer(selectedText);
        _playerHasAnswered = true;
        OnAnswer?.Invoke(_playerAnswersCorrectly, clickedButton);
    }

    //Verifica si la respuesta es correcta (pinta la carta de verde) o incorrecta (pinta la carta de rojo).
    private void HandleAnswerResult(CardBoard card, int difficulty)
    {
        if (!card) return;

        if (_playerAnswersCorrectly)
        {
            card.ChangeColor(CardBoard.CardColor.Green);
            GameManager.Instance.GetPlayer().AddStats(card.GetCardType(), difficulty);
            GameManager.Instance.GetAudioManager().PlayAudio(AudioManager.AudioList.RightAnswer);
        }
        else
        {
            card.ChangeColor(CardBoard.CardColor.Red);
            GameManager.Instance.GetAudioManager().PlayAudio(AudioManager.AudioList.WrongAnswer);
        }
    }

    #endregion

    #region LoadQuestions
    private void InitializeQuestionBank()
    {
        _questionBank = new Dictionary<(QuestionData.Subject, QuestionData.Difficulty), List<QuestionData>>();

        foreach (QuestionData.Subject subject in Enum.GetValues(typeof(QuestionData.Subject)))
        {
            if (subject == QuestionData.Subject.Principal) continue;

            foreach (QuestionData.Difficulty diff in Enum.GetValues(typeof(QuestionData.Difficulty)))
            {
                _questionBank[(subject, diff)] = new List<QuestionData>();
            }
        }
    }

    private void LoadQuestions(string lang)
    {
        string path = lang;
        var allQuestions = Resources.LoadAll<QuestionData>(path);

        foreach (var question in allQuestions)
        {
            var key = (question.subject, question.difficulty);
            if (_questionBank.ContainsKey(key))
                _questionBank[key].Add(question);
        }
    }

    private void ClearQuestionBank()
    {
        _questionBank.Clear();
    }

    #endregion

    #region Utilities
    private bool CheckAnswer(string selectedText)
    {
        return selectedText == _selectedQuestion.GetCorrectAnswer();
    }
    public void PlayerAnswersCorrectly(bool correct) => _playerAnswersCorrectly = correct;
    public void TimeRanOut(bool ranOut) => _timeRanOut = ranOut;

    private void ChangeLanguage(Locale lang)
    {
        language = (lang.ToString() == "English (en)") ? "en" : "es";
        print(language);
        ClearQuestionBank();
        StartCoroutine(WaitForQuestions());
    }

    private IEnumerator WaitForQuestions()
    {
        yield return new WaitForEndOfFrame();
        InitializeQuestionBank();
        LoadQuestions(language);
    }

    #endregion

    #region Getters
    public bool PlayerHasAnswered() => _playerHasAnswered;
    #endregion
}
