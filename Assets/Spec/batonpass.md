# 🏃‍♂️ BATON PASS: Photal Frame (Fatal Frame Clone) Handover

Este documento contém o estado detalhado do projeto para que outro agente ou sessão de chat possa assumir a tarefa exatamente de onde parou.

---

## 📋 Contexto do Projeto
- **Nome:** Photal Frame (Protótipo/Clone de Fatal Frame).
- **Engine/Ambiente:** Unity 6, Universal Render Pipeline (URP), novo Input System.
- **Diretório Local:** `g:\Prototipos\Photal Frame`
- **Estado de Compilação:** Compilação limpa (0 erros, 0 avisos críticos). Todos os 9 scripts validados.
- **Fases Implementadas:** Fase 1 a Fase 7 completas.

---

## 🛠️ Estado Atual: Fase 7 Concluída

A **Fase 7: Salvamento e Permanência** foi totalmente implementada, testada (save file criado, contagem regressiva, reload) e o menu `Tools/Photal Frame/Setup Scene` executado no Editor.

### Arquivos Modificados / Adicionados na Fase 7:

1. **[SaveSystem.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/SaveSystem.cs) [NEW]:**
   - Classe estática com `Save(SaveData)`, `Load()`, `SaveExists()`, `DeleteSave()`.
   - Persiste em JSON via `JsonUtility` em `Application.persistentDataPath + "/savegame.json"`.
   - `SaveData` serializável contém: posição/rotação, HP, upgrades (níveis + pontos/orbes), inventário (filmes, medicine, fitas), `ghostTestDefeated`, `collectedItemIds`.

2. **[SavePoint.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/SavePoint.cs) [NEW]:**
   - Gatilho de proximidade (2.5m) no update. Ao apertar **E** (Interact), chama `SaveMenuUI.OpenSavePrompt(this)`.
   - `PerformSave()`: coleta todos os dados do jogador, fantasma e inventário e chama `SaveSystem.Save()`.
   - Pequeno cubo dourado com lanterna flutuante, material emissivo.

3. **[SaveMenuUI.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/UI/SaveMenuUI.cs) [NEW]:**
   - Pop-up com botões CONFIRMAR/CANCELAR + atalhos **Y** (confirmar) e **ESC** (cancelar).
   - Ao confirmar: consome 1 Fita Virgem, salva, esconde botões, mostra contagem regressiva **"Saved! 3...2...1..."** (via `Time.unscaledDeltaTime`) e sai do pause automaticamente.
   - Propriedade `IsOpen` para evitar re-abertura.

4. **[CollectibleItem.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Player/CollectibleItem.cs) [MODIFY]:**
   - Adicionado campo `itemId` (string) como identificador único persistente.
   - Ao `Start()`, se `itemId` já foi coletado (via `PlayerInventory.WasItemCollected`), o objeto é destruído.
   - Ao coletar (`OnTriggerEnter`), registra o `itemId` no inventário.

5. **[PlayerInventory.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Player/PlayerInventory.cs) [MODIFY]:**
   - Adicionados métodos: `AddVirginTape`, `ConsumeVirginTape`, `RegisterCollectedItem`, `WasItemCollected`.
   - Setters para save/load: `SetFilmType61Count`, `SetFilmType90Count`, `SetHerbalMedicineCount`, `SetVirginTapeState`, `SetCollectedItems`.
   - Removido `using System` duplicado.

6. **[PlayerHealth.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Player/PlayerHealth.cs) [MODIFY]:**
   - `LoadFromSave()` agora restaura: posição, rotação, HP, upgrades (via reflection para pontos/orbes), inventário completo (filmes, medicine, fitas, collectedItemIds), destrói `Ghost_Test` se derrotado.
   - **Q** na tela de morte → carrega do save (`ShouldLoadOnStart = true` + reload).
   - **R** na tela de morte → reinicia cena do zero.
   - **O+P** → kill dev (teste).
   - `[RuntimeInitializeOnLoadMethod]` + `Awake()` garantem `Time.timeScale = 1f` ao iniciar (evita pausa residual com Domain Reload desligado).

7. **[SceneSetupHelper.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Editor/SceneSetupHelper.cs) [MODIFY]:**
   - Cria `SavePoint` (cubo dourado + lanterna) na posição (0, 0.5, 7).
   - Cria 2 fitas virgens coletáveis: `Collectible_VirginTape` (-1.5, 0.5, 2) e `Collectible_VirginTape2` (0, 0.5, 5.5).
   - Cria painel `SaveMenuPanel` sob o canvas com botões Confirmar/Cancelar, texto e `SaveMenuUI`.
   - Sempre religa referências dos botões mesmo se o painel já existir.
   - `CreateCollectible` agora atribui `itemId` (nome do objeto) para persistência.
   - Texto do GameOver: `"Q: Carregar Save  |  R: Reiniciar"`.

8. **[PlayerUI.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/UI/PlayerUI.cs) [MODIFY]:**
   - Já possuía campo `virginTapeText` e atualização no HUD.

9. **[InputReader.cs](file:///g:/Prototipos/Photal%20Frame/Assets/Scripts/Input/InputReader.cs) [MODIFY]:**
   - Já possuía ação `Interact` (tecla **E**) com fallback.

### Controles Atuais:
| Tecla | Ação |
|-------|------|
| WASD | Movimento |
| Shift | Correr |
| Clique Direito | Alternar visão (Viewfinder) |
| Clique Esquerdo | Fotografar (atacar) |
| **E** | Interagir (salvar no SavePoint) |
| **F** | Lente Especial (Paralisia) |
| **TAB** | Menu de Upgrades |
| **Q** | Carregar Save (na morte) |
| **R** | Reiniciar cena (na morte) |
| **Y** | Confirmar save (no pop-up) |
| **ESC** | Cancelar save (no pop-up) |
| **1/2** | Ciclar filmes |
| **H** | Curar (Medicine) |
| **O+P** | Kill dev (teste) |

---

## 🎯 Próxima Fase: Fase 8 - Atmosfera e Interface Final

**IMPORTANTE:** Ignorar áudio por enquanto. Focar apenas em:

1. **HUD mínimo:** Apenas contador de filme e (opcional) HP. Fora do viewfinder, quase nada.
2. **Pós-processamento:** Vinheta, grão de filme, distorção, partículas.
3. **Sistema de Umidade (opcional):** Molhado afeta dano/defesa; item para secar.

---

## 💡 Instruções para o Próximo Agente

1. **Setup Programático:** Toda UI é criada via `SceneSetupHelper.cs`. Execute `Tools/Photal Frame/Setup Scene` para recriar a cena.
2. **Física Sem Atrito:** O material `FrictionlessPlayer.physicMaterial` do PlayerCapsule deve ser preservado.
3. **Setup Manual se Necessário:** Se a cena estiver vazia, execute o Setup Scene para gerar corredor, jogador, fantasma, itens e save point.
4. **Próximo Passo:** Implementar áudio 3D e pós-processamento (Fase 8).
