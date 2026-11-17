# A05 — FX-LEVEL (subida de nível)

Destaque curto de **nível ganho** (`PlayerProgression.LeveledUp`). Um único anel ciano, fino e aberto atrás do Hero, sobe das botas até a cintura enquanto se estreita um pouco. Oito motes largos de latão acompanham pelas laterais. No fim, o anel se desfaz em facetas e some. O centro fica livre: não há disco, coluna de luz, texto, número nem personagem. É uma **fonte raster**: não é mecânica, prefab, ParticleSystem nem AnimationClip corporal.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-LEVEL-Sheet.png`: 1024×768 RGBA, grade de 4 colunas × 3 linhas com células 256×256, lida linha a linha (quadros 0–3, 4–7, 8–11). Tem `.meta` de textura de partícula, igual a `FX_Spark`/`FX-HEAL` |
| Quadros isolados | `frames/FX-LEVEL-F0.png` … `F11.png` (256×256 RGBA). Cada um é idêntico byte a byte à célula correspondente da sheet |
| Timing | `FX-LEVEL.timing.json`: 0,7 s, 12 quadros de 0,0583 s (~17,1 fps), sem loop. O quadro 11 é totalmente transparente. O JSON também traz altura e raio do anel por quadro |
| Pivô | Centro dos pés, em (128, 178) px a partir do canto superior esquerdo da célula. Na convenção da Unity (origem embaixo) fica em (0,5; 0,305). É o mesmo em todos os quadros |
| Escala | 128 px/m no plano dos pés, célula de 2,0 m. Anel de 0,58 m (botas) → 0,50 m (cintura, 1,0 m de altura). Motes a até 0,80 m do centro (limite da ficha: 0,8 m) |
| Câmera | Pitch 45°, offset (0, 10, −10), como `ArtTestSceneBuilder`/`DungeonMvpSceneBuilder`. Anel e alturas já vêm projetados (elipse 1 : 0,707) |
| Orçamento | 1 anel + 8 motes = 9 (≤ 20 partículas da ficha). Alpha máximo do anel 0,82 e dos motes 0,88. Cobertura com alpha > 50% abaixo de 6% da célula em todos os quadros |
| Fonte editável | `Tools/FxLevelGenerator/generate.py`. Edite `PAL` (cores), `RING` (raio, subida, largura, abertura, fade e dissolução por quadro), `MOTES` (posição, altura, subida, atraso, tamanho, deriva), `MOTE`, `PPM`, `PIVOT` e `DURATION`. Os parâmetros exportados ficam em `FX-LEVEL.source.json` |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG/GIF. Os hashes da sheet e dos quadros ficam em `FxLevel.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt
python Tools/FxLevelGenerator/generate.py
```

O script reescreve a sheet, `frames/`, as pranchas de revisão, `preview_floor.gif`, `FX-LEVEL.timing.json`, `FX-LEVEL.source.json` e `FxLevel.validation.json`. Ele **só cria a `.meta` quando ela não existe**, para preservar o GUID depois que a Unity importar. O template da `.meta` é o mesmo do A04 (`Tools/FxHealGenerator/texture.meta.template`). O script sai com código 1 quando alguma verificação falha. `--out <pasta>` grava só a sheet, os quadros e as pranchas fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais:**
  - Piso e Hero: captura `Reports/F05/Visual-final/1920-level.png`.
  - Orbe de XP: `ItemModels.mjs`, `M.xp` `#4FD8FF`, emissivo `#2FB8E8` e anel `M.xpRing` `#DFFBFF` (toro de 12 segmentos). O anel do FX usa os mesmos três tons, em facetas chapadas. A orbe não foi refeita.
  - Burst atual de nível do F05: `PlayerRewardFeedback.OnLevel`, 12 partículas de latão `Color(.9,.76,.42)`.
  - Telegraph do Boss: `Mat_TelegraphFill` `(1, .2, .15)` com borda `(0.35, 0.05, 0.05)`, disco preenchido no piso.
  - Altura do Hero: `CharacterController.height = 2` → cintura em ~1,0 m.
  - Câmera: builders citados acima.
