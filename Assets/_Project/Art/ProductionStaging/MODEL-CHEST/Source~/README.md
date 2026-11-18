# A11 — MODEL-CHEST (baú de pedra e latão)

Um **modelo 3D real** com tampa separada e um **take mecânico real** para substituir só o visual do baú F08 de primitivas (`RoomRewardChest_F08*`: `Body`/`LidPivot`/`Latch`, madeira e metal). É uma caixa baixa de pedra azul-acinzentada com cintas de latão gasto, espelho de fechadura, tampa de quatro águas com aro de latão, pega larga na frente e dobradiça traseira. Não há recompensa, loot, moeda, colisor nem rig humanoide.

A condição da ficha ("F08 decidido antes da produção final") está cumprida: F08 está integrado na `main` (PRs #6 e #7, um baú em cada uma das cinco salas). Nada do domínio foi alterado: `RoomRewardChest.cs`, `RoomRewardPresenter.cs`, `RoomRewardSceneBuilder.cs`, cenas, prefabs e materiais `Mat_Chest_F08_*` só foram lidos. A integração é uma tarefa Codex separada.

| Item | Valor |
| --- | --- |
| Malha | `../MODEL-CHEST.fbx`: FBX 7.4 ASCII, **828 triângulos** (alvo ≤ 1200): `Base` 624 (24 peças fechadas e convexas, 360 pontos de controle) + `Lid` 204 (13 peças, 128 pontos) |
| Dimensões / pivôs | Fechado 1,200 × 0,812 × 0,728 m (L × A × P; ficha ~1,2 × 0,8 × 0,7, a profundidade inclui pega e nós da dobradiça). Metros, +Y para cima, **+Z frente** (ficha). `Base` na origem = base central (fundo em y 0, centro XZ a 3,9 mm). `Lid` com pivô no **eixo da dobradiça** (0; 0,54; −0,342), rotação de repouso 0 = fechada |
| Partes | Dois nós Mesh de topo com os nomes da ficha, `Base` e `Lid` (sem nó raiz extra: a Unity os põe sob o objeto do arquivo e o caminho animado é `Lid`) |
| Materiais | **Dois**: `MODEL-CHEST_Stone` (pedra, URP/Lit fosco, metallic 0, smoothness 0,12, como `GA_Stone_*`) e `MODEL-CHEST_Brass` (latão gasto, metallic 0,45, smoothness 0,3). Os dois usam `../MODEL-CHEST_Palette.png` (16×16, células 4×4 px) como BaseMap; cada face aponta para o centro de uma célula. Índice de material por polígono |
| Paleta | pedra clara `#55596B` · pedra escura `#3E4152` · interior `#25262F` (derivado de `#3E4152`) · latão `#E0B040` · latão gasto `#9C7A30` (derivado de `#E0B040`). As derivadas estão registradas porque não constam da ficha |
| Take | `Open`, **0,6 s** a 30 fps (19 chaves), só `Lcl Rotation X` do `Lid`: 0° → destrava −4° (0,07 s) → passa do ponto −111° (0,43 s) → volta −103,5° (0,52 s) → assenta **−105°** (0,60 s). Segmentos smoothstep, passo máx. 14,8°/quadro. `Base` e raiz sem curvas: **in-place**. Sem loop |
| Revisão | `MODEL-CHEST.glb` (glTF 2.0: Base + Lid, cor por vértice, clip `Open` em quaternion; não é importado pela Unity, serve para DCC/revisão) |
| Fonte editável | `Tools/Lemo3DGenerator/src/production/ModelChest.mjs`: `DIM`, `SHAPE` (plinto, paredes/blocos, dobradiça, tampa, aro, cintas, fechadura, pega), `PALETTE`, `MATERIALS`, `CLIP` (chaves do take) |
| Validador + previews | `Tools/Lemo3DGenerator/src/production/ModelChest_preview.py`: relê FBX/PNG **do disco** (sem three.js), valida 17 itens (incluindo interpenetração ao longo do take), mede a leitura e gera as pranchas |
| Ferramenta | Node 24.18 + three.js r170 do Lemo-Opuscar (somente leitura: `ConvexGeometry`, `GLTFExporter`) e o vocabulário low-poly do `Lemo3DGenerator` (`geo.mjs`: `bevelBox`, hull convexo, normais de face). Python 3.11 + NumPy 2.4.6 + Pillow 13.0.0 no validador. Blender não está instalado; o "DCC" é o gerador por código, como em ITEMS-COMBAT e A08 |
| Determinismo | Duas execuções seguidas dão os mesmos SHA256 em FBX, `.meta`, PNG, GLB e `source.json`. Hashes em `ModelChest.validation.json` (FBX ASCII gerado com LF; com `core.autocrlf` o checkout pode virar CRLF — regenere para comparar) |

## Reproduzir

```bash
cd Tools/Lemo3DGenerator
node src/production/ModelChest.mjs           # malhas, FBX, GLB, paleta, .meta, source/generator JSON
node src/production/ModelChest.mjs --check   # só valida em memória
cd ../..
python -I Tools/Lemo3DGenerator/src/production/ModelChest_preview.py   # valida do disco + previews
```

As `.meta` têm **GUIDs estáveis** (SHA256 de `Eldoria/A11/<nome>`):

- **FBX:** ModelImporter de `Art/Items/Models/Loot_XpOrb.fbx.meta` (Unity 6000.6, serializedVersion 24600; escala 1, `useFileUnits`, sem colliders, normais importadas, `materialImportMode 2`). Mudam só GUID, `importAnimation: 1`, `animationType: 2` (Generic, como `Art/Models/Generated/*`) e `avatarSetup: 1`. `clipAnimations` fica vazio: a Unity cria o clip padrão a partir do take `Open` (sem loop por padrão).
- **Paleta:** template de `FX_Spark` com point, sem mipmaps, sem compressão, sem alpha, sRGB, tamanho máximo 32.

O validador sai com código 1 se alguma verificação falhar.

## Direção visual

- **Referências reais:** pedra `#55596B`/`#3E4152` e materiais `GA_Stone_*` (smoothness 0,10–0,15) de `_GameArt`; latão oficial `#E0B040` da paleta da direção de arte; baú F08 atual (1,4 × 0,8 × 0,85 m, frente em −Z, dobradiça atrás); câmera do jogo pitch 45° em −Z (`DungeonMvpSceneBuilder.CameraOffset (0, 14, −14)`); captura real `Reports/F08/Visual/chest-locked.png`.
- **Forma:** plinto chanfrado de pedra escura (1,2 × 0,7 m); paredes em blocos chanfrados (frente e trás em três blocos, lados em um) para ler alvenaria como as paredes da sala; interior oco e escuro com fundo. Duas cintas de latão na frente e duas atrás (mesmo X das cintas da tampa), espelho de fechadura com furo escuro. Tampa de quatro águas (faixa vertical de pedra escura, águas e topo em pedra clara, face de baixo escura) com aro de latão na frente e nos lados, duas cintas que passam por cima e descem até a dobradiça, pega larga (0,40 m) em barra sobre dois apoios. Sem caveiras, gemas, texto ou ornamento de baú comercial.
- **Dobradiça:** eixo X em y 0,54 (junta) e z −0,342, 2,7 cm atrás da pedra da tampa. Cada dobradiça tem um nó da tampa (no X da cinta) entre dois nós da base, coaxiais, com 6 mm de folga em X. Toda a geometria da tampa fora dos nós fica à frente do eixo (z local ≥ 0) e a cinta traseira da base termina abaixo do nó da tampa; por isso a tampa nunca entra na base ao girar.
- **Leitura pela câmera do jogo** (`ModelChest.validation.json`, baú girado 180° como na integração, rumos −30…+30°):
  - IoU da silhueta fechado × aberto: 0,636–0,641 (silhuetas bem diferentes); a área aberta é 1,40–1,42× a fechada.
  - Interior escuro visível: 54–58% da silhueta aberta (tampa por baixo + caixa) contra ≤ 0,3% fechada (só o furo da fechadura).
  - Latão: 18% da silhueta fechada. Contraste WCAG latão × piso sob a mesma luz: 5,02 (`#3E4152`) e 3,45 (`#55596B`).
  - Pedra: a cor média do baú fechado tem contraste 1,56 e **1,07** com o piso — a pedra oficial é a mesma cor do piso claro. Ver limitações.

## Aceite da ficha

| Critério | Evidência |
| --- | --- |
| Tampa não corta a base | SAT entre as 13 peças convexas da tampa e as 24 da base em 73 instantes do take (120 Hz + as 19 chaves, ângulo interpolado das chaves lidas do FBX, incluindo o pico de −111°): maior sobreposição **0,000 mm** (tolerância de contato 0,5 mm; o único contato é a tampa apoiada nas paredes em repouso). Uma primeira versão tinha 1,03 mm entre a cinta traseira da base e o nó da tampa; a cinta foi encurtada |
| Pivôs, escala, take | `pivo_base`, `pivo_tampa_dobradica`, `unidade_eixos`, `dimensoes`, `take_open` (17/17 em `ModelChest.validation.json`) |
| Silhuetas aberto/fechado legíveis | `aberto_fechado_distinguiveis` + `preview_ortho.png`, `preview_take_strip.png`, `preview_open.gif`, `review_f08_scale.png` |
| F08 decidido antes da produção final | F08 integrado (PRs #6/#7; `GAME_DESIGN.md` F08, `MVP.md`) |

## Pranchas (todas rotuladas PREVIEW)

Rasterizador próprio em NumPy, ortográfico, luz ambiente + difusa. Não são captura da Unity.

- `preview_ortho.png`: frente, lado, topo e câmera do jogo 45°, fechado e aberto.
- `preview_take_strip.png` / `preview_open.gif`: o take `Open` pela câmera do jogo, sobre o piso `#55596B`.
- `review_f08_scale.png`: A11 fechado e aberto em escala (~112 px/m) dos dois lados do baú de primitivas, sobre `Reports/F08/Visual/chest-locked.png`.
- `contact_sheet.png`: 45° fechado/aberto + paleta.

## Integração proposta (tarefa Codex separada)

Detalhes em `MODEL-CHEST.source.json` → `integration_proposal`.

- **Orientação:** o baú F08 tem a frente em **−Z local** (Latch em z −0,46, `dropOrigin` em z −1,2, câmera em −Z). O modelo segue a ficha (+Z frente). O filho `Visual` precisa de `localRotation` Y = 180°.
- **Tampa:** apontar `RoomRewardPresenter.lid` para o nó importado `Lid`. No espaço do modelo, `Euler(−105, 0, 0)` ergue a frente: é o valor que o presenter já aplica. O estado binário funciona sem mudar código. O take `Open` é opcional, sem eventos, disparado por `Opened`, e nunca entrega recompensa. `RestoreCheckpoint(true)` deve aplicar a pose final direto, sem tocar o take.
- **Primitivas:** desligar `Body`/`LidPivot`/`Latch`, ou criar variante/prefab novo por builder idempotente. Materiais URP compartilhados pelos cinco baús.
- **Collider:** simples e separado, se a regra pedir (~1,2 × 0,8 × 0,7 m). O FBX não traz colisor. Manter fora de spawns, portas e NavMesh crítico.
- **Testes:** F perto/longe, sala bloqueada/limpa, pausa, morte; reativação/reset sem novo sorteio; checkpoint aberto; pickups não colidem com a tampa aberta (recua até ~0,47 m atrás do centro); leitura nas cinco salas e no Boss.

## Achado no F08 atual (análise estática + captura existente; não corrigido aqui)

`RoomRewardPresenter.Present()` aplica `lid.localRotation = Euler(−105, 0, 0)` ao `LidPivot`, mas `RoomRewardSceneBuilder` monta o baú de primitivas com a **frente em −Z** (`LidPivot` em z +0,425, tampa estendida para −Z, `Latch` em z −0,46). Com a frente em −Z, X negativo gira a borda da frente **para baixo e para trás**: a tampa termina pendurada atrás do baú, em parte dentro do `Body`, em vez de ficar em pé. A captura existente `Reports/F08/Visual/chest-opened.png` é coerente com isso: a tampa laranja some da vista no estado aberto. Na `chest-locked.png`, fechada, ela aparece por cima.

Reproduzir: abrir a cena de release, limpar a sala 1, F perto do baú, olhar o baú pela câmera do jogo ou de lado na Scene view. Correção provável: +105° no presenter para o baú de primitivas. Com o A11 integrado (Visual a 180°, `lid` = nó `Lid`), os −105° atuais ficam corretos. As duas mudanças não devem coexistir. Fica para a tarefa Codex de integração/correção.

## Limitações

- **NÃO VALIDADO NO EDITOR.** Não foram observados importação, Generic/avatar, clip padrão do take, materiais criados pela MaterialDescription (conferir `_BaseMap`, metallic/smoothness), URP real, cena F08, presenter, collider e PlayMode.
- **Leitura da pedra.** A pedra oficial `#55596B` é a cor do piso claro (contraste médio 1,07). O baú se separa do chão pelo sombreamento das faces verticais, pelo plinto escuro, pelo latão (18% da silhueta) e, aberto, pelo interior escuro. Conferir em PlayMode nas cinco salas. Se faltar leitura, o ajuste preferido é trocar a célula da paleta (por exemplo, paredes em `#3E4152`) sem refazer a malha.
- Profundidade de 0,728 m e altura de 0,812 m, contra ~0,7/0,8 da ficha: a pega, os nós e o latão por cima da tampa somam 1–3 cm.
- O baú A11 é menor que o de primitivas (1,2 × 0,7 m contra 1,4 × 0,85 m), como pede a ficha. `dropOrigin` (z −1,2) continua à frente.
- Previews são do rasterizador próprio (ortográfico), não da Unity. A captura F08 de fundo é perspectiva, então a escala é aproximada.
- Aprovação artística humana pendente.
