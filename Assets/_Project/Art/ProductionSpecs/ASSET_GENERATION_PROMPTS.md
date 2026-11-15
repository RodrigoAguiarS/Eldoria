# Prompts completos de produção — não executar nesta auditoria

> Consolidação de 14/11/2026: fichas repetidas foram substituídas por índices por ID; os dados e prompts completos permanecem no manifesto, sem alteração. Ler somente o registro necessário. Lotes ANIM-TORCH e ITEMS-COMBAT já foram produzidos/integrados; o diagnóstico introdutório antigo abaixo é histórico onde divergir desses lotes. Novas sugestões prontas para escolha estão em [PROMPT_CATALOG](../../../../PROMPT_CATALOG.md).

Índice dos 30 briefs originais, cujos prompts completos permanecem em `ASSET_MANIFEST.json`. Vários já foram executados; consultar status/lotes antes de produzir. O catálogo geral novo contém 15 briefs selecionados, sem recomendar regerar os itens/ícones/chama/VFX já integrados.

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

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-ANCIENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-iron"></a>

### ICON-ITEM-IRON — Espada de Ferro — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-IRON]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-warden"></a>

### ICON-ITEM-WARDEN — Lâmina do Guardião — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-WARDEN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P1 / Inventário e itens

<a id="icon-item-leather"></a>

### ICON-ITEM-LEATHER — Colete de couro — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-LEATHER]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P1 / Poções e consumíveis

<a id="hud-potions"></a>

### HUD-POTIONS — Atalho/quantidade de poções no HUD

**Estado atual:** EXISTENTE. **Tipo:** interface. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=HUD-POTIONS]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-item-health"></a>

### ICON-ITEM-HEALTH — Poção de vida — ícone

**Estado atual:** EXISTENTE. **Tipo:** ícone 2D. **Escopo:** aprovado_1_0.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ITEM-HEALTH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Personagem principal

<a id="tex-atlas-hero"></a>

### TEX-ATLAS-HERO — Hero — atlas histórico

**Estado atual:** AUSENTE. **Tipo:** textura. **Escopo:** conflito_bloqueado.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=TEX-ATLAS-HERO]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Inimigos e boss

<a id="tex-atlas-melee"></a>

### TEX-ATLAS-MELEE — SkeletonMelee — atlas histórico

**Estado atual:** AUSENTE. **Tipo:** textura. **Escopo:** conflito_bloqueado.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=TEX-ATLAS-MELEE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Armas e equipamentos

<a id="model-item-ancient"></a>

### MODEL-ITEM-ANCIENT — Fio Antigo — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-ANCIENT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-iron"></a>

### MODEL-ITEM-IRON — Espada de Ferro — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-IRON]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-item-warden"></a>

### MODEL-ITEM-WARDEN — Lâmina do Guardião — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-WARDEN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Inventário e itens

<a id="model-item-leather"></a>

### MODEL-ITEM-LEATHER — Colete de couro — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-LEATHER]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Poções e consumíveis

<a id="model-item-health"></a>

### MODEL-ITEM-HEALTH — Poção de vida — malha dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-ITEM-HEALTH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Menus e interfaces

<a id="ui-skin"></a>

### UI-SKIN — Partes modulares da UI

**Estado atual:** AUSENTE. **Tipo:** interface. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-SKIN]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="ui-transition"></a>

### UI-TRANSITION — Transições discretas

**Estado atual:** AUSENTE. **Tipo:** interface. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=UI-TRANSITION]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / HUD e gameplay

<a id="icon-action-dash"></a>

### ICON-ACTION-DASH — Dash — ícone

**Estado atual:** AUSENTE. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ACTION-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="icon-action-special"></a>

### ICON-ACTION-SPECIAL — Giro de espada — ícone

**Estado atual:** AUSENTE. **Tipo:** ícone 2D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ICON-ACTION-SPECIAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Loot e recompensas

<a id="model-loot-gold"></a>

### MODEL-LOOT-GOLD — Ouro — forma dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-LOOT-GOLD]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="model-loot-xp"></a>

### MODEL-LOOT-XP — Orbe de XP — forma dedicada

**Estado atual:** EXISTENTE. **Tipo:** modelo 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-LOOT-XP]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / Cenários e objetos interativos

<a id="model-destructible"></a>

### MODEL-DESTRUCTIBLE — Obstáculo destrutível futuro

**Estado atual:** AUSENTE. **Tipo:** modelo 3D. **Escopo:** planejado_futuro_condicional.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=MODEL-DESTRUCTIBLE]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

## P2 / VFX e animações adicionais

<a id="anim-torch"></a>

### ANIM-TORCH — Loop discreto da chama

**Estado atual:** EXISTENTE. **Tipo:** animação 3D. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=ANIM-TORCH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-dash"></a>

### FX-DASH — Rastro do dash

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-DASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-heal"></a>

### FX-HEAL — Recuperação de vida

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-HEAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-hit"></a>

### FX-HIT — Impacto de acerto

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-HIT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-level"></a>

### FX-LEVEL — Subida de nível

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-LEVEL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-loot"></a>

### FX-LOOT — Coleta aceita

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-LOOT]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-projectile-trail"></a>

### FX-PROJECTILE-TRAIL — Cauda do projétil

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-PROJECTILE-TRAIL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-slam"></a>

### FX-SLAM — Impacto Slam

**Estado atual:** AUSENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SLAM]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-slash"></a>

### FX-SLASH — Rastro do combo

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SLASH]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.

<a id="fx-special"></a>

### FX-SPECIAL — Rastro do giro

**Estado atual:** EXISTENTE. **Tipo:** VFX. **Escopo:** sugestão_polimento.

Ficha completa: [ASSET_MANIFEST.json](ASSET_MANIFEST.json), registro `resources[id=FX-SPECIAL]`: descrição, evidências, dependências, aceite e, quando houver, `production` (requisitos, formato e prompt inglês). Conferir `production_state`/`production_batches` antes de produzir; existência não equivale a aprovação artística.
