using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

namespace DorkyProductions.AI
{
    // Attach this to your Player Prefab (or a variant of it).
    // This script only runs on the Server.
    [RequireComponent(typeof(NetworkPlayer))]
    public class BotNetworkPlayer : NetworkBehaviour
    {
        [Header("Bot Settings")]
        [Tooltip("How long the client will see the 'Thinking' state before the move resolves.")]
        public float VisualThinkingDuration = 1.5f;
        public int skillId = -1;
        private Dictionary<SkillType, Action<GameBoard>> _skillMap;
        private IBotBrain _botBrain;
        public override void OnStartServer()
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.OnServerTurnStarted += MakeMove;
                GameMaster.Instance.OnServerChestMatched += SaveChestRewardForOpening;
                GameMaster.Instance.OnServerSwapDenied += MakeSwap;

                InitializeSkillMap();
            }
        }

        public override void OnStopServer()
        {
            if (GameMaster.Instance != null)
            {
                GameMaster.Instance.OnServerTurnStarted -= MakeMove;
                GameMaster.Instance.OnServerChestMatched -= SaveChestRewardForOpening;
                GameMaster.Instance.OnServerSwapDenied -= MakeSwap;
            }
        }

        [Server]
        public void InitializeBotBrain(LobbyType lobbyType)
        {
            // The Factory Method.
            switch (lobbyType)
            {
                case LobbyType.Beginner:
                    _botBrain = new EasyBotBrain();
                    break;
                case LobbyType.Intermediate:
                    _botBrain = new MediumBotBrain();
                    break;
                case LobbyType.Advanced:
                    _botBrain = new HardBotBrain();
                    break;
                default:
                    _botBrain = new MediumBotBrain();
                    break;
            }
        }
        
        [Server]
        private void MakeMove(Context context, GameBoard board)
        {
            // If it's not my turn, ignore
            if (netId != context.ActivePlayerNetId) return;

            Debug.Log("Bot is Making a Move.");
            if (context.ChestsLeft > 0)
            {
                // TODO: The selected skill should be decided by GameMaster, bot should just execute.
                skillId = Random.Range(0, _skillMap.Count);
                SkillType type = (SkillType)skillId;
                if (_skillMap.TryGetValue(type, out Action<GameBoard> skill))
                {
                    skill.Invoke(board);
                }
            }
            else
            {
                MakeSwap(context, board);
            }
        }


        [Server]
        private void MakeSwap(Context context, GameBoard board)
        {
            List<Vector2Int> foundPos = new List<Vector2Int>(_botBrain.FindSwap(board));
            GameMaster.Instance.ProcessPlayerSwap(netIdentity, foundPos[0], foundPos[1], VisualThinkingDuration);
        }
        [Server]
        private void PerformLightningSkill(GameBoard gameBoard)
        {
            var targetTile = _botBrain.GetRandomTile();
            GameMaster.Instance.ProcessPlayerLightningSkillUse(netIdentity, targetTile, VisualThinkingDuration);
        }

        [Server]
        private void PerformPhantomMatchSkill(GameBoard board)
        {
            List<Vector2Int> targetTiles = _botBrain.GetPhantomMatchSkillInput(board);
            GameMaster.Instance.ProcessPlayerPhantomMatchSkill(netIdentity,targetTiles, VisualThinkingDuration);
        }
        
        //TODO: implement phaseshift
        [Server]
        private void PerformPhaseShiftSkill(GameBoard board)
        {
            List<Vector2Int> targetTiles = _botBrain.GetPhaseShiftSkillInput(board);
            GameMaster.Instance.ProcessPlayerPhaseShiftSkill(netIdentity, targetTiles, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformStoneGuardSkill(GameBoard board)
        {
            int amount = RemoteConfigManager.Instance.GetRewardShield();
            GameMaster.Instance.ProcessPlayerStoneGuardSkill(amount, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformSoulReaverSkill(GameBoard board)
        {
            int amount = RemoteConfigManager.Instance.GetStolenHealth();
            GameMaster.Instance.ProcessPlayerSoulReaverSkill(amount, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformArcaneSweepSkill(GameBoard board)
        {
            var targetTile = _botBrain.GetRandomTile();
            GameMaster.Instance.ProcessPlayerSweepSkill(targetTile, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void PerformArcaneCleaveSkill(GameBoard board)
        {
            var targetTile = _botBrain.GetRandomTile();
            GameMaster.Instance.ProcessPlayerCleaveSkill(targetTile, netIdentity, VisualThinkingDuration);
        }
        
        [Server]
        private void SaveChestRewardForOpening(Context ctx, int rewardSkillId)
        {
            // If it's not my turn, ignore
            if (netId != ctx.ActivePlayerNetId) return;
            
            skillId = rewardSkillId;
        }
        
        private void InitializeSkillMap()
        {
            _skillMap = new Dictionary<SkillType, Action<GameBoard>>
            {
                { SkillType.Lightning, PerformLightningSkill },
                { SkillType.PhantomMatch, PerformPhantomMatchSkill },
                { SkillType.PhaseShift, PerformPhaseShiftSkill },
                { SkillType.StoneGuard, PerformStoneGuardSkill },
                { SkillType.SoulReaver, PerformSoulReaverSkill },
                { SkillType.ArcaneSweep, PerformArcaneSweepSkill },
                { SkillType.ArcaneCleave, PerformArcaneCleaveSkill }
            };
        }
        
        
    }
}