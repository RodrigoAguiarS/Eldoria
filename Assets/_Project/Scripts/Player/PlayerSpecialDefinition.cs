using UnityEngine;

namespace DungeonCrawler.Player
{
    [CreateAssetMenu(menuName = "Dungeon Crawler/Player Special")]
    public sealed class PlayerSpecialDefinition : ScriptableObject
    {
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float staminaCost = 30f;
        [Min(0f)] public float regenerationPerSecond = 20f;
        [Min(0f)] public float regenerationDelay = .8f;
        [Min(0f)] public float cooldown = 6f;
        [SerializeField] private PlayerComboStep motion = new PlayerComboStep(
            "Special", .9f, .36f, .9f, .9f, .15f, 2f, 3f, 360f, .4f);
        public PlayerComboStep Motion => motion;
    }
}
