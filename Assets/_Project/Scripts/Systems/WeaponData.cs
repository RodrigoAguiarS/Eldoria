using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>Arma coletável: bônus plano somado ao dano base antes da progressão.</summary>
    [CreateAssetMenu(menuName = "Dungeon Crawler/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        public string displayName = "Arma";
        [Min(0f)] public float damageBonus = 5f;
    }
}
