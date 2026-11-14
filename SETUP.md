# SETUP.md — Passo a passo Windows

1. Instale Unity Hub e Unity 6 LTS com módulo Windows Build Support (IL2CPP opcional no início).
2. Unity Hub → New project → **Universal 3D (URP)** → nome `DungeonCrawler` → crie na pasta desejada.
3. Feche o Unity. Extraia os arquivos deste kit na raiz do projeto, preservando `Assets/`, `Packages/` e `ProjectSettings/` gerados pelo Unity. Não sobrescreva esses diretórios com versões de outro projeto.
4. Abra o projeto. Package Manager → instale/valide `Input System`, `Cinemachine`, `AI Navigation`, `Test Framework` (versões compatíveis com Editor). Ao habilitar o New Input System, aceite reiniciar o Editor.
5. Project Settings → Player → Active Input Handling: `Input System Package (New)`; evite `Both` para impedir uso acidental de API legada.
6. Project Settings → Editor → Asset Serialization: `Force Text`; Version Control: `Visible Meta Files` (se a opção existir).
7. No terminal PowerShell na raiz: `git init`; `git add .`; `git commit -m "chore: initialize unity dungeon crawler"` (configure identidade Git se necessário).
8. Abra Codex na raiz do projeto. Solicite: "Leia AGENTS.md, GAME_DESIGN.md, ARCHITECTURE.md e MVP.md. Execute apenas M0; planeje antes de editar; valide no Editor quando disponível."
9. Abra Claude Code na mesma raiz para revisão, sem edição concorrente. Solicite: "Leia os quatro documentos e CLAUDE.md; revise o plano M0/M1, não altere arquivos; apresente riscos e sugestões."
10. Após M0 validado, delegue M1 ao Codex em tarefas pequenas: câmera+movimento; mira+ataque; dash+HP; inimigo+NavMesh; HUD+reinício; builder+testes.

## Observações
Este kit contém documentos e pastas, não um projeto Unity completo nem código jogável. A Unity cria seus próprios arquivos de configuração e `.meta`. Não copie diretórios vazios esperando que o Unity os preserve; um arquivo `.gitkeep` está presente em cada pasta inicial.
