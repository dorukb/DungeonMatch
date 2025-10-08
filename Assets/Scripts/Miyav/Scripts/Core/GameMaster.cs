using System.Collections.Generic;
using System.Linq;
using DorkyProductions.States;
using UnityEngine;

namespace DorkyProductions.Core
{
    
public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance { get; private set; }
    
    private CardRegistry _cardRegistry; // Not really needed. might remove.

    private Context _context;

    private List<Player> _players = new List<Player>();
    public IGameState CurrentState { get; private set; }

    private int CurrentPlayerIdx = -1;
    public int TotalPlayers => _players.Count;
    public Context context { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else if (Instance != this)
        {
            Debug.LogWarning("GameMaster already exists.Trying to create another one signals something is wrong.");
            Destroy(gameObject);
        }
        
        // TODO: Remove.
        Application.targetFrameRate = 144;
        QualitySettings.vSyncCount = 0;
    }
    void Start()
    {
        _cardRegistry = CardRegistry.Instance;
        if (_cardRegistry == null)
        {
            Debug.LogError("No card registry found.");
        }
        // Players need to be "connected" and 'set-up' BEFORE starting the game.
        _players.Add(new HumanPlayer(0, "Master Dork"));    
        _players.Add(new PlayerAI(1, "Dumb AI"));

        // TODO: Remove this, should start when user presses 'Play' :))
        Invoke(nameof(StartNewGame), 1.5f);
    }   
    private void Update()
    {
        CurrentState?.Update(this.context);
    }
    private void StartNewGame()
    {
        this.context = new Context(this);
        TransitionToState(new GameStartState());
    }
    public void TransitionToState(IGameState newState)
    {
        CurrentState?.Exit(this.context);
        CurrentState = newState;
        CurrentState.Enter(this.context);
    }

    public Player GetPlayer(int playerId)
    {
        return _players.FirstOrDefault(t => t.id == playerId);
    }

    public int GetCurrentPlayerID()
    {
        return this.context.currentPlayer.id;
    }
    public void NextPlayer()
    {
        CurrentPlayerIdx = (CurrentPlayerIdx + 1) % TotalPlayers;
        this.context.currentPlayer = _players[CurrentPlayerIdx];
    }
    public void DealCardsToAllPlayers(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            foreach (var player in _players)
            {
                player.ReceiveCatCard(context.DrawPile.Draw());
            }
        }
    }

    public void PlaceCardsInTheShelterRow(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            this.context.ShelterRow.DealCard(context.DrawPile.Draw().data);
        }
    }
}

}