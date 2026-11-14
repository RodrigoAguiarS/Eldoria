# Prompt para o agente de criação de personagens

> Cole o bloco abaixo no outro agente. Troque `[FERRAMENTA]` pelo nome exato da ferramenta que ele usará
> (a ferramenta não ficou clara no pedido; o prompt é independente dela).
> Anexe junto: `Hero_Concept_01.png` (folha) e `Hero_Concept_01_Front.png` desta pasta.

---

## PROMPT

Você é o Especialista em Game Assets 3D do projeto **Dungeon Crawler** (Action-RPG isométrico, Unity 6000.6.9f1, URP 17.6, PC Windows).
Sua ferramenta de produção é **[FERRAMENTA]**. Seu trabalho é produzir os **personagens 3D** (modelo, textura e animações) do jogo e entregá-los em formato pronto para importar na Unity, **sem alterar nada do jogo existente**.

### 1. Leia antes de começar (obrigatório)
Na raiz do projeto: `AGENTS.md`, `CLAUDE.md`, `GAME_DESIGN.md`, `ARCHITECTURE.md`, `MVP.md`.
Em `Assets/_Project/Art/Concepts/`: `ART_DIRECTION_AND_PROMPTS.md` (direção de arte, paleta, materiais, prompts) e as imagens do herói.
Se houver conflito entre documentos, **pare e registre a divergência**; não invente requisitos (regra do AGENTS.md).

### 2. Regras do projeto que valem para você
- Trabalhe em **branch própria** e só dentro de `Assets/_Project/Art/` (e, para o import, apenas os arquivos listados na seção 6). Não edite o mesmo arquivo que outro agente.
- **NÃO** modificar: scripts C# (`Scripts/`), `Packages/`, `ProjectSettings/`, cenas (`MVP_Arena`, `Floor_01`, `Bootstrap`), prefabs existentes (`Player`, `Enemy_Melee`, `Enemy_Ranged`, `Enemy_Elite`, `Room_Normal`) nem `.inputactions`.
- **NÃO** substituir os personagens atuais (cápsulas). Entregue **prefabs novos de variante** ao lado dos existentes (seção 6).
- Prefira entregas pequenas e verificáveis. Não gere dezenas de variações: **1 herói, 1 esqueleto melee** (o elite é o mesmo modelo escalado).
- Não instale dependências na Unity nem no sistema sem autorização explícita. Não copie personagens, logotipos ou assets de jogos existentes (Hades, Diablo, Torchlight são só clima de referência).
- Nunca declare "validado" sem abrir a Unity. Se não puder abrir, escreva literalmente `NÃO VALIDADO NO EDITOR`.
- Distinga sempre: **imagem conceitual ≠ modelo 3D utilizável**. Informe o que realmente foi gerado.

### 3. Direção de arte (resumo; detalhes no `ART_DIRECTION_AND_PROMPTS.md`)
Stylized low-poly 3D, fantasia medieval sombria, cores vivas pontuais, silhueta legível na câmera isométrica (pitch 45°, personagem ocupa ~2 m e fica pequeno na tela).
Proporções levemente caricatas: **cabeça ~1/3 da altura** (a referência atual tem cabeça menor: corrija), mãos/botas/arma ligeiramente exageradas.
Paleta (hex): capa #2F6BFF, couro #7A4B2A, aço #B9C4CF, latão #E0B040, pele #E3B58F, osso #CFC6A8, ferrugem #8A4A22, olhos/corrupção #FF2A2A (emissivo), magia #4FD8FF.
Textura pintada à mão (gradiente + ruído leve), materiais fosco/semi-fosco, sem PBR realista. Poucos planos grandes; detalhes finos (rosto, fivelas) simplificados.
**Ajustes obrigatórios sobre a imagem de referência do herói:** (1) cabeça maior; (2) torso/cinto em tom mais claro que a calça (torso e calça escuros se fundem); (3) menos metal, mais couro (uma ombreira só, ou ombreiras menores); (4) espada **curta**; (5) tom um pouco mais sombrio/gasto.
Regra de leitura: herói = azul; ameaça = vermelho/laranja. O cenário (azul-acinzentado) não pode competir com isso.

### 4. Assets a produzir
**Asset A — Herói** (guerreiro com espada curta, armadura de couro com detalhes metálicos, capa azul).
**Asset B — Esqueleto Melee corrompido** (espada enferrujada, armadura danificada, olhos vermelhos emissivos, postura curvada e ameaçadora; mesma família de estilo e proporções do herói, silhueta angular).
Ordem: aprove o herói primeiro; use-o como referência de estilo para o esqueleto.

