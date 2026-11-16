using DungeonCrawler.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    public sealed class InventoryStatusPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Text label;
        [SerializeField] private RuntimeInputBindings bindings;
        private float hideAt;
        private bool showing;
        private void OnEnable() { inventory.CollectionDenied += OnDenied; inventory.ItemDropped += OnDropped; label.enabled = false; }
        private void OnDisable() { if (inventory != null) { inventory.CollectionDenied -= OnDenied; inventory.ItemDropped -= OnDropped; } }
        private void OnDropped(Pickup _) => Show("Item deixado no chão à sua frente.");
        private void OnDenied(InventoryCollectionFailure reason)
        {
            string controls = bindings != null ? bindings.Bindings.Display("Inventory") : "a bolsa";
            string message = reason == InventoryCollectionFailure.Full ? $"Inventário cheio. Abra {controls} para usar ou largar itens." :
                reason == InventoryCollectionFailure.PlayerDead ? "Não é possível coletar após a morte." : "Este item não pode ser coletado.";
            Show(message);
        }
        private void Show(string message)
        {
            label.text = message;
            label.enabled = true; showing = true; hideAt = Time.unscaledTime + 3f;
        }
        private void Update()
        {
            if (!showing || Time.unscaledTime < hideAt) return;
            showing = false; label.enabled = false;
        }
    }
}
