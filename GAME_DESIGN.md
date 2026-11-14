# GAME_DESIGN.md — Dungeon Crawler

## Visão
Action-RPG / Dungeon Crawler isométrico 3D para Windows, inspirado no ritmo de combate e progressão de jogos do gênero, com identidade visual própria. Unity 6 URP, estilo low-poly estilizado, câmera fixa em orientação e inclinada ~45°.

## Loop completo (após MVP)
Entrar na sala → portas fecham → inimigos surgem → derrotar todos → portas abrem → coletar XP/loot → avançar. Um andar tem quatro salas normais e uma arena de Boss.

## Controles
- WASD: movimento relativo à projeção horizontal da câmera, sem deslocamento vertical.
- Mouse: mira via raycast contra plano do chão; o personagem gira horizontalmente para o alvo.
- Botão esquerdo: ataque básico; espaço: dash com cooldown e janela curta de invulnerabilidade; botão direito ou E: especial (pós-MVP inicial).
- Câmera Cinemachine segue suavemente, sem rotação controlada pelo jogador.

## Jogador
Usa CharacterController; atributos HP, recurso (mana/estamina), ataque, defesa, velocidade. Ao receber dano, feedback vermelho temporário e shake leve, sem comprometer legibilidade. Morte encerra combate e permite reiniciar.

## Inimigos
Máquina de estados Idle → Chase → Attack → Dead. Melee aproxima e golpeia; ranged mantém distância e dispara; elite tem maior HP/tamanho/dano e concede mais XP. Navegação por NavMeshAgent.

## Boss
Duas fases. Aos 50% HP: transição única, invulnerável por 3 segundos, rugido e mudança visual; segunda fase altera ataques. Golpes pesados mostram telegraph no chão pelo menos 1 segundo antes do dano. HUD de Boss apenas durante luta.

## Dungeon
Salas como prefabs, com pontos de entrada/saída, portas e pontos de spawn definidos. Inicialmente sequência fixa; aleatoriedade só após estabilização. Inimigos da sala são rastreados por referências/eventos, não por busca global.

## Progressão
Inimigos concedem orbes de XP; ao subir de nível, cura configurável e +10% de dano (multiplicador por nível definido em código de progressão e testado). Drops possíveis: ouro, poção e arma. Itens, raridades e valores são ScriptableObjects.

Inventário interativo e raridades integram o escopo da versão 1.0 por aprovação do usuário; ver [decisão D-001 e fronteira do produto](ROADMAP.md#decisões-de-produto). A implementação deve preservar a fórmula de progressão e definir explicitamente as regras de coleta, capacidade e equipamento.

Regras aprovadas para a implementação da 1.0 (D-002): inventário com 16 espaços, equipamento manual em um slot de arma e um de armadura, poções guardadas para consumo com Q e pilhas de até 5 por potência, XP/ouro imediatos e pickup mantido no chão se não houver capacidade. I/Tab abre o inventário e pausa a partida offline. Raridades Comum/Raro/Épico não aplicam multiplicadores ocultos; as armas existentes mantêm bônus +6/+12. Detalhes, valores iniciais e aceite em [regras da 1.0](docs/PRODUCT-1.0-PROPOSAL.md).

O especial aprovado é um giro de espada em 360° (botão direito/E), alcance inicial 3 m, dano 2× o básico calculado, custo 30 de estamina e cooldown 6 s. Estamina inicial máxima 100, regeneração 20/s após 0,8 s sem especial; básico/dash mantêm suas regras. Defesa base 0, fornecida por armadura; dano recebido positivo = max(1, dano × 100/(100 + defesa)). Esses valores novos são configuráveis e sujeitos a playtest, sem alterar a progressão por nível.

## Direção visual
Low-poly estilizado, silhuetas legíveis na câmera isométrica, cores vivas e contraste adequado entre personagem, inimigos, perigos e cenário. Pedra, tochas e obstáculos destrutíveis no escopo posterior. Não copiar assets ou personagens de jogos existentes.

## Escopo do primeiro marco
Uma arena com jogador, câmera, WASD, mira, ataque básico, dash, um inimigo melee, HP, HUD e reinício. Sem Boss, loot, geração de salas, habilidades especiais ou arte definitiva.

## Parâmetros iniciais ajustáveis (não definitivos)
Vida do jogador 100; inimigo 40; dano do ataque 20; velocidade do jogador 5 unidades/s; cooldown do dash 2 s. Ajustar por playtest, não tratar como valores canônicos.
