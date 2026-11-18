using System;
using DungeonCrawler.Systems;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// V05: parâmetros de emissão das sheets/atlas A04–A06 para o <see cref="PlayerRewardFeedback"/>.
    /// Só apresentação: nada aqui lê ou altera HP, XP, consumo, tabelas ou RNG de loot.
    /// Preenchido por <c>RewardFxArtIntegrator</c> a partir dos contratos FX-HEAL/FX-LEVEL/FX-LOOT.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/UI/Reward FX Profile")]
    public sealed class RewardFxProfile : ScriptableObject
    {
        [Serializable]
        public struct Emission
        {
            [Min(1)] public int particles;
            [Tooltip("Raio do círculo de posições fixas (m). 0 = uma partícula no pivô.")]
            [Min(0f)] public float radius;
            [Tooltip("Altura sobre o pivô dos pés (m).")]
            public float height;
            [Tooltip("Velocidade vertical (m/s).")]
            public float riseSpeed;
            [Min(.01f)] public float lifetime;
            [Min(.01f)] public float size;
        }

        /// <summary>Semente fixa das partículas emitidas; nenhuma emissão usa UnityEngine.Random.</summary>
        public const uint EmitSeed = 1543u;

        public Emission healing, collection, level;
        [Tooltip("Célula do atlas FX-LOOT por LootKind (índice = valor do enum).")]
        public int[] lootFrameByKind = { 0, 1, 2, 2, 2 };
        [Min(1)] public int lootFrameCount = 3;

        public int LootFrame(LootKind kind)
        {
            int index = (int)kind;
            int frame = lootFrameByKind != null && index >= 0 && index < lootFrameByKind.Length ? lootFrameByKind[index] : lootFrameCount - 1;
            return Mathf.Clamp(frame, 0, lootFrameCount - 1);
        }

        /// <summary>Valor normalizado de frameOverTime no centro da célula, longe das bordas de arredondamento.</summary>
        public float LootFrameOverTime(LootKind kind) => (LootFrame(kind) + .5f) / lootFrameCount;
    }
}
