# Prompts completos de produção — não executar nesta auditoria

Data: 14/11/2026. São30 briefs de recursos específicos ausentes, todos com ID correspondente ao ASSET_MANIFEST.json. Não gerar novamente os 96 existentes. Os 5 incompletos têm base funcional: completar UI via ícones/estados e pickup/AttackVisual/projétil via propostas relacionadas, sem substituir automaticamente recursos aprovados.

## Instrução de uso para o agente produtor

Ler AGENTS, GAME_DESIGN, ARCHITECTURE, MVP, ART_DIRECTION_AND_PROMPTS, RELEASE-1.0-VALIDATION e este pacote. Referências de identidade: Hero/SkeletonMelee FBX/GLB e conceitos existentes; ambiente _GameArt; PROVENANCE define origem/limites. Anexar referências locais à ferramenta, sem apenas citar o nome do arquivo e presumir que o gerador o viu. Nenhum upload ou produção foi iniciado aqui.

Cada bloco abaixo é completo em inglês e pode ser enviado à ferramenta adequada após escolher lote e resolver condições. Dimensões novas/orçamentos são propostas de produção, não especificação retroativa do produto. `scope=sugestão_polimento` é opcional; `conflito_bloqueado` não é executável até decisão; destrutível futuro não autoriza comportamento novo. DCC é obrigatório para malhas/rigs/clips funcionais; gerador de imagem só pode produzir o que é bitmap ou conceito. Em UI, texto, interação e estados permanecem componentes editáveis.

Produção de ícones → QAalpha/família → revisão a 32/48 px → integração futura. Produção3D → DCC/escala/pivô/topologia → FBX/URP → revisão de variante isolada. VFX → prefab visual sem dano → reset/pool/pausa → integração de hooks em tarefa separada. Nenhuma dessas etapas pode ser declarada concluída por ter uma imagem bonita.

## Índice por prioridade e categoria

| Prioridade | Categoria | ID | Recurso | Condição |
| --- | --- | --- | --- | --- |
| P1 | C | ICON-ITEM-ANCIENT | Fio Antigo — ícone | aprovado_1_0 |
| P1 | C | ICON-ITEM-IRON | Espada de Ferro — ícone | aprovado_1_0 |
| P1 | C | ICON-ITEM-WARDEN | Lâmina do Guardião — ícone | aprovado_1_0 |
| P1 | D | ICON-ITEM-LEATHER | Colete de couro — ícone | aprovado_1_0 |
| P1 | E | HUD-POTIONS | Atalho/quantidade de poções no HUD | aprovado_1_0 |
| P1 | E | ICON-ITEM-HEALTH | Poção de vida — ícone | aprovado_1_0 |
| P2 | A | TEX-ATLAS-HERO | Hero — atlas histórico | conflito_bloqueado |
| P2 | B | TEX-ATLAS-MELEE | SkeletonMelee — atlas histórico | conflito_bloqueado |
| P2 | C | MODEL-ITEM-ANCIENT | Fio Antigo — malha dedicada | sugestão_polimento |
| P2 | C | MODEL-ITEM-IRON | Espada de Ferro — malha dedicada | sugestão_polimento |
| P2 | C | MODEL-ITEM-WARDEN | Lâmina do Guardião — malha dedicada | sugestão_polimento |
| P2 | D | MODEL-ITEM-LEATHER | Colete de couro — malha dedicada | sugestão_polimento |
| P2 | E | MODEL-ITEM-HEALTH | Poção de vida — malha dedicada | sugestão_polimento |
| P2 | F | UI-SKIN | Partes modulares da UI | sugestão_polimento |
| P2 | F | UI-TRANSITION | Transições discretas | sugestão_polimento |
| P2 | G | ICON-ACTION-DASH | Dash — ícone | sugestão_polimento |
| P2 | G | ICON-ACTION-SPECIAL | Giro de espada — ícone | sugestão_polimento |
| P2 | H | MODEL-LOOT-GOLD | Ouro — forma dedicada | sugestão_polimento |
| P2 | H | MODEL-LOOT-XP | Orbe de XP — forma dedicada | sugestão_polimento |
| P2 | I | MODEL-DESTRUCTIBLE | Obstáculo destrutível futuro | planejado_futuro_condicional |
| P2 | J | ANIM-TORCH | Loop discreto da chama | sugestão_polimento |
| P2 | J | FX-DASH | Rastro do dash | sugestão_polimento |
| P2 | J | FX-HEAL | Recuperação de vida | sugestão_polimento |
| P2 | J | FX-HIT | Impacto de acerto | sugestão_polimento |
| P2 | J | FX-LEVEL | Subida de nível | sugestão_polimento |
| P2 | J | FX-LOOT | Coleta aceita | sugestão_polimento |
| P2 | J | FX-PROJECTILE-TRAIL | Cauda do projétil | sugestão_polimento |
| P2 | J | FX-SLAM | Impacto Slam | sugestão_polimento |
| P2 | J | FX-SLASH | Rastro do combo | sugestão_polimento |
| P2 | J | FX-SPECIAL | Rastro do giro | sugestão_polimento |

