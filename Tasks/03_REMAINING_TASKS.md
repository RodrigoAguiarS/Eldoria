# Demais tasks — escopos separados

Status: backlog preparado; nenhuma execução autorizada automaticamente por esta lista. Fonte do diagnóstico: `MVP_AUDIT.md`.

| Task | Escopo exclusivo | Critério de aceite |
| --- | --- | --- |
| 03 — Armas e Missing Script | Separar WeaponData no arquivo correto, reparar assets via Editor preservando GUIDs e rastrear os avisos do executável. | Assets reais carregam; drops/equipamento funcionam; build sem os Missing Script diagnosticados; testes de assets reais passam. |
| 04 — Visual da fase 2 do Boss | Ligar cor/emissão e feedback de transição ao Warden visível, mantendo as regras de fases existentes. | Observar aos 50% uma única transição, 3 s de invulnerabilidade, novo visual no modelo e padrão preservado. Usa o modelo atual; imagens novas não são requisito. |
| 05 — Modelos, texturas e cenário 3D no jogo | Aplicar os assets visuais aprovados e criar/importar o kit modular necessário, preservando colisores, escala, portas, NavMesh e regras. Resolver atlas/UV e eventual migração Humanoid somente com escopo específico documentado. | Visual aparece no Editor e build; salas continuam navegáveis, portas funcionam, modelos/animações permanecem válidos. Imagens da Task 01 são referências, não substituem malhas/rigs. |
| 06 — Áudio | Sons de ataque, impacto, dano, morte, coleta e Boss, com reprodução e volume configuráveis. | Sons corretos no fluxo real, sem sobreposição excessiva nem erros. Não alterar combos, loot ou regras de fases. |
| 07 — Acessibilidade | Definir e implementar ajustes úteis, por exemplo intensidade de shake e legibilidade de perigos sem depender apenas da cor. | Opções verificáveis em jogo e legibilidade preservada. Escopo/configurações devem ser documentados antes da implementação. |
| 08 — VFX e desempenho | Feedback adicional, pools de efeitos repetidos e medição em Windows. | Pools reutilizam objetos; comparação CPU/GPU, GC, memória e frame time sob carga registrada; sem declarar meta de FPS que o projeto não definiu. |
| 09 — Git e reprodutibilidade | Inicializar repositório, conferir exclusões e versionar Assets/.meta, Packages, ProjectSettings e documentos. | Commit inicial reproduzível; caches/builds fora do histórico. Não mudar gameplay. |
| 10 — Aceite completo do MVP e balanceamento | Smoke test Bootstrap, arena, movimento/mira/dash/combate, morte/reinício, quatro salas, Boss e vitória; ajustes de valores fundamentados em playtest. | Checklists com evidências reais, testes pertinentes passando, percurso completo sem softlock/erros e limitações documentadas. |

## Dependências e ordem

- Task 01 (imagens/importação) e Task 02 (combos) são independentes, em arquivos separados. Task 02 não espera novas imagens.
- Task 03 deve preceder o aceite final de loot/build; Task 09 permite isolar as próximas alterações em branches.
- Task 05 usa os conceitos/texturas aplicáveis da Task 01. Aplicar imagens como conceitos no Project não conclui a substituição do cenário 3D.
- Task 04 e Task 02 podem envolver a mesma infraestrutura de arte/builders; executar sequencialmente quando houver sobreposição de arquivos.
- Tasks 06–08 e correções necessárias precedem o aceite final completo da Task 10. Registrar uma medição-base antes de otimizar.
- Não trabalhar simultaneamente no mesmo arquivo, cena, prefab ou ProjectSettings. Cada execução deve ter seu relatório e testes específicos.

Esta lista divide o trabalho; não transforma pendências em itens implementados nem autoriza um agente a executar todo o backlog em uma única task.
