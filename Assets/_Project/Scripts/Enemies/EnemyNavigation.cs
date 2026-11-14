using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Enemies
{
    /// <summary>Encapsula o NavMeshAgent: destino, parada e rotação durante parada.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        private NavMeshAgent cachedAgent;

        // Lazy: EnemyBrain.Awake pode rodar antes do Awake deste componente.
        private NavMeshAgent agent => cachedAgent != null ? cachedAgent : (cachedAgent = GetComponent<NavMeshAgent>());

        public bool IsOnNavMesh => agent != null && agent.enabled && agent.isOnNavMesh;

        public void SetSpeed(float speed) => agent.speed = speed;

        public void SetStoppingDistance(float distance) => agent.stoppingDistance = distance;

        public void MoveTo(Vector3 destination)
        {
            if (!IsOnNavMesh) return;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        /// <summary>Move-se para longe da ameaça, se houver ponto válido no NavMesh. Retorna true se iniciou o recuo.</summary>
        public bool TryRetreatFrom(Vector3 threat, float distance)
        {
            if (!IsOnNavMesh) return false;

            Vector3 away = transform.position - threat;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            away.Normalize();

            if (!NavMesh.SamplePosition(transform.position + away * distance, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return false;

            MoveTo(hit.position);
            return true;
        }

        public void Stop()
        {
            if (!IsOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        public void Disable()
        {
            if (agent != null) agent.enabled = false;
        }

        public void FaceTowards(Vector3 worldPoint, float degreesPerSecond)
        {
            Vector3 flat = worldPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * Time.deltaTime);
        }
    }
}