## P1 / Armas e equipamentos

<a id="icon-item-ancient"></a>

### ICON-ITEM-ANCIENT — Fio Antigo — ícone

**Tipo:** ícone 2D. **Finalidade:** Identidade visual do item em slots e detalhes

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Formato:** PNG RGBA + fonte editável

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

**Integração pendente:** Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

```text
ASSET ID: ICON-ITEM-ANCIENT
PURPOSE AND VISUAL BRIEF: Inventory icon for the existing item Fio Antigo. Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One isolated three-quarter object, soft top-left light; no rarity frame, quantity or name baked.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Verify real alpha and distinguish all five items by silhouette in greyscale at 32px.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="icon-item-iron"></a>

### ICON-ITEM-IRON — Espada de Ferro — ícone

**Tipo:** ícone 2D. **Finalidade:** Identidade visual do item em slots e detalhes

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Formato:** PNG RGBA + fonte editável

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

**Integração pendente:** Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

```text
ASSET ID: ICON-ITEM-IRON
PURPOSE AND VISUAL BRIEF: Inventory icon for the existing item Espada de Ferro. Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One isolated three-quarter object, soft top-left light; no rarity frame, quantity or name baked.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Verify real alpha and distinguish all five items by silhouette in greyscale at 32px.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="icon-item-warden"></a>

### ICON-ITEM-WARDEN — Lâmina do Guardião — ícone

**Tipo:** ícone 2D. **Finalidade:** Identidade visual do item em slots e detalhes

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Formato:** PNG RGBA + fonte editável

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

**Integração pendente:** Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

```text
ASSET ID: ICON-ITEM-WARDEN
PURPOSE AND VISUAL BRIEF: Inventory icon for the existing item Lâmina do Guardião. Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One isolated three-quarter object, soft top-left light; no rarity frame, quantity or name baked.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Verify real alpha and distinguish all five items by silhouette in greyscale at 32px.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P1 / Inventário e itens

<a id="icon-item-leather"></a>

### ICON-ITEM-LEATHER — Colete de couro — ícone

**Tipo:** ícone 2D. **Finalidade:** Identidade visual do item em slots e detalhes

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Formato:** PNG RGBA + fonte editável

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

**Integração pendente:** Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

```text
ASSET ID: ICON-ITEM-LEATHER
PURPOSE AND VISUAL BRIEF: Inventory icon for the existing item Colete de couro. Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One isolated three-quarter object, soft top-left light; no rarity frame, quantity or name baked.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Verify real alpha and distinguish all five items by silhouette in greyscale at 32px.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P1 / Poções e consumíveis

<a id="hud-potions"></a>

### HUD-POTIONS — Atalho/quantidade de poções no HUD

**Tipo:** interface. **Finalidade:** Leitura de Q sem abrir inventário

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Proposta: ícone frasco 48×48, Q e total; zero/HP cheio/morto indisponível, sem sugerir cooldown.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** Componente 160×64 no canvas 1920×1080; âncora superior esquerda/margem 24, proposta y=-180

**Formato:** UGUI editável; ícone referenciado; textos runtime

**Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Dependências:** ICON-ITEM-HEALTH, HUD-STAMINA

**Integração pendente:** Presenter futuro somente leitura em Inventory.Changed/Health.Changed; não mudar escolha de Q nem capacidade.

```text
ASSET ID: HUD-POTIONS
PURPOSE AND VISUAL BRIEF: Potion quick-use HUD for the current Q action. Use the health-flask icon at 48x48, runtime Q label and total count, matching the existing HUD. Empty shows 0 and an unavailable label; full HP and dead states are disabled without implying empty inventory.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Editable 160x64 UGUI block, top-left anchor, 24px margin, proposed y=-180 below current stamina. Document ready, empty, full-health, dead and focus states. Proposed placement requires in-game review.
ENGINE CONTRACT: Deliver editable UGUI components and separate art sprites, 1920x1080 reference layout with documented anchors. Keep text/counters native runtime UI. Preserve the current input module, presenters, menu stack and pause rules. Do not modify existing code or saved scenes in the art delivery.
VALIDATION: Count stored PotionData stacks, preserve potency-specific stacks and the current first-potion choice. No potion cooldown exists: do not invent one. Test inventory/health event refresh and I/Tab pause.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="icon-item-health"></a>

### ICON-ITEM-HEALTH — Poção de vida — ícone

**Tipo:** ícone 2D. **Finalidade:** Identidade visual do item em slots e detalhes

**Escopo/condição:** aprovado_1_0 — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48 e 32 px

**Formato:** PNG RGBA + fonte editável

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

**Integração pendente:** Tarefa futura: mapa serializado ItemDefinition→Sprite, Image dedicado no SlotView/detalhes. Arte não adiciona esse binding sozinha.

```text
ASSET ID: ICON-ITEM-HEALTH
PURPOSE AND VISUAL BRIEF: Inventory icon for the existing item Poção de vida. Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One isolated three-quarter object, soft top-left light; no rarity frame, quantity or name baked.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Verify real alpha and distinguish all five items by silhouette in greyscale at 32px.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Personagem principal

