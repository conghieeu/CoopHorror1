using Fusion;
using UnityEngine;

namespace FusionImpostor
{
    /// <summary>
    /// SimulationBehaviour that spawns and despawns players as they join and leave.
    /// </summary>
    public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        [Tooltip("Prefab of that character that will be spawned.")]
        public NetworkObject playerObject;

        public void PlayerJoined(PlayerRef player)
        {
            // Only the server can spawn.
            if (Runner.IsServer)
            {
                Runner.Spawn(playerObject, position: GameManager.Instance.preGameMapData.GetSpawnPosition(player.AsIndex), inputAuthority: player);
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            // Only the server can despawn.
            if (Runner.IsServer)
            {
                PlayerObject leftPlayer = PlayerRegistry.GetPlayer(player);
                if (leftPlayer != null)
                {
                    Runner.Despawn(leftPlayer.Object);
                }
            }
        }
    }
}