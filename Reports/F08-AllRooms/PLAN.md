# Correção F08 — todas as salas e tochas

Base: `77e4b13` (`main` atualizada). Branch: `codex/f08-all-rooms-light-fix`.
Pedido do usuário: um baú em todas as salas e corrigir as luzes/chamas no centro.

## Diagnóstico e incremento

- Integrar um baú independente em cada uma das cinco salas fixas da cena de
  release, incluindo Boss, preservando a instância original da primeira sala.
  Cada um continua bloqueado até a própria sala limpar; F próximo e sorteio único.
- Avisos separados por baú para impedir que presenters distantes apaguem o aviso
  de outro baú. Conservar recompensa, tabelas, coleta e remapeamento existentes.
- Garantir acesso ao baú do Boss depois da vitória com botão para voltar à sala
  e coletar recompensas; vitória 5/5 e notificações de domínio não mudam.
- O snapshot anterior de auditoria já contém 30 Torch_FlameAnimated órfãs nas
  raízes das salas, em posição local zero. TorchLight continua nas paredes.
  Inspecionar a cena atual por API e reparar somente as instâncias das chamas,
  usando a integração existente, sem reposicionar tochas ou luzes pontuais.
- Investigar/corrigir a reincidência: DungeonMvpSceneBuilder.Build chama
  ApplyEnvironment global, recriando decoração de todos os prefabs. O teste M1
  restaura prefabs mas não a cena gerada da release. Limitar arte do builder M1
  à própria arena; ampliar snapshot/restauração do teste e verificar byte a byte
  salas/cena de release durante o rebuild. Sincronizar fonte Tools e cópia instalada.

## Arquivos reservados e validação

RoomRewardSceneBuilder, RoomRewardPresenter/avisos, ProductionAnimationIntegrator,
GeneratedArtApplier (Tools/cópia), DungeonMvpSceneBuilder, VictoryPresenter e
GameMenuController se necessário; testes de composição, preservação M1 e PlayMode.
Documentação essencial e evidências novas apenas em Reports/F08-AllRooms.

Unity 6000.6.9f1 está aberto no projeto original. Compilar/integrar/testar em cópia
isolada, conferir hashes antes/depois e transferir somente a cena validada. Não
salvar/reconstruir a sessão do usuário. Aceite: cinco baús independentes, trinta
chamas nas tochas, dez luzes pontuais preservadas, idempotência, regressão M1 sem
efeito sobre salas/release, PlayMode gráfico por sala e build Windows isolada.
Commit/push/MR para main; sem merge sem aprovação.