<a id="tex-atlas-hero"></a>

### TEX-ATLAS-HERO — Hero — atlas histórico

**Tipo:** textura. **Finalidade:** Meta antiga que conflita com preservação da release

**Escopo/condição:** conflito_bloqueado — BLOQUEADO: resolver briefing atlas/Humanoid antigo versus Generic/materiais preservados pela 1.0. Não produzir/aplicar automaticamente.

**Descrição visual:** Atlas pintado 512 previsto no briefing; produto atual preserva materiais/malha. Alteração afetada interrompida.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; UV exclusiva em cópia isolada

**Formato:** PNG sRGB + fonte e overlay UV

**Unity:** PNG sRGB para cor, dados lineares. URP/Lit com _BaseColor para flash. Estudo de superfície não é atlas aplicável às UVs sobrepostas.

**Dependências:** Referências e pipeline atuais.

```text
ASSET ID: TEX-ATLAS-HERO
PURPOSE AND VISUAL BRIEF: Conditional hand-painted atlas for an isolated Hero review variant, only after resolving the preservation conflict. Match current material colours and faceted shading.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: 512x512 UV-aligned base-colour atlas, editable source and UV overlay; separate copied model with approved non-overlapping UVs. Emissive mask only where already used.
ENGINE CONTRACT: Deliver editable source and PNG base colour in sRGB, linear data maps if required. Preserve URP _BaseColor support. A surface study sheet is not a UV atlas.
VALIDATION: Check seams/texel density and preserve rig/clips. A surface contact sheet is not an atlas; material consolidation alone does not prove a performance gain.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Inimigos e boss

<a id="tex-atlas-melee"></a>

### TEX-ATLAS-MELEE — SkeletonMelee — atlas histórico

**Tipo:** textura. **Finalidade:** Meta antiga que conflita com preservação da release

**Escopo/condição:** conflito_bloqueado — BLOQUEADO: resolver briefing atlas/Humanoid antigo versus Generic/materiais preservados pela 1.0. Não produzir/aplicar automaticamente.

**Descrição visual:** Atlas pintado 512 previsto no briefing; produto atual preserva materiais/malha. Alteração afetada interrompida.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; UV exclusiva em cópia isolada

**Formato:** PNG sRGB + fonte e overlay UV

**Unity:** PNG sRGB para cor, dados lineares. URP/Lit com _BaseColor para flash. Estudo de superfície não é atlas aplicável às UVs sobrepostas.

**Dependências:** Referências e pipeline atuais.

```text
ASSET ID: TEX-ATLAS-MELEE
PURPOSE AND VISUAL BRIEF: Conditional hand-painted atlas for an isolated SkeletonMelee review variant, only after resolving the preservation conflict. Match current material colours and faceted shading.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: 512x512 UV-aligned base-colour atlas, editable source and UV overlay; separate copied model with approved non-overlapping UVs. Emissive mask only where already used.
ENGINE CONTRACT: Deliver editable source and PNG base colour in sRGB, linear data maps if required. Preserve URP _BaseColor support. A surface study sheet is not a UV atlas.
VALIDATION: Check seams/texel density and preserve rig/clips. A surface contact sheet is not an atlas; material consolidation alone does not prove a performance gain.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Armas e equipamentos

<a id="model-item-ancient"></a>

### MODEL-ITEM-ANCIENT — Fio Antigo — malha dedicada

**Tipo:** modelo 3D. **Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0,95×0,24×0,07 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Formato:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ICON-ITEM-ANCIENT

**Integração pendente:** Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint. Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

```text
ASSET ID: MODEL-ITEM-ANCIENT
PURPOSE AND VISUAL BRIEF: Optional dedicated 3D visual for the existing item Fio Antigo. Short broad ancient steel sword, stepped angular guard, one brass geometric inset and subtle wear. No new element or damage type.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Match the existing 2-metre hero. Nominal dimensions 0.95×0.24×0.07 m. Sword pivot at hand grip; vest/flask at base centre. Swords 300-800 triangles, vest 500-1200, flask 150-350, one or two reused URP material slots. Reuse the current Hero sword for the iron variant if suitable before authoring another mesh.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Show floor and hand previews. A wearable vest needs a separate optional skinned variant bound to the current Generic rig; a static mesh alone is not wearable. Preserve damage, range, defence and current body/clothing.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="model-item-iron"></a>

### MODEL-ITEM-IRON — Espada de Ferro — malha dedicada

**Tipo:** modelo 3D. **Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0,85×0,16×0,06 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Formato:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ICON-ITEM-IRON

**Integração pendente:** Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint. Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

```text
ASSET ID: MODEL-ITEM-IRON
PURPOSE AND VISUAL BRIEF: Optional dedicated 3D visual for the existing item Espada de Ferro. Short straight steel sword, simple brass crossguard, dark leather grip, broad readable blade; match the existing Hero_Sword family, no magical glow.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Match the existing 2-metre hero. Nominal dimensions 0.85×0.16×0.06 m. Sword pivot at hand grip; vest/flask at base centre. Swords 300-800 triangles, vest 500-1200, flask 150-350, one or two reused URP material slots. Reuse the current Hero sword for the iron variant if suitable before authoring another mesh.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Show floor and hand previews. A wearable vest needs a separate optional skinned variant bound to the current Generic rig; a static mesh alone is not wearable. Preserve damage, range, defence and current body/clothing.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="model-item-warden"></a>

