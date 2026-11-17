using UnityEngine;
using UnityEngine.AI;

namespace DungeonCrawler.Enemies
{
    /// <summary>Encapsula o NavMeshAgent: destino, parada e rotação durante parada.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        private NavMeshAgent cachedAgent;
        private float stoppingDistance;

        // Lazy: EnemyBrain.Awake pode rodar antes do Awake deste componente.
        private NavMeshAgent agent => cachedAgent != null ? cachedAgent : (cachedAgent = GetComponent<NavMeshAgent>());

        public bool IsOnNavMesh => agent != null && agent.enabled && agent.isOnNavMesh;

        public void SetSpeed(float speed) => agent.speed = speed;

        /// <summary>Raio do agente (largura usada para validar a faixa da investida F13).</summary>
        public float Radius => agent != null ? agent.radius : 0.5f;

        /// <summary>F13: recorta o segmento na primeira borda do NavMesh (paredes assadas). Uma consulta no compromisso.</summary>
        public Vector3 ClampLane(Vector3 from, Vector3 to)
        {
            if (!IsOnNavMesh) return from;
            return NavMesh.Raycast(from, to, out NavMeshHit hit, agent.areaMask) ? hit.position : to;
        }

        /// <summary>F13: deslocamento relativo restrito ao NavMesh (NavMeshAgent.Move); nunca Rigidbody.</summary>
        public void MoveBy(Vector3 delta)
        {
            if (IsOnNavMesh) agent.Move(delta);
        }

        public void SetStoppingDistance(float distance)
        {
            stoppingDistance = distance;
            agent.stoppingDistance = distance;
        }

        public void MoveTo(Vector3 destination, bool approachCover = false)
        {
            if (!IsOnNavMesh) return;
            // While occluded, do not stop at the ranged attack radius behind the wall.
            agent.stoppingDistance = approachCover ? agent.radius : stoppingDistance;
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
            agent.ResetPath();
            agent.isStopped = true;
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
