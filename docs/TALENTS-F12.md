# Talento por nível — F12

Expansão explícita pedida pelo usuário em 16/11/2026 (D-005), sobre a progressão
existente. Base `151c675`; branch `claude/f12-level-talents`. Proposta registrada
antes da implementação; valores são **ajustáveis em SO** e sujeitos a playtest.

## Regra

- Cada nível ganho concede **uma** escolha de talento. Escolhas disponíveis =
  `(nível − 1) − talentos já escolhidos`, limitadas à capacidade restante de ranks.
  O número é derivado do estado, nunca um contador separado: não há como duplicar.
- A progressão base continua intacta: +10% de dano por nível (`ExperienceModel`),
  cura de 40% por nível ganho e armas +6/+12/+18. Talento é um termo **próprio**,
  identificado pela origem; não altera o multiplicador de nível, a arma ou os SOs.
- Escolha não é automática nem obrigatória no momento: subir de nível **não pausa**
  o combate. O HUD avisa enquanto houver escolha pendente; o jogador abre
  **Pausa → Talentos** quando quiser. Voltar/Esc cancela sem gastar a escolha.
- Vários níveis numa coleta ficam enfileirados: a tela mostra a quantidade e
  continua aberta até a última escolha. Cada clique aceito gasta exatamente uma;
  há um intervalo de 0,3 s (tempo real) entre escolhas para evitar duplo clique.
- Jogador morto não escolhe. Reiniciar/Nova run começa sem talentos.
- Sem RNG: as três opções são sempre as mesmas, na mesma ordem.

## Opções iniciais (máx. 3 ranks cada)

| ID | Nome | Efeito por rank | Máximo | Composição |
| --- | --- | --- | --- | --- |
| `talent.blade` | Lâmina Afiada | +5% de dano | +15% | fator separado: `(20 + arma) × nível × (1 + talento) × golpe` |
| `talent.guard` | Couraça Firme | +5 de defesa | +15 | somada a base + armadura antes de `100/(100+defesa)` |
| `talent.whirl` | Giro Ágil | −0,5 s no cooldown do giro | −1,5 s (6 → 4,5 s) | aplicado ao iniciar o giro; custo/estamina inalterados |

Três opções × três ranks = nove escolhas = níveis 2–10. A vitória observada na 1.0
ocorreu no nível 5 (quatro escolhas), logo uma run típica não completa todos os
ranks. Referências: Ferro +6 no nível 3 = 31,2 de dano base; com Lâmina 1 = 32,76.
Armadura de couro (10) + Couraça 1 = 15 de defesa ≈ 13% de redução (antes 9,1%).

## Persistência

Somente pelo checkpoint F11 existente (sem save novo ou implícito). O schema
passa a **v2** com `talents: [{id, rank}]`. v1 continua aceito e é migrado sem
talentos: as escolhas dos níveis já ganhos ficam pendentes ao continuar. Validação
recusa ID desconhecido/duplicado, rank fora de 1..máx e soma maior que `nível − 1`.

## Fora do escopo

Árvore, pré-requisitos, respec, slots/afixos, talentos aleatórios, novo binding,
pausa automática ao subir de nível, arte nova. Ajustes numéricos dependem de
playtest humano; esta entrega não afirma melhoria de balanceamento.