### MODEL-ITEM-WARDEN — Lâmina do Guardião — malha dedicada

**Tipo:** modelo 3D. **Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0,95×0,22×0,07 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Formato:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ICON-ITEM-WARDEN

**Integração pendente:** Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint. Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

```text
ASSET ID: MODEL-ITEM-WARDEN
PURPOSE AND VISUAL BRIEF: Optional dedicated 3D visual for the existing item Lâmina do Guardião. Short broad steel sword, heavier angular guard, one restrained cyan inset. Keep it one-handed, distinct from the boss maul.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Match the existing 2-metre hero. Nominal dimensions 0.95×0.22×0.07 m. Sword pivot at hand grip; vest/flask at base centre. Swords 300-800 triangles, vest 500-1200, flask 150-350, one or two reused URP material slots. Reuse the current Hero sword for the iron variant if suitable before authoring another mesh.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Show floor and hand previews. A wearable vest needs a separate optional skinned variant bound to the current Generic rig; a static mesh alone is not wearable. Preserve damage, range, defence and current body/clothing.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Inventário e itens

<a id="model-item-leather"></a>

### MODEL-ITEM-LEATHER — Colete de couro — malha dedicada

**Tipo:** modelo 3D. **Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0,60×0,65×0,25 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Formato:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ICON-ITEM-LEATHER

**Integração pendente:** Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint. Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

```text
ASSET ID: MODEL-ITEM-LEATHER
PURPOSE AND VISUAL BRIEF: Optional dedicated 3D visual for the existing item Colete de couro. Sleeveless chunky brown leather vest, broad plain panels, dark seams and two brass fasteners; no helmet, shield or accessory set.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Match the existing 2-metre hero. Nominal dimensions 0.60×0.65×0.25 m. Sword pivot at hand grip; vest/flask at base centre. Swords 300-800 triangles, vest 500-1200, flask 150-350, one or two reused URP material slots. Reuse the current Hero sword for the iron variant if suitable before authoring another mesh.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Show floor and hand previews. A wearable vest needs a separate optional skinned variant bound to the current Generic rig; a static mesh alone is not wearable. Preserve damage, range, defence and current body/clothing.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Poções e consumíveis

<a id="model-item-health"></a>

### MODEL-ITEM-HEALTH — Poção de vida — malha dedicada

**Tipo:** modelo 3D. **Finalidade:** Forma no chão; equipamento visível é extensão visual opcional

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0,22×0,32×0,22 m (proposta a ajustar ao Hero); pivô na empunhadura para espada, base para demais

**Formato:** FBX + .blend; espadas 300–800 tris, colete 500–1200, frasco 150–350; 1–2 materiais

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ICON-ITEM-HEALTH

**Integração pendente:** Seletor visual futuro por LootKind/ItemDefinition; Pickup atual recebe um Renderer/tint. Troca visual futura por referências/eventos nos sockets; não duplicar Hero_Sword fixa.

