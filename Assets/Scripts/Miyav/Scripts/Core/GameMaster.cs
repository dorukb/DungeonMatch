using DorkyProductions.States;
using Mirror;
using UnityEngine;

namespace DorkyProductions.Core
{
    
public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance { get; private set; }
    
    private Context _context;

    public IGameState CurrentState { get; private set; }

    private Player currentPlayer = null;
    public Context context { get; private set; }
    
    private Player localPlayer;
    private Player opponent;
    
    
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

        RegisterLocalPlayer("");
    }
 
    private void Update()
    {
        CurrentState?.Update(this.context);
    }

    public void RegisterLocalPlayer(string playerName)
    {
        if (localPlayer == null)
        {
            localPlayer = new HumanPlayer(0, playerName);
        }
        else
        {
            Debug.LogError("LocalPlayer already registered. Why are you trying to register again?");
        }
    }

    public void RegisterOpponent(string playerName)
    {
        if (opponent == null)
        {
            opponent = new HumanPlayer(1, playerName);
        }
        else
        {
            Debug.LogError("Opponent already registered. Why are you trying to register the opponent again?");
        }
    }

    public void ChangeLocalPlayerName(string playerName)
    {
       localPlayer.DisplayName = playerName;
       Debug.Log($"Local player is now called: {playerName}");
    }
    
    public void ChangeOpponentName(string playerName)
    {
        opponent.DisplayName = playerName;
        Debug.Log($"Opponent is now called: {playerName}");
    }
    public void TransitionToState(IGameState newState)
    {
        CurrentState?.Exit(this.context);
        CurrentState = newState;
        CurrentState.Enter(this.context);
    }
    public void StartNewGame()
    {
        this.context = new Context(this);
        TransitionToState(new GameStartState());
    }
    public Player GetLocalPlayer()
    {
        return localPlayer;
    }
    public Player GetOpponent()
    {
        return opponent;
    }

    public int GetCurrentPlayerID()
    {
        return this.context.currentPlayer.id;
    }
    public void NextPlayer()
    {
        currentPlayer = (currentPlayer == localPlayer) ? opponent : localPlayer;
        this.context.currentPlayer = currentPlayer;
    }
    public void DealCardsToAllPlayers(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            localPlayer.ReceiveCatCard(context.DrawPile.Draw());
            opponent.ReceiveCatCard(context.DrawPile.Draw());
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