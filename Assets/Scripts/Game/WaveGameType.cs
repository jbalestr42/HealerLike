using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using UnityEngine.Events;

public class WaveGameType : AGameType
{
    public static UnityEvent OnWaveEnd = new UnityEvent();

    public enum State
    {
        None,
        InitializeWave,
        WaitForWaveToStart,
        WaitingForEndOfWave,
        SelectUpgrade,
        SelectWave,
        GameEnd,
        GameOver,
    }

    [SerializeField] AWaveData _nextWaveData;

    State _state = State.None;
    EntityManager _entities = null;
    GameView _gameView;
    UpgradeView _upgradeView;
    WaveView _waveView;
    int _currentWave = 0;

    Coroutine _spawnCor = null;

    public override int currentWave => _currentWave;

    void Start()
    {
        _entities = EntityManager.instance;
        _gameView = UIManager.instance.GetView<GameView>(ViewType.Game);
        _upgradeView = UIManager.instance.GetView<UpgradeView>(ViewType.Upgrade);
        _waveView = UIManager.instance.GetView<WaveView>(ViewType.Wave);

        _gameView.gameHUD.nextWaveButton.onClick.AddListener(StartWave);
        _upgradeView.OnItemSelected.AddListener(OnItemSelected);
        _waveView.OnWaveSelected.AddListener(OnWaveSelected);

        PlayerBehaviour.instance.OnLifeChanged.AddListener(OnLifeChanged);
    }

    void Update()
    {
        switch (_state)
        {
            case State.None:
                break;

            case State.InitializeWave:
                _currentWave++;
                GameManager.instance.OnWaveChanged.Invoke(_currentWave);
                _gameView.gameHUD.inventoryButton.interactable = true;
                _gameView.gameHUD.nextWaveButton.interactable = true;
                SetState(State.WaitForWaveToStart);
                break;

            case State.WaitForWaveToStart:
                // Wait for player to click on "nextWaveButton"
                break;

            case State.WaitingForEndOfWave:
                if (_spawnCor == null && _entities.AreAllEnemyDead())
                {
                    // Show Upgrade UI
                    UIManager.instance.AddView(ViewType.Upgrade);
                    _upgradeView.FillChoices();
                    OnWaveEnd.Invoke();
                    SetState(State.SelectUpgrade);
                }
                break;

            case State.SelectUpgrade:
                    // Wait for player to select an item
                break;

            case State.SelectWave:
                    // Wait for player to select a wave
                break;

            case State.GameEnd:
                break;

            case State.GameOver:
                UIManager.instance.AddView(ViewType.GameOver);
                SetState(State.None);
                break;

            default:
                Debug.Log("State not implemented: " + _state);
                break;
        }
    }

    void StartWave()
    {
        SetState(State.WaitingForEndOfWave);
        // TODO add method to set the mode in the UI manager, then call this method here
        _gameView.gameHUD.inventoryButton.interactable = false;
        _gameView.gameHUD.nextWaveButton.interactable = false;
        _gameView.playerInventory.HideInventory();
        _spawnCor = StartCoroutine(StartWave(_nextWaveData));
    }

    void OnLifeChanged(int life)
    {
        if (life <= 0)
        {
            SetState(State.GameOver);
        }
    }

    public override void StartGame()
    {
        SetState(State.InitializeWave);
    }

    public void OnItemSelected(AItem item)
    {
        Debug.Log("[DEBUG] OnItemSelected " + item);
        _gameView.playerInventory.AddItem(item);
        _upgradeView.ClearChoices();
        UIManager.instance.PopCurrentView();
        UIManager.instance.AddView(ViewType.Wave);
        _waveView.FillChoices(_currentWave);
        SetState(State.SelectWave);
    }

    public void OnWaveSelected(AWaveData waveData)
    {
        Debug.Log("[DEBUG] OnWaveSelected " + waveData);
        _nextWaveData = waveData;
        _waveView.ClearChoices();
        UIManager.instance.PopCurrentView();
        SetState(State.InitializeWave);
    }

    public override bool IsOver()
    {
        return _state == State.GameEnd;
    }

    void SetState(State newState)
    {
        Debug.Log($"[WaveGameType] {_state} -> {newState}");
        _state = newState;
    }

    IEnumerator StartWave(AWaveData wave)
    {
        int stepCount = 0;
        CheckPoint start = CheckPointManager.instance.start;
        while (stepCount < wave.count)
        {
            AWaveStep step = wave.GetStep(stepCount);
            WaitForSeconds wait = new WaitForSeconds(step.spawnRate);
            
            int count = step.count;
            EnemyData data = step.GetEnemyData();
            while (count != 0)
            {
                GameObject go = EntityManager.instance.SpawnEnemy(data, start.transform.position);
                go.GetComponent<CheckPointMove>().SetNextDestination(start.next);
                count--;
                yield return wait;
            }
            stepCount++;
        }
        yield return null;
        _spawnCor = null;
    }
}