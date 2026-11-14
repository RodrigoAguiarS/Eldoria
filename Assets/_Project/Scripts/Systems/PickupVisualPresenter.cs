using UnityEngine;

namespace DungeonCrawler.Systems
{
    /// <summary>
    /// Shows the catalog model for a pickup. Presentation only: the trigger, collection retry and lifetime stay in Pickup.
    /// The shared tinted sphere remains as a fallback when no model is mapped.
    /// </summary>
    public sealed class PickupVisualPresenter : MonoBehaviour
    {
        [SerializeField] private ItemVisualCatalog catalog;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Renderer fallbackVisual;

        private GameObject current;
        private GameObject currentSource;

        public GameObject CurrentModel => current;
        public GameObject CurrentSource => currentSource;
        public bool ShowsFallback => fallbackVisual != null && fallbackVisual.enabled;

        /// <summary>Returns true when a dedicated model is shown; false means the caller should tint the fallback.</summary>
        public bool Present(in LootDrop drop)
        {
            GameObject source = catalog != null ? catalog.GetGroundPrefab(drop) : null;
            if (source != currentSource && current != null)
            {
                Destroy(current);
                current = null;
            }
            currentSource = source;
            if (source != null && current == null)
                current = Instantiate(source, modelRoot != null ? modelRoot : transform, false);
            if (fallbackVisual != null) fallbackVisual.enabled = current == null;
            return current != null;
        }
    }
}
