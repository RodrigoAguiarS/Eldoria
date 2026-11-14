// Campos preenchidos pelo Inspector/builder.
#pragma warning disable 0649
using UnityEngine;

namespace DungeonCrawler.CharacterArt
{
    /// <summary>
    /// Referências do modelo gerado: Animator, renderers, soquetes e equipamentos.
    /// Não contém regra de jogo; serve para outros componentes encontrarem arma/soquete sem buscas globais.
    /// </summary>
    public sealed class GeneratedCharacterRig : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private Transform rightHandSocket;
        [SerializeField] private Transform leftHandSocket;
        [SerializeField] private Transform[] equipment;

        public Animator Animator => animator;
        public SkinnedMeshRenderer BodyRenderer => bodyRenderer;
        public Transform RightHandSocket => rightHandSocket;
        public Transform LeftHandSocket => leftHandSocket;
        public Transform[] Equipment => equipment;
    }
}
