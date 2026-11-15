# Direção de Arte e Prompts — Dungeon Crawler

Direção visual preservada. Conceitos, cinco personagens Generic e ambiente 3D já existem; procedência e limites em [PROVENANCE](PROVENANCE.md). Materiais atuais usam cores por parte/UV projetada; estudos de superfície não são atlas prontos. As sugestões antigas de atlas/Humanoid não autorizam migração: o produto D-002 preserva rig Generic, malhas e materiais. A parte conflitante permanece bloqueada.

As skills estão instaladas em `.claude/skills` (awesome-gamedev-agent-skills); carregar apenas a necessária. Usar [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md) para novas entregas e o manifesto por ID em `../ProductionSpecs/ASSET_MANIFEST.json` antes de encomendar arte. Os prompts abaixo são referências históricas de identidade, não uma ordem de regenerar assets existentes.

## 1. Quadro técnico (fixo)
- Unity 6000.6.9f1, URP 17.6, 3D, câmera isométrica fixa a 45° (offset 0,10,-10), jogo para Windows.
- Escala existente: personagens ~2 m de altura (cápsula raio 0,5); paredes das salas 2 m; vão de porta 4 m;
  sala 16×16 m; elite 1,4×.
- Regra de legibilidade: herói, inimigos e perigos devem se separar do cenário por **valor e cor**, não por detalhe.
  O cenário fica em azul/cinza dessaturado; personagens e perigos carregam as cores saturadas.

## 2. Linguagem visual
- **Estilo:** low-poly 3D estilizado, fantasia medieval sombria, cores vivas pontuais, luz cinematográfica.
- **Forma:** herói = formas redondas e capa em "V" (legível, amigável); inimigos = formas angulares e
  espinhosas (ameaça); cenário = blocos grandes e pesados com chanfros.
- **Proporção:** cabeça ~1/3 da altura (caricata), mãos e armas ligeiramente superdimensionadas.
- **Silhueta:** testar em preto sólido a 128 px de altura; herói e esqueleto precisam ser distinguíveis só pela forma.
- **Detalhe:** poucos planos grandes, textura pintada à mão (gradiente + ruído leve), contorno sutil por valor, sem PBR realista.
- **Luz:** key light fria da lua (azul-acinzentada) + tochas quentes (laranja) como luzes de preenchimento; sombras roxo-azuladas.
- Referências de clima (Hades, Diablo, Torchlight): usar apenas o clima geral; não copiar personagens, logos ou cenários.

## 3. Paleta (hex)
| Papel | Cor | Hex |
|---|---|---|
| Capa/herói (acento principal) | azul real | #2F6BFF |
| Magia azul / runas | ciano brilhante | #4FD8FF |
| Couro do herói | marrom médio | #7A4B2A |
| Metal do herói | aço claro | #B9C4CF |
| Detalhe dourado | latão | #E0B040 |
| Pele do herói | bege quente | #E3B58F |
| Osso do esqueleto | marfim sujo | #CFC6A8 |
| Ferrugem | laranja queimado | #8A4A22 |
| Olhos/corrupção | vermelho emissivo | #FF2A2A |
| Pedra do chão | cinza-azulado | #55596B |
| Pedra das paredes | cinza escuro | #3E4152 |
| Musgo/desgaste | verde apagado | #4C5A44 |
| Tocha/fogo | laranja | #FF9A2E |
| Luz da lua (key) | azul claro | #9FB8FF |
| Sombra | roxo escuro | #1E1830 |

