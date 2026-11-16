using DungeonCrawler.Core;
using UnityEngine;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Prebuilt intact/rubble visuals; no debris physics, instantiation or loot.</summary>
    public sealed class DestructiblePropPresenter : MonoBehaviour
    {
        [SerializeField] private DestructibleProp prop;
        [SerializeField] private GameObject intact;
        [SerializeField] private GameObject rubble;
        [SerializeField] private Collider[] hitColliders;
        [SerializeField] private DamageFlash flash;

        private void OnEnable()
        {
            if (prop == null) return;
            prop.Damaged += OnDamaged;
            prop.Broken += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (prop == null) return;
            prop.Damaged -= OnDamaged;
            prop.Broken -= Refresh;
        }
        private void OnDamaged(float amount)
        {
            if (prop.IsAlive && flash != null) flash.Flash(new Color(1f, .65f, .3f), .12f);
        }
        private void Refresh()
        {
            bool alive = prop.IsAlive;
            if (intact != null) intact.SetActive(alive);
            if (rubble != null) rubble.SetActive(!alive);
            foreach (var hit in hitColliders) if (hit != null) hit.enabled = alive;
        }
    }
}
