using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Destructible Prop", fileName = "Prop_Vase")]
    public sealed class DestructiblePropDefinition : ScriptableObject
    {
        [SerializeField, Min(1f)] private float maxHealth = 40f;
        public float MaxHealth => float.IsFinite(maxHealth) ? Mathf.Max(1f, maxHealth) : 40f;
    }
}
