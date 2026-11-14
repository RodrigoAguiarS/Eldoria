# AGENTS.md — Dungeon Crawler

## Missão
Construir incrementalmente um Action-RPG isométrico 3D para Windows usando Unity 6 URP. Priorize uma cena jogável, testável e reproduzível antes de expandir funcionalidades.

## Fonte de verdade e precedência
1. Este AGENTS.md: regras de execução e qualidade.
2. GAME_DESIGN.md: comportamento do jogo.
3. ARCHITECTURE.md: decisões técnicas.
4. MVP.md: escopo e critérios de aceite por marco.
Em conflito, interrompa a alteração afetada e registre a divergência; não invente requisitos.

## Colaboração Codex + Claude
- Codex: responsável primário por implementação, integração, testes e scripts de Editor.
- Claude: design, balanceamento, revisão técnica e direção visual; só altera código após tarefa delegada e com escopo de arquivos explícito.
- Não editem simultaneamente o mesmo arquivo, cena, prefab ou ProjectSettings. Use branches e commits separados; a integração ocorre após revisão.
- Antes de editar: leia os quatro documentos, `git status`, versão do Editor (`ProjectSettings/ProjectVersion.txt`), manifesto de pacotes e arquivos envolvidos.
- Não sobrescreva mudanças não commitadas de outro agente. Se houver alterações desconhecidas, pare e relate.

## Regras obrigatórias
- C# com namespaces `DungeonCrawler.*`, classes de responsabilidade única e nomes descritivos.
- New Input System exclusivamente; nunca `Input.GetAxis` ou API legada.
- Player usa `CharacterController.Move`, nunca Rigidbody.
- IA com `NavMeshAgent` e estados Idle/Chase/Attack/Dead; decisões em intervalos controlados, não buscas pesadas em Update.
- Não use `FindObjectOfType`, `FindAnyObjectByType` ou buscas globais como mecanismo de injeção de dependência. Prefira referências serializadas, composição e eventos.
- Eventos C# para notificações de domínio; inscreva/desinscreva corretamente no ciclo de vida.
- Não instancie/destroia projéteis a cada disparo: use pool.
- Dados de itens, inimigos e habilidades em ScriptableObjects; comportamento desacoplado de dados.
- UI não deve controlar regras de combate ou progressão.
- Prefabs/cenas devem ter referências resolvidas e não apresentar Missing Script.
- Não crie arte final, sistemas de inventário ou geração procedural no primeiro marco.
- Prefira primitivas 3D e materiais temporários para validar jogabilidade.

## Fluxo por tarefa
1. Inspecionar projeto e estado Git.
2. Descrever plano, arquivos afetados, dependências e critério de aceite.
3. Implementar apenas o menor incremento funcional.
4. Adicionar/atualizar testes EditMode e PlayMode pertinentes.
5. Validar compilação e testes quando Unity estiver acessível; se não estiver, registrar explicitamente `NÃO VALIDADO NO EDITOR`.
6. Entregar resumo: arquivos alterados, comportamento, testes executados, limitações e próximo passo.

## Automação Unity
- Para construir cenas/prefabs, crie scripts idempotentes em `Assets/_Project/Scripts/Editor` com menu `Tools/Dungeon Crawler/...`.
- Não edite arquivos `.unity`/`.prefab` textuais às cegas quando scripts de Editor forem mais seguros.
- Nunca declare teste visual concluído sem abrir a cena e executar PlayMode.
- Pacotes: identifique versões compatíveis com a instalação real; não assuma namespaces/APIs de Cinemachine entre versões.

## Definição de pronto
Compila sem erros, cena abre sem referências faltantes, controles e combate funcionam em PlayMode, critérios de aceite do MVP atendidos e testes/limitações documentados.
