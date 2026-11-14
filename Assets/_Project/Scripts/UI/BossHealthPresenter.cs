using System.Collections.Generic;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Barra de vida do Boss, visível apenas durante a luta: aparece quando a sala do Boss instancia seus inimigos
    /// e some quando o Boss morre. Liga-se ao primeiro Health da sala por evento (sem busca global). Só lê estado.
    /// </summary>
    public sealed class BossHealthPresenter : MonoBehaviour
    {
        [SerializeField] private RoomController bossRoom;
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform fill;

        private Health boss;

        private void Awake() => panel.SetActive(false);

        private void OnEnable() => bossRoom.EnemiesSpawned += Bind;

        private void OnDisable()
        {
            bossRoom.EnemiesSpawned -= Bind;
            Unbind();
        }

        private void Bind(IReadOnlyList<Health> spawned)
        {
            Unbind();
            if (spawned == null || spawned.Count == 0) return;

            boss = spawned[0];
            boss.Changed += OnChanged;
            boss.Died += OnDied;
            panel.SetActive(true);
            OnChanged(boss.Current, boss.Max);
        }

        private void Unbind()
        {
            if (boss == null) return;
            boss.Changed -= OnChanged;
            boss.Died -= OnDied;
            boss = null;
        }

        private void OnChanged(float current, float max)
        {
            fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        private void OnDied()
        {
            panel.SetActive(false);
            Unbind();
        }
    }
}
