# Task 01 — Somente imagens e importação no Unity

Status: preparada; não executada. Esta task autoriza exclusivamente gerar imagens/texturas e importá-las no projeto Unity.

## Prompt para copiar

Trabalhe no projeto `G:/DungeonCrawler/My project`. Leia `AGENTS.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, `MVP.md` e `Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md`. Use os documentos como referência; os trechos de prompts antigos não ampliam o escopo desta task.

Sua única entrega é **gerar as imagens de arte pendentes e colocá-las no Unity como assets de imagem/textura**, com importação correta e procedência registrada. Não implemente funcionalidades nem altere código.

### Imagens a produzir

1. `SkeletonMelee_Concept_01.png`: frente, lado, costas e vista isométrica a 45°. Espada enferrujada, osso #CFC6A8, ferrugem #8A4A22, olhos emissivos vermelhos #FF2A2A. Inclua uma folha separada do elite mantendo a mesma família visual, ombreiras maiores e olhos laranja.
2. `SkeletonRanged_Concept_01.png`: proposta coerente com o modelo atual de capuz/cajado. Identifique no relatório como proposta visual, pois ainda não tem ficha oficial no documento de arte.
3. `Warden_Concept_01.png` e `Warden_Phase2_Concept_01.png`: propostas coerentes com elmo de chifres e marreta atuais, fases distinguíveis pela aparência e mesma silhueta. Apenas imagens, sem ligar a transição no jogo.
4. `DungeonRoom_Concept_01.png`: pedra azul-acinzentada #55596B, paredes #3E4152, musgo #4C5A44, tochas #FF9A2E e runas #4FD8FF. Interior 16×16 m, vão de porta 4 m e centro livre aproximado de 10×10 m. Entregue também a folha visual das peças modulares de 2 m.
5. Texturas pintadas do Hero e SkeletonMelee, alvo 512×512, quando houver UV/layout correspondente fornecido pelo asset. Use as imagens atuais do herói como referência de identidade. Sem layout compatível, entregue estudos de superfície identificados como referência; não apresente uma ilustração genérica como atlas pronto para aplicar ao modelo.

Preserve o estilo low-poly 3D estilizado, fantasia medieval sombria, cabeça grande, poucos planos legíveis e cores de acento. Anexe/use `Hero_Concept_01.png` e `Hero_Concept_01_Front.png` existentes como referências. Não substitua essas imagens sem necessidade. Gere os arquivos com ferramenta de imagem real; não simule entrega com arquivos vazios ou renomeados.

### Arquivos permitidos

- `Assets/_Project/Art/Concepts/`: novas imagens, `PROVENANCE.md` e seus `.meta`.
- `Assets/_Project/Art/Textures/Generated/`: novas texturas e seus `.meta`.
- Metadados de importação somente dessas novas imagens/texturas.

Faça a importação pelo Unity. Conceitos: textura comum para consulta no Project. BaseColor: sRGB; emissão: importar conforme o uso previsto e documentado. Configure tamanho/compressão adequados ao arquivo, sem converter a arte do jogo inteiro em sprites. Verifique que os arquivos abrem e aparecem corretamente no Inspector.

### Limites obrigatórios

- Não editar nenhum C#, JS do gerador, Animator Controller, input, Package ou ProjectSettings.
- Não alterar cenas, prefabs, materiais existentes, colliders, NavMesh, combate, combos, dano, cooldown, loot ou progressão.
- Não gerar animações de combo nem implementar a fase 2 do Boss nesta task.
- Não criar ou trocar modelos FBX/GLB. Imagens de referência não são modelos 3D; modelagem/aplicação dos visuais no cenário jogável tem task própria.
- Não instalar dependências como parte desta task.

### Aceite e entrega

- Imagens geradas, coerentes entre si e importadas no Unity sem erros de importação.
- Arquivos existentes preservados; mudanças limitadas às pastas permitidas e respectivos `.meta`.
- `PROVENANCE.md` com ferramenta/modelo usado, prompt, data, referências e condições de uso conhecidas; não inventar licença.
- Relatório com caminhos completos, imagens produzidas, o que é conceito versus textura aplicável e evidência do Inspector. Sem acesso ao Editor, registrar `NÃO VALIDADO NO EDITOR`.

Não executar nenhuma outra task ao terminar esta entrega.
