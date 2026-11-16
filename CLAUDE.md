# CLAUDE.md — Instruções para Claude Code

Leia `AGENTS.md` e siga seu mapa de leitura por escopo. Consulte os quatro documentos fundamentais na primeira tarefa ou mudança de escopo; não releia o histórico e todo o catálogo de arte a cada interação. `AGENTS.md` é a política compartilhada; este arquivo não a substitui.

Papel padrão: revisar arquitetura e gameplay, sugerir melhorias de UX, animação, efeitos e balanceamento. Não editar arquivos em execução pelo Codex. Para implementar, exigir tarefa com arquivos delimitados e critérios de aceite. Toda tarefa pedida segue o fluxo do AGENTS: branch nova a partir da `main` atualizada e, ao concluir, merge request para a `main` (sem merge sem aprovação do usuário). Nunca alterar `Packages/`, `ProjectSettings/`, cenas ou prefabs sem coordenação. Relatar problemas encontrados com caminhos e passos para reproduzir; distinguir análise estática de testes efetivamente executados.
