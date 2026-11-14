// Herói — Asset 01 de ART_DIRECTION_AND_PROMPTS.md, traduzido para pixel art no grammar HD-2D do Lemo.
//   aventureiro guerreiro · espada curta · armadura de couro escuro · ombreiras e braçadeiras de aço ·
//   capa azul real em "V" · cabeça ~1/3 da altura · mãos e botas robustas · formas redondas.
// Desenho procedural pixel a pixel numa grade lógica 64×64 (ampliada ×2 → 128×128), usando as
// primitivas do Lemo (PX.ellipse/poly/rect/line, ramp com Bayer, outline "selout").
// Luz da tela: sempre do alto à esquerda (key fria da lua), também nas vistas espelhadas.

export const meta = {
  id: 'hero',
  name: 'Hero',
  description: 'Herói jogável (Asset 01). Protótipo de sprite 2D, NÃO substitui o modelo low-poly oficial.',
  logical: 64,          // grade de desenho
  scale: 2,             // ampliação nearest → 128×128
  footRow: 59,          // última linha opaca da sola (antes do contorno), em pixels lógicos
  heightMeters: 2.0,    // escala do jogo (personagens ~2 m)
  handedness: 'right',  // espada na mão direita do herói em todas as direções
  assumptions: [
    'Cor do cabelo não definida na direção de arte: castanho escuro #4A2C1A (derivado do couro), sem capacete.',
    'Olhos, sobrancelhas e boca em tinta #1E1830 (cor oficial de sombra), nunca preto puro.',
    'Couro "escuro": rampa construída do #7A4B2A oficial em direção à sombra #1E1830.',
  ],
};

export const PALETTE = {
  cape: '#2F6BFF', rune: '#4FD8FF', leather: '#7A4B2A', metal: '#B9C4CF', brass: '#E0B040', skin: '#E3B58F',
  shadow: '#1E1830', moon: '#9FB8FF', torch: '#FF9A2E', hair: '#4A2C1A', highlight: '#E8EEFF',
};

// Poses como dados (grammar do Lemo: "a parameter object per drawing").
//  torso/head: deslocamento vertical (+ = para baixo) · fA/fB: avanço do pé direito/esquerdo ao longo
//  da direção de marcha (px) · lA/lB: elevação do pé · aA/aB: balanço do braço direito/esquerdo ·
//  sway: balanço lateral da barra da capa · flare: capa ao vento (vistas laterais)
export const ANIMATIONS = {
  Idle: {
    fps: 4, loop: true,
    // respiração com ação sobreposta: o tronco desce, a cabeça segue um quadro depois
    frames: [
      { torso: 0, head: 0 },
      { torso: 1, head: 0 },
      { torso: 1, head: 1 },
      { torso: 0, head: 1 },
    ],
  },
  Walk: {
    fps: 10, loop: true,
    frames: [0, 1, 2, 3, 4, 5].map(i => ({
      torso: [0, 1, 0, 0, 1, 0][i], head: [0, 1, 0, 0, 1, 0][i],
      fA: [4, 2, -2, -4, -2, 2][i], lA: [0, 0, 0, 1, 3, 2][i],
      fB: [-4, -2, 2, 4, 2, -2][i], lB: [1, 3, 2, 0, 0, 0][i],
      aA: [-4, -2, 2, 4, 2, -2][i], aB: [4, 2, -2, -4, -2, 2][i],
      sway: [0, 1, 1, 0, -1, -1][i], flare: [1, 2, 2, 1, 2, 2][i],
    })),
  },
};

export const DIRECTIONS = ['Front', 'Back', 'Left', 'Right'];