```text
ASSET ID: MODEL-ITEM-HEALTH
PURPOSE AND VISUAL BRIEF: Optional dedicated 3D visual for the existing item Poção de vida. Squat faceted glass flask, broad round body, short neck, cork and dark leather collar; ruby-red liquid, clear highlight, chunky silhouette.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Match the existing 2-metre hero. Nominal dimensions 0.22×0.32×0.22 m. Sword pivot at hand grip; vest/flask at base centre. Swords 300-800 triangles, vest 500-1200, flask 150-350, one or two reused URP material slots. Reuse the current Hero sword for the iron variant if suitable before authoring another mesh.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Show floor and hand previews. A wearable vest needs a separate optional skinned variant bound to the current Generic rig; a static mesh alone is not wearable. Preserve damage, range, defence and current body/clothing.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Menus e interfaces

<a id="ui-skin"></a>

### UI-SKIN — Partes modulares da UI

**Tipo:** interface. **Finalidade:** Polimento opcional preservando telas atuais

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Manter azul-preto/dourado; chanfros discretos e foco por cantos além da cor. Não redesenhar telas.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** Painel 128×128/borda16; botão 256×80/borda12; slot192×112/borda8; canto foco32×32

**Formato:** Sprites RGBA separados 9-slice + fonte e mapa de estados

**Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Dependências:** UI-TYPE

```text
ASSET ID: UI-SKIN
PURPOSE AND VISUAL BRIEF: Optional reusable parts for current dark blue-black panels and muted-gold buttons. Simple broad bevels, restrained worn-brass lines, no skull ornaments/heavy frames. Geometric corner focus marker distinct from hover and selection.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Panel 128x128 with 16px slice borders, button 256x80/border12, slot192x112/border8, focus corner32x32. Separate normal/hover/focus/pressed/disabled/empty/selected/equipped/rarity layers; no baked text.
ENGINE CONTRACT: Deliver editable UGUI components and separate art sprites, 1920x1080 reference layout with documented anchors. Keep text/counters native runtime UI. Preserve the current input module, presenters, menu stack and pause rules. Do not modify existing code or saved scenes in the art delivery.
VALIDATION: Preserve click targets/layout, test stretched edges at 720p. Current inventory slots use Transition.None: focus/hover integration is later code work.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="ui-transition"></a>

### UI-TRANSITION — Transições discretas

**Tipo:** interface. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short panel opacity fade and subtle focus-corner settle; no panel sliding, no delayed interaction.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.10–0.15 s unscaled time; no input blocking

**Formato:** UGUI editável + spec de tween

**Unity:** UGUI editável; sprites e texto separados, referência 1920×1080/CanvasScaler match .5. Preservar Text/LegacyRuntime.ttf, InputSystemUIInputModule, pilha de menus e presenters atuais; imagem achatada não é interface funcional.

**Dependências:** UI-SKIN

**Integração pendente:** MenuScreenStack atual mostra/oculta de imediato; camada de tween ainda ausente.

```text
ASSET ID: UI-TRANSITION
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Transições discretas. Short panel opacity fade and subtle focus-corner settle; no panel sliding, no delayed interaction.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.10–0.15 s unscaled time; no input blocking. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver editable UGUI components and separate art sprites, 1920x1080 reference layout with documented anchors. Keep text/counters native runtime UI. Preserve the current input module, presenters, menu stack and pause rules. Do not modify existing code or saved scenes in the art delivery.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / HUD e gameplay

<a id="icon-action-dash"></a>

### ICON-ACTION-DASH — Dash — ícone

**Tipo:** ícone 2D. **Finalidade:** Leitura de comando existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Blue cape sweep with a broad forward chevron-shaped cutout, no weapon arrow, no text.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48/32 px

**Formato:** PNG RGBA + fonte

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

```text
ASSET ID: ICON-ACTION-DASH
PURPOSE AND VISUAL BRIEF: Optional action icon for Dash. Blue cape sweep with a broad forward chevron-shaped cutout, no weapon arrow, no text.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One icon, no baked key/timer, runtime overlays remain separate.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Distinguish dash/special in greyscale; no new skill or skill-tree entry.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="icon-action-special"></a>

### ICON-ACTION-SPECIAL — Giro de espada — ícone

**Tipo:** ícone 2D. **Finalidade:** Leitura de comando existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Short steel sword inside a broad cyan circular sweep, open centre, suggesting one complete rotation.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 512×512; leitura 48/32 px

**Formato:** PNG RGBA + fonte

**Unity:** PNG RGBA 512×512 + fonte; alpha real, conteúdo 416×416 central/margem 48 px. Sprite Single, sRGB, mipmaps off, bilinear, clamp, PPU 100; testar a 48/32 px. Sem texto, números ou moldura de raridade baked.

**Dependências:** Referências e pipeline atuais.

```text
ASSET ID: ICON-ACTION-SPECIAL
PURPOSE AND VISUAL BRIEF: Optional action icon for Giro de espada. Short steel sword inside a broad cyan circular sweep, open centre, suggesting one complete rotation.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: One icon, no baked key/timer, runtime overlays remain separate.
ENGINE CONTRACT: Deliver one 512x512 PNG RGBA with genuine transparent alpha and editable source. Fit silhouette in a centred 416x416 area with 48px padding. Sprite Single, sRGB, no mipmaps, bilinear, clamp, PPU 100. No checkerboard, baked text, numbers, rarity border, labels, watermark, scenery or duplicate object. Verify at 48px and 32px on light, dark and stone backgrounds.
VALIDATION: Distinguish dash/special in greyscale; no new skill or skill-tree entry.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Loot e recompensas

<a id="model-loot-gold"></a>

### MODEL-LOOT-GOLD — Ouro — forma dedicada

**Tipo:** modelo 3D. **Finalidade:** Leitura de recompensa imediata

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** A small chunky stack of three brass-gold coins, plain broad bevels, bright upper edge, dark underside, no lettering.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.30 x 0.14 x 0.30 m; pivô base

**Formato:** FBX + .blend; 100–250 tris, 1 material

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** LOOT-SHARED

```text
ASSET ID: MODEL-LOOT-GOLD
PURPOSE AND VISUAL BRIEF: Optional pickup mesh for Ouro. A small chunky stack of three brass-gold coins, plain broad bevels, bright upper edge, dark underside, no lettering.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: 0.30 x 0.14 x 0.30 m, base-centred pivot, 100-250 triangles, one material.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Distinguish gold, XP, potions and hostile projectiles at the gameplay camera; retain current trigger/bob/spin and immediate collection rules.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="model-loot-xp"></a>

