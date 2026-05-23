# Plano de Desenvolvimento: Clone de Fatal Frame

## Fase 0: Preparação do Projeto
**Duração estimada:** 2-3 dias

- Escolha da Engine (recomendado Unity pela documentação e facilidade de prototipagem; Unreal também viável).
- Configuração inicial:
  - Projeto 3D com física básica.
  - Personagem simples com animações (idle, andar, correr) via Asset Store ou Mixamo.
  - Cenário de teste: corredores estreitos, salas escuras, iluminação reduzida.
  - Controle de versão (Git).

---

## Fase 1: Núcleo da Vulnerabilidade – Movimento e Câmera
**Objetivo:** Sentir o desamparo do personagem.

- Controle "Tanque": movimento sempre relativo ao personagem, nunca à câmera. Sem strafe.
- Câmera de Exploração: terceira pessoa com ângulo fixo/semi-fixo ou sobre o ombro com zoom limitado.
- Corrida Limitada: velocidade reduzida, resistência curta ou corrida lenta contínua.
- Transição para Viewfinder: botão dedicado (ex: clique direito) muda instantaneamente para primeira pessoa. Movimento ainda mais lento, rotação amortecida.

**Teste:** Navegar pelo cenário e sentir a lentidão. A transição deve transmitir vulnerabilidade.

---

## Fase 2: Coração – Camera Obscura (v1)
**Objetivo:** Fotografar alvos estáticos e causar dano.

- Viewfinder UI: moldura da câmera, círculo de captura, contador de filme.
- Mecânica de foto: raycast ou área cônica; captura um frame.
- Detecção de alvo: script "Fantasma" com campo `Hostil` (bool).
- Cálculo de dano básico: dano base do filme × multiplicador de distância (bônus por centralizar).
- Filme Inesgotável: munição infinita, dano mínimo.
- Feedback: fantasma pisca em negativo, partículas, dano flutuante.

**Teste:** Posicionar fantasma estático, mirar, fotografar e ver reação.

---

## Fase 3: O Fatal Frame
**Objetivo:** Mecânica de risco/recompensa suprema.

- Gatilho de ataque: evento `AtaqueIniciado` no início da animação de ataque.
- Janela de oportunidade: flag `PodeFatalFrame` ativa por ~0,5s.
- Lógica da foto: se disparar com flag ativa e fantasma no quadro → dano massivo (5x), efeito sonoro/visual, empurrão.
- Sincronia com animação de investida do fantasma.

**Teste:** Fantasma se aproxima, ataca, jogador acerta o timing. Sem Fatal Frame, dano insignificante.

---

## Fase 4: O Inimigo – IA Básica do Fantasma
**Objetivo:** Inimigo que se move erraticamente e ataca.

- Navegação Espectral: ignora paredes, move-se por waypoints ou flutuação ruidosa em direção ao jogador.
- Estados:
  - **Oculto:** invisível, aguarda gatilho.
  - **Aparição:** surge de parede/chão.
  - **Perseguição:** flutua, desaparece e ressurge.
  - **Ataque:** investida rápida, aciona janela de Fatal Frame.
  - **Recuo:** após foto, recua/desaparece brevemente.
- Filamento: agulha no HUD que aponta para o fantasma hostil mais próximo.

**Teste:** Combate completo com aparição, movimento, ataque e repulsão por fotos.

---

## Fase 5: Gestão de Recursos e Estado
**Objetivo:** Inventário funcional e consequências.

- Inventário: slots para filmes (quantidade) e itens de cura.
- Troca de filme: botão para ciclar tipos (Tipo 14, 61 etc.). Contador do viewfinder atualiza.
- Sistema de vida: barra de HP. Zerou → Game Over.
- Coleta de itens: pontos brilhantes no cenário.
- Dano ao jogador: contato com fantasma reduz HP; breve invencibilidade após dano.

**Teste:** Coletar, trocar, curar, gastar recursos durante confronto.

---

## Fase 6: Progressão – Pontos Espirituais e Upgrades
**Objetivo:** Fotografar recompensa com evolução.

- Cálculo de pontos por foto (proximidade, centralização, tipo de filme, Fatal Frame).
- Menu de upgrade: usar pontos para melhorar:
  - Poder base.
  - Velocidade de recarga.
  - Alcance.
  - Lentes (habilidades).
- Lentes: equipáveis, consomem recurso (Orbes Espirituais). Exemplo: Paralisar (congela fantasma por 3s).

**Teste:** Derrotar fantasmas, acumular pontos, comprar recarga, usar lente tática.

---

## Fase 7: Salvamento e Permanência
**Objetivo:** Tensão estratégica no progresso.

- Pontos de salvamento fixos (lanterna, cabine).
- Item consumível raro necessário para salvar ("Fita Virgem").
- Sistema de Save/Load: persiste inventário, HP, upgrades, posição, estado de inimigos derrotados.

**Teste:** Encontrar ponto, gastar item, salvar. Morrer e retornar exatamente ao estado salvo.

---

## Fase 8: Atmosfera e Interface Final
**Objetivo:** Imersão sensorial completa.

- Áudio 3D: passos, ambiente, sussurros direcionais. Som de estática crescente ao apontar para fantasma.
- HUD mínimo: apenas contador de filme e (opcional) HP. Fora do viewfinder, quase nada.
- Pós-processamento: vinheta, grão de filme, distorção, partículas.
- Sistema de Umidade (opcional): molhado afeta dano e defesa; item para secar.

**Teste:** Jogar segmento com fones, sentir o medo com áudio e escuridão.

---

## Fase 9: Conteúdo e Polimento
**Objetivo:** Transformar o protótipo em jogo completo.

- Puzzles e chaves: salas trancadas, itens-chave, inventário de chaves.
- Narrativa: diários, notas, história de fantasmas e rituais.
- Variação de inimigos: ataques diferentes, múltiplos fantasmas.
- Balanceamento: ajuste de danos, escassez, dificuldade.
- Otimização e polimento visual.

---

## Resumo da Ordem de Implementação

1. Movimento tanque, câmera de exploração, transição para viewfinder.
2. Viewfinder, fotografia, detecção básica.
3. Cálculo de dano e feedback.
4. Evento de ataque e janela de Fatal Frame.
5. IA do fantasma (movimento, aparição, perseguição, ataque).
6. Inventário, tipos de filme, HP, coleta.
7. Pontos espirituais e tela de upgrade.
8. Sistema de salvamento com item.
9. Áudio 3D e atmosfera.

Com essas fases concluídas, o "Vertical Slice" estará pronto: loop completo de exploração, combate, coleta, evolução e salvamento. O restante é conteúdo e refinamento.