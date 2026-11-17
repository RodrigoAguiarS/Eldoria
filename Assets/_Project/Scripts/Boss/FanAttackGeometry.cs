using UnityEngine;

namespace DungeonCrawler.Boss
{
    /// <summary>Setor fechado XZ: centro do alvo, sem expandir o perigo por colliders invisíveis.</summary>
    public static class FanAttackGeometry
    {
        public static bool Contains(Vector3 origin, Vector3 forward, Vector3 point, float radius, float angle, float heightTolerance)
        {
            Vector3 offset = point - origin;
            if (Mathf.Abs(offset.y) > heightTolerance) return false;
            offset.y = 0f; forward.y = 0f;
            if (offset.sqrMagnitude > radius * radius) return false;
            if (offset.sqrMagnitude < .000001f) return true;
            return Vector3.Dot(forward.normalized, offset.normalized) >= Mathf.Cos(angle * .5f * Mathf.Deg2Rad) - .000001f;
        }
    }
}