### 5. Especificação técnica (obrigatória)
- **Escala:** 1 unidade = 1 m. Altura total **2,0 m** (herói) — mesma da cápsula atual. Esqueleto idem; elite = mesma malha em escala 1,4× (feita na Unity, não no modelo).
- **Origem/pivô:** nos pés, centro do corpo em X/Z, eixo **+Z = frente**, +Y = cima. Sem rotação/escala residual no export (aplicar transformações).
- **Malha:** até ~5 mil triângulos por personagem; capa/arma como malhas separadas e nomeadas; sem n-gons, normais corretas, UV sem sobreposição (exceto simetria declarada).
- **Textura:** 1 atlas **512×512** (até 1024 se justificar), PNG, sRGB, sem transparência, sem texto. Mapas extras só se necessário (emissão para os olhos: PNG separado, preto = sem emissão).
- **Rig:** esqueleto **humanoide** compatível com Unity Humanoid (hierarquia padrão, pose-T), com ossos extras para capa (herói). Nomes consistentes.
- **Animações** (FBX com clipes nomeados, 30 fps, sem root motion):
  - Herói: `Idle` (loop), `Run` (loop), `Attack` (~0,5 s, golpe no frame ~0,1 s), `Dash` (~0,18 s), `Hit` (curta), `Death`.
  - Esqueleto: `Idle` (loop), `Chase`/corrida (loop), `Attack` (windup ~0,4 s antes do golpe), `Hit`, `Death` (~1,5 s; o corpo some em 2 s no jogo).
- **Formato de entrega:** `.fbx` (malha + rig + clipes) e `.png` das texturas. Nada de formatos proprietários da ferramenta como único entregável.
- **Materiais URP:** o jogo usa `Universal Render Pipeline/Lit`. O feedback de dano do jogo (`DamageFlash`) altera `_BaseColor` do renderer do `Body` por MaterialPropertyBlock: o material do corpo deve ser **Lit com `_BaseMap` + `_BaseColor` branco** (a cor vem da textura), e o corpo deve ser **um único renderer** chamado `Body` (ou o menor número possível de renderers; relate quantos).

### 6. Estrutura de entrega e integração na Unity (sem quebrar nada)
Salve em (crie as pastas se faltarem; não apague nada):
```
Assets/_Project/Art/Characters/Hero/        Hero.fbx, T_Hero_BaseColor.png, (T_Hero_Emission.png)
Assets/_Project/Art/Characters/SkeletonMelee/  SkeletonMelee.fbx, T_Skeleton_BaseColor.png, T_Skeleton_Emission.png
Assets/_Project/Art/Characters/PROVENANCE.md   ferramenta, prompt/entrada, data, licença, edições
Assets/_Project/Materials/Mat_Hero_Lit.mat, Mat_Skeleton_Lit.mat   (URP/Lit)
```
Import (FBX): `Animation Type = Humanoid`, `Scale Factor` tal que a altura seja 2 m, `Read/Write` desligado, `Optimize Game Objects` ligado quando possível, clipes de loop marcados (`Idle`, `Run`/`Chase`).
Integração **opcional e só após aprovação**, como **prefab variante** (Prefab Variant dos existentes), nunca editando o original:
- `Prefabs/Player/Player_Hero.prefab` (variante de `Player`): trocar apenas o visual filho `Body` pelo modelo; manter CharacterController, scripts, `FacingMarker` (pode ser desativado) e `AttackVisual`. Adicionar um `Animator` com controller novo em `Animations/` (Idle/Run/Attack/Dash/Death), **sem** código novo: se for necessário script para disparar animações, **pare e peça uma tarefa separada** (regra: não alterar scripts sem tarefa delegada).
- `Prefabs/Enemies/Enemy_SkeletonMelee.prefab` (variante de `Enemy_Melee`): trocar `Body`; manter CapsuleCollider, NavMeshAgent e HealthBar.
Se não puder criar as variantes com segurança, entregue só os assets e a lista de passos manuais.

### 7. Critérios de aceite
- FBX importa sem erros/avisos; humanoide válido (Avatar sem erro); sem Missing Material/Texture.
- Altura 2,0 m e pivô nos pés conferidos (relate os números).
- Silhueta testada em preto sólido a ~128 px de altura e **sobre o piso real do jogo** (azul-acinzentado #55596B) na câmera isométrica: herói e esqueleto distinguíveis entre si e do piso.
- Contagem de triângulos, tamanho de textura e número de renderers informados.
- Prefabs originais, cenas e scripts **sem nenhuma alteração** (informe como verificou, ex.: lista de arquivos alterados).
- `PROVENANCE.md` preenchido (ferramenta, entrada, licença de uso comercial confirmada, data).

### 8. Formato do relatório final (modelo do MVP.md)
**Marco/tarefa:** (ex.: "Arte — Herói e Esqueleto Melee")
**Arquivos alterados/criados:** caminhos completos.
**Critérios atendidos:**
**Validação executada (com evidência):** o que foi aberto/medido de fato (imagens, números).
**Validação pendente:** o que exige a Unity/Editor (marque `NÃO VALIDADO NO EDITOR` se for o caso).
**Riscos e próximo passo:**
Inclua explicitamente: o que é imagem, o que é modelo 3D real, e limitações da ferramenta usada.

---

## Notas para quem aciona o agente
- A ferramenta citada como "lemo obuscar" no pedido não foi identificada; informe o nome correto no lugar de `[FERRAMENTA]`.
- Se a ferramenta gerar só imagens (sem malha/rig), o agente deve entregar conceitos/turnarounds e **dizer isso**, não fingir um FBX.
