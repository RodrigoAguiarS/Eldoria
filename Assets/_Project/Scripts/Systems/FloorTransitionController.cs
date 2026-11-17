using System;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonCrawler.Systems
{
    /// <summary>Commits arrival before loading; completion alone never forces a transition.</summary>
    public sealed class FloorTransitionController : MonoBehaviour
    {
        [SerializeField] private FixedFloorConfiguration configuration;
        private FixedFloorSequence Sequence => configuration != null ? configuration.Sequence : null;
        [SerializeField] private RunCheckpointController checkpoint;
        [SerializeField] private FloorController floor;
        [SerializeField] private Health health;
        private bool loading;
        public string Status { get; private set; } = "Colete as recompensas antes de descer. Não há retorno.";
        public event Action StatusChanged;
        public bool CanAdvance => isActiveAndEnabled && !loading && Sequence != null && checkpoint != null &&
            floor != null && floor.IsCompleted && health != null && health.IsAlive && Time.timeScale > 0f && checkpoint.IsSafe &&
            Sequence.Next(checkpoint.FloorId) != null;
        public void Advance() => TryAdvance();
        public bool TryAdvance()
        {
            if (!CanAdvance) return Fail("Descida indisponível: conclua o andar, vivo e fora da pausa.");
            var next = Sequence.Next(checkpoint.FloorId);
            if (!Application.CanStreamedLevelBeLoaded(next.scenePath)) return Fail("Cena do próximo andar ausente na build.");
            var data = FixedFloorSequence.PrepareArrival(checkpoint.Capture(), next.catalog);
            if (data == null) return Fail("Dados ou referências incompatíveis com o próximo andar.");
            loading = true;
            if (!RunCheckpointSession.Store.TrySave(data, next.catalog, out string error)) { loading = false; return Fail(error); }
            checkpoint.CommitDeparture();
            // Protect only the departing instance from a projectile later in this frame. Destination restores normal HP state.
            health.SetInvulnerableFor(float.PositiveInfinity);
            RunCheckpointSession.RequestContinue(data);
            Time.timeScale = 1f;
            SceneManager.LoadScene(next.scenePath);
            return true;
        }
        private bool Fail(string message) { Status = message; StatusChanged?.Invoke(); return false; }
    }
}