export function createHero(lemo) {
  const { ramp, hex, mixc } = lemo;
  const H = PALETTE;
  const mix = (a, b, t) => mixc(hex(a), hex(b), t);
  // rampa a partir da cor oficial: t<0 escurece em direção à sombra roxa, t>0 clareia em direção à luz fria
  const tone = (base, ts) => ts.map(t => (t < 0 ? mix(base, H.shadow, -t) : mix(base, H.highlight, t)));
  const DZ = .22;   // faixa de dither recomendada pelo STYLE.md do HD-2D para sprites
  const C = {
    cape: ramp(tone(H.cape, [-.72, -.5, -.26, 0, .28]), DZ),
    leather: ramp(tone(H.leather, [-.76, -.54, -.3, 0, .16]), DZ),
    metal: ramp(tone(H.metal, [-.7, -.48, -.24, 0, .55]), DZ),
    brass: ramp(tone(H.brass, [-.62, -.32, 0, .42]), DZ),
    skin: ramp(tone(H.skin, [-.5, -.26, 0, .32]), .12),
    hair: ramp(tone(H.hair, [-.55, -.25, 0, .3]), DZ),
    boot: ramp(tone(H.leather, [-.88, -.74, -.56, -.36]), DZ),
  };
  const INK = hex(H.shadow), SHINE = hex(H.highlight), BLUSH = mix(H.skin, H.torch, .35);
  const clamp01 = v => Math.max(0, Math.min(.999, v));

  // —— peças comuns ——
  // Lâmina: segmento a partir de (x0,y0) na direção (dx,dy); sombreada pelo lado da luz.
  function blade(P, x0, y0, dx, dy, len, L) {
    const n = Math.hypot(dx, dy); dx /= n; dy /= n; const px = -dy, py = dx;   // perpendicular
    const lit = (px * -L + py * -1) > 0 ? 1 : -1;  // qual lado da lâmina olha para a luz (alto-esquerda)
    const xs = [x0, x0 + dx * len], ys = [y0, y0 + dy * len];
    for (let y = Math.floor(Math.min(...ys)) - 2; y <= Math.ceil(Math.max(...ys)) + 2; y++)
      for (let x = Math.floor(Math.min(...xs)) - 2; x <= Math.ceil(Math.max(...xs)) + 2; x++) {
        const rx = x + .5 - x0, ry = y + .5 - y0, a = rx * dx + ry * dy, b = rx * px + ry * py;
        if (a < 0 || a > len) continue;
        const w = a > len - 2.2 ? 1.5 * (len - a) / 2.2 : 1.5;   // ponta afinada
        if (Math.abs(b) > w) continue;
        const side = b * lit;          // >0 lado iluminado
        const v = side > .5 ? .97 : side > -.5 ? .62 : .3;
        P.set(x, y, C.metal(v, x, y));
      }
  }
  function guard(P, cx, cy, dx, dy) {   // guarda-mão de latão perpendicular à lâmina + punho
    const n = Math.hypot(dx, dy); dx /= n; dy /= n; const px = -dy, py = dx;
    for (let k = -3; k <= 3; k++) P.set(Math.round(cx + px * k), Math.round(cy + py * k), C.brass(k < 0 ? .9 : .55, 0, 0));
    for (let k = -2; k <= 2; k++) P.set(Math.round(cx + px * k + dx), Math.round(cy + py * k + dy), C.brass(.25, 0, 0));
  }
  function fist(P, x, y, L, v0 = .62) {   // mão robusta 5×5 com cantos aparados
    for (let j = -2; j <= 2; j++) for (let i = -2; i <= 2; i++) {
      if (Math.abs(i) === 2 && Math.abs(j) === 2) continue;
      P.set(x + i, y + j, C.skin(v0 - i * L * .07 - j * .08, x + i, y + j));
    }
    P.set(x - L, y - 1, C.skin(v0 - .3, 0, 0)); P.set(x, y - 1, C.skin(v0 - .3, 0, 0));   // dedos
  }
  function pauldron(P, cx, cy, L, rx = 5) {
    P.ellipse(cx, cy, rx, 3.7, (x, y, dx, dy) => C.metal(.78 - dx * L * .28 - dy * .4 + (dy > .25 && dy < .55 ? -.25 : 0), x, y));
    P.set(Math.round(cx), Math.round(cy - 1), C.brass(.95, 0, 0));   // rebite dourado
    for (let i = -3; i <= 3; i++) P.set(Math.round(cx) + i, Math.round(cy + 3), C.brass(.45 - i * L * .05, 0, 0));   // friso de latão
  }

  // ======================= FRENTE / COSTAS =======================
  function legFront(P, cx, foot, back) {
    const top = 44, ankle = foot - 6;
    for (let y = top; y <= ankle; y++) for (let x = cx - 3; x <= cx + 2; x++)
      P.set(x, y, C.leather(.3 + (x === cx - 3 ? .18 : 0) - (x === cx + 2 ? .12 : 0), x, y));
    for (let y = ankle + 1; y <= foot; y++) for (let x = cx - 3; x <= cx + 3; x++) {
      let v = y === ankle + 1 ? .82 : .5 + (x <= cx - 2 ? .14 : 0) - (x >= cx + 2 ? .14 : 0);
      if (y === foot) v = .06;
      if (!back && y === foot - 2 && x === cx - 1) v = .95;   // brilho na biqueira
      P.set(x, y, C.boot(v, x, y));
    }
  }
  function armFront(P, x0, t, ad, L) {   // x0 = coluna esquerda do braço (5 px de largura)
    for (let y = 27 + t; y <= 33 + t + ad; y++) for (let x = x0; x < x0 + 5; x++) P.set(x, y, C.leather(.42 - (x - x0) * L * .07, x, y));
    for (let y = 34 + t + ad; y <= 38 + t + ad; y++) for (let x = x0; x < x0 + 5; x++) {
      let v = .8 - (x - x0) * .14; if (L < 0) v = .8 - (x0 + 4 - x) * .14;
      if (y === 34 + t + ad) v = .95;
      P.set(x, y, y === 38 + t + ad ? C.brass(.5, 0, 0) : C.metal(v, x, y));
    }
    fist(P, x0 + 2, 41 + t + ad, 1);
    return { hx: x0 + 2, hy: 41 + t + ad };
  }
  function headFront(P, hy) {
    P.ellipse(32, hy, 9.2, 9, (x, y, dx, dy) => C.hair(.62 - dx * .28 - dy * .3 + ((x * 3 + y) % 5 === 0 ? -.12 : 0), x, y));
    P.rect(22, hy + 1, 2, 3, C.skin(.5, 0, 0)); P.rect(41, hy + 1, 2, 3, C.skin(.28, 0, 0));
    P.ellipse(32, hy + 2.2, 7.6, 6.6, (x, y, dx, dy) => C.skin(.7 - dx * .22 - dy * .12 + (dy > .8 ? -.14 : 0), x, y));
    const jag = [2, 3, 2, 1, 3, 2, 2, 3, 1, 2, 3, 2, 2, 3, 2, 1, 2];
    for (let x = 24; x <= 40; x++) for (let y = hy - 5; y < hy - 4 + jag[x - 24]; y++) P.set(x, y, C.hair(.55 - (x - 32) * .025, x, y));
    for (let y = hy - 2; y <= hy + 3; y++) { P.set(24, y, C.hair(.4, 0, 0)); P.set(40, y, C.hair(.25, 0, 0)); }
    // olhos (2×2, brilho frio), sobrancelhas, nariz, boca, bochechas
    for (const ex of [28, 35]) {
      P.rect(ex, hy + 1, 2, 2, INK); P.set(ex, hy + 1, SHINE);
      P.rect(ex - 1, hy - 1, 3, 1, C.hair(.15, 0, 0));
    }
    P.set(32, hy + 3, C.skin(.3, 0, 0));
    P.rect(31, hy + 5, 2, 1, C.skin(.08, 0, 0));
    P.set(27, hy + 4, BLUSH); P.set(37, hy + 4, BLUSH);
  }
  function headBack(P, hy) {
    P.rect(22, hy + 1, 2, 3, C.skin(.45, 0, 0)); P.rect(41, hy + 1, 2, 3, C.skin(.25, 0, 0));
    P.ellipse(32, hy, 9.2, 9, (x, y, dx, dy) => {
      let v = .6 - dx * .28 - dy * .32;
      if ((x + (y >> 1)) % 4 === 0) v -= .14;   // mechas
      return C.hair(v, x, y);
    });
    P.rect(29, hy + 8, 6, 1, C.hair(.12, 0, 0));
  }

  function drawFrontBack(P, p, back) {
    const t = p.torso || 0, h = p.head || 0, sw = p.sway || 0;
    // pernas: frente → pé direito do herói à esquerda da tela; costas → à direita
    const fwdA = Math.trunc((p.fA || 0) / 3), fwdB = Math.trunc((p.fB || 0) / 3);
    const s = back ? -1 : 1;   // avançar = descer na tela (frente) ou subir (costas)
    // o pé que fica atrás sobe 1 px; nenhum pé desce abaixo da linha de apoio (pivô fixo)
    const footA = 59 + Math.min(0, s * fwdA) - (p.lA || 0), footB = 59 + Math.min(0, s * fwdB) - (p.lB || 0);
    const xA = back ? 36 : 28, xB = back ? 28 : 36;
    const adA = Math.trunc((p.aA || 0) / 2) * s, adB = Math.trunc((p.aB || 0) / 2) * s;
    const armRightX = back ? 43 : 17, armLeftX = back ? 17 : 43;   // coluna esquerda do braço direito/esquerdo
    const capePts = [[22, 23 + t], [43, 23 + t], [46 + sw, 49], [32 + sw, 56], [19 + sw, 49]];

    if (!back) {   // capa por trás do corpo: vemos o forro mais escuro nas laterais e na ponta do V
      P.poly(capePts, (x, y) => { let v = .3 + (x < 32 ? .08 : -.04); if ((x + (y >> 2)) % 5 === 0) v -= .1; if (y >= 50) v -= .12; return C.cape(v, x, y); });
    }
    // pescoço (evita fresta quando cabeça e tronco se movem em quadros diferentes)
    P.rect(29, 20 + Math.min(t, h), 7, 8, C.skin(.18, 0, 0));
    legFront(P, xA, footA, back); legFront(P, xB, footB, back);
    if (!back) {
      // tronco de couro + talabarte + cinto + saiote
      P.poly([[22, 24 + t], [43, 24 + t], [41, 43 + t], [24, 43 + t]], (x, y) => {
        const sx = (x - 22) / 21, u = (y - 24 - t) / 19; let v = .62 - sx * .4 + (1 - u) * .12;
        if (x === 32 && y > 30 + t) v -= .22;
        return C.leather(v, x, y);
      });
      P.line(40, 25 + t, 25, 40 + t, C.leather(.08, 0, 0)); P.line(41, 25 + t, 26, 40 + t, C.leather(.16, 0, 0));
      P.set(34, 31 + t, C.brass(.9, 0, 0)); P.set(26, 29 + t, C.brass(.7, 0, 0)); P.set(38, 29 + t, C.brass(.55, 0, 0));
      P.rect(24, 40 + t, 17, 2, C.leather(.06, 0, 0));
      P.rect(31, 40 + t, 3, 1, C.brass(.95, 0, 0)); P.rect(31, 41 + t, 3, 1, C.brass(.5, 0, 0));
      P.poly([[24, 42 + t], [41, 42 + t], [42, 47 + t], [23, 47 + t]], (x, y) => {
        let v = .52 - (x - 23) / 19 * .32; if (x === 32) v = .05; if (y === 46 + t) v -= .25; return C.leather(v, x, y);
      });
    }
    // braços
    const R = armFront(P, armRightX, t, adA, 1), Lf = armFront(P, armLeftX, t, adB, 1);
    // espada na mão direita, lâmina para baixo e levemente para fora
    const out = back ? 1 : -1;
    P.rect(R.hx - 1, R.hy - 4, 2, 2, C.brass(.85, 0, 0));          // pomo acima do punho
    guard(P, R.hx, R.hy + 3, out * .18, 1);
    blade(P, R.hx + out * .3, R.hy + 4.5, out * .18, 1, 11.5, 1);
    fist(P, R.hx, R.hy, 1);   // punho por cima do cabo
    void Lf;
    if (back) {
      P.poly(capePts, (x, y) => {
        const u = (y - 23 - t) / (55 - 23 - t), cx = 32 + sw * u;
        let v = .62 - (x - cx) * .035 + (1 - u) * .1;
        if (Math.abs(((x - cx) * (1 - u * .35)) % 5) < .6 && u > .15) v -= .2;   // dobras convergindo nos ombros
        if (y <= 24 + t) v -= .3;                                               // gola
        return C.cape(v, x, y);
      });
    }
    pauldron(P, 21, 26 + t, 1); pauldron(P, 43, 26 + t, 1);
    if (back) headBack(P, 15 + h); else headFront(P, 15 + h);
  }

  // ======================= LATERAL (desenhado olhando para a direita) =======================
  // L = +1 normal, -1 quando o resultado será espelhado (mantém a luz da tela no alto-esquerda)
  // swordNear: espada na mão do lado da câmera (direita do herói quando ele olha para a direita)
  function legSide(P, legX, lift, dark, L) {
    const hipX = 31, fx = 31 + legX, ankle = 52 - lift, foot = 59 - lift;
    for (let y = 44; y <= ankle; y++) {
      const u = (y - 44) / Math.max(1, ankle - 44), cx = Math.round(hipX + (fx - hipX) * u);
      for (let x = cx - 2; x <= cx + 2; x++) P.set(x, y, C.leather(.32 - dark + (x === cx - 2 ? .14 * L : 0) - (x === cx + 2 ? .12 * L : 0), x, y));
    }
    for (let y = ankle + 1; y <= foot; y++) for (let x = fx - 3; x <= fx + 3 + (y >= foot - 1 ? 1 : 0); x++) {
      let v = (y === ankle + 1 ? .8 : .5) - dark; if (y === foot) v = .05;
      if (y === foot - 2 && x === fx + 2) v = .95 - dark;
      P.set(x, y, C.boot(v, x, y));
    }
  }
  function armSide(P, swing, t, dark, L) {
    const sx = 31, sy = 27 + t, hx = 31 + swing, hy = 40 + t - (Math.abs(swing) >= 3 ? 1 : 0);
    const n = 14;
    for (let i = 0; i <= n; i++) {
      const u = i / n, cx = Math.round(sx + (hx - sx) * u), cy = Math.round(sy + (hy - 3 - sy) * u);
      for (let k = -2; k <= 1; k++) {
        const x = cx + k, metal = u > .55;
        const v = (metal ? .78 : .42) - dark - (k + .5) * L * .1;
        P.set(x, cy, metal ? C.metal(v, x, cy) : C.leather(v, x, cy));
      }
    }
    for (let k = -2; k <= 1; k++) P.set(Math.round(hx) + k, hy - 2, C.brass(.5 - dark, 0, 0));
    fist(P, Math.round(hx), hy + 1, L, .62 - dark);
    return { hx: Math.round(hx), hy: hy + 1 };
  }
  function swordSide(P, hand, dark, L) {
    const dx = .55, dy = .85;
    P.rect(hand.hx - 2, hand.hy - 4, 2, 2, C.brass(.85 - dark, 0, 0));            // pomo atrás/acima do punho
    guard(P, hand.hx + dx * 3, hand.hy + dy * 3, dx, dy);
    blade(P, hand.hx + dx * 4, hand.hy + dy * 4, dx, dy, 11.5, L);
    fist(P, hand.hx, hand.hy, L, .62 - dark);
  }
  function headSide(P, hy, L) {
    P.ellipse(31.5, hy, 8.8, 9, (x, y, dx, dy) => {
      let v = .6 - dx * L * .3 - dy * .3; if ((x + (y >> 1)) % 4 === 0) v -= .13; return C.hair(v, x, y);
    });
    P.ellipse(34.6, hy + 2.2, 6.4, 6.6, (x, y, dx, dy) => C.skin(.7 - dx * L * .2 - dy * .12 + (dy > .8 ? -.14 : 0), x, y));
    const jag = [3, 2, 3, 2, 1, 2, 3, 2, 1, 2];
    for (let x = 30; x <= 39; x++) for (let y = hy - 5; y < hy - 4 + jag[x - 30]; y++) P.set(x, y, C.hair(.5, x, y));
    for (let y = hy - 2; y <= hy + 1; y++) for (let x = 29; x <= 31; x++) P.set(x, y, C.hair(.38, x, y));   // costeleta
    P.rect(29, hy + 1, 2, 3, C.skin(.42, 0, 0)); P.set(29, hy + 2, C.skin(.12, 0, 0));          // orelha
    P.rect(36, hy + 1, 2, 2, INK); P.set(36, hy + 1, SHINE); P.rect(35, hy - 1, 3, 1, C.hair(.15, 0, 0));
    P.rect(39, hy + 1, 1, 2, INK); P.rect(39, hy - 1, 2, 1, C.hair(.15, 0, 0));
    P.set(41, hy + 3, C.skin(.5, 0, 0)); P.set(41, hy + 2, C.skin(.7, 0, 0));                     // nariz
    P.set(39, hy + 5, C.skin(.08, 0, 0)); P.set(35, hy + 4, BLUSH);
  }
  function drawSide(P, p, L, nearIsRight) {
    const t = p.torso || 0, h = p.head || 0, fl = p.flare || 0;
    // perna/braço "near" (lado da câmera) e "far"
    const nearLeg = nearIsRight ? [p.fA || 0, p.lA || 0] : [p.fB || 0, p.lB || 0];
    const farLeg = nearIsRight ? [p.fB || 0, p.lB || 0] : [p.fA || 0, p.lA || 0];
    const nearArm = nearIsRight ? (p.aA || 0) : (p.aB || 0), farArm = nearIsRight ? (p.aB || 0) : (p.aA || 0);
    // capa (atrás): pende das costas e se abre para trás em V ao andar
    P.poly([[25, 23 + t], [33, 23 + t], [32, 49], [24 - fl, 51], [18 - fl * 2, 53], [21 - fl, 37 + t]], (x, y) => {
      let v = .5 - (x < 24 ? .05 : 0) + (y < 30 ? .1 : 0); if ((x * 2 + y) % 7 === 0) v -= .18; if (y >= 51) v -= .15;
      return C.cape(v, x, y);
    });
    P.rect(29, 20 + Math.min(t, h), 6, 8, C.skin(.18, 0, 0));
    const farHand = armSide(P, farArm, t, .3, L);
    if (!nearIsRight) swordSide(P, farHand, .12, L);
    legSide(P, farLeg[0], farLeg[1], .26, L);
    legSide(P, nearLeg[0], nearLeg[1], 0, L);
    P.poly([[24, 24 + t], [38, 24 + t], [39, 33 + t], [37, 43 + t], [25, 43 + t]], (x, y) => {
      const sx = (x - 24) / 15; let v = .45 + (L > 0 ? (1 - sx) : sx) * .2 + (y < 28 + t ? .08 : 0);
      if (x >= 37) v -= .14; return C.leather(v, x, y);
    });
    P.line(37, 25 + t, 34, 40 + t, C.leather(.1, 0, 0));   // talabarte
    P.set(36, 31 + t, C.brass(.8, 0, 0)); P.set(26, 30 + t, C.brass(.6, 0, 0));
    P.rect(25, 40 + t, 13, 2, C.leather(.06, 0, 0)); P.rect(36, 40 + t, 2, 1, C.brass(.95, 0, 0)); P.rect(36, 41 + t, 2, 1, C.brass(.5, 0, 0));
    P.poly([[25, 42 + t], [38, 42 + t], [39, 47 + t], [24, 47 + t]], (x, y) => {
      let v = .45 + (L > 0 ? .1 : -.05); if (x === 32) v = .08; if (y === 46 + t) v -= .25; return C.leather(v, x, y);
    });
    const nearHand = armSide(P, nearArm, t, 0, L);
    if (nearIsRight) swordSide(P, nearHand, 0, L);
    pauldron(P, 31, 27 + t, L, 4.3);
    headSide(P, 15 + h, L);
  }

  // —— API ——
  function drawFrame(P, mirror, dir, pose) {
    if (dir === 'Front') drawFrontBack(P, pose, false);
    else if (dir === 'Back') drawFrontBack(P, pose, true);
    else if (dir === 'Right') drawSide(P, pose, 1, true);
    else if (dir === 'Left') drawSide(mirror(P), pose, -1, false);   // espelhado: espada fica na mão do lado oposto à câmera
    else throw new Error('direção desconhecida ' + dir);
  }
  return { meta, PALETTE, ANIMATIONS, DIRECTIONS, drawFrame, outlineTint: hex(H.shadow), outlineK: .3, clamp01 };
}
