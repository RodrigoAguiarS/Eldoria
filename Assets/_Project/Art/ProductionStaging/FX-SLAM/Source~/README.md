# A10 — FX-SLAM (impacto do Slam do Guardião)

Uma **spritesheet de chão** para o rescaldo do Slam já resolvido: um anel baixo e fino de poeira de pedra que sai do pé do Guardião e chega ao **raio real do golpe**, com borda laranja apagada, tufos de poeira quebrados logo atrás da frente e seis lascas angulares de pedra que saem do centro, giram e somem. O centro fica aberto, o piso continua legível e o último quadro é vazio: **nada persiste no chão**.

É só apresentação. Não é aviso: o telegraph existente continua igual e já está escondido quando o golpe resolve. Também não é área de dano nem novo ataque, e não traz shake embutido. Não é prefab, ParticleSystem configurado nem integração.

| Item | Valor |
| --- | --- |
| Arquivo final | `../FX-SLAM-Sheet.png`: 1024×512 RGBA, 8 quadros 256×256 em grade 4×2, com `.meta` de textura de partícula (template de `FX_Spark`/A04, sem mudanças) |
| Quadros individuais | `frames/FX-SLAM-F0.png` … `F7.png` (iguais às células da sheet, conferido byte a byte) |
| Tempo | **0,35 s** (ficha: 0,25–0,45 s), 8 quadros de 0,04375 s, sem loop. Quadro 7 totalmente transparente |
| Vista | Ortográfica de cima, plano do piso, para um quad deitado (`Horizontal Billboard`). Centro fixo em (128, 128) em todos os quadros |
| Escala ligada ao raio real | Na célula, **1 R = 116 px** do centro. O anel termina em 0,985 R e nada passa de 1,0 R. **Lado do quad = R × 2,206897**: **7,724 m** para R = 3,5 m (fase 1) e **10,041 m** para R = 4,55 m (fase 2 = 3,5 × 1,3). Os dois valores são lidos de `Boss_Warden_Data.asset` pelo gerador |
| Orçamento | 1 partícula por golpe (ficha: ≤ 24). O anel, os 28 tufos e as 6 lascas estão desenhados na sheet |
| Fonte editável | `Tools/FxSlamGenerator/generate.py`: `PAL` (cores), `RING` (raio, largura, alpha e laranja por quadro, bandas, recorte facetado), `PUFF` (tufos de poeira), `CHIPS`/`CHIP` (ângulo, percurso, tamanho, giro, arco, sombra, aresta quente), `CENTRE_OPEN_R` e `UNIT_PX`. Parâmetros também em `FX-SLAM.source.json` |
| Ferramenta | Python 3.11.9 + Pillow 13.0.0 + NumPy 2.4.6. Polígonos com supersampling 8×, redução BOX com alpha pré-multiplicado e RGB zerado onde alpha = 0 |
| Determinismo | Duas execuções seguidas deram o mesmo SHA256 em todos os PNG e no GIF. O hash está em `FxSlam.validation.json` |

## Reproduzir

```bash
python -m pip install -r .claude/skills/create-game-assets/scripts/requirements.txt numpy
python Tools/FxSlamGenerator/generate.py
```

O script reescreve a sheet, `frames/`, as pranchas de revisão, `preview_slam.gif`, `FX-SLAM.timing.json`, `FX-SLAM.source.json` e `FxSlam.validation.json`. As `.meta` (textura e pasta) **só são criadas se não existirem**, para preservar o GUID depois que a Unity importar. O script sai com código 1 quando alguma verificação falha. `--out <pasta>` grava a sheet, os quadros e as pranchas fora de `Assets/`, para iterar sem disparar importação. `asset_report.json` e `contact_sheet.png` vêm dos scripts da skill `create-game-assets` (veja "Verificações").

## Direção visual

