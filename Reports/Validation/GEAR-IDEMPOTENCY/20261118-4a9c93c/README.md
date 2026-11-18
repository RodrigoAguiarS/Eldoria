# GEAR-IDEMPOTENCY — reaplicar ITEMS-GEAR sem regravar bytes

- Base: `main@4a9c93c` · branch `claude/gear-integrator-idempotent` · Unity 6000.6.9f1 (batchmode) · 2026-11-18
- Origem: falha anterior à V06 (`Reports/Validation/V06/20261118-4a9c93c/gear-baseline/` na branch `claude/v06-dash-trail`).

## Diagnóstico

`GearArtTests.ReapplyingBothItemIntegrators_KeepsGearAndCombatArtByteIdentical` falhava 4/5 (`baseline/GearArtTests-4a9c93c.xml`).
Depois de uma reaplicação, `Ground_Item_CopperAmulet.prefab`, `Ground_Item_LeatherGloves.prefab` e `ItemVisualCatalog.asset`
tinham **conteúdo idêntico ao commit, exceto o fim de linha**: `git diff` vazio, `tr -d '\r'` igual, `git ls-files --eol`
passando de `w/crlf` para `w/lf`. Os fileIDs, GUIDs, pose, materiais e a ordem do catálogo ficaram iguais.

- O checkout usa `core.autocrlf=true` (no gitconfig do sistema do Git for Windows) e não há `.gitattributes`, então os YAML
  da Unity ficam CRLF no disco. Qualquer regravação pela Unity grava LF, o que muda o hash.
- **Prefabs:** `GearVisualIntegrator.Matches` comparava `child.localEulerAngles == euler`. A Unity guarda só o quaternion
  e devolve os ângulos em [0, 360), então as poses -28° (luvas) e -20° (amuleto) voltavam como 332°/340°. A comparação
  falhava e o prefab era salvo de novo a cada execução. O elmo e as botas (0°) não eram regravados.
- **Catálogo:** `EditorSet` + `SetDirty` eram incondicionais nos dois integradores, então o `SaveAssets` regravava o catálogo.
- **ITEMS-COMBAT:** `UpsertItemMaterials` chamava `SetDirty` em todo material, incluindo o `Item_steel.mat` rastreado.
  Com o gear corrigido, a segunda asserção do teste (depois de `ImportAssets`) passaria a falhar por esse motivo.

## Correção

- `GearVisualIntegrator.Matches`: a rotação passa a ser comparada como quaternion (`Quaternion.Angle < 0.01°`). A função
  também confere as configurações do renderer (sombra e probes) e que `Model` não tem filhos.
- `GearVisualIntegrator.Materials`: um material criado ou com o shader trocado conta como alteração, e `_EmissionColor` é
  conferida. `SetDirty` só é chamado quando algo muda.
- `ProductionVisualIntegrator.SetCatalogIfChanged` (compartilhado): compara cada entrada (assetId, item, icon, ground e
  equipped), além da contagem e da ordem, antes de `EditorSet`/`SetDirty`. É usado pelos dois integradores.
- `ProductionVisualIntegrator.UpsertItemMaterials`: compara antes de gravar.
- A arte, os GUIDs, os stats e a ordem do catálogo não mudaram (nenhum asset é commitado nesta branch).

## Evidência executada

| Execução | Resultado |
|---|---|
| `baseline/` GearArtTests @4a9c93c | 4/5 (falha reproduzida) |
| `fix/` GearArtTests run1, run2 | 5/5, 5/5 |
| `fix/h0→h1→h2.sha256` (114 arquivos de `Art/Items` + catálogo + `.meta`) | gear, catálogo e materiais `Item_*` inalterados em h0→h1; h1 = h2 |
| `full/` EditMode completo | 345/345 |
| `full/` PlayMode completo (`DUNGEON_CRAWLER_TEST_REPORT_DIR=full/perf`) | 226 passed, 0 failed, 20 skipped |

O diff h0→h1 mostra só os prefabs `Ground_/Equip_` do ITEMS-COMBAT, porque `UpsertMeshPrefab` sempre salva de novo.
Os `FX_*.mat` não entram no hash, mas também são regravados (por `UpsertFxMaterials`). Nos dois casos é só o fim de linha
(`fix/side-effects-after-run1.txt`, todos `eol-only`). Esses arquivos não são rastreados pelo teste e foram restaurados.

### `full-attempt0/` (descartada, mantida como registro)
- EditMode 344/345: `DestructiblePropAssetTests.Builder_Idempotent_...` falhou em `GameOptions.asset` (esperado `\r`
  no byte 9, veio `\n`). A causa é a mesma do CRLF num checkout recém-feito, e não tem relação com os integradores de itens.
  Passou na reexecução, quando o arquivo já estava LF.
- PlayMode: `RewardFeedbackPlayModeTests.WarmCallbacks...` falhou com "Preserve existing measurements" porque a variável
  `DUNGEON_CRAWLER_TEST_REPORT_DIR` não estava definida (erro de execução meu). Passou com o diretório isolado.

## Efeitos colaterais restaurados
Restaurados com `git checkout` depois das execuções: os prefabs/FX do ITEMS-COMBAT, as cenas e os prefabs dos builders,
`ProjectSettings/*` (incluindo `SENTIS_ANALYTICS_ENABLED` em `ProjectSettings.asset`), `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
e os `Reports/Leadership|Task02`. Em `*.log`, os arquivos do `.gitignore` não são versionados, e o `CombatProfiler.raw` (2,4 MB) foi removido.

## Pendências (fora do escopo)
- `ProductionVisualIntegrator.UpsertMeshPrefab` e `UpsertFxMaterials` continuam regravando sempre.
- Um `.gitattributes` com `eol=lf` para os YAML da Unity (`*.prefab *.unity *.asset *.mat *.meta …`) acabaria com toda essa
  classe de falha dependente de checkout (como a do `DestructibleProp`). É uma mudança de política do repositório, a decidir.
