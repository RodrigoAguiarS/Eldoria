using UnityEngine;

namespace DungeonCrawler.Enemies
{
    public enum LungePhase { Ready, Telegraph, Dash, Recover }

    /// <summary>
    /// Subetapas puras da investida (sem Unity Time): Telegraph → Dash → Recover → Ready.
    /// O chamador fornece o relógio escalado; pausa congela porque o relógio não avança.
    /// Uma única reivindicação de contato por investida garante um dano por alvo.
    /// </summary>
    public struct LungeTimeline
    {
        private float telegraphStartedAt;
        private float telegraphEndsAt;
        private float dashDuration;
        private float recoverDuration;
        private float recoverEndsAt;
        private bool contactClaimed;

        public LungePhase Phase { get; private set; }
        public bool IsBusy => Phase != LungePhase.Ready;
        public float TelegraphEndsAt => telegraphEndsAt;
        public float TelegraphDuration => telegraphEndsAt - telegraphStartedAt;
        public bool ContactClaimed => contactClaimed;

        public static float EffectiveTelegraph(float seconds) =>
            Mathf.Max(EnemyLungeData.MinTelegraphSeconds, float.IsNaN(seconds) ? 0f : seconds);

        public static float DashDuration(float distance, float speed) =>
            Mathf.Max(0.05f, Mathf.Max(0f, distance) / Mathf.Max(0.01f, speed));

        public void Begin(float now, float telegraphSeconds, float dashSeconds, float recoverSeconds)
        {
            telegraphStartedAt = now;
            telegraphEndsAt = now + EffectiveTelegraph(telegraphSeconds);
            dashDuration = Mathf.Max(0.05f, dashSeconds);
            recoverDuration = Mathf.Max(0f, recoverSeconds);
            recoverEndsAt = float.MaxValue;
            contactClaimed = false;
            Phase = LungePhase.Telegraph;
        }

        public float TelegraphProgress(float now) => Phase != LungePhase.Telegraph || TelegraphDuration <= 0f
            ? 0f : Mathf.Clamp01((now - telegraphStartedAt) / TelegraphDuration);

        /// <summary>Telegraph termina somente no horário travado; o dash começa nesse instante, não no frame.</summary>
        public bool TryStartDash(float now)
        {
            if (Phase != LungePhase.Telegraph || now < telegraphEndsAt) return false;
            Phase = LungePhase.Dash;
            return true;
        }

        /// <summary>0 → 1 ao longo da faixa, medido a partir do fim do aviso.</summary>
        public float DashProgress(float now) => Phase != LungePhase.Dash
            ? 0f : Mathf.Clamp01((now - telegraphEndsAt) / dashDuration);

        /// <summary>Primeira chamada por investida retorna true; as demais, false.</summary>
        public bool TryClaimContact()
        {
            if (Phase != LungePhase.Dash || contactClaimed) return false;
            contactClaimed = true;
            return true;
        }

        public void EndDash(float now)
        {
            if (Phase != LungePhase.Dash) return;
            Phase = LungePhase.Recover;
            recoverEndsAt = now + recoverDuration;
        }

        public bool TryFinish(float now)
        {
            if (Phase != LungePhase.Recover || now < recoverEndsAt) return false;
            Phase = LungePhase.Ready;
            return true;
        }

        public void Cancel()
        {
            Phase = LungePhase.Ready;
            contactClaimed = false;
        }

        /// <summary>Distância no plano XZ entre o ponto e o segmento percorrido no frame.</summary>
        public static float FlatDistanceToSegment(Vector3 a, Vector3 b, Vector3 point)
        {
            var a2 = new Vector2(a.x, a.z);
            var ab = new Vector2(b.x, b.z) - a2;
            var ap = new Vector2(point.x, point.z) - a2;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq > 1e-8f ? Mathf.Clamp01(Vector2.Dot(ap, ab) / lengthSq) : 0f;
            return (ap - ab * t).magnitude;
        }
    }
}