- **Referências reais, só lidas:**
  - **`BossBrain.cs`:** `SlamRadius` = `slamRadius` × `phase2SlamRadiusMultiplier` na fase 2. `ResolveAttack` chama `telegraph.Hide()` e **depois** `ResolveSlam()`, que mede a distância no plano e aplica dano.
  - **`BossAttackTelegraph.cs`:** disco externo `Mat_TelegraphOuter` (0,35; 0,05; 0,05) e preenchimento `Mat_TelegraphFill` (1; 0,2; 0,15), mínimo de 1 s.
  - **`Boss_Warden_Data.asset`:** raio 3,5, multiplicador 1,3, aviso 1,4 s / 1,0 s, `recoverTime` 0,8 s, cooldown 2,5 s / 1,6 s.
  - **Paleta:** pedra `#55596B`/`#3E4152` da direção de arte; ferrugem `#8A4A22` e laranja `#FF9A2E` do A08/A09.
- **Anel:**
  - polígono de 28 lados com recorte facetado simétrico em ponto. O recorte parece irregular, mas mantém o centroide exato (0,000 px de desvio), então não há tremor embutido;
  - três bandas, de fora para dentro: a borda laranja apagada (`#C4773D`, que esfria para a poeira a partir do quadro 2), o núcleo de poeira clara `#A2A6B4` com o alpha mais alto da faixa e um rastro mais escuro e fraco;
  - raio externo por quadro: 0,38 → 0,60 → 0,76 → 0,86 → 0,92 → 0,96 → 0,985 R (ease-out). O alpha cai de 0,94 para 0,13.
- **Tufos de poeira:** em ~70% dos segmentos, um aglomerado facetado logo atrás da frente do anel, com alpha de 0,40 × o do anel. É o que faz o anel ler como poeira, e não como contorno.
- **Lascas:**
  - 6 estilhaços alongados de 4–5 vértices, com platô claro e faces pela luz de cima/esquerda;
  - a aresta voltada ao centro é ferrugem/laranja apagado (calor do impacto);
  - têm uma sombra no piso que cresce no topo do arco;
  - saem de ~0,32 R e param entre 0,52 e 0,72 R: o anel sempre passa à frente delas;
  - comprimento no quadro 2: 0,47–0,75 m na fase 1 e 0,61–0,97 m na fase 2.
- **Centro aberto e piso legível:**
  - alpha 0 dentro de 0,22 R em todos os quadros;
  - a área com alpha > 50% cobre no máximo 6,7% da célula;
  - o alpha médio dentro de R fica em no máximo 0,104;
  - contraste WCAG da poeira composta: 2,47 sobre `#55596B` e 3,53 sobre `#3E4152`.
- **Interpretação da ficha:** "alpha alto no centro" foi lido como alpha alto no **centro da faixa do anel**, porque o prompt pede "Keep the centre open and the floor readable" e o centro do disco fica sob o próprio Guardião.

## Verificações executadas (fora da Unity)

- `FxSlam.validation.json`: 27 checagens nos quadros e na sheet, todas ok:
  - sheet 1024×512 e quadros 256×256 RGBA; cada célula igual ao quadro; 0 pixels transparentes com RGB sujo; margem mínima de 14 px na célula;
  - **fim limpo:** quadro 7 com alpha máximo 0; quadros 0–6 visíveis; o quadro 6 tem 12% da soma de alpha do pico;
  - **centro aberto** (alpha 0 < 0,22 R); **piso legível** (cobertura > 50% ≤ 10%; alpha médio em R ≤ 0,16);
  - **centro fixo, sem shake embutido:** centroide do anel a 0,000 px de (128, 128) em todos os quadros;
  - o anel se expande de forma monotônica e termina entre 0,95 e 1,0 R; as lascas ficam em ≤ 0,85 R (máx. 0,755 R); 6 lascas;
  - **cores:** 0 px verdes, 0 px azul/ciano saturado (Hero), 0 px vermelho saturado de telegraph; laranja em 8,6% dos pixels visíveis (limite: 25%);
  - poeira legível sobre a pedra (WCAG ≥ 1,8);
  - **tempo:** 0,35 s dentro do envelope. O efeito é menor que `recoverTime` (0,8 s) e que o menor cooldown (1,6 s), então **nunca coexiste com o próximo aviso**. Aviso de 1,4 s / 1,0 s ≥ 1 s;
  - raios 3,5/4,55 m lidos do asset real;
  - orçamento de 1 partícula;
  - `BossBrain.cs`, `BossAttackTelegraph.cs`, `BossData.cs` e `Boss_Warden_Data.asset` sem alterações (`git status`).