- **Forma:** o anel é um polígono de 24 lados com planos largos, como o low-poly do projeto. Cada faceta tem tom chapado pela normal, com luz de cima/esquerda (mesma direção do UI-SKIN, dos ícones e do FX-HEAL). O arco de trás fica com 55% da opacidade do arco da frente para dar profundidade. Os motes são losangos verticais largos em 4 facetas de latão, com halo curto de 6 degraus.
- **Cor:** ciano `#4FD8FF`, sombra `#2FB8E8` e realce `#DFFBFF` no anel. Latão `#E0B040`, `#F2D27A` e `#B08A2A` nos motes. As faces escuras de latão ficam com matiz ≥ 40°, longe do laranja de ameaça (`#FF9A2E`, 32°). Não há branco opaco: nenhum pixel tem saturação < 0,18, valor > 0,92 e alpha > 200.
- **Sucesso, não perigo** (`review_danger.png` põe o pico ao lado de um telegraph):
  - o anel é fino, vazado e **aberto 120° atrás**. O telegraph é um disco vermelho preenchido e fechado;
  - o anel **sai do piso** já no quadro 1. O telegraph fica no chão;
  - ciano e latão nunca entram no matiz de ameaça de 0–36°.
- **Centro livre e combate visível:** o miolo do anel fica vazio em todos os quadros. Os motes ficam só nas laterais, com |x| ≥ 0,70 m, fora da coluna do corpo. Também não encostam no anel, porque latão sobre ciano mistura em verde. O alcance máximo é 0,8 m.
- **Por que o anel é aberto atrás:** no billboard, o arco de trás seria desenhado por cima do tronco do Hero. A abertura de 120° deixa só as pontas laterais, fora da silhueta (|x| > ~0,43 m).
- **Ritmo:**
  - quadro 0: anel nasce nas botas, ainda tênue;
  - quadros 1–8: sobe com início e assentamento suaves, no máximo 14 px por quadro, e os motes nascem perto da altura dele;
  - quadros 8–10: chega à cintura e se dissolve em facetas, de forma simétrica;
  - quadro 11: vazio.

## Verificações executadas (fora da Unity)

- `FxLevel.validation.json`: 18 checagens no PNG decodificado do disco, todas ok:
  - sheet 1024×768 RGBA e 12 quadros 256×256 RGBA, com célula k igual ao quadro k;
  - 0 pixels transparentes com RGB não nulo;
  - margem mínima de 23 px até a borda da célula, sem corte;
  - quadro 11 com alpha máximo 0, quadros 0–10 visíveis e quadro 10 abaixo de 30% do pico;
  - pivô coerente: nos quadros 0–7, o anel fica centrado em x = 128 e a base dele fica onde altura e raio mandam, com tolerância de 1,5 px;
  - **continuidade:** a base do anel anda no máximo 14 px entre quadros consecutivos (limite 16 px);
  - **centro livre:** miolo do anel (elipse a 75% do raio) com alpha 0 em todos os quadros, e nenhum mote na coluna |x| < 0,30 m;
  - 0 px verdes, 0 px saturados em matiz de ameaça, 0 px de branco opaco;
  - sem flash amplo;
  - alcance ≤ 0,8 m e 9 partículas ≤ 20.
- `asset_report.py --expect-size … --require-alpha --json` da skill `create-game-assets`: a sheet (1024×768) e os 12 quadros (256×256) saíram com código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Revisão visual** (composições só para revisão, não entregáveis):
  - `review_floor.png`: os 12 quadros sobre a captura real do F05 de nível, na escala estimada de gameplay (~75 px/m a 1080p, ~0,59×), ampliados 2×. O bloco de cima tem o Hero, o de baixo é piso limpo;
  - `review_backgrounds.png`: quadros sobre pedra `#3E4152`/`#55596B`, painel `#0E131A` e aço `#B9C4CF`, com cruz no pivô;
  - `review_danger.png`: quadro 4 ao lado de um telegraph vermelho de 1,2 m na mesma escala;
  - `preview_floor.gif`: animação a ~58 ms por quadro sobre o recorte do Hero.

  Ajustes feitos depois da revisão:
  - motes afastados de |x| ≈ 0,6 para ≥ 0,70 m e halo reduzido, porque latão sobre o anel gerava 110 px verdes;
  - curva de subida suavizada (meio linear, meio smoothstep), porque havia um salto de 18 px;
  - **removido o anel-rastro**, porque nos quadros 2–5 ele lia como um segundo anel, o que contraria "um único anel";
  - altura final de 0,95 para 1,0 m.

