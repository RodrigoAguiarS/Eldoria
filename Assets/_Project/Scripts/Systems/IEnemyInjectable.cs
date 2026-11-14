using DungeonCrawler.Core;

namespace DungeonCrawler.Systems
{
    /// <summary>Contrato dos inimigos instanciados por spawners: recebem alvo e pool de projéteis por injeção.</summary>
    public interface IEnemyInjectable
    {
        void Inject(Health target, ProjectilePool pool);
    }
}