- `asset_report.py --expect-size … --require-alpha --json` da skill `create-game-assets`: a sheet (1024×512) e os 8 quadros (256×256) saíram com código 0 e `problems: []` (`asset_report.json`). `contact_sheet.png` vem de `build_preview_sheet.py`.
- **Prévias** (todas rotuladas *PREVIEW*, só para revisão, não entregáveis):
  - `review_floor_preview.png` (a prévia de 45° da ficha): a captura real da arena do Boss (F08, 2560×1440). O centro do golpe fica 2 m à frente do Hero, posição em que o Slam dispara (≤ 0,85 R). A coluna 0 é uma aproximação 2D do aviso existente; depois vêm os quadros 0–7. Linha 1: R 3,5 m; linha 2: R 4,55 m. O quad é projetado no piso por aproximação ortográfica local (~122,5 px/m), e o Hero e o baú são redesenhados por cima, como o depth test faria na Unity;
  - `review_floor_zoom.png`: quadro 2, fase 2, com o Hero dentro do raio, para avaliar o "não encobre o player";
  - `review_topdown.png`: a fonte vista de cima sobre pedra `#3E4152`/`#55596B`, painel `#0E131A` e aço `#B9C4CF`. Na primeira linha, guias de 1,0 R e do centro aberto;
  - `preview_slam.gif`, 2× mais lento:
    - aviso existente (aproximado) por 1,0 s, `Hide`, impacto e repouso;
    - **segunda ativação** reinicia do quadro 0;
    - **pausa** congela o quadro 3;
    - **cancelamento/morte** chama `Clear()` e o efeito some;
    - reuso completo.

  Ajustes feitos depois da revisão:
  - o recorte do anel passou a ser simétrico em ponto (antes o centroide desviava 1,5 px);
  - entraram os tufos de poeira (antes o anel lia como um contorno geométrico limpo);
  - as lascas ficaram alongadas e 28% menores. Antes pareciam pastilhas e chegavam a 1,36 m na fase 2, do tamanho do Hero.

## Integração proposta (tarefa Codex separada)

`FX-SLAM.timing.json` → `unity_integration_proposal` descreve:

- **Hook:** um evento público só de apresentação no `BossBrain`, por exemplo `event Action<Vector3, float> SlamResolved`. Ele é disparado em `ResolveAttack` logo depois de `ResolveSlam()`, com `(transform.position, SlamRadius)`, acertando o Hero ou não. O efeito só escuta: nunca chama `ResolveSlam`/`TakeDamage` nem muda tempos, raios ou estado. O evento não é publicado em `EnterIdle`, na morte, na transição de fase nem no cancelamento do windup.
- **Material:** um `sharedMaterial` `URP/Particles/Unlit`, Transparent/Alpha, cor branca, sem emissão nem bloom.
- **ParticleSystem:**
  - um só, reutilizado, com `maxParticles` 1, sem loop nem `playOnAwake` e `Emit(1)` por golpe;
  - `startLifetime` 0,35 s; `startSize` = **lado do quad calculado do `SlamRadius` recebido**, nunca um tamanho fixo;
  - Texture Sheet Animation em grade 4×2, Whole Sheet, linear 0→1, 1 ciclo;
  - renderer `Horizontal Billboard` a y = 0,04, entre o piso e o telegraph (0,03/0,05), que já está escondido.
