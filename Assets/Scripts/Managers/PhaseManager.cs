using System;
using System.Collections;
using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    public enum GamePhase {Draw, Discard, Colocation, Questions, Resolution}
    
    private GamePhase _currentPhase;
    private bool _gameOver;

    private void Awake()
    {
        GameFlowManager.OnLevelStart += StartPhases;
        GameFlowManager.OnLevelOver += EndPhases;
    }

    private void OnDestroy()
    {
        GameFlowManager.OnLevelStart -= StartPhases;
        GameFlowManager.OnLevelOver -= EndPhases;
    }

    private void StartPhases()
    {
        CurrentPhase = GamePhase.Draw;
        _gameOver = false;
        AdvancePhase();
    }

    private void EndPhases()
    {
        _gameOver = true;
        StopAllCoroutines();
    }

    private void AdvancePhase()
    {
        if (_gameOver) return;
        StartCoroutine(ExecutePhase());
    }
    
    //Se ejecuta la fase actual y luego de terminar se avanza a la siguiente.
    private IEnumerator ExecutePhase()
    {
        switch (_currentPhase)
        {
            case GamePhase.Draw:
                yield return DrawPhase();
                CurrentPhase = GamePhase.Discard;
                break;
            
            case GamePhase.Discard:
                yield return DiscardPhase();
                CurrentPhase = GamePhase.Colocation;
                break;
            
            case GamePhase.Colocation:
                yield return ColocationPhase();
                CurrentPhase = GamePhase.Questions;
                break;

            case GamePhase.Questions:
                yield return QuestionsPhase();
                CurrentPhase = GamePhase.Resolution;
                break;

            case GamePhase.Resolution:
                yield return ResolutionPhase();
                CurrentPhase = GamePhase.Draw;
                break;
        }
        AdvancePhase();
    }

    #region Phases

    private IEnumerator DrawPhase()
    {
        GameManager.Instance.GetPlayersHand().DrawCards();
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator DiscardPhase()
    {
        GameManager.Instance.UpdateZones(_currentPhase.ToString());
        GameManager.Instance.GetDiscardZone().ToggleLightAnimation();
        yield return new WaitUntil(() => GameManager.Instance.GetPlayersHand().CantCardsInHand() <= 3);
    }
    
    private IEnumerator ColocationPhase()
    {
        GameManager.Instance.UpdateZones(_currentPhase.ToString());
        GameManager.Instance.GetDiscardZone().ToggleLightAnimation();
        yield return new WaitUntil(() => GameManager.Instance.GetPlayersHand().CantCardsInHand() <= 0);
    }

    private IEnumerator QuestionsPhase()
    {
        yield return GameManager.Instance.GetUIManager().QuestionMode();
        yield return GameManager.Instance.GetQuestionManager().StartQuestions();
    }

    private IEnumerator ResolutionPhase()
    {
        GameManager.Instance.GetAudioManager().PlayMusicWithFade(AudioManager.MusicList.Game);
        yield return GameManager.Instance.GetUIManager().BattleMode();
        yield return GameManager.Instance.GetCombatManager().ResolveCombat();
        yield return GameManager.Instance.GetUIManager().NormalMode();
        GameManager.Instance.EndTurn();
    }

    #endregion
    
    public GamePhase CurrentPhase
    {
        get
        {
            return _currentPhase;
        }
        set
        {
            _currentPhase = value;
            GameManager.Instance.GetUIManager().UpdatePhaseText(_currentPhase);
        }
    }
}
