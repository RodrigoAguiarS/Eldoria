using UnityEngine;
using DungeonCrawler.Systems;

namespace DungeonCrawler.CharacterArt.GameBridge
{
    /// <summary>
    /// Feedback de dano para os modelos gerados (o DamageFlash original age só sobre a cápsula, agora oculta).
    /// Tinge os renderers via MaterialPropertyBlock e restaura cada submesh após o flash.
    /// Uma cor persistente opcional representa a fase do Boss sem instanciar materiais.
    /// </summary>
    internal sealed class GeneratedHitFlash
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly Renderer[] renderers;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private readonly Color[][] baseColors;
        private float until;
        private bool active;
        private bool tinted;
        private Color tint;
        private readonly GameOptionsDefinition options;
        private Color currentFlash;
        private float lastIntensity;
        private float Intensity => options != null ? options.Current.flash : 1f;

        public GeneratedHitFlash(Renderer[] renderers, GameOptionsDefinition options = null)
        {
            this.options = options;
            this.renderers = renderers;
            baseColors = new Color[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i] != null ? renderers[i].sharedMaterials : new Material[0];
                baseColors[i] = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                    baseColors[i][j] = materials[j] != null && materials[j].HasProperty(BaseColorId)
                        ? materials[j].GetColor(BaseColorId) : Color.white;
            }
        }

        public void SetBaseTint(Color color)
        {
            tint = color;
            tinted = true;
            if (!active) Apply(null);
        }

        public void ClearFlash()
        {
            active = false;
            Apply(null);
        }

        public void Flash(Color color, float seconds)
        {
            currentFlash = color;
            lastIntensity = Intensity;
            Apply(color);
            until = Time.time + seconds;
            active = true;
        }

        public void Tick()
        {
            if (!active) return;
            if (lastIntensity != Intensity) { lastIntensity = Intensity; Apply(currentFlash); }
            if (Time.time < until) return;
            ClearFlash();
        }

        private void Apply(Color? flashColor)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;
                for (int j = 0; j < baseColors[i].Length; j++)
                {
                    if (!flashColor.HasValue && !tinted) { renderer.SetPropertyBlock(null, j); continue; }
                    Color baseline = tinted ? Color.Lerp(baseColors[i][j], tint, .65f) : baseColors[i][j];
                    Color color = flashColor.HasValue ? Color.Lerp(baseline, flashColor.Value, Intensity) : baseline;
                    color.a = baseColors[i][j].a;
                    block.Clear();
                    block.SetColor(BaseColorId, color);
                    renderer.SetPropertyBlock(block, j);
                }
            }
        }
    }
}
