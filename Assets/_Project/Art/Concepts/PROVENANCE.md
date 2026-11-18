# Procedência das imagens de arte

## Imagens anteriores (procedência NÃO registrada)
| Arquivo | Situação |
|---|---|
| `Hero_Concept_01.png`, `Hero_Concept_01_Front.png` (13/11/2026 02:23) | Ilustrações pintadas já presentes no projeto. Ferramenta, prompt e licença **não foram registrados** por quem as criou. Usadas apenas como referência de identidade (capa azul, couro, aço, espada curta); não foram alteradas. Completar esta linha com a origem real. |

## Task 01 — 13/11/2026 (Claude Code)

**Ferramenta:** renderização 3D em tempo real com **three.js r170** (o pacote instalado em `G:/lemo-opuscar/lemo-opuscar/node_modules/three`, licença MIT), executado via WebGL2 no navegador embutido do Claude Code (Chromium). Pós-processo: `EffectComposer` + `UnrealBloomPass` + `OutputPass`, tone mapping Neutral, sRGB.
**Não é geração por IA de imagem:** a sessão não dispunha de gerador de imagens. Os personagens são **renders dos modelos 3D low-poly do próprio projeto**; a sala, as peças modulares e os estudos de superfície foram **construídos/proceduralmente desenhados só para o render**. Nenhuma arte de terceiros foi usada.
**Condições de uso conhecidas:** imagens derivadas de assets do próprio projeto; three.js e Lemo-Opuscar são MIT (código). Nenhuma licença adicional foi presumida.
**Scripts de render:** página/servidor temporários na pasta de rascunho da sessão (não versionados; nenhum código do projeto foi editado). Parâmetros essenciais abaixo para reprodução.

