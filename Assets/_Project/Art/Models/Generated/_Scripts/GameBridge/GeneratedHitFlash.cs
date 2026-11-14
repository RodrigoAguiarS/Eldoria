using UnityEngine;

namespace DungeonCrawler.CharacterArt.GameBridge
{
    /// <summary>
    /// Feedback de dano para os modelos gerados (o DamageFlash original age só sobre a cápsula, agora oculta).
    /// Tinge todos os renderers por um instante via MaterialPropertyBlock e depois remove o bloco,
    /// restaurando as cores originais de cada submesh. Não cria instâncias de material.
    /// </summary>
    internal sealed class GeneratedHitFlash
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly Renderer[] renderers;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private float until;
        private bool active;

        public GeneratedHitFlash(Renderer[] renderers)
        {
            this.renderers = renderers;
        }

        public void Flash(Color color, float seconds)
        {
            block.Clear();
            block.SetColor(BaseColorId, color);
            foreach (Renderer r in renderers)
            {
                if (r != null) r.SetPropertyBlock(block);
            }

            until = Time.time + seconds;
            active = true;
        }

        public void Tick()
        {
            if (!active || Time.time < until) return;
            foreach (Renderer r in renderers)
            {
                if (r != null) r.SetPropertyBlock(null);
            }

            active = false;
        }
    }
}
