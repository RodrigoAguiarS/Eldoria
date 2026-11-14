# Task 02 — Implementação do combo do jogador

Status: preparada; não executada. Independente da geração das imagens da Task 01. Esta task cuida de gameplay, clips de animação e integração do ataque.

## Prompt para copiar

Trabalhe no projeto `G:/DungeonCrawler/My project`. Leia `AGENTS.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, `MVP.md`, `MVP_AUDIT.md` e `Tools/Lemo3DGenerator/README.md`. Confira o estado dos arquivos antes de editar e preserve trabalho desconhecido. O projeto atualmente não tem repositório Git; não alegue uma branch/commit inexistente.

Implemente um ataque mais fluido com até três golpes distintos, mantendo o herói, materiais e cenário atuais. A tarefa de imagens é separada e não é dependência desta implementação.

### Comportamento proposto

- Attack_01: corte diagonal rápido.
- Attack_02: contracorte horizontal, movimento distinto do primeiro.
- Attack_03: finalizador vertical; HeavyAttack existente pode servir de base.
- Um clique executa um golpe; dois cliques executam o combo curto; três adicionam o finalizador. Sem outro comando, completar o golpe atual e retornar à locomoção.
- Preservar o autoataque ao segurar o botão, avançando pelos três golpes e respeitando a recuperação final.
- No máximo um comando antecipado para o próximo golpe; buffer inicial ajustável de 0,15 s. Sem fila ilimitada de cliques nem reinício acidental da animação.
- Configurar por ScriptableObject os tempos, multiplicador de dano, alcance, cone, encadeamento e recuperação de cada golpe. Valores iniciais são ajustes desta task, não parâmetros canônicos do MVP.
- Preservar cálculo de progressão e bônus de arma.

### Integração e desempenho

- Criar/ajustar clips reais no rig Generic atual: in-place, root motion desligado, espada no soquete. Preservar demais clips do Hero; não migrar o rig nem regenerar os inimigos.
- Combate é a autoridade do índice e do instante de dano; ponte do Animator recebe eventos C#. Substituir a visibilidade de AttackVisual como sinal para iniciar animação.
- Sincronizar impacto visual com o dano e impedir duplicação entre timer e AnimationEvent. Lógica deve funcionar sem Animator.
- Definir cancelamento por morte, desativação e dash; nenhum dano atrasado após cancelar.
- Manter CharacterController.Move, New Input System, namespaces DungeonCrawler.*, referências explícitas e eventos com ciclo de vida correto.
- Preservar consultas sem alocação recorrente; deduplicar alvos com vários colliders. Testar saturação do buffer de 16 colliders e tratar o limite sem perda silenciosa de alvos.
- Efeitos de ataque, se necessários, simples e reutilizados com pool aquecido; nenhuma produção de novas imagens nesta task. Não alterar a arte dos personagens ou do ambiente.
- Medir CPU e GC Alloc antes/depois sob a mesma carga. Mais velocidade na animação não é evidência de ganho de desempenho.

### Escopo de arquivos

- `Assets/_Project/Scripts/Player/PlayerCombat.cs` e `PlayerInputReader.cs`.
- Novos `PlayerComboModel.cs` e `PlayerComboDefinition.cs` nessa pasta; dados em `Assets/_Project/ScriptableObjects/Combat/`.
- `Tools/Lemo3DGenerator/src/characters/hero.mjs` e `src/anim.mjs`, apenas clips do Hero; saídas correspondentes do Hero gerado.
- `GeneratedPlayerAnimationBridge.cs` em `Tools/Lemo3DGenerator/unity/GameBridge/` e cópia instalada correspondente.
- Builders já envolvidos em `Tools/Lemo3DGenerator/unity/Editor/` e cópias instaladas, apenas para configurar os estados/referências novos.
- Builders em `Assets/_Project/Scripts/Editor/` somente se necessário para atribuir a configuração do combo e manter a reconstrução idempotente.
- Novos testes pertinentes em `Assets/_Project/Tests/EditMode/` e `PlayMode/`.

Preservar `.meta`/GUIDs. Atualizar prefabs/cenas pelas APIs do Editor, nunca editando YAML às cegas. Não alterar Packages, ProjectSettings, imagens, loot, Boss, áudio ou acessibilidade. Não editar arquivos da Task 01.

### Aceite e entrega

Testar sequências de um/dois/três golpes, buffer, segurar botão, reset, vários colliders no mesmo inimigo, multidão, cancelamento e multiplicadores de progressão. Executar testes EditMode/PlayMode. Abrir `Floor_01_GeneratedCharacters` e conferir os três movimentos e impactos em PlayMode; gerar/testar build Windows. Registrar resultado do Profiler após aquecimento, arquivos alterados e limitações. Se algo não for executado no Editor, marcar explicitamente a validação pendente.

Não executar as demais pendências do MVP nesta task.