### MODEL-LOOT-XP — Orbe de XP — forma dedicada

**Tipo:** modelo 3D. **Finalidade:** Leitura de recompensa imediata

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** A simple faceted cyan orb, dark lower facet and restrained bright core, clearly distinct from an orange hostile projectile.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.25 m diameter; pivô base

**Formato:** FBX + .blend; 100–250 tris, 1 material

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** LOOT-SHARED

```text
ASSET ID: MODEL-LOOT-XP
PURPOSE AND VISUAL BRIEF: Optional pickup mesh for Orbe de XP. A simple faceted cyan orb, dark lower facet and restrained bright core, clearly distinct from an orange hostile projectile.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: 0.25 m diameter, base-centred pivot, 100-250 triangles, one material.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: Distinguish gold, XP, potions and hostile projectiles at the gameplay camera; retain current trigger/bob/spin and immediate collection rules.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / Cenários e objetos interativos

<a id="model-destructible"></a>

### MODEL-DESTRUCTIBLE — Obstáculo destrutível futuro

**Tipo:** modelo 3D. **Finalidade:** Conteúdo posterior citado no GAME_DESIGN, sem sistema atual

**Escopo/condição:** planejado_futuro_condicional — Futuro previsto; integração bloqueada até tarefa de comportamento. Dimensões propostas, não requisito aprovado.

**Descrição visual:** Proposta: alvenaria gasta coerente com paredes, sem inventar HP/drops/combate.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** Proposta 1×1×1 m; confirmar footprint antes do lote

**Formato:** FBX + .blend; intacto + 3 fragmentos, total <=600 tris

**Unity:** Fonte .blend + FBX real; GLB opcional para revisão, sem dependência de importador novo. Metros, Unity +Y cima/+Z frente, transforms aplicadas, pivô conforme ficha, normais facetadas, URP/Lit, sem colliders de gameplay no visual.

**Dependências:** ENV-WALL

```text
ASSET ID: MODEL-DESTRUCTIBLE
PURPOSE AND VISUAL BRIEF: Conditional art-only destructible stone obstacle: a chunky worn blue-grey masonry block matching current walls, restrained base moss, one broad crack and three coarse visual fracture pieces.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: An intact 1x1x1m mesh and three separate coarse debris meshes, base-centred pivots, at most 600 triangles total, one reused stone material.
ENGINE CONTRACT: Deliver editable Blender source and a real FBX mesh, optional GLB review copy. One Unity unit per metre, Unity +Y up/+Z forward, applied transforms, correct normals and named parts, URP/Lit materials. No gameplay scripts or physics changes.
VALIDATION: No destruction logic, triggers, rewards, collision or combat. Integration waits for a separate approved gameplay brief; keep combat centre open.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.

## P2 / VFX e animações adicionais

<a id="anim-torch"></a>

### ANIM-TORCH — Loop discreto da chama

**Tipo:** animação 3D. **Finalidade:** Polimento de chama estática

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Pequena oscilação só do filho visual, suporte/luzes fixos.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** Torch_FlameLoop; 1,2 s, loop, 30 fps; escala .98–1.02

**Formato:** AnimationClip .anim + fonte de autoria; variante visual isolada Support/Flame se necessária, sem rig esquelético

**Unity:** Animação de Transform de objeto, sem skin, Avatar ou rig esquelético. A tocha atual combina ferro/chama em uma malha com duas submalhas: criar só uma variante isolada com filhos Support/Flame se esse polimento for adotado; preservar GA_Torch original. Entregar fonte e AnimationClip .anim por API Editor, 30 fps, raiz fixa; sem dano, luz animada ou colisão.

**Dependências:** ENV-TORCH