Compatibilidade com o jogo atual: herói já é azul (`Mat_Player` #3380F2) e inimigo vermelho (`Mat_Enemy_*`);
a paleta mantém essa leitura (azul = jogador, vermelho = ameaça).

## 4. Assets

### Asset 01 — Herói (`Hero_Concept_01.png`)
**Prompt:** `Full-body character concept of a stylized low-poly 3D fantasy adventurer warrior, short sword, dark leather armor with steel pauldrons and bracers, flowing royal-blue cape (#2F6BFF), slightly oversized head, chunky hands and boots, confident heroic stance, shown in 3/4 view from a 45-degree isometric camera, clean readable silhouette, hand-painted gradient textures, matte materials, soft rim light, dark neutral background, orthographic character turnaround sheet (front, side, back), game art, consistent proportions`
**Negative:** `photorealistic, anime, text, logo, extra limbs, copyrighted characters, busy background, thin details, gore`
- **Materiais URP (Lit):** pele #E3B58F (smoothness 0,2), couro #7A4B2A (0,25), metal #B9C4CF (metallic 0,6, smoothness 0,55), capa #2F6BFF (0,15), detalhes #E0B040 (metallic 0,7).
- **Conversão para Unity:** modelar em DCC (Blender) em ~3–5 mil triângulos, 1 atlas 512×512 pintado, origem nos pés,
  escala 2 m; exportar FBX; preservar o rig Generic atual (Idle, Run, Attack, Dash); capa como malha separada com ossos (ou cloth leve em fase posterior).
  Importar em `Art/Characters/Hero`, criar prefab variante do `Player` trocando só o visual (`Body`); manter CharacterController,
  scripts e `DamageFlash` (usa `_BaseColor` do renderer) e manter `AttackVisual` somente como diagnóstico, desligado por padrão.
- **Tipo do resultado:** conceito 2D (imagem). **Não é sprite nem modelo 3D.**

### Asset 02 — Inimigo Melee (`SkeletonMelee_Concept_01.png`)
**Prompt:** `Full-body character concept of a corrupted skeleton warrior, stylized low-poly 3D dark fantasy, rusty jagged sword, damaged cracked bone-and-iron armor, glowing red eyes (#FF2A2A), hunched aggressive stance, exaggerated angular shapes and oversized skull, same art style and proportions family as a heroic blue-caped adventurer, 3/4 view from a 45-degree isometric camera, strong readable silhouette, hand-painted gradient textures, dark neutral background, turnaround sheet (front, side, back), game art`
**Negative:** `photorealistic, text, logo, realistic gore, thin details, copyrighted characters, cute style`
- **Materiais URP:** osso #CFC6A8 (smoothness 0,2), armadura corroída #3E4152 com ferrugem #8A4A22 (metallic 0,4), lâmina enferrujada #8A4A22 (metallic 0,5, smoothness 0,3),
  olhos #FF2A2A com **emissão** (HDR ~3).
- **Conversão:** ~3–4 mil triângulos, atlas 512, origem nos pés, 2 m de altura (colisão do prefab atual: cápsula raio 0,5/altura 2).
  Rig Generic atual: Idle, Chase (corrida), Attack (windup ~0,4 s alinhado a `EnemyData.attackWindup`) e Death. Trocar apenas o filho `Body` do
  prefab `Enemy_Melee`; o tint de windup do `DamageFlash` continua funcionando. Elite: mesmo modelo escalado 1,4× com ombreiras extras e olhos laranja.
- **Tipo:** conceito 2D. **Não é sprite nem modelo 3D.**

### Asset 03 — Sala da Masmorra (`DungeonRoom_Concept_01.png`)
**Prompt:** `Isometric 45-degree view concept of a stylized low-poly medieval dungeon combat arena, worn cracked grey-blue stone floor with moss patches, heavy dark stone walls with a 4-meter doorway gap, four chunky stone pillars, wall torches with warm orange fire (#FF9A2E) casting pools of light, subtle glowing blue magic runes (#4FD8FF) carved into the floor edges, large empty open center for combat, moody cinematic lighting with cool moonlight, hand-painted gradient textures, readable gameplay space, camera-friendly low walls, game environment concept art`
**Negative:** `photorealistic, text, clutter in the center, props blocking the floor, extremely dark, fog hiding the floor, people, copyrighted environments`
- **Materiais URP:** piso #55596B, paredes #3E4152, musgo #4C5A44, runas #4FD8FF (emissão HDR ~2), tochas #FF9A2E (emissão) + Light pontual cor #FF9A2E.
- **Conversão:** respeitar o gabarito de `Room_Normal` (piso 18×18, interior 16×16, paredes 1 m de espessura e 2 m de altura, vão central de 4 m),
  centro livre de ~10×10 m. Kit modular em módulos de 2 m: piso, parede reta, parede com vão, canto, pilar 2×3×2, suporte de tocha, runas (decal ou quad emissivo).
  Tochas: no máximo 1–2 luzes realtime por sala, o restante com emissão + luz baked/Light Probes; luz da lua = Directional existente.
  Substituir só os visuais; manter layers (`Ground`, `Obstacle`, `Door`), colliders e o NavMesh. Pós-processamento (Volume URP): Bloom leve, Vignette, Color Adjustments.
- **Tipo:** conceito 2D. **Não é sprite nem modelo 3D.**

## 5. Uso atual e limites

Preservar os conceitos/modelos existentes; anexar referências reais ao agente artístico. Consultar PROVENANCE antes de atribuir origem/licença. Validar silhueta a128px e sobre piso real, fonte/malha/escala/pivô/alpha conforme o tipo. Orçamentos de triângulos, atlas e valores de material desta direção são metas históricas, não novas medições nem obrigação de refazer arte aceita. Novas produções são selecionadas pelo catálogo; modelagem/rig/integração têm aceites separados.
