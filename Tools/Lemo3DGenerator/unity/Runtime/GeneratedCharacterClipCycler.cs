using UnityEngine;

namespace DungeonCrawler.CharacterArt
{
    /// <summary>Somente para a cena de teste: toca cada estado do Animator em sequência.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class GeneratedCharacterClipCycler : MonoBehaviour
    {
        [SerializeField] private string[] states = new string[0];
        [SerializeField, Min(0.5f)] private float secondsPerState = 2.5f;

        private Animator animator;
        private int index = -1;
        private float timer;

        public string CurrentState => index >= 0 && index < states.Length ? states[index] : string.Empty;

        public void Configure(string[] stateNames, float seconds)
        {
            states = stateNames;
            secondsPerState = seconds;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (states.Length == 0)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            index = (index + 1) % states.Length;
            timer = secondsPerState;
            animator.Play(states[index], 0, 0f);
        }
    }
}
