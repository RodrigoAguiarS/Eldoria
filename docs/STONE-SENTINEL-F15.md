# Sentinela de Pedra — F15 experimental

Protótipo de **uma fase**, sem ligação com os andares de release, XP, drops ou
checkpoint. O Guardião mantém duas fases, limiar de 50% e transição de 3 s.
Não é arte final. O conceito 2D A14 existe desde 17/11/2026
(`Assets/_Project/Art/Concepts/StoneSentinel_Concept_01.png`) e segue a escala desta primitiva, mas não
há malha, rig nem take. [Proposta anterior ao código](../Reports/F15/PROPOSAL.md).

Abrir `Assets/_Project/Scenes/F15_StoneSentinel_Test.unity`. Entrar pelo sul
ativa a sala; a Sentinela aparece, portas fecham e a barra surge durante a luta.
WASD, mira, combo, dash, giro, bolsa e pausa usam os controles existentes e seus
bindings. **Reiniciar protótipo** ou R recarrega somente a arena e não toca o
checkpoint da run normal. O menu da release não oferece acesso a esta cena.

| Parâmetro em `Boss_StoneSentinel_Data` | Proposta inicial |
| --- | --- |
| Vida | 240 HP |
| Movimento / decisões | 2,4 m/s / 0,2 s |
| Introdução | 1,5 s |
| Leque | 100°, 6 m |
| Dano | 24, antes da defesa |
| Aviso | 1,4 s; código garante ≥1 s |
| Recuperação / intervalo adicional | 1,1 s / 1,2 s |
| Tolerância vertical | 2 m entre raízes |

O ataque começa a ≤85% do alcance. A Sentinela para, olha para o jogador e
trava origem/direção/área/dano/ritmo daquele golpe. O setor escuro mostra a área
inteira; o laranja cresce até a borda. A resolução consulta **uma vez** o centro
do jogador no plano XZ: raio e bordas angulares inclusivos, sem aumentar o setor
pelo tamanho dos colliders. O mesh tem 96 segmentos; a diferença entre arco
matemático e segmentos visuais é inferior a 0,3 mm a 6 m.

Sair pelas laterais, para trás da Sentinela ou além do raio evita dano. Dash
invulnerável consome o único contato sem dano tardio. Após o impacto, a
recuperação explícita permite contra-ataque. Não há onda viajante, projéteis,
consulta global ou dano por animação/telegraph. Arena aberta, sem pilares:
este protótipo não define propagação do leque através de cobertura.

Morte/desativação do atacante/alvo ou pausa cancelam o aviso e param a
navegação. O cooldown comprometido não é reembolsado; ao retomar é preciso um
aviso completo novo. Reativar não cura/revive nem resolve ataque antigo. Morte
desliga corpo/agente e mantém a primitiva morta na arena até reiniciar; última
morte conclui a sala/andar uma única vez. HUD desaparece com morte, desativação
ou fim do encontro. Pausa mantém o HP visível, mas cancela o golpe.

Builder: **Tools → Dungeon Crawler → Boss → Build Stone Sentinel Prototype (F15)**
(`DungeonCrawler.Editor.StoneSentinelSceneBuilder.Build`). Cria apenas assets
F15, preserva dados/layout/GUIDs ao reexecutar e usa shell/jogador existentes.
A composição não reconstrói o Guardião, cenas de release nem loot global.
Fora das Build Settings de release. Build própria pelo menu **Build → Windows
Stone Sentinel Validation (F15)**, em `Builds/F15/Eldoria.exe`, só com a arena;
reexecuções preservam build/relatório anteriores em subpastas Windows com timestamp.

Roteiro: entrar e observar aviso/área, sair de cada borda, ficar dentro e receber
um impacto, dash no impacto, pausar no aviso, matar/desativar no aviso, reativar,
vencer, verificar HUD/portas/vitória única e reiniciar com HP/sala novos.
Repetir o contrato de duas fases no Guardião existente. Evidências e limites da
execução em [Reports/F15](../Reports/F15/README.md). Números não são balanceamento
aprovado; novas fases/padrões, malha/rig a partir do A14 e integração no andar exigem tarefa posterior.
