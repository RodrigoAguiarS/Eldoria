# Linha de base de balanceamento — candidato RC2

Valores inspecionados no candidato `b288c29`. Este registro documenta as regras vigentes e os resultados disponíveis; não estabelece uma duração de partida nem altera os dados aprovados em D-002.

## Jogador e progressão

HP 100, dano base 20. Dano = `(20 + bônus da arma) × (1 + 0,1 × (nível − 1)) × multiplicador do golpe`. Armas Ferro/Guardião/Ancestral concedem +6/+12/+18; raridade não multiplica dano. Cada nível cura 40% do HP máximo, limitado ao máximo. XP para o próximo nível parte de 30, cresce por 1,35 e é arredondado pelo modelo; nível máximo 10.

| Exemplo | Attack_01 ×1 | Attack_02 ×1,1 | Attack_03 ×1,5 | Especial ×2 |
| --- | --- | --- | --- | --- |
| Nível 1, sem arma | 20 | 22 | 30 | 40 |
| Nível 1, Ferro +6 | 26 | 28,6 | 39 | 52 |
| Nível 3, Ferro +6 | 31,2 | 34,32 | 46,8 | 62,4 |
| Nível 5, Guardião +12 | 44,8 | 49,28 | 67,2 | 89,6 |

O último exemplo ilustra a fórmula; a Lâmina do Guardião cai do Boss e não deve ser presumida disponível para enfrentá-lo.

| Movimento | Duração | Impacto | Recuperação ao encerrar | Alcance / cone |
| --- | --- | --- | --- | --- |
| Attack_01 | 0,45 s | 0,16 s | 0,06 s | 2,2 m / 120° |
| Attack_02 | 0,50 s | 0,19 s | 0,08 s | 2,3 m / 145° |
| Attack_03 | 0,72 s | 0,32 s | 0,28 s | 2,5 m / 90° |
| Especial | 0,90 s | 0,36 s | 0,15 s | 3 m / 360° |

Encadear evita a recuperação intermediária; os três golpes completos somam 1,67 s de animação e 0,28 s de recuperação final, antes de considerar a discretização dos frames. Não usar essa soma como DPS observado: alcance, cancelamentos, transição do Boss e acertos reais afetam o resultado. Buffer de entrada 0,15 s, com um comando pendente.

Especial custa 30/100 de estamina, cooldown 6 s, regeneração 20/s após 0,8 s. Recuperar 30 pontos demanda 1,5 s de regeneração; sob essas condições, o cooldown limita a repetição antes da estamina. Dash dura 0,18 s a 16 m/s, invulnerabilidade 0,25 s, cooldown 2 s. Armadura de couro concede defesa 10: dano positivo recebido é `max(1, dano × 100/110)`, aproximadamente 9,09% de redução acima do piso.

## Inimigos e loot

| Ator | HP | Dano | Preparação / cooldown |
| --- | --- | --- | --- |
| Melee | 40 | 10 | 0,4 / 1,2 s |
| Ranged | 25 | 8 | 0,5 / 2 s |
| Elite | 120 | 20 | 0,5 / 1,4 s |
| Guardião | 300 | Slam 25; projétil 12 | Slam 1,4 s na fase 1 / 1 s na fase 2; cooldown 2,5 / 1,6 s |

Guardião muda de fase uma vez a 50% HP, fica invulnerável por 3 s, acelera ×1,3 e amplia o raio do slam ×1,3. Não mudar esse intervalo para compensar DPS do jogador sem playtest específico.

Entradas anteriores de loot foram preservadas. Armadura tem entrada independente de 10% em melee/ranged; arma épica tem entrada independente de 10% no elite, além da chance anterior de 25% da Ferro. Não são pesos mutuamente exclusivos. Guardião mantém sua arma garantida. Capacidade de 16 espaços e poções em pilhas de cinco por potência são regras de gestão, sem venda/conversão automática.

## Evidência e decisão corrente

O usuário concluiu o andar normal no Editor e no Windows antes da integração de áudio. Com inventário e especial, a vitória normal no Editor foi observada em nível 5, XP 85/100, ouro 55 e arma do Guardião equipada (`Reports/Leadership/editor-inventory-special-victory.json`). Isso comprova um percurso concluível; não mede dificuldade média, tempo de conclusão ou distribuição de drops.

Manter esta linha de base no RC2. O aceite Windows atual deve registrar derrota→reinício, vitória 5/5 e qualquer problema concreto de dificuldade, recuperação, cura, legibilidade ou som. Nenhum ajuste numérico adicional foi justificado pelos dados disponíveis. QA e parecer final de balanceamento permanecem pendentes; seguir `RELEASE-CANDIDATE-QA.md`.

Fontes locais: `Prefabs/Player/Player.prefab`, `ScriptableObjects/Combat`, `ScriptableObjects/Enemies`, `ScriptableObjects/Items`, `ScriptableObjects/Loot`, `PlayerComboModel`, `ExperienceModel` e decisão D-002. Arquivos de cena/prefab foram apenas lidos nesta revisão.