### Conceitos (`Assets/_Project/Art/Concepts/`)
| Arquivo | Tamanho | Entrada | "Prompt" / parâmetros |
|---|---|---|---|
| `SkeletonMelee_Concept_01.png` | 1600×1200 | `Art/Models/Generated/SkeletonMelee/SkeletonMelee.glb` | Pose `Idle` em 0,4 s. Painel esquerdo 700 px: câmera ortográfica isométrica, elevação 45°, guinada −40°. Três painéis 300 px: frente (0°), lado direito (−90°) e costas (180°), ortográficas, enquadradas pela altura e pela largura visível. Luz: hemisférica #9FB8FF/#1E1830, key fria seguindo a câmera, contorno quente #FF9A2E, sombra projetada, bloom 0,45. Fundo em gradiente #2B2442→#121019. |
| `SkeletonElite_Concept_01.png` | 1600×1200 | `SkeletonElite.glb` (1,4×, ombreiras extras com 3 espinhos, olhos laranja #FF9A2E) | Mesmos parâmetros (folha separada do melee, mesma família visual). |
| `SkeletonRanged_Concept_01.png` | 1600×1200 | `SkeletonRanged.glb` | Mesmos parâmetros. **PROPOSTA VISUAL**: o inimigo ranged não tem ficha no `ART_DIRECTION_AND_PROMPTS.md` (capuz #4C5A44, cajado com orbe #FF2A2A). |
| `Warden_Concept_01.png` | 1600×1200 | `Warden.glb` (elmo com chifres, marreta) | Mesmos parâmetros. **PROPOSTA VISUAL** (boss sem ficha oficial). Fase 1. |
| `Warden_Phase2_Concept_01.png` | 1600×1200 | `Warden.glb` | **PROPOSTA VISUAL — fase 2.** Mesma malha e pose (mesma silhueta); só alterações de material no render: runas e olhos com emissão 7, ferrugem emissiva #FF2A2A 0,45, armadura escurecida para #2A1622 com emissão 0,08, chifres tingidos 25 % de #FF2A2A, luz pontual vermelha no peito, fundo #3A1A2A→#140C14. **Apenas imagem**; a transição não foi ligada ao jogo. |
| `DungeonRoom_Concept_01.png` | 1600×1200 | Sala montada só para o render | Interior 16×16 m (8×8 placas de 2 m, pedra #55596B com variação de ±6 % de luminosidade), paredes de 1 m × 2 m #3E4152 em módulos de 2 m, **vãos de porta de 4 m** ao norte e ao sul (x ∈ [−2, 2]), quatro pilares 2×3×2 m em (±6, ±6), **centro livre ≈ 10×10 m**, 8 tochas #FF9A2E (chama emissiva + luz pontual), runas #4FD8FF nas bordas do piso e nos cantos, musgo #4C5A44 em cantos, bases e topo das paredes. Luz da lua #9FB8FF direcional com sombra. Câmera perspectiva 38° em (0, 21, −21), olhando o centro (mesma orientação da câmera do jogo). |
| `DungeonRoom_ModularKit_01.png` | 1600×1000 | Mesmas peças da sala | Folha das peças modulares de 2 m: piso 2×2, parede reta, batente do vão (porta de 4 m = 2 módulos), canto, pilar 2×3×2, parede com tocha, piso com runa e decal de musgo. Ortográfica, elevação 35°, grade ciano de 2 m sob cada peça, rótulos em PT-BR. |

### Texturas (`Assets/_Project/Art/Textures/Generated/`)
Os modelos gerados usam **UV projetada por face (sobreposta)**, sem layout de atlas. Por isso **não há atlas aplicável**: os arquivos abaixo são **ESTUDOS DE SUPERFÍCIE (referência de pintura)**, procedurais (gradiente de luz do alto-esquerda, ruído fbm, pinceladas/placas/rachaduras, borda escurecida), em 512×512 sRGB.

| Arquivo | Amostras (esquerda→direita, cima→baixo) |
|---|---|
| `Hero_SurfaceStudy_01.png` | capa #2F6BFF · couro #7A4B2A · couro escuro #4E3020 (placas) · aço #B9C4CF · latão #E0B040 · pele #E3B58F · cabelo #4A2C1A · botas #3B2418 · tinta #1E1830 |
| `SkeletonMelee_SurfaceStudy_01.png` | osso #CFC6A8 · osso sombreado #8F8770 · armadura corroída #3E4152 · ferrugem #8A4A22 · órbitas #1E1830 · olhos #FF2A2A (só cor base; a emissão continua no material) |

Gerados por script Node (sem dependências) usando o codificador PNG de `Tools/LemoSpriteLab/src/png.mjs`, apenas importado, sem alteração.

### Importação no Unity 6000.6.9f1 (verificada no Inspector)
- **Conceitos (7):** Texture Type *Default*, sRGB, *Non-Power of 2 = None* (sem isso o Unity redimensionava 1600×1200 para 2048×1024 e distorcia a proporção), **mipmaps desligados** (imagem de consulta; com mipmaps o Unity recusa comprimir textura NPOT), Max Size 2048, Normal Quality → 1600×1200 BC1 sRGB (~0,9 MB cada).
- **Estudos (2):** Default, sRGB, mipmaps ligados, 512×512 BC1 sRGB (~171 KB). Sem mapa de emissão: a emissão dos olhos/runas continua nos materiais URP.
- Console sem erros nem avisos após a importação. `Hero_Concept_01*.png` não foram reimportados nem alterados.

## A13 — 17/11/2026 (Claude Code): `SkeletonLancer_Concept_01.png`

**Ferramenta:** a mesma técnica da Task 01: render 3D em tempo real com **three.js r170** (somente leitura no Lemo-Opuscar, MIT) no navegador embutido do Claude Code (Chromium/WebGL2), tone mapping Neutral, sRGB. O brilho dos olhos é um blur 2D só dos materiais emissivos, sem bloom.
**Não é geração por IA de imagem.** O lanceiro é montado com o kit de esqueleto do próprio projeto (`Tools/Lemo3DGenerator/src/characters/_skeleton_kit.mjs`) mais lança, ombreira assimétrica e tassetes modelados por código em `Tools/Lemo3DGenerator/src/production/ConceptLancer.mjs`, posado e renderizado por `Tools/Lemo3DGenerator/preview/concept_lancer.html`. Nenhuma arte de terceiros. Desta vez os scripts **estão versionados**.
**Conteúdo:** folha 2048×1536 sem rótulos e fundo cinza neutro #2B2C31: frente/lado/costas ortográficas (380 px/m), vista de jogo 45° (guinada −40°), pose de windup e silhuetas pretas. Paleta da ficha A13: osso #CFC6A8, ferro #3E4152, ferrugem #8A4A22, olhos #FF2A2A.
**Fonte e evidências:** `Source~/SkeletonLancer/` (README, GLB de render, source.json, medidas, quadros de silhueta a 128 px e de escala, `ConceptLancer.validation.json` 13/13).
**Importação:** `.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default, sRGB, NPOT None, sem mipmaps, máx. 2048), só com o GUID novo. **NÃO VALIDADO NO EDITOR** nesta sessão.
**Condições de uso:** projeto Eldoria; three.js MIT; Pillow (validador) MIT-CMU/HPND. É conceito 2D: não há FBX, rig nem clip. Aprovação artística pendente.

## A14 — 17/11/2026 (Claude Code): `StoneSentinel_Concept_01.png`

**Ferramenta:** a mesma técnica da Task 01/A13: render 3D em tempo real com **three.js r170** (somente leitura no Lemo-Opuscar, MIT) no navegador embutido do Claude Code (Chromium/WebGL2), tone mapping Neutral, sRGB. O brilho do núcleo/runas é um blur 2D só dos materiais emissivos, sem bloom. Luz da família levemente neutralizada (key #ECEEF6, hemisférica #AAB6D6) para a pedra não azular.
**Não é geração por IA de imagem.** A Sentinela é geometria própria (blocos chanfrados com irregularidade determinística) modelada por código em `Tools/Lemo3DGenerator/src/production/ConceptSentinel.mjs`, posada e renderizada por `Tools/Lemo3DGenerator/preview/concept_sentinel.html`; herói, Warden e esqueletos do próprio projeto aparecem só como escala. Nenhuma arte de terceiros. Scripts versionados.
**Conteúdo:** folha 2048×1536 sem rótulos e fundo cinza neutro #2B2C31: frente/lado/costas ortográficas (200 px/m) com o herói na vista lateral, vista de jogo 45° (guinada −40°), antecipação do leque frontal sobre piso com o setor do protótipo F15 (100°, 6 m) e silhuetas pretas. Paleta da ficha A14: pedra #55596B/#3E4152, musgo #4C5A44, runas/núcleo #4FD8FF, rachadura opcional #FF9A2E.
**Fonte e evidências:** `Source~/StoneSentinel/` (README, GLB de render, source.json, medidas, quadros de silhueta a 128 px e de escala, `ConceptSentinel.validation.json` 13/13).
**Importação:** `.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default, sRGB, NPOT None, sem mipmaps, máx. 2048), só com o GUID novo. **NÃO VALIDADO NO EDITOR** nesta sessão.
**Condições de uso:** projeto Eldoria; three.js MIT; Pillow (validador) MIT-CMU/HPND. É conceito 2D: não há FBX, rig nem take. F15 sem playtest humano; aprovação artística pendente.

## A15 — 17/11/2026 (Claude Code): `FloodedCrypt_Concept_01.png`

**Ferramenta:** a mesma técnica de A13/A14: render 3D em tempo real com **three.js r170** (somente leitura no Lemo-Opuscar, MIT) no navegador embutido do Claude Code (Chromium/WebGL2), tone mapping Neutral, sRGB, câmera ortográfica. O brilho de chamas e runas é um blur 2D só dos materiais emissivos, sem bloom. Luz: lua direcional #D8DFF2 com sombra, hemisférica #B4BACE/#2E2838 e 8 luzes pontuais #FF9A2E nas tochas (+4 ciano fracas nos nichos), mais forte e neutra que a da Task 01 para o piso sair perto de #55596B.
**Não é geração por IA de imagem.** A cripta é geometria própria (blocos chanfrados com irregularidade determinística, módulos de 2 m) modelada por código em `Tools/Lemo3DGenerator/src/production/ConceptCrypt.mjs` e renderizada/composta por `Tools/Lemo3DGenerator/preview/concept_crypt.html`; herói e esqueletos do próprio projeto aparecem só como escala. Nenhuma arte de terceiros. Scripts versionados.
**Conteúdo:** folha 2048×1536 opaca sem rótulos: em cima, a sala 16×16 na câmera de jogo (sul, elevação 45°, ortográfica) com canais rasos nas bordas, passarela seca porta-centro-porta, centro 10×10 livre, pilares baixos, nichos de runa, tochas e o encontro F14 de escala (herói, 2 melee, 2 ranged); embaixo, estudo de 6 peças sobre grade ciano de 2 m (piso, canal reto, canto, travessia da porta, pilar baixo, nicho). Paleta da ficha A15: pedra #55596B/#3E4152, musgo #4C5A44, água #313E55, tochas #FF9A2E, runas #4FD8FF.
**Fonte e evidências:** `Source~/FloodedCrypt/` (README, GLB de render, source.json com caixas por peça, planta de cima `review_layout.png`, medidas, `ConceptCrypt.validation.json` 14/14).
**Importação:** `.meta` copiado do `SkeletonMelee_Concept_01.png.meta` (Default, sRGB, NPOT None, sem mipmaps, máx. 2048), só com o GUID novo. **NÃO VALIDADO NO EDITOR** nesta sessão.
**Condições de uso:** projeto Eldoria; three.js MIT; Pillow (validador) MIT-CMU/HPND. É conceito 2D de ambiente proposto: não há kit 3D, cena, collider, NavMesh nem shader de água. Aprovação artística pendente.
