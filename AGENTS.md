# AGENTS.md — Eldoria

## Missão
O jogo se chama **Eldoria** (D-003). `DungeonCrawler` continua como identificador técnico de namespaces, assemblies, menus `Tools/Dungeon Crawler`, chaves e variáveis de ambiente; não renomear esses identificadores sem tarefa própria.

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
- **Branch e merge request obrigatórios:** toda tarefa pedida começa numa branch nova criada a partir da `main` atualizada e termina com merge request para a `main`. Nunca commitar diretamente na `main` nem fazer o merge sem aprovação do usuário. Detalhes em [Fluxo por tarefa](#fluxo-por-tarefa).
- Antes de editar: consulte o mapa de leitura abaixo, `git status`, versão do Editor (`ProjectSettings/ProjectVersion.txt`), manifesto/lock de pacotes e arquivos envolvidos. Na primeira tarefa ou ao mudar o escopo, leia os quatro documentos fundamentais; durante a mesma tarefa, releia somente seções afetadas ou alteradas.
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
1. Inspecionar projeto e estado Git. Com árvore limpa, atualizar a `main` (`git fetch` + `git switch main` + `git pull --ff-only` quando houver remoto) e criar a branch da tarefa a partir dela: `git switch -c <agente>/<id-curto> main` (ex.: `codex/f05-feedback`, `claude/f02-inventory-sort`). Alterações locais desconhecidas: parar e relatar, sem trocar de branch.
2. Descrever plano, arquivos afetados, dependências e critério de aceite.
3. Implementar apenas o menor incremento funcional.
4. Adicionar/atualizar testes EditMode e PlayMode pertinentes.
5. Validar compilação e testes quando Unity estiver acessível; se não estiver, registrar explicitamente `NÃO VALIDADO NO EDITOR`.
6. Commitar somente na branch da tarefa, enviar ao remoto (`git push -u origin <branch>`) e abrir merge request para a `main` com objetivo, arquivos, testes executados, limitações e passos de validação. Não fazer merge sem aprovação do usuário. Se não houver remoto ou CLI configurados, informar o usuário e deixar a branch commitada, pronta para revisão.
7. Entregar resumo: branch, link do merge request (ou motivo de não haver), arquivos alterados, comportamento, testes executados, limitações e próximo passo.

## Mapa de leitura e continuidade
- `AGENTS.md`: política compartilhada; ler no início da tarefa. Regras acima prevalecem sobre prompts e skills.
- `GAME_DESIGN.md`: regras e decisões de produto; consultar o sistema afetado. `docs/PRODUCT-1.0-PROPOSAL.md` detalha D-002; não é uma proposta ainda aguardando aprovação.
- `ARCHITECTURE.md`: contratos técnicos e composição; consultar os módulos afetados.
- `MVP.md`: escopo, critérios de aceite e estado resumido; não usar checklists antigos como backlog.
- `NEXT_STEPS.md`: única fila de próximas entregas e dependências. `PROJECT_AUDIT.md`: diagnóstico/evidências/limites; ler ao auditar ou retomar uma pendência citada.
- `PROMPT_CATALOG.md`: selecionar somente um ID F01–F15 ou A01–A15. Sugestões não autorizam execução em lote nem alteram o produto aprovado.
- Arte: começar pela direção em `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`, procedência e pelo ID em `Art/ProductionSpecs/ASSET_MANIFEST.json`. Abrir somente a ficha especializada necessária; status das fichas históricas não prevalece sobre lotes já integrados. Não carregar os sete documentos inteiros em toda tarefa.
- Distribuição/testes: `docs/BUILD-WINDOWS.md`. Aceite da release antiga: `docs/RELEASE-1.0-VALIDATION.md`. Valores medidos: `docs/BALANCE-1.0.md` e relatório específico em `Reports`; não ler todo o histórico por padrão.
- Skills locais `awesome-gamedev-agent-skills`: `.claude/skills/<nome>/SKILL.md` (origem/licença em `SOURCE.md`/NOTICE). Selecionar Unity/C#, RPG, UI, arte ou performance conforme a tarefa; não ler bibliotecas de outras engines ou cópias aninhadas. Baseline das skills 6000.3; instalação real prevalece. Ausência de busca textual não prova desuso de asset/pacote.
- Codex conduz arquitetura, implementação, auditoria e integração. Agente artístico recebe ID, referências reais, formatos, diretório exclusivo e limites; entrega fontes/procedência, sem editar domínio. Unity valida importação, funcionamento e apresentação reais. Claude mantém o papel de revisão definido acima.
- Handoff curto: commit/base e branch (criada da `main`), merge request, objetivo, arquivos reservados, decisões, evidência executada, pendência e próximo comando. Não copiar documentos completos no prompt. Fontes em `Tools/*/unity` e cópias instaladas correspondentes devem permanecer sincronizadas; verificar com `Tools/Audit/audit_project.py`.
- Auditoria e testes produzem relatórios novos em `Reports/ProjectAudit` (ou pasta exclusiva da tarefa); nunca substituir evidências históricas. Definir `DUNGEON_CRAWLER_TEST_REPORT_DIR` para isolar benchmarks. Conferir hashes/diff após testes que executam builders; preservar dados e GUIDs existentes.

## Automação Unity
- Para construir cenas/prefabs, crie scripts idempotentes em `Assets/_Project/Scripts/Editor` com menu `Tools/Dungeon Crawler/...`.
- Não edite arquivos `.unity`/`.prefab` textuais às cegas quando scripts de Editor forem mais seguros.
- Nunca declare teste visual concluído sem abrir a cena e executar PlayMode.
- Pacotes: identifique versões compatíveis com a instalação real; não assuma namespaces/APIs de Cinemachine entre versões.

## Definição de pronto
Compila sem erros, cena abre sem referências faltantes, controles e combate funcionam em PlayMode, critérios de aceite do MVP atendidos e testes/limitações documentados.