```text
ASSET ID: ANIM-TORCH
PURPOSE AND VISUAL BRIEF: Optional gentle motion for the existing faceted orange torch flame. Keep the support, root, light and base anchored; gently lean the flame left then right and vary its scale from 0.98 to 1.02. Match the current low-poly dark medieval fantasy, orange #FF9A2E, with no change of silhouette or art direction.
DELIVERY: A named Torch_FlameLoop AnimationClip (.anim), editable authoring source, 1.2 seconds, seamless loop at 30fps. This is object Transform animation, not a skinned character animation: no skeleton, Humanoid Avatar or Generic character rig is required.
ENGINE CONTRACT: Unity 6000.6.9f1 URP 17.6.0. The current GA_Torch is one mesh containing iron and flame submeshes, not a hierarchy with an independently movable flame. If adopted, create only an isolated review variant with separate Support and Flame visual children and animate the Flame child. Preserve the original GA_Torch and all existing scene references. Keep the root fixed, no root motion, no gameplay scripts, damage, collision, new light animation or packages.
VALIDATION: First and last poses and tangents match. No strobing light intensity, broad flashes, penetration, support movement or navigation changes. Review in the real camera and pause context before integration. The visual split and clip delivery do not by themselves integrate the effect into the saved rooms.
LOCKS: Work only in a new staging folder, preserve the release models/materials/rigs/timing, record tool/version, source references, edits and licence. A still image or video is not a working AnimationClip. Never claim in-engine approval unless actually observed.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-dash"></a>

### FX-DASH — Rastro do dash

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Two or three faint blue cape sweeps or broad dust puffs; never opaque full-character duplicates.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** tail <=0.20 s; <=8 particles; clear on cancellation

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** ANIM-HERO-DASH

**Integração pendente:** PlayerDash expõe IsDashing, sem evento; ponte atual detecta início por borda.

```text
ASSET ID: FX-DASH
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Rastro do dash. Two or three faint blue cape sweeps or broad dust puffs; never opaque full-character duplicates.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: tail <=0.20 s; <=8 particles; clear on cancellation. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-heal"></a>

### FX-HEAL — Recuperação de vida

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Brief soft ruby-to-warm-white upward motes close to the hero and a low-opacity foot ring; no poison-like effect.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.35–0.60 s; radius <=0.6 m; <=16 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** ICON-ITEM-HEALTH

**Integração pendente:** Health.Changed genérico; não há PotionUsed. Distinguir consumo aceito de cura de nível.

```text
ASSET ID: FX-HEAL
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Recuperação de vida. Brief soft ruby-to-warm-white upward motes close to the hero and a low-opacity foot ring; no poison-like effect.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.35–0.60 s; radius <=0.6 m; <=16 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-hit"></a>

### FX-HIT — Impacto de acerto

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Small directional ivory chips and two orange sparks for bone/metal contact; no blood, large flash or persistent debris.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.15–0.25 s; radius <=0.3 m; <=10 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** FX-DAMAGE

**Integração pendente:** Health.Damaged só fornece quantidade; posição/direção precisariam ponte futura.

```text
ASSET ID: FX-HIT
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Impacto de acerto. Small directional ivory chips and two orange sparks for bone/metal contact; no blood, large flash or persistent debris.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.15–0.25 s; radius <=0.3 m; <=10 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-level"></a>

### FX-LEVEL — Subida de nível

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** One thin cyan ring rising boots-to-waist, sparse brass motes; combat floor remains visible.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.60–0.80 s; radius <=0.8 m; <=20 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** HUD-PROGRESSION

**Integração pendente:** PlayerProgression.LeveledUp(int), sem mudar cura ou fórmula.

```text
ASSET ID: FX-LEVEL
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Subida de nível. One thin cyan ring rising boots-to-waist, sparse brass motes; combat floor remains visible.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.60–0.80 s; radius <=0.8 m; <=20 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-loot"></a>

### FX-LOOT — Coleta aceita

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Small cyan glint for XP, brass for gold, neutral spark for stored gear/potions; restrained size and brightness.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.20–0.35 s; radius <=0.35 m; <=8 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** LOOT-SHARED

**Integração pendente:** PlayerProgression.Collected(LootKind) existe, sem quantidade/posição/item completo.

```text
ASSET ID: FX-LOOT
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Coleta aceita. Small cyan glint for XP, brass for gold, neutral spark for stored gear/potions; restrained size and brightness.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.20–0.35 s; radius <=0.35 m; <=8 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-projectile-trail"></a>

### FX-PROJECTILE-TRAIL — Cauda do projétil

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Compact tapered orange tail on existing hostile orb, clear against stone, no explosion or larger danger radius.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** tail <=0.15 s and 0.6 m; <=6 particles per projectile

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** FX-PROJECTILE

**Integração pendente:** Reset OnEnable/retorno do ProjectilePool; não criar nova instância a cada tiro.

```text
ASSET ID: FX-PROJECTILE-TRAIL
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Cauda do projétil. Compact tapered orange tail on existing hostile orb, clear against stone, no explosion or larger danger radius.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: tail <=0.15 s and 0.6 m; <=6 particles per projectile. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-slam"></a>

### FX-SLAM — Impacto Slam

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Low thin shock ring and a few angular stone chips with muted orange edge; no persistent damaging patch.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.25–0.45 s; radius 3.5 or 4.55 m from actual phase data; <=24 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** FX-TELEGRAPH

**Integração pendente:** BossBrain aplica Slam, sem evento público de impacto dedicado.

```text
ASSET ID: FX-SLAM
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Impacto Slam. Low thin shock ring and a few angular stone chips with muted orange edge; no persistent damaging patch.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.25–0.45 s; radius 3.5 or 4.55 m from actual phase data; <=24 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-slash"></a>