## Integração proposta (tarefa de código separada)

`FX-LEVEL.timing.json` → `unity_integration_proposal` descreve:

- **Material:** `URP/Particles/Unlit`, Transparent/Alpha, com a opacidade multiplicada pela opção de flash do F05 (0 desliga).
- **ParticleSystem:** 1 partícula, burst único em `LeveledUp`, `startLifetime` 0,7 s, `startSize` 2,0 m, simulação Local ao Hero (o anel acompanha o corpo enquanto sobe).
- **Texture Sheet Animation:** Grid 4×3, Whole Sheet, linear, 1 ciclo.
- **Renderer:** Billboard com `pivot` (0; 0,195; −0,2):
  - y alinha o pivô dos pés com a partícula;
  - z aproxima o quad ~0,4 m da câmera, para as pernas não esconderem o arco da frente;
  - **confira os dois sinais no Editor.**

Regras que a integração precisa manter:

- vários níveis no mesmo ganho de XP geram **um único restart** do mesmo emissor (Clear + Emit), sem cura, dano ou burst extra;
- a cura de `LevelUp` continua sem disparar o FX-HEAL;
- pausa congela;
- morte, reset e desativação limpam;
- não altera o Animator nem cria AnimationClip.

O burst atual de 12 partículas de latão pode ser substituído ou reduzido pela integração, mas isso é decisão da tarefa de código.

## Importação na Unity

A `.meta` da sheet usa o template do A04, que é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`, a textura de partícula já integrada:

- Default, sRGB, `alphaIsTransparency`, bilinear, clamp, mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

A sheet tem 1024×768, e 768 não é potência de 2. O template traz `nPOTScale: 1` (ToNearest), então a Unity deve **reescalar a altura para uma potência de 2** (512 ou 1024) na importação. A Texture Sheet Animation trabalha com coordenadas UV, então a grade 4×3 continua proporcional. As bordas das células, porém, deixam de cair em pixel inteiro, e as margens de ≥ 23 px absorvem esse filtro. Confira no Editor. Se a reescala for um problema, use `nPOTScale: 0` (None), sem mudar a sheet.

As margens de ≥ 23 px por célula toleram os mips usados a ~0,59× sem vazar entre quadros. O GUID da sheet e o da pasta são derivados do nome por SHA256: estáveis e novos. `Source~` é ignorada pela Unity, e os quadros isolados não são importados.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, ParticleSystem, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação. Grade, alpha, reset e flash reduzido precisam ser conferidos em PlayMode.
- **Sem integração.** `LeveledUp` continua com o burst procedural do F05. Ligar a sheet é uma tarefa de código separada, com builder e testes de pausa/reset/multinível/opções.
- `review_floor.png` usa a captura de nível do F05, que **já contém o burst procedural atual** (pontos de latão e ciano no piso). Eles não fazem parte desta sheet.
- **Altura da cintura:** 1,0 m vem do `CharacterController` de 2 m. Na captura, com a escala estimada, o anel termina na metade de baixo da capa, e o cinto parece mais alto. Ajuste `RING["rise_m"]` se o PlayMode confirmar isso. A margem de cima comporta até ~1,2 m.
- A escala in-game é uma estimativa a partir da captura. A leitura final depende do PlayMode.
- A ficha do manifesto sugeria `FX-LEVEL.prefab`. Ele **não foi criado** de propósito: prefab exige coordenação (CLAUDE.md), e o A05 pede só a fonte raster.
- Aprovação artística humana pendente.

## Procedência e direitos

Geometria, cores e render são próprios do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND). Não houve IA generativa de imagem nem asset de terceiros. A captura do F05 aparece só nas pranchas de revisão em `Source~`, nunca na sheet.

O prompt e o negative prompt da ficha A05 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem texto de nível, números, disco opaco, vermelho de perigo do Boss, explosão, câmera diferente por quadro, células cortadas, xadrez ou marca d'água.
