# 🏃‍♂️ BATON PASS: Photal Frame (Fatal Frame Clone) Handover

Este documento contém o estado detalhado do projeto para que outro agente ou sessão de chat possa assumir a tarefa exatamente de onde parou.

---

## 📋 Contexto do Projeto
- **Nome:** Photal Frame (Protótipo/Clone de Fatal Frame).
- **Engine/Ambiente:** Unity 6, Universal Render Pipeline (URP), novo Input System.
- **Diretório Local:** `g:\Prototipos\Photal Frame`
- **Estado de Compilação:** Compilação limpa (0 erros, 0 avisos críticos).

---

## 🛠️ Estado Atual: Fase 6 Concluída e Configurada

A **Fase 6: Progressão, Pontos Espirituais e Upgrades** foi totalmente implementada e testada. O menu `Tools/Photal Frame/Setup Scene` foi executado no Editor, reconstruindo e vinculando todos os novos elementos na cena ativa (`SampleScene`).

### Arquivos Modificados / Adicionados na Fase 6:
1. **[PlayerUpgrades.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Player/PlayerUpgrades.cs) [NEW]:**
   - Gerencia `spiritPoints` e `spiritOrbs` (máx 3).
   - Armazena níveis (1 a 4) de **Poder** (base damage × $1.0$/$1.2$/$1.4$/$1.6$), **Recarga** (cooldown × $1.0$/$0.85$/$0.7$/$0.55$) e **Alcance** (distância × $1.0$/$1.15$/$1.3$/$1.45$).
2. **[UpgradeMenuUI.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/UI/UpgradeMenuUI.cs) [NEW]:**
   - Script anexado à raiz do **Canvas (`UI_Canvas`)** (para permanecer ativo e receber ticks de Update mesmo com o painel fechado).
   - Controla o painel do menu de upgrades aberto com **TAB** que pausa o jogo (`Time.timeScale = 0f`), libera o cursor e desativa movimentos/olhar.
   - Mostra níveis graficamente (ex: `[ X ][ X ][   ][   ]`) e deduz pontos ao melhorar atributos.
3. **[CameraObscura.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Camera/CameraObscura.cs) [MODIFY]:**
   - Aplica os upgrades de dano, alcance e recarga.
   - Calcula pontuação por foto (Base $100$ + Proximidade + Centralização × Multiplicador de Filme).
   - Adiciona bônus: `Core Shot` ($+200$ pts), `Close Shot` ($+300$ pts) e `Fatal Frame` ($+1000$ pts + restauração de 1 Orbe).
   - Aciona a Lente de Paralisia (**F**) consumindo 1 orbe se houver inimigo na mira.
4. **[GhostTarget.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Ghost/GhostTarget.cs) [MODIFY]:**
   - Implementa o estado `Paralyzed` (congela movimentos por 3s, pulsa a emissão spectral em tom dourado/azul).
   - Método `SpawnSpiritPointsText` cria textos flutuantes ciano/ouro indicando a pontuação e tipo de tiro.
5. **[PlayerUI.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/UI/PlayerUI.cs) [MODIFY]:**
   - Atualiza o HUD para exibir os pontos espirituais e os orbes espirituais (`LENS: ● ● ○`).
6. **[InputReader.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Input/InputReader.cs) [MODIFY]:**
   - Vincula **TAB** (Toggle Upgrade Menu) e **F** (Special Lens) com suporte a polling manual de fallback.
7. **[SceneSetupHelper.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Editor/SceneSetupHelper.cs) [MODIFY]:**
   - Programaticamente cria o painel de Upgrades no canvas, vincula componentes, adiciona `PlayerUpgrades` ao jogador, e anexa `UpgradeMenuUI` diretamente ao **Canvas** para garantir o processamento de inputs.

---

## 🎯 Próximo Passo: Fase 7 - Salvamento e Permanência

O plano de implementação para a Fase 7 já está estruturado em [implementation_plan.md](file:///C:/Users/Vi%27gurashi/.gemini/antigravity/brain/ba38b571-5929-43e4-a14b-eb8af32e5dfa/implementation_plan.md) com as seguintes mecânicas:

1. **Item Consumível Fita Virgem:**
   - Adicionar `virginTapeCount` no `PlayerInventory.cs`.
   - Adicionar `VirginTape` como `CollectibleType` em `CollectibleItem.cs` com identificadores únicos persistentes (`itemId`).
   - Atualizar HUD em `PlayerUI.cs` para exibir a quantidade de fitas virgens.
2. **Ponto de Salvamento Fixo:**
   - Criar `SavePoint.cs` que age como gatilho de proximidade e ouve interação do jogador.
   - Criar `SaveMenuUI.cs` sob o Canvas para abrir um pop-up de confirmação de salvamento (pausando o jogo e consumindo 1 Fita Virgem).
3. **Save/Load System JSON:**
   - Criar `SaveSystem.cs` para salvar/carregar dados via `JsonUtility` em `Application.persistentDataPath + "/savegame.json"`.
   - Dados a persistir: Posição/Rotação do jogador, HP atual, níveis de upgrades, inventário completo, se o fantasma de teste (`Ghost_Test`) foi derrotado, e lista de IDs dos itens coletados.
4. **Carregamento pós-Morte:**
   - Ajustar `PlayerHealth.cs` para que ao apertar **R** na tela de morte, se houver um arquivo de save, a cena recarregue definindo a flag `SaveSystem.ShouldLoadOnStart = true` para aplicar o save no frame 1.

---

## 💡 Instruções para o Próximo Agente

1. **Aprovação do Usuário:** O usuário já aprovou as 3 perguntas (usar tecla **E** para salvar, abrir pop-up de confirmação de save, e tecla **R** recarregar save na morte). Pode partir direto para a implementação do plano.
2. **Setup Programático:** Lembre-se de manter toda a estrutura de UI criada programaticamente no script `SceneSetupHelper.cs` para permitir que o usuário recrie a cena a qualquer momento.
3. **Física Sem Atrito:** O material físico de atrito zero do PlayerCapsule deve ser preservado.
4. **Passo a Passo Recomendado:**
   - **Passo 1:** Atualize `PlayerInventory.cs`, `PlayerHealth.cs` e `PlayerUpgrades.cs` para suportar Fitas Virgens e os métodos de carga/aplicação de estado.
   - **Passo 2:** Modifique `CollectibleItem.cs` para implementar o ID único persistente e lógica de destruição se já coletado.
   - **Passo 3:** Crie `SaveSystem.cs` gerenciando a gravação/leitura do JSON e aplicação de estado (incluindo destruição do fantasma e itens coletados).
   - **Passo 4:** Crie `SavePoint.cs` e `SaveMenuUI.cs` para interação física e visual de salvamento.
   - **Passo 5:** Atualize `InputReader.cs` para detectar a tecla **E** (Interact) e `PlayerUI.cs` para atualizar o HUD e o comportamento de Game Over.
   - **Passo 6:** Atualize `SceneSetupHelper.cs` para gerar o Ponto de Salvamento na cena, o painel pop-up sob o canvas, a fita coletável no corredor de testes e as novas referências. Execute a Action `Tools/Photal Frame/Setup Scene` na Unity via Unity MCP para testar.
   - **Passo 7:** Valide a persistência salvando, alterando status, morrendo e recarregando.
