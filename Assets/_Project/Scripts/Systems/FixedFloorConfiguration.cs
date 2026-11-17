using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Prefab-owned asset reference, explicitly injected into the binary gameplay scenes.</summary>
    public sealed class FixedFloorConfiguration : MonoBehaviour
    {
        [SerializeField] private FixedFloorSequence sequence;
        public FixedFloorSequence Sequence => sequence;
    }
}
