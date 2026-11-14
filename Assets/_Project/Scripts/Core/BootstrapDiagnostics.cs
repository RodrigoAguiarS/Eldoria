using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Core
{
    /// <summary>
    /// Smoke-test do M0: confirma em PlayMode que a cena Bootstrap inicializa e que o
    /// asset de Input Actions serializado resolve o mapa Gameplay. Sem lógica de jogo.
    /// </summary>
    public sealed class BootstrapDiagnostics : MonoBehaviour
    {
        public const string GameplayMapName = "Gameplay";

        [SerializeField] private InputActionAsset inputActions;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("[Bootstrap] InputActionAsset não atribuído.", this);
                return;
            }

            InputActionMap map = inputActions.FindActionMap(GameplayMapName, false);
            if (map == null)
            {
                Debug.LogError($"[Bootstrap] Mapa '{GameplayMapName}' não encontrado em {inputActions.name}.", this);
                return;
            }

            Debug.Log($"[Bootstrap] OK. Mapa '{map.name}' com {map.actions.Count} ações.", this);
        }
    }
}
