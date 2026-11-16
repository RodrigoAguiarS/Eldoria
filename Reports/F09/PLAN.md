# F09 — vasos destrutíveis

Base `6fdf184` da main atualizada; branch `codex/f09-destructible-props`.
Unity 6000.6.9f1; manifest/lock preservados; árvore inicialmente limpa.

Incremento: um tipo de vaso temporário de 40 HP em SO, uma instância lateral por
sala de release. Sem drops, XP, física de estilhaços ou registro como inimigo.
Domínio próprio IDamageable, estado por instância, evento Broken único e guarda
contra reentrada. Toggle não cura/recria. Cena nova cria instância nova.

Arquivos: três componentes/definição em Scripts/Dungeon, builder em Scripts/Editor,
máscara adicional de PlayerCombat/PlayerSpecial, hooks nos builders do jogador,
testes EditMode/PlayMode, assets compostos pela Unity e documentação afetada.

Layer Destructible exclusiva para consultas de básico/especial; preservar enemyMask
e o HashSet por IDamageable. Colliders sólidos para overlap, sem colisão física;
não são cobertura e ficam excluídos do bake. Sem alterar NavMesh existente.
Centro/corredor/portas/spawns e prefabs de sala preservados.
Apresentação usa DamageFlash existente e destroços estáticos pré-compostos.

Aceite: dano uma vez por golpe com vários colliders, quebra uma vez sob dano
sequencial/reentrante e ataques concorrentes, pausa/morte/reativação seguras,
limpeza/vitória continuam dependendo apenas da onda. Builder idempotente, GUIDs,
dados, iluminação e NavMesh existentes preservados. Executar testes pertinentes,
captura gráfica PlayMode na cena real e inspeção de imagens; distinguir diagnóstico
do aceite humano de uma run normal. Relatórios somente nesta pasta.
