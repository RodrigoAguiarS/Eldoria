namespace DungeonCrawler.Core
{
    /// <summary>Callbacks de ativação/desativação para objetos de pool.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
