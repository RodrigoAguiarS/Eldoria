using System;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Trigger que avisa quando o jogador (por layer) entra na sala.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask playerMask;

        public event Action PlayerEntered;

        private void OnTriggerEnter(Collider other)
        {
            if ((playerMask.value & (1 << other.gameObject.layer)) != 0)
                PlayerEntered?.Invoke();
        }
    }
}
