using System;

namespace DungeonCrawler.Dungeon
{
    /// <summary>Contagem pura de inimigos vivos de uma sala; dispara Cleared quando o último morre.</summary>
    public sealed class EnemyTracker
    {
        public int Alive { get; private set; }
        public int Total { get; private set; }

        public event Action Cleared;

        public void Register()
        {
            Alive++;
            Total++;
        }

        public void NotifyDied()
        {
            if (Alive <= 0) return;
            Alive--;
            if (Alive == 0) Cleared?.Invoke();
        }

        public void Reset()
        {
            Alive = 0;
            Total = 0;
        }
    }
}