### FX-SLASH — Rastro do combo

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** Three concise steel/cyan sword arcs: right diagonal, left diagonal and compact downward finisher. No broad opaque glow.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.12–0.18 s; <=12 particles per impact; radius never exceeds actual combo range

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** ANIM-HERO-ATTACK-01, ANIM-HERO-ATTACK-02, ANIM-HERO-ATTACK-03

**Integração pendente:** PlayerCombat.AttackStarted/AttackImpacted/AttackCancelled; sem hit-confirm/posição por alvo.

```text
ASSET ID: FX-SLASH
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Rastro do combo. Three concise steel/cyan sword arcs: right diagonal, left diagonal and compact downward finisher. No broad opaque glow.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.12–0.18 s; <=12 particles per impact; radius never exceeds actual combo range. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
<a id="fx-special"></a>

### FX-SPECIAL — Rastro do giro

**Tipo:** VFX. **Finalidade:** Polimento opcional de ação existente

**Escopo/condição:** sugestão_polimento — Lote futuro; nenhuma geração autorizada nesta etapa.

**Descrição visual:** One thin cyan circular sword sweep, empty centre, restrained pale edge, clearly distinct from a red hostile ground telegraph.

**Direção:** Low-poly estilizado, fantasia medieval sombria, planos grandes, silhueta clara a 45°. Herói azul #2F6BFF, ciano #4FD8FF, couro #7A4B2A, aço #B9C4CF, latão #E0B040; ameaça #FF2A2A/laranja #FF9A2E; pedra #55596B/#3E4152, musgo #4C5A44. Preservar modelos, materiais e direção atuais.

**Dimensões/tempo:** 0.18–0.30 s; radius <=3 m; <=24 particles

**Formato:** Prefab visual + fontes; PNG RGBA 128/256 se necessário

**Unity:** Prefab Unity 6000.6.9f1/URP 17.6.0, fontes e PNG RGBA quando necessários. ParticleSystem/mesh/trail simples, sem pacote novo, collider ou dano. Reset e reuso/pool; respeitar pausa, cancelamento, flash/shake.

**Dependências:** ANIM-HERO-SPECIAL

**Integração pendente:** PlayerSpecial.AttackImpacted .36 s; não há VFX específico inscrito.

```text
ASSET ID: FX-SPECIAL
PURPOSE AND VISUAL BRIEF: Optional presentation effect: Rastro do giro. One thin cyan circular sword sweep, empty centre, restrained pale edge, clearly distinct from a red hostile ground telegraph.
ART DIRECTION: Match the existing stylized low-poly dark medieval fantasy: chunky silhouettes, large faceted planes, restrained hand-painted gradients and matte surfaces. Hero blue #2F6BFF/cyan #4FD8FF, leather #7A4B2A, steel #B9C4CF, brass #E0B040; threat red #FF2A2A/orange #FF9A2E; desaturated stone #55596B/#3E4152, moss #4C5A44. Use current project models and concepts as references, with a fixed 45-degree isometric gameplay camera. Preserve identity, costume, palette and proportions. Do not copy commercial game assets, characters or logos.
DELIVERY: Envelope: 0.18–0.30 s; radius <=3 m; <=24 particles. Reuse plain geometry or one small texture sheet; isolated review prefab/component.
ENGINE CONTRACT: Deliver a Unity 6000.6.9f1 URP 17.6.0 presentation prefab, editable sources and necessary alpha textures. Lightweight particles/meshes/trails, no new packages, damage or colliders. Reset pooled effects and clear trails/particles on return; honour pause, cancellation and flash/shake options.
VALIDATION: Effect returns to rest and resets after two rapid activations, pause, death, cancellation and pool return. Never apply damage or delay input. Honour existing flash/shake options and compare in real gameplay.
LOCKS: Work in a new isolated art staging folder. Preserve the release assets, timing and rules. Any required code integration is a separate scoped task. A still image or video cannot replace a real mesh, rig, animation clip or functional UI. Record tool/version, prompts, references, edits, licence and actual validation. Never claim in-engine approval without observing it.
```

**Validação e aceite:**

- Preservar identidade e regras atuais.
- Validar no enquadramento real sem referências faltantes ou regressões.
- Entregar formatos/dimensões/fonte/procedência da ficha.
- Concluir QA técnico e PlayMode em contexto antes de marcar aprovado.

O agente entrega fonte/asset, prova técnica e contexto de revisão; o integrador implementa bindings necessários apenas no escopo de arquivos delegado. Não mudar o status AUSENTE até o recurso existir; importar não basta para marcar integrado/aprovado.
