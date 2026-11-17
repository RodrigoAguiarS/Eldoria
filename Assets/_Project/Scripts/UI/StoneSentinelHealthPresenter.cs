using System.Collections.Generic;
using DungeonCrawler.Boss;
using DungeonCrawler.Core;
using DungeonCrawler.Dungeon;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>HUD isolado; observa vida/sala, incluindo reconciliação após desativação.</summary>
    public sealed class StoneSentinelHealthPresenter : MonoBehaviour
    {
        [SerializeField] private RoomController room;
        [SerializeField] private Health player;
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform fill;
        private Health boss;
        private StoneSentinelBrain brain;
        private void Awake() => panel.SetActive(false);
        private void OnEnable()
        {
            room.EnemiesSpawned += Bind; player.Died += Refresh;
            Bind(room.SpawnedEnemies);
        }
        private void OnDisable()
        {
            room.EnemiesSpawned -= Bind; player.Died -= Refresh;
            if (boss != null) boss.Changed -= OnChanged;
            panel.SetActive(false);
        }
        private void Bind(IReadOnlyList<Health> spawned)
        {
            if (boss != null) boss.Changed -= OnChanged;
            boss = spawned.Count > 0 ? spawned[0] : null;
            brain = boss != null ? boss.GetComponent<StoneSentinelBrain>() : null;
            if (boss != null) boss.Changed += OnChanged;
            Refresh();
        }
        private void OnChanged(float current, float maximum) => Refresh();
        private void Update() => Refresh(); // referências locais: detecta toggle do ator sem buscar na cena.
        private void Refresh()
        {
            bool visible = room.isActiveAndEnabled && room.State == RoomState.Active && player.IsAlive && player.isActiveAndEnabled &&
                boss != null && boss.IsAlive && boss.isActiveAndEnabled && brain != null && brain.isActiveAndEnabled;
            if (panel.activeSelf != visible) panel.SetActive(visible);
            if (visible) fill.anchorMax = new Vector2(boss.Normalized, 1f);
        }
    }
}