- **Reset e reuso:**
  - duas ativações rápidas: `Clear()` + `Emit(1)` no novo centro e raio;
  - pausa: tempo escalado (conferir que congela com `timeScale` 0);
  - morte do Boss, reset do andar, desativação e retorno de pool: `Stop(true, StopEmittingAndClear)`/`Clear()`.
- **Flash/shake:** o efeito não adiciona flash nem trauma de câmera, e as opções existentes ficam intactas.
- **Testes sugeridos para a tarefa Codex:** o evento dispara uma vez por Slam resolvido e nunca no cancelamento; dano e tempos idênticos com e sem o efeito; duas ativações, pausa, morte e reset deixam 0 partículas vivas; o telegraph continua com ≥ 1 s.

## Importação na Unity

A `.meta` da textura parte do template do A04 (`Tools/FxHealGenerator/texture.meta.template`), que é cópia de `Assets/_Project/Art/Items/VFX/FX_Spark.png.meta`:

- Default, sRGB, `alphaIsTransparency`, bilinear, **clamp**, mipmaps on;
- padrão 2048, Standalone/WebGL com compressão normal.

1024×512 é potência de 2. O GUID da textura e o da pasta são derivados por SHA256 (`Eldoria/A10/…`): estáveis, novos e sem colisão com nenhuma `.meta` do projeto. A Unity ignora `Source~`.

## Limitações e pendências

- **NÃO VALIDADO NO EDITOR.** Não houve importação, material, ParticleSystem, PlayMode nem captura. A Unity pode reserializar a `.meta` na primeira importação.
- **Sem integração:** em jogo, o Slam continua sem efeito de impacto. O hook `SlamResolved`, o ParticleSystem e os testes de reuso, pausa e cancelamento são tarefa Codex separada, que mexe em código, cena e prefab.
- **Orientação:** confira no Editor a orientação de +Z da textura no `Horizontal Billboard`. O efeito é quase radial; só a luz das facetas (de cima/esquerda da imagem) depende disso.
- **Mipmaps:** com o quad de 7,7–10 m, a textura aparece perto de 1:1 na tela, mas o anel fino pode perder detalhe em mips altos. Conferir na resolução real.
- **Escala única:** como a mesma sheet serve aos dois raios, as lascas e a espessura do anel crescem 30% na fase 2. Isso foi aceito como leitura de golpe maior; se incomodar, basta uma variante com `CHIPS` menores.
- **As prévias não são captura da Unity:** projeção ortográfica local sobre a captura F08, sem perspectiva real, luz, bloom nem ordenação de transparências. O Guardião não aparece porque a captura é posterior à vitória, e o centro foi posto onde ele estaria. O aviso da coluna 0 e do GIF é uma aproximação 2D das cores dos materiais.
- **Prefab não criado:** a ficha do manifesto sugeria `FX-SLAM.prefab`, mas criar prefab exige coordenação (CLAUDE.md), e o A10 pede a spritesheet e a fonte.
- **Aprovação artística humana pendente.**

## Procedência e direitos

Geometria, cores e render são do projeto Eldoria, gerados pelo script acima com Pillow (licença MIT-CMU/HPND) e NumPy (BSD). Não houve IA generativa de imagem nem asset de terceiros. A captura F08 aparece só nas pranchas de revisão em `Source~`, nunca na sheet. As legendas das pranchas usam a fonte do sistema (Segoe UI/Arial), só nas prévias.

O prompt e o negative prompt da ficha A10 (`PROMPT_CATALOG.md`) serviram de brief e não foram enviados a nenhum gerador. Itens do negative prompt verificados: sem poça de lava, novo ataque, disco opaco, texto, shake embutido, explosão gigante, xadrez nem marca d'água.
