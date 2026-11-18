# SLASH-TRAIL — rastro de espada entre golpes (ITEMS-COMBAT)

- Base: `main` 4a9c93c (`base-commit.txt`). Unity 6000.6.9f1, Windows 11, Direct3D12.
- Cena: `Assets/_Project/Art/Models/Generated/_GameTest/Floor_01_GeneratedCharacters.unity` (release).
- Pergunta: o `SlashTrail` de `PlayerAttackVfxPresenter` (só `trail.emitting = false` no fim do golpe) desenha rastro
  quando o Hero anda ou fica em idle entre golpes? Hipótese vinda das sondas da V06 (branch `claude/v06-dash-trail`,
  `Reports/Validation/V06/20261118-4a9c93c/trail-probe`).

## Resultado: NÃO reproduz visualmente. Nenhuma mudança de código.

Os contadores crescem, mas a imagem não muda:

| Situação (presenter da `main`) | `positionCount` | vértices (BakeMesh) | pixels alterados pelo rastro (1280×720) |
|---|---|---|---|
| Impacto do golpe (controle) | 11–21 | 44–84 | **765–876** |
| Idle logo após `AttackFinished` | 25 | 100 | **0** |
| Idle `trail.time`+0,05 s / 0,7 s depois | 1 / 0 | 0 / 0 | 0 / 0 |
| Andando 1 s depois do golpe (A, S, W+D) | 1–30 | 0–120 | **0** |

Medida: `pixel-probe/main*/trail-pixels.txt`. A sonda renderiza a câmera principal duas vezes no mesmo quadro, com o
TrailRenderer ligado e desligado, e conta os pixels que mudam (Editor gráfico, não batchmode).

Leitura: na Unity 6000.6 os pontos gravados enquanto `emitting = false` entram em `positionCount` e no `BakeMesh`
(alfa das cores até 0,80), mas o renderer não os desenha: são a "lacuna" do rastro. Os pontos emitidos no golpe
envelhecem normalmente depois dele, porque a animação de idle/locomoção move o socket da mão. Assim, as conclusões das
sondas da V06 que se baseiam só em `positionCount`/`BakeMesh` não provam rastro visível. O caso do dash (cauda emitida que
não envelhece com o Hero parado) é outro e não foi reavaliado aqui.

## Evidências

- `playmode-main/`: testes novos com o presenter da `main` (batchmode com GPU). Falham porque verificam contadores:
  andando 37 pts/148 vts antes e depois do golpe, idle 3 pts. Ver `[SLASH-TRAIL]` no log.
- `playmode-main-attempt0-animator-culled/`: primeira tentativa. O Animator do Hero usa `CullUpdateTransforms` e o
  batchmode não renderiza a Game view, então os ossos (e a espada) não se movem: pico de 0 pontos no golpe. Por isso o teste
  força `AlwaysAnimate` só na instância da cena.
- `playmode-hardening/`: os mesmos testes mais 3 de `ItemsCombatSceneTests` (golpe, pausa/morte/anel, equipamento) com a
  correção opcional: 6/6 passaram. O wrapper `timeout` perdeu o código de saída do Unity (`exit=127`); o XML é a fonte.
- `graphical/main` e `graphical/hardening`: capturas no Editor gráfico a 2560×1440 (impacto do golpe e andando 0,6–1 s
  em 4 direções), com `diagnostics.txt` (pontos, vértices e retângulo da faixa na tela).
  `compare-main-vs-hardening-zoom.png` amplia a região onde a faixa estaria: nenhuma diferença visível.
- `pixel-probe/`: medida de pixels acima (`main`, `main-idle`, `hardening`).
- `source/SlashTrailSceneTests.cs.txt`: fonte dos testes e sondas usados (não ficam em `Assets/`).
- `source/optional-presenter-hardening.diff`: correção opcional, no mesmo padrão do `DashTrailPresenter`: renderer
  desligado fora da janela, cauda congelada e limpeza após `trail.time`. Não muda o que aparece na tela; só zera os
  contadores e evita pontos invisíveis. Está na branch local `claude/slash-trail-lifecycle-hardening` (não enviada).

## Efeitos colaterais das execuções

- Cada execução do Unity reescreveu `ProjectSettings/*.asset` só com fins de linha (`git diff --ignore-space-at-eol`
  vazio); restaurados com `git checkout`.
- Filtrar `-testFilter` pelo nome da classe também executa testes `[Explicit]`. Em batchmode,
  `ItemsCombatSceneTests.Capture_ReleaseFloorEvidence` fica preso em `WaitForEndOfFrame`; a execução foi encerrada
  e repetida com os nomes dos testes.
- As suítes completas não foram executadas, porque nenhum código foi alterado.
