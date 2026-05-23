using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PhotalFrame.Player;
using PhotalFrame.Camera;
using PhotalFrame.Input;
using PhotalFrame.UI;
using PhotalFrame.Ghost;
using PhotalFrame.World;

namespace PhotalFrame.Editor
{
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Tools/Photal Frame/Setup Scene")]
        public static void SetupScene()
        {
            // 1. Find or create the Player
            GameObject player = GameObject.Find("Capsule");
            if (player == null)
            {
                player = GameObject.Find("PlayerCapsule");
            }
            if (player == null)
            {
                player = GameObject.FindWithTag("Player");
            }
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "PlayerCapsule";
                player.tag = "Player";
                player.transform.position = new Vector3(0f, 1f, 0f);
            }
            else
            {
                player.name = "PlayerCapsule";
                player.tag = "Player";
            }

            // Always reset player position to start on the test corridor floor
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            player.transform.rotation = Quaternion.identity;

            // Ensure visual forward indicator (visor) exists on Player Capsule
            Transform visorTrans = player.transform.Find("Visor");
            if (visorTrans == null)
            {
                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Visor";
                visor.transform.SetParent(player.transform);
                
                // Position it at head height, facing forward (Local Z)
                visor.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
                visor.transform.localRotation = Quaternion.identity;
                visor.transform.localScale = new Vector3(0.6f, 0.15f, 0.2f);
                
                // Destroy its collider so it doesn't interfere with physics/raycasts
                Collider visorCol = visor.GetComponent<Collider>();
                if (visorCol != null) UnityEngine.Object.DestroyImmediate(visorCol);
                
                // Apply a dark material for contrast
                Material darkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkLitMaterial.mat");
                if (darkMat != null)
                {
                    visor.GetComponent<Renderer>().sharedMaterial = darkMat;
                }
            }

            // 2. Setup Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody>();
            }
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Fixes camera tracking jitter

            // Ensure capsule collider exists and is properly sized
            CapsuleCollider col = player.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = player.AddComponent<CapsuleCollider>();
            }
            col.height = 2f;
            col.center = Vector3.zero;
            col.isTrigger = false;
            col.enabled = true;

            // Create and assign a frictionless Physic Material to slide smoothly along walls
            PhysicsMaterial playerMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Materials/FrictionlessPlayer.physicMaterial");
            if (playerMat == null)
            {
                playerMat = new PhysicsMaterial("FrictionlessPlayer");
                playerMat.dynamicFriction = 0f;
                playerMat.staticFriction = 0f;
                playerMat.frictionCombine = PhysicsMaterialCombine.Minimum;
                
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(playerMat, "Assets/Materials/FrictionlessPlayer.physicMaterial");
            }
            col.sharedMaterial = playerMat;

            // 3. Setup Input Reader
            InputReader inputReader = player.GetComponent<InputReader>();
            if (inputReader == null)
            {
                inputReader = player.AddComponent<InputReader>();
            }
            
            // Try to find the Input Actions asset in the project
            string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                
                SerializedObject so = new SerializedObject(inputReader);
                so.FindProperty("inputActionsAsset").objectReferenceValue = asset;
                so.ApplyModifiedProperties();
            }

            // 4. Setup Player Controller, Health, and Inventory
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = player.AddComponent<PlayerController>();
            }
            
            // Link InputReader to PlayerController
            SerializedObject soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("inputReader").objectReferenceValue = inputReader;
            soPlayer.ApplyModifiedProperties();

            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.AddComponent<PlayerHealth>();
            }

            PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory == null)
            {
                playerInventory = player.AddComponent<PlayerInventory>();
            }
            
            // Link references for PlayerInventory
            SerializedObject soInventory = new SerializedObject(playerInventory);
            soInventory.FindProperty("inputReader").objectReferenceValue = inputReader;
            soInventory.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            // Set starting values for testing so user gets some items immediately
            soInventory.FindProperty("startingType61").intValue = 10;
            soInventory.FindProperty("startingType90").intValue = 5;
            soInventory.FindProperty("startingHerbalMedicine").intValue = 2;
            soInventory.ApplyModifiedProperties();

            // Setup PlayerUpgrades component
            PlayerUpgrades playerUpgrades = player.GetComponent<PlayerUpgrades>();
            if (playerUpgrades == null)
            {
                playerUpgrades = player.AddComponent<PlayerUpgrades>();
            }

            // 5. Setup Flashlight (Spotlight)
            Light flashlight = null;
            foreach (Light l in player.GetComponentsInChildren<Light>())
            {
                if (l.type == LightType.Spot)
                {
                    flashlight = l;
                    break;
                }
            }

            if (flashlight == null)
            {
                GameObject lightGo = new GameObject("Flashlight");
                lightGo.transform.SetParent(player.transform);
                lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0.4f); // relative chest level
                lightGo.transform.localRotation = Quaternion.identity;
                
                flashlight = lightGo.AddComponent<Light>();
                flashlight.type = LightType.Spot;
                flashlight.range = 18f;
                flashlight.spotAngle = 40f;
                flashlight.intensity = 3f;
                flashlight.color = new Color(0.95f, 0.95f, 0.85f);
            }

            // 6. Setup Camera, AudioSource and CameraObscura
            GameObject mainCam = GameObject.FindWithTag("MainCamera");
            if (mainCam == null)
            {
                mainCam = new GameObject("Main Camera");
                mainCam.tag = "MainCamera";
                mainCam.AddComponent<UnityEngine.Camera>();
            }

            if (mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.AddComponent<AudioListener>();
            }

            // Add AudioSource for Camera Obscura sound effects
            AudioSource cameraAudio = mainCam.GetComponent<AudioSource>();
            if (cameraAudio == null)
            {
                cameraAudio = mainCam.AddComponent<AudioSource>();
            }
            cameraAudio.playOnAwake = false;
            cameraAudio.spatialBlend = 0f; // 2D sound for camera UI/shutter

            CameraController cameraController = mainCam.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = mainCam.AddComponent<CameraController>();
            }

            // Link CameraController fields
            SerializedObject soCam = new SerializedObject(cameraController);
            soCam.FindProperty("playerController").objectReferenceValue = playerController;
            soCam.FindProperty("inputReader").objectReferenceValue = inputReader;
            soCam.ApplyModifiedProperties();

            // Add Camera Obscura script
            CameraObscura cameraObscura = mainCam.GetComponent<CameraObscura>();
            if (cameraObscura == null)
            {
                cameraObscura = mainCam.AddComponent<CameraObscura>();
            }

            // Link CameraObscura fields
            SerializedObject soObscura = new SerializedObject(cameraObscura);
            soObscura.FindProperty("playerController").objectReferenceValue = playerController;
            soObscura.FindProperty("inputReader").objectReferenceValue = inputReader;
            soObscura.FindProperty("playerInventory").objectReferenceValue = playerInventory;
            soObscura.FindProperty("playerUpgrades").objectReferenceValue = playerUpgrades;
            soObscura.ApplyModifiedProperties();

            // 7. Setup UI Canvas
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("UI_Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            // 8. Create Viewfinder Panel
            Transform viewfinderTrans = canvasGo.transform.Find("ViewfinderPanel");
            GameObject viewfinderPanel;
            Image reticleImage = null;
            if (viewfinderTrans == null)
            {
                viewfinderPanel = new GameObject("ViewfinderPanel");
                viewfinderPanel.transform.SetParent(canvasGo.transform, false);
                
                RectTransform rect = viewfinderPanel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                // Add dark vignette borders
                GameObject vignette = new GameObject("Vignette");
                vignette.transform.SetParent(viewfinderPanel.transform, false);
                Image vignetteImg = vignette.AddComponent<Image>();
                vignetteImg.color = new Color(0f, 0f, 0f, 0.45f);
                RectTransform vigRect = vignette.GetComponent<RectTransform>();
                vigRect.anchorMin = Vector2.zero;
                vigRect.anchorMax = Vector2.one;
                vigRect.sizeDelta = Vector2.zero;

                // Add circular reticle outline
                GameObject reticle = new GameObject("ReticleCircle");
                reticle.transform.SetParent(viewfinderPanel.transform, false);
                reticleImage = reticle.AddComponent<Image>();
                reticleImage.color = new Color(0.9f, 0.9f, 0.9f, 0.4f);
                
                Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (knobSprite != null) reticleImage.sprite = knobSprite;
                
                RectTransform reticleRect = reticle.GetComponent<RectTransform>();
                reticleRect.sizeDelta = new Vector2(40f, 40f);
            }
            else
            {
                viewfinderPanel = viewfinderTrans.gameObject;
                Transform reticleCircle = viewfinderPanel.transform.Find("ReticleCircle");
                if (reticleCircle != null)
                {
                    reticleImage = reticleCircle.GetComponent<Image>();
                }
            }

            // Create Fatal Warning Text under ViewfinderPanel
            Transform fatalTrans = viewfinderPanel.transform.Find("FatalWarning");
            GameObject fatalGo;
            if (fatalTrans == null)
            {
                fatalGo = new GameObject("FatalWarning");
                fatalGo.transform.SetParent(viewfinderPanel.transform, false);

                RectTransform rect = fatalGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector3(0f, 65f, 0f); // just above reticle
                rect.sizeDelta = new Vector2(200f, 40f);

                Text warningText = fatalGo.AddComponent<Text>();
                warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (warningText.font == null) warningText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                warningText.text = "FATAL";
                warningText.fontSize = 28;
                warningText.fontStyle = FontStyle.Bold;
                warningText.alignment = TextAnchor.MiddleCenter;
                warningText.color = new Color(1f, 0.1f, 0.1f, 0.9f);
                
                fatalGo.AddComponent<Outline>().effectColor = Color.black;
                fatalGo.AddComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);
            }
            else
            {
                fatalGo = fatalTrans.gameObject;
            }

            // Create Shutter Flash Panel
            Transform flashTrans = canvasGo.transform.Find("ShutterFlash");
            GameObject flashGo;
            CanvasGroup flashCanvasGroup = null;
            if (flashTrans == null)
            {
                flashGo = new GameObject("ShutterFlash");
                flashGo.transform.SetParent(canvasGo.transform, false);
                flashCanvasGroup = flashGo.AddComponent<CanvasGroup>();
                flashCanvasGroup.alpha = 0f;
                flashCanvasGroup.blocksRaycasts = false;
                flashCanvasGroup.interactable = false;

                RectTransform rect = flashGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                Image flashImg = flashGo.AddComponent<Image>();
                flashImg.color = Color.white;
            }
            else
            {
                flashGo = flashTrans.gameObject;
                flashCanvasGroup = flashGo.GetComponent<CanvasGroup>();
            }

            // Create Recharge Slider under ViewfinderPanel
            Transform rechargeTrans = viewfinderPanel.transform.Find("RechargeSlider");
            GameObject rechargeGo;
            Slider rechargeSlider = null;
            if (rechargeTrans == null)
            {
                rechargeGo = new GameObject("RechargeSlider");
                rechargeGo.transform.SetParent(viewfinderPanel.transform, false);

                RectTransform rect = rechargeGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector3(0f, -50f, 0f);
                rect.sizeDelta = new Vector2(100f, 6f);

                rechargeSlider = rechargeGo.AddComponent<Slider>();

                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(rechargeGo.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(rechargeGo.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;

                rechargeSlider.targetGraphic = fillImg;
                rechargeSlider.fillRect = fillRect;
            }
            else
            {
                rechargeGo = rechargeTrans.gameObject;
                rechargeSlider = rechargeGo.GetComponent<Slider>();
            }

            // Create Film Text under ViewfinderPanel
            Transform filmTrans = viewfinderPanel.transform.Find("FilmText");
            GameObject filmGo;
            Text filmText = null;
            if (filmTrans == null)
            {
                filmGo = new GameObject("FilmText");
                filmGo.transform.SetParent(viewfinderPanel.transform, false);

                RectTransform rect = filmGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.9f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.1f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector3(0f, 0f, 0f);
                rect.sizeDelta = new Vector2(200f, 35f);

                filmText = filmGo.AddComponent<Text>();
                filmText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (filmText.font == null) filmText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                filmText.fontSize = 20;
                filmText.alignment = TextAnchor.LowerRight;
                filmText.color = new Color(0.7f, 0.9f, 1f, 0.7f);
                
                filmGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);
            }
            else
            {
                filmGo = filmTrans.gameObject;
                filmText = filmGo.GetComponent<Text>();
            }

            // 9. Create Stamina Panel
            Transform staminaTrans = canvasGo.transform.Find("StaminaPanel");
            GameObject staminaPanel;
            Slider staminaSlider = null;
            CanvasGroup staminaCanvasGroup = null;
            if (staminaTrans == null)
            {
                staminaPanel = new GameObject("StaminaPanel");
                staminaPanel.transform.SetParent(canvasGo.transform, false);
                staminaCanvasGroup = staminaPanel.AddComponent<CanvasGroup>();

                RectTransform rect = staminaPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.05f);
                rect.anchorMax = new Vector2(0.5f, 0.05f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector3(0f, 20f, 0f);
                rect.sizeDelta = new Vector2(250f, 12f);

                staminaSlider = staminaPanel.AddComponent<Slider>();
                
                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(staminaPanel.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(staminaPanel.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.2f, 0.85f, 0.4f, 0.75f);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;

                staminaSlider.targetGraphic = fillImg;
                staminaSlider.fillRect = fillRect;
            }
            else
            {
                staminaPanel = staminaTrans.gameObject;
                staminaSlider = staminaPanel.GetComponent<Slider>();
                staminaCanvasGroup = staminaPanel.GetComponent<CanvasGroup>();
            }

            // 9.5 Create Resource and Health Panel UI under UI_Canvas
            Transform oldHealth = canvasGo.transform.Find("HealthSlider");
            if (oldHealth != null && oldHealth.parent == canvasGo.transform)
            {
                UnityEngine.Object.DestroyImmediate(oldHealth.gameObject);
            }
            Transform oldMed = canvasGo.transform.Find("MedicineText");
            if (oldMed != null && oldMed.parent == canvasGo.transform)
            {
                UnityEngine.Object.DestroyImmediate(oldMed.gameObject);
            Transform oldTape = canvasGo.transform.Find("VirginTapeText");
            if (oldTape != null && oldTape.parent == canvasGo.transform)
            {
                UnityEngine.Object.DestroyImmediate(oldTape.gameObject);
            }
            }

            Transform healthPanelTrans = canvasGo.transform.Find("HealthPanel");
            GameObject healthPanelGo;
            Slider healthSlider = null;
            Slider damageSlider = null;
            Text medicineText = null;
            Text statusText = null;
            Text virginTapeText = null;

            if (healthPanelTrans == null)
            {
                healthPanelGo = new GameObject("HealthPanel");
                healthPanelGo.transform.SetParent(canvasGo.transform, false);

                RectTransform panelRect = healthPanelGo.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.05f, 0.05f);
                panelRect.anchorMax = new Vector2(0.05f, 0.05f);
                panelRect.pivot = new Vector2(0f, 0f);
                panelRect.anchoredPosition = new Vector3(20f, 20f, 0f);
                panelRect.sizeDelta = new Vector2(220f, 60f);

                // --- 1. DAMAGE CATCH-UP SLIDER (Background & Red Lag Bar) ---
                GameObject damageGo = new GameObject("DamageCatchUpSlider");
                damageGo.transform.SetParent(healthPanelGo.transform, false);
                RectTransform damageRect = damageGo.AddComponent<RectTransform>();
                damageRect.anchorMin = Vector2.zero;
                damageRect.anchorMax = Vector2.zero;
                damageRect.pivot = Vector2.zero;
                damageRect.anchoredPosition = new Vector3(0f, 5f, 0f);
                damageRect.sizeDelta = new Vector2(200f, 12f);

                damageSlider = damageGo.AddComponent<Slider>();
                damageSlider.interactable = false;
                damageSlider.transition = Selectable.Transition.None;

                // Damage Slider Background
                GameObject dbg = new GameObject("Background");
                dbg.transform.SetParent(damageGo.transform, false);
                Image dbgImg = dbg.AddComponent<Image>();
                dbgImg.color = new Color(0.12f, 0.02f, 0.02f, 0.8f);
                RectTransform dbgRect = dbg.GetComponent<RectTransform>();
                dbgRect.anchorMin = Vector2.zero;
                dbgRect.anchorMax = Vector2.one;
                dbgRect.sizeDelta = Vector2.zero;

                // Damage Slider Fill Area
                GameObject dFillArea = new GameObject("Fill Area");
                dFillArea.transform.SetParent(damageGo.transform, false);
                RectTransform dFillAreaRect = dFillArea.AddComponent<RectTransform>();
                dFillAreaRect.anchorMin = Vector2.zero;
                dFillAreaRect.anchorMax = Vector2.one;
                dFillAreaRect.sizeDelta = Vector2.zero;

                // Damage Slider Fill
                GameObject dFill = new GameObject("Fill");
                dFill.transform.SetParent(dFillArea.transform, false);
                Image dFillImg = dFill.AddComponent<Image>();
                dFillImg.color = new Color(0.55f, 0.1f, 0.1f, 0.85f);
                RectTransform dFillRect = dFill.GetComponent<RectTransform>();
                dFillRect.anchorMin = Vector2.zero;
                dFillRect.anchorMax = Vector2.one;
                dFillRect.sizeDelta = Vector2.zero;

                damageSlider.targetGraphic = dFillImg;
                damageSlider.fillRect = dFillRect;

                // --- 2. ACTIVE HEALTH SLIDER (Transparent background, Cyan/Green Fill) ---
                GameObject activeHealthGo = new GameObject("HealthSlider");
                activeHealthGo.transform.SetParent(healthPanelGo.transform, false);
                RectTransform activeHealthRect = activeHealthGo.AddComponent<RectTransform>();
                activeHealthRect.anchorMin = Vector2.zero;
                activeHealthRect.anchorMax = Vector2.zero;
                activeHealthRect.pivot = Vector2.zero;
                activeHealthRect.anchoredPosition = new Vector3(0f, 5f, 0f);
                activeHealthRect.sizeDelta = new Vector2(200f, 12f);

                healthSlider = activeHealthGo.AddComponent<Slider>();
                healthSlider.interactable = false;
                healthSlider.transition = Selectable.Transition.None;

                // Health Slider Fill Area
                GameObject hFillArea = new GameObject("Fill Area");
                hFillArea.transform.SetParent(activeHealthGo.transform, false);
                RectTransform hFillAreaRect = hFillArea.AddComponent<RectTransform>();
                hFillAreaRect.anchorMin = Vector2.zero;
                hFillAreaRect.anchorMax = Vector2.one;
                hFillAreaRect.sizeDelta = Vector2.zero;

                // Health Slider Fill
                GameObject hFill = new GameObject("Fill");
                hFill.transform.SetParent(hFillArea.transform, false);
                Image hFillImg = hFill.AddComponent<Image>();
                hFillImg.color = new Color(0.15f, 0.8f, 0.9f, 0.85f);
                RectTransform hFillRect = hFill.GetComponent<RectTransform>();
                hFillRect.anchorMin = Vector2.zero;
                hFillRect.anchorMax = Vector2.one;
                hFillRect.sizeDelta = Vector2.zero;

                healthSlider.targetGraphic = hFillImg;
                healthSlider.fillRect = hFillRect;

                // --- 3. STATUS TEXT ---
                GameObject statusGo = new GameObject("StatusText");
                statusGo.transform.SetParent(healthPanelGo.transform, false);
                RectTransform statusTextRect = statusGo.AddComponent<RectTransform>();
                statusTextRect.anchorMin = Vector2.zero;
                statusTextRect.anchorMax = Vector2.zero;
                statusTextRect.pivot = Vector2.zero;
                statusTextRect.anchoredPosition = new Vector3(0f, 22f, 0f);
                statusTextRect.sizeDelta = new Vector2(90f, 25f);

                statusText = statusGo.AddComponent<Text>();
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (statusText.font == null) statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                statusText.text = "FINE";
                statusText.fontSize = 18;
                statusText.fontStyle = FontStyle.Bold;
                statusText.alignment = TextAnchor.MiddleLeft;
                statusText.color = new Color(0.15f, 0.8f, 0.9f, 0.85f);
                statusGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);

                // --- 4. MEDICINE TEXT ---
                GameObject medGo = new GameObject("MedicineText");
                medGo.transform.SetParent(healthPanelGo.transform, false);
                RectTransform medTextRect = medGo.AddComponent<RectTransform>();
                medTextRect.anchorMin = Vector2.zero;
                medTextRect.anchorMax = Vector2.zero;
                medTextRect.pivot = Vector2.zero;
                medTextRect.anchoredPosition = new Vector3(100f, 22f, 0f);
                medTextRect.sizeDelta = new Vector2(100f, 25f);

                medicineText = medGo.AddComponent<Text>();
                medicineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (medicineText.font == null) medicineText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                medicineText.text = "Med: 0";
                medicineText.fontSize = 18;
                medicineText.fontStyle = FontStyle.Bold;
                medicineText.alignment = TextAnchor.MiddleRight;
            // Create Virgin Tape count text next to Medicine
            GameObject tapeGo = new GameObject("VirginTapeText");
            tapeGo.transform.SetParent(healthPanelGo.transform, false);
            RectTransform tapeRect = tapeGo.AddComponent<RectTransform>();
            tapeRect.anchorMin = new Vector2(0f, 0f);
            tapeRect.anchorMax = new Vector2(0f, 0f);
            tapeRect.pivot = new Vector2(0f, 1f);
            tapeRect.anchoredPosition = new Vector3(0f, -5f, 0f);
            tapeRect.sizeDelta = new Vector2(200f, 20f);

            virginTapeText = tapeGo.AddComponent<Text>();
            virginTapeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (virginTapeText.font == null) virginTapeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            virginTapeText.text = "Tape: 0";
            virginTapeText.fontSize = 18;
            virginTapeText.fontStyle = FontStyle.Bold;
            virginTapeText.alignment = TextAnchor.MiddleRight;
            virginTapeText.color = new Color(0.95f, 0.8f, 0.1f, 0.8f);
            virginTapeText.gameObject.AddComponent<Shadow>().effectColor = Color.black;
                medicineText.color = new Color(0.4f, 0.85f, 0.4f, 0.8f);
                medGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            }
            else
            {
                healthPanelGo = healthPanelTrans.gameObject;
                healthSlider = healthPanelGo.transform.Find("HealthSlider").GetComponent<Slider>();
                damageSlider = healthPanelGo.transform.Find("DamageCatchUpSlider").GetComponent<Slider>();
                statusText = healthPanelGo.transform.Find("StatusText").GetComponent<Text>();
                medicineText = healthPanelGo.transform.Find("MedicineText").GetComponent<Text>();
                virginTapeText = healthPanelGo.transform.Find("VirginTapeText")?.GetComponent<Text>();
                if (virginTapeText == null)
                {
                    GameObject tapeGo2 = new GameObject("VirginTapeText");
                    tapeGo2.transform.SetParent(healthPanelGo.transform, false);
                    RectTransform tapeRect2 = tapeGo2.AddComponent<RectTransform>();
                    tapeRect2.anchorMin = new Vector2(0f, 0f);
                    tapeRect2.anchorMax = new Vector2(0f, 0f);
                    tapeRect2.pivot = new Vector2(0f, 1f);
                    tapeRect2.anchoredPosition = new Vector3(0f, -5f, 0f);
                    tapeRect2.sizeDelta = new Vector2(200f, 20f);
                    virginTapeText = tapeGo2.AddComponent<Text>();
                    virginTapeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (virginTapeText.font == null) virginTapeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    virginTapeText.text = "Tape: 0";
                    virginTapeText.fontSize = 18;
                    virginTapeText.fontStyle = FontStyle.Bold;
                    virginTapeText.alignment = TextAnchor.MiddleRight;
                    virginTapeText.color = new Color(0.95f, 0.8f, 0.1f, 0.8f);
                    virginTapeText.gameObject.AddComponent<Shadow>().effectColor = Color.black;
                }
            }

            Transform dmgTrans = canvasGo.transform.Find("DamageVignette");
            GameObject dmgGo;
            CanvasGroup dmgCanvasGroup = null;
            if (dmgTrans == null)
            {
                dmgGo = new GameObject("DamageVignette");
                dmgGo.transform.SetParent(canvasGo.transform, false);

                RectTransform rect = dmgGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                Image dmgImg = dmgGo.AddComponent<Image>();
                dmgImg.color = new Color(1f, 0f, 0f, 0.25f);

                dmgCanvasGroup = dmgGo.AddComponent<CanvasGroup>();
                dmgCanvasGroup.alpha = 0f;
                dmgCanvasGroup.blocksRaycasts = false;
                dmgCanvasGroup.interactable = false;
            }
            else
            {
                dmgGo = dmgTrans.gameObject;
                dmgCanvasGroup = dmgGo.GetComponent<CanvasGroup>();
            }

            Transform gameOverTrans = canvasGo.transform.Find("GameOverPanel");
            GameObject gameOverPanel = null;
            if (gameOverTrans == null)
            {
                gameOverPanel = new GameObject("GameOverPanel");
                gameOverPanel.transform.SetParent(canvasGo.transform, false);

                RectTransform rect = gameOverPanel.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                Image bgImg = gameOverPanel.AddComponent<Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0.85f);

                GameObject titleGo = new GameObject("TitleText");
                titleGo.transform.SetParent(gameOverPanel.transform, false);
                RectTransform titleRect = titleGo.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 0.6f);
                titleRect.anchorMax = new Vector2(0.5f, 0.6f);
                titleRect.anchoredPosition = Vector3.zero;
                titleRect.sizeDelta = new Vector2(400f, 50f);

                Text titleText = titleGo.AddComponent<Text>();
                titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                titleText.text = "GAME OVER";
                titleText.fontSize = 42;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = new Color(0.9f, 0.1f, 0.1f);

                GameObject subGo = new GameObject("SubtitleText");
                subGo.transform.SetParent(gameOverPanel.transform, false);
                RectTransform subRect = subGo.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0.5f, 0.4f);
                subRect.anchorMax = new Vector2(0.5f, 0.4f);
                subRect.anchoredPosition = Vector3.zero;
                subRect.sizeDelta = new Vector2(400f, 40f);

                Text subText = subGo.AddComponent<Text>();
                subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (subText.font == null) subText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                subText.text = "Pressione R para Reiniciar";
                subText.fontSize = 20;
                subText.alignment = TextAnchor.MiddleCenter;
                subText.color = Color.white;

                gameOverPanel.SetActive(false);
            }
            else
            {
                gameOverPanel = gameOverTrans.gameObject;
            }

            // 9.6 Create Spirit Points Text in top-right
            Transform ptsTrans = canvasGo.transform.Find("SpiritPointsText");
            GameObject ptsGo;
            Text ptsText = null;
            if (ptsTrans == null)
            {
                ptsGo = new GameObject("SpiritPointsText");
                ptsGo.transform.SetParent(canvasGo.transform, false);

                RectTransform rect = ptsGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.95f, 0.95f);
                rect.anchorMax = new Vector2(0.95f, 0.95f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector3(-20f, -20f, 0f);
                rect.sizeDelta = new Vector2(250f, 35f);

                ptsText = ptsGo.AddComponent<Text>();
                ptsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (ptsText.font == null) ptsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                ptsText.fontSize = 20;
                ptsText.fontStyle = FontStyle.Bold;
                ptsText.alignment = TextAnchor.UpperRight;
                ptsText.color = new Color(0.2f, 0.85f, 1f, 0.85f);
                
                ptsGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            }
            else
            {
                ptsGo = ptsTrans.gameObject;
                ptsText = ptsGo.GetComponent<Text>();
            }

            // 9.7 Create Spirit Orbs Text in viewfinder (above FilmText)
            Transform orbsTrans = viewfinderPanel.transform.Find("SpiritOrbsText");
            GameObject orbsGo;
            Text orbsText = null;
            if (orbsTrans == null)
            {
                orbsGo = new GameObject("SpiritOrbsText");
                orbsGo.transform.SetParent(viewfinderPanel.transform, false);

                RectTransform rect = orbsGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.9f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.1f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector3(0f, 35f, 0f); // 35 units above filmText
                rect.sizeDelta = new Vector2(200f, 35f);

                orbsText = orbsGo.AddComponent<Text>();
                orbsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (orbsText.font == null) orbsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                orbsText.fontSize = 18;
                orbsText.fontStyle = FontStyle.Bold;
                orbsText.alignment = TextAnchor.LowerRight;
                orbsText.color = new Color(0.95f, 0.8f, 0.1f, 0.85f); // Golden
                
                orbsGo.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            }
            else
            {
                orbsGo = orbsTrans.gameObject;
                orbsText = orbsGo.GetComponent<Text>();
            }

            // 9.8 Create Upgrade Menu Panel
            Transform menuTrans = canvasGo.transform.Find("UpgradeMenuPanel");
            GameObject menuPanelGo = null;
            Text menuPointsText = null;
            Text pLvlText = null, pCostText = null;
            Text rLvlText = null, rCostText = null;
            Text gLvlText = null, gCostText = null;
            Button pBtn = null, rBtn = null, gBtn = null;

            if (menuTrans == null)
            {
                menuPanelGo = new GameObject("UpgradeMenuPanel");
                menuPanelGo.transform.SetParent(canvasGo.transform, false);

                RectTransform menuRect = menuPanelGo.AddComponent<RectTransform>();
                menuRect.anchorMin = Vector2.zero;
                menuRect.anchorMax = Vector2.one;
                menuRect.sizeDelta = Vector2.zero;

                Image bgImg = menuPanelGo.AddComponent<Image>();
                bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f); // Dark blue-black tone

                // Menu Title
                GameObject titleGo = new GameObject("TitleText");
                titleGo.transform.SetParent(menuPanelGo.transform, false);
                RectTransform tRect = titleGo.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.5f, 0.85f);
                tRect.anchorMax = new Vector2(0.5f, 0.85f);
                tRect.pivot = new Vector2(0.5f, 1f);
                tRect.sizeDelta = new Vector2(400f, 50f);

                Text tText = titleGo.AddComponent<Text>();
                tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (tText.font == null) tText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                tText.text = "EVOLUÇÃO DA CÂMERA";
                tText.fontSize = 32;
                tText.fontStyle = FontStyle.Bold;
                tText.alignment = TextAnchor.MiddleCenter;
                tText.color = new Color(0.2f, 0.85f, 1f, 0.95f);
                titleGo.AddComponent<Shadow>().effectColor = Color.black;

                // Menu Points Display
                GameObject menuPtsGo = new GameObject("PointsText");
                menuPtsGo.transform.SetParent(menuPanelGo.transform, false);
                RectTransform mpRect = menuPtsGo.AddComponent<RectTransform>();
                mpRect.anchorMin = new Vector2(0.5f, 0.75f);
                mpRect.anchorMax = new Vector2(0.5f, 0.75f);
                mpRect.pivot = new Vector2(0.5f, 0.5f);
                mpRect.anchoredPosition = new Vector3(0f, 150f, 0f);
                mpRect.sizeDelta = new Vector2(400f, 40f);

                menuPointsText = menuPtsGo.AddComponent<Text>();
                menuPointsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (menuPointsText.font == null) menuPointsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                menuPointsText.text = "PONTOS ESPIRITUAIS: 0";
                menuPointsText.fontSize = 20;
                menuPointsText.alignment = TextAnchor.MiddleCenter;
                menuPointsText.color = Color.white;
                menuPtsGo.AddComponent<Shadow>().effectColor = Color.black;

                // --- Attributes Rows Container ---
                GameObject rowsGo = new GameObject("Rows");
                rowsGo.transform.SetParent(menuPanelGo.transform, false);
                RectTransform rowsRect = rowsGo.AddComponent<RectTransform>();
                rowsRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowsRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowsRect.pivot = new Vector2(0.5f, 0.5f);
                rowsRect.sizeDelta = new Vector2(500f, 200f);

                // Row 1: Power (Y = 60)
                GameObject rowPower = CreateRowObject(rowsGo, "Row_Power", "PODER", 60f, out pLvlText, out pCostText, out pBtn);
                // Row 2: Reload (Y = 0)
                GameObject rowReload = CreateRowObject(rowsGo, "Row_Reload", "RECARGA", 0f, out rLvlText, out rCostText, out rBtn);
                // Row 3: Range (Y = -60)
                GameObject rowRange = CreateRowObject(rowsGo, "Row_Range", "ALCANCE", -60f, out gLvlText, out gCostText, out gBtn);

                // Instruction Text at bottom
                GameObject instGo = new GameObject("InstructionText");
                instGo.transform.SetParent(menuPanelGo.transform, false);
                RectTransform instRect = instGo.AddComponent<RectTransform>();
                instRect.anchorMin = new Vector2(0.5f, 0.15f);
                instRect.anchorMax = new Vector2(0.5f, 0.15f);
                instRect.pivot = new Vector2(0.5f, 0f);
                instRect.sizeDelta = new Vector2(400f, 30f);

                Text instText = instGo.AddComponent<Text>();
                instText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (instText.font == null) instText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                instText.text = "Pressione TAB para Fechar e Voltar";
                instText.fontSize = 16;
                instText.alignment = TextAnchor.MiddleCenter;
                instText.color = new Color(0.7f, 0.7f, 0.7f);
                instGo.AddComponent<Shadow>().effectColor = Color.black;

                // Add UpgradeMenuUI script on the Canvas (so it remains active and checks input when the panel is disabled)
                UpgradeMenuUI menuUI = canvasGo.GetComponent<UpgradeMenuUI>();
                if (menuUI == null)
                {
                    menuUI = canvasGo.AddComponent<UpgradeMenuUI>();
                }

                // Clean up any old script attached directly to the panel
                UpgradeMenuUI oldUI = menuPanelGo.GetComponent<UpgradeMenuUI>();
                if (oldUI != null)
                {
                    DestroyImmediate(oldUI);
                }

                // Serialize property bindings on UpgradeMenuUI
                SerializedObject soMenuUI = new SerializedObject(menuUI);
                soMenuUI.FindProperty("inputReader").objectReferenceValue = inputReader;
                soMenuUI.FindProperty("playerUpgrades").objectReferenceValue = playerUpgrades;
                soMenuUI.FindProperty("menuPanel").objectReferenceValue = menuPanelGo;
                soMenuUI.FindProperty("pointsText").objectReferenceValue = menuPointsText;
                
                soMenuUI.FindProperty("powerLevelText").objectReferenceValue = pLvlText;
                soMenuUI.FindProperty("powerCostText").objectReferenceValue = pCostText;
                soMenuUI.FindProperty("powerUpgradeButton").objectReferenceValue = pBtn;

                soMenuUI.FindProperty("reloadLevelText").objectReferenceValue = rLvlText;
                soMenuUI.FindProperty("reloadCostText").objectReferenceValue = rCostText;
                soMenuUI.FindProperty("reloadUpgradeButton").objectReferenceValue = rBtn;

                soMenuUI.FindProperty("rangeLevelText").objectReferenceValue = gLvlText;
                soMenuUI.FindProperty("rangeCostText").objectReferenceValue = gCostText;
                soMenuUI.FindProperty("rangeUpgradeButton").objectReferenceValue = gBtn;
                soMenuUI.ApplyModifiedProperties();

                menuPanelGo.SetActive(false);
            }

            // 10. Add PlayerUI script to Canvas
            PlayerUI playerUI = canvasGo.GetComponent<PlayerUI>();
            if (playerUI == null)
            {
                playerUI = canvasGo.AddComponent<PlayerUI>();
            }

            // Link PlayerUI fields
            SerializedObject soUI = new SerializedObject(playerUI);
            soUI.FindProperty("playerController").objectReferenceValue = playerController;
            soUI.FindProperty("cameraObscura").objectReferenceValue = cameraObscura;
            soUI.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            soUI.FindProperty("playerInventory").objectReferenceValue = playerInventory;
            soUI.FindProperty("playerUpgrades").objectReferenceValue = playerUpgrades;
            soUI.FindProperty("viewfinderPanel").objectReferenceValue = viewfinderPanel;
            soUI.FindProperty("staminaPanel").objectReferenceValue = staminaPanel;
            soUI.FindProperty("staminaSlider").objectReferenceValue = staminaSlider;
            soUI.FindProperty("staminaCanvasGroup").objectReferenceValue = staminaCanvasGroup;
            soUI.FindProperty("reticleImage").objectReferenceValue = reticleImage;
            soUI.FindProperty("rechargeSlider").objectReferenceValue = rechargeSlider;
            soUI.FindProperty("flashCanvasGroup").objectReferenceValue = flashCanvasGroup;
            soUI.FindProperty("filmText").objectReferenceValue = filmText;
            soUI.FindProperty("fatalWarningText").objectReferenceValue = fatalGo;
            soUI.FindProperty("healthSlider").objectReferenceValue = healthSlider;
            soUI.FindProperty("damageCatchUpSlider").objectReferenceValue = damageSlider;
            soUI.FindProperty("statusText").objectReferenceValue = statusText;
            soUI.FindProperty("medicineText").objectReferenceValue = medicineText;
            soUI.FindProperty("virginTapeText").objectReferenceValue = virginTapeText;
            soUI.FindProperty("spiritPointsText").objectReferenceValue = ptsText;
            soUI.FindProperty("spiritOrbsText").objectReferenceValue = orbsText;
            soUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            soUI.FindProperty("damageIndicatorCanvasGroup").objectReferenceValue = dmgCanvasGroup;
            soUI.ApplyModifiedProperties();

            // 10.5 Create Filament UI under ViewfinderPanel
            Transform filamentTrans = viewfinderPanel.transform.Find("FilamentUI");
            GameObject filamentGo;
            CanvasGroup filamentCanvasGroup = null;
            if (filamentTrans == null)
            {
                filamentGo = new GameObject("FilamentUI");
                filamentGo.transform.SetParent(viewfinderPanel.transform, false);
                filamentCanvasGroup = filamentGo.AddComponent<CanvasGroup>();
                filamentCanvasGroup.alpha = 0f;

                RectTransform rect = filamentGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector3.zero;
                rect.sizeDelta = new Vector2(200f, 200f);

                // Create pointer container (which rotates)
                GameObject pointerContainer = new GameObject("PointerContainer");
                pointerContainer.transform.SetParent(filamentGo.transform, false);
                RectTransform pointerRect = pointerContainer.AddComponent<RectTransform>();
                pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
                pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
                pointerRect.anchoredPosition = Vector3.zero;
                pointerRect.sizeDelta = new Vector2(200f, 200f);

                // Create needle (the visual tick revolving around the center)
                GameObject needle = new GameObject("FilamentNeedle");
                needle.transform.SetParent(pointerContainer.transform, false);
                Image needleImg = needle.AddComponent<Image>();
                needleImg.color = new Color(0.2f, 0.8f, 1.0f, 0.8f); // start light blue

                RectTransform needleRect = needle.GetComponent<RectTransform>();
                needleRect.anchorMin = new Vector2(0.5f, 0.5f);
                needleRect.anchorMax = new Vector2(0.5f, 0.5f);
                // Position it at a radius of 90 units above the center
                needleRect.anchoredPosition = new Vector3(0f, 90f, 0f);
                needleRect.sizeDelta = new Vector2(8f, 24f); // vertical tick

                // Add FilamentUI script to parent
                FilamentUI filamentUI = filamentGo.AddComponent<FilamentUI>();
                
                // Link references via SerializedObject
                SerializedObject soFil = new SerializedObject(filamentUI);
                soFil.FindProperty("playerController").objectReferenceValue = playerController;
                soFil.FindProperty("pointerRect").objectReferenceValue = pointerRect;
                soFil.FindProperty("canvasGroup").objectReferenceValue = filamentCanvasGroup;
                soFil.FindProperty("filamentImage").objectReferenceValue = needleImg;
                soFil.ApplyModifiedProperties();
            }

            // 11. Create a Corridor Wall for Testing
            if (GameObject.Find("TestCorridor") == null)
            {
                GameObject corridor = new GameObject("TestCorridor");
                
                GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall1.name = "Wall_Left";
                wall1.transform.SetParent(corridor.transform);
                wall1.transform.position = new Vector3(-2.8f, 2f, 0f);
                wall1.transform.localScale = new Vector3(0.4f, 4f, 25f);

                GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall2.name = "Wall_Right";
                wall2.transform.SetParent(corridor.transform);
                wall2.transform.position = new Vector3(2.8f, 2f, 0f);
                wall2.transform.localScale = new Vector3(0.4f, 4f, 25f);

                GameObject wall3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall3.name = "Wall_Back";
                wall3.transform.SetParent(corridor.transform);
                wall3.transform.position = new Vector3(0f, 2f, -12.5f);
                wall3.transform.localScale = new Vector3(6f, 4f, 0.4f);

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.SetParent(corridor.transform);
                floor.transform.position = new Vector3(0f, 0f, 0f);
                floor.transform.localScale = new Vector3(3f, 1f, 3f);

                // Create and apply dark material
                Material darkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                darkMat.color = new Color(0.12f, 0.12f, 0.15f);
                darkMat.name = "DarkLitMaterial";
                
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                
                AssetDatabase.CreateAsset(darkMat, "Assets/Materials/DarkLitMaterial.mat");

                wall1.GetComponent<Renderer>().sharedMaterial = darkMat;
                wall2.GetComponent<Renderer>().sharedMaterial = darkMat;
                wall3.GetComponent<Renderer>().sharedMaterial = darkMat;
                floor.GetComponent<Renderer>().sharedMaterial = darkMat;
            }

            // 12. Setup Test Ghost Target
            GameObject ghost = GameObject.Find("Ghost_Test");
            if (ghost == null)
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ghost.name = "Ghost_Test";
            }
            
            // Reconfigure or initialize position and scale
            ghost.transform.position = new Vector3(0f, 1f, 8f); // 8 meters ahead of player
            ghost.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the player

            // Setup Rigidbody (kinematic so we control movement programmatically)
            Rigidbody ghostRb = ghost.GetComponent<Rigidbody>();
            if (ghostRb == null)
            {
                ghostRb = ghost.AddComponent<Rigidbody>();
            }
            ghostRb.useGravity = false;
            ghostRb.isKinematic = true;

            // Setup Ghost Target script
            GhostTarget ghostTarget = ghost.GetComponent<GhostTarget>();
            if (ghostTarget == null)
            {
                ghostTarget = ghost.AddComponent<GhostTarget>();
            }

            // Create spectral material
            Material ghostMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GhostMaterial.mat");
            if (ghostMat == null)
            {
                ghostMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ghostMat.name = "GhostMaterial";
                
                // URP Transparent properties
                ghostMat.SetFloat("_Surface", 1f); // Transparent
                ghostMat.SetFloat("_Blend", 0f); // Alpha blend
                ghostMat.SetColor("_BaseColor", new Color(0.35f, 0.65f, 1f, 0.55f)); // Translucent ghostly blue
                
                ghostMat.SetOverrideTag("RenderType", "Transparent");
                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                
                // Set soft emission
                ghostMat.EnableKeyword("_EMISSION");
                ghostMat.SetColor("_EmissionColor", new Color(0.08f, 0.25f, 0.5f));

                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(ghostMat, "Assets/Materials/GhostMaterial.mat");
            }

            ghost.GetComponent<Renderer>().sharedMaterial = ghostMat;

            // 13. Adjust Directional Light
            Light dirLight = UnityEngine.Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.intensity = 0.12f;
                dirLight.color = new Color(0.55f, 0.65f, 0.85f);
            }

            // 14. Setup Collectible Items in the Corridor
            CollectibleItem[] existingItems = UnityEngine.Object.FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None);
            foreach (var item in existingItems)
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            // Create new collectibles at (0, 0.5, 4) - Film Type 61, (0, 0.5, -4) - Film Type 90, (1.5, 0.5, 0) - Medicine
            CreateCollectible(new Vector3(0f, 0.5f, 4f), CollectibleType.Film61, 15, "Collectible_Film61");
            CreateCollectible(new Vector3(0f, 0.5f, -4f), CollectibleType.Film90, 5, "Collectible_Film90");
            CreateCollectible(new Vector3(1.5f, 0.5f, 0f), CollectibleType.Medicine, 1, "Collectible_Medicine");

            // Mark scene as dirty and save it so changes are not lost
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            
            // 15. Setup Save Point and Virgin Tape in corridor
            // Destroy old save point if exists
            GameObject oldSavePoint = GameObject.Find("SavePoint");
            if (oldSavePoint != null) UnityEngine.Object.DestroyImmediate(oldSavePoint);

            // Create Save Point (a glowing cube)
            GameObject savePoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            savePoint.name = "SavePoint";
            savePoint.transform.position = new Vector3(0f, 0.5f, 7f);
            savePoint.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            Collider spCol = savePoint.GetComponent<Collider>();
            if (spCol != null) spCol.isTrigger = true;
            SavePoint spScript = savePoint.AddComponent<SavePoint>();

            // Apply a glowing lantern-like material
            Material savePointMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            savePointMat.name = "SavePointMaterial";
            savePointMat.SetFloat("_Surface", 1f);
            savePointMat.SetFloat("_Blend", 0f);
            savePointMat.SetColor("_BaseColor", new Color(1f, 0.75f, 0.2f, 0.85f));
            savePointMat.SetOverrideTag("RenderType", "Transparent");
            savePointMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            savePointMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            savePointMat.SetInt("_ZWrite", 0);
            savePointMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 1.5f);
            savePointMat.EnableKeyword("_EMISSION");
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.CreateAsset(savePointMat, "Assets/Materials/SavePointMaterial.mat");
            savePoint.GetComponent<Renderer>().sharedMaterial = savePointMat;

            // Create a floating lantern visual (small sphere above)
            GameObject lanternSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lanternSphere.name = "LanternSphere";
            lanternSphere.transform.SetParent(savePoint.transform);
            lanternSphere.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            lanternSphere.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Collider lsCol = lanternSphere.GetComponent<Collider>();
            if (lsCol != null) UnityEngine.Object.DestroyImmediate(lsCol);
            Renderer lsRen = lanternSphere.GetComponent<Renderer>();
            if (lsRen != null) lsRen.sharedMaterial = savePointMat;

            // Create a Virgin Tape collectible in the corridor (at the start)
            // Check if it already exists
            GameObject tapePickup = GameObject.Find("Collectible_VirginTape");
            if (tapePickup == null)
            {
                CreateCollectible(new Vector3(-1.5f, 0.5f, 2f), CollectibleType.VirginTape, 1, "Collectible_VirginTape");
            }

            // Create a second Virgin Tape for testing near the save point
            GameObject tapePickup2 = GameObject.Find("Collectible_VirginTape2");
            if (tapePickup2 == null)
            {
                CreateCollectible(new Vector3(0f, 0.5f, 5.5f), CollectibleType.VirginTape, 1, "Collectible_VirginTape2");
            }
            Debug.Log("Photal Frame: Setup concluído com sucesso! A cena foi configurada e salva.");
        }

        private static GameObject CreateRowObject(GameObject parent, string rowName, string labelStr, float yOffset, out Text lvlText, out Text costText, out Button btn)
        {
            GameObject row = new GameObject(rowName);
            row.transform.SetParent(parent.transform, false);
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0.5f);
            rowRect.anchorMax = new Vector2(1f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector3(0f, yOffset, 0f);
            rowRect.sizeDelta = new Vector2(0f, 40f);

            // Label (Left side)
            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(row.transform, false);
            RectTransform lblRect = lblGo.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.zero;
            lblRect.pivot = Vector2.zero;
            lblRect.anchoredPosition = new Vector3(10f, 5f, 0f);
            lblRect.sizeDelta = new Vector2(100f, 30f);

            Text lblText = lblGo.AddComponent<Text>();
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (lblText.font == null) lblText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            lblText.text = labelStr;
            lblText.fontSize = 18;
            lblText.fontStyle = FontStyle.Bold;
            lblText.alignment = TextAnchor.MiddleLeft;
            lblText.color = new Color(0.2f, 0.85f, 1f);
            lblGo.AddComponent<Shadow>().effectColor = Color.black;

            // Level indicator text (Center)
            GameObject lvlGo = new GameObject("Levels");
            lvlGo.transform.SetParent(row.transform, false);
            RectTransform lvlR = lvlGo.AddComponent<RectTransform>();
            lvlR.anchorMin = new Vector2(0.35f, 0.5f);
            lvlR.anchorMax = new Vector2(0.35f, 0.5f);
            lvlR.pivot = new Vector2(0f, 0.5f);
            lvlR.anchoredPosition = new Vector3(0f, 0f, 0f);
            lvlR.sizeDelta = new Vector2(160f, 30f);

            lvlText = lvlGo.AddComponent<Text>();
            lvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (lvlText.font == null) lvlText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            lvlText.text = "[ X ][   ][   ][   ]";
            lvlText.fontSize = 16;
            lvlText.fontStyle = FontStyle.Bold;
            lvlText.alignment = TextAnchor.MiddleCenter;
            lvlText.color = Color.white;
            lvlGo.AddComponent<Shadow>().effectColor = Color.black;

            // Cost (Right side)
            GameObject costGo = new GameObject("Cost");
            costGo.transform.SetParent(row.transform, false);
            RectTransform costR = costGo.AddComponent<RectTransform>();
            costR.anchorMin = new Vector2(0.72f, 0.5f);
            costR.anchorMax = new Vector2(0.72f, 0.5f);
            costR.pivot = new Vector2(0f, 0.5f);
            costR.anchoredPosition = new Vector3(0f, 0f, 0f);
            costR.sizeDelta = new Vector2(90f, 30f);

            costText = costGo.AddComponent<Text>();
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (costText.font == null) costText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            costText.text = "1,500 PTS";
            costText.fontSize = 16;
            costText.alignment = TextAnchor.MiddleRight;
            costText.color = new Color(0.95f, 0.8f, 0.1f);
            costGo.AddComponent<Shadow>().effectColor = Color.black;

            // Button (Far Right)
            GameObject btnGo = new GameObject("Button_Upgrade");
            btnGo.transform.SetParent(row.transform, false);
            RectTransform btnR = btnGo.AddComponent<RectTransform>();
            btnR.anchorMin = new Vector2(0.92f, 0.5f);
            btnR.anchorMax = new Vector2(0.92f, 0.5f);
            btnR.pivot = new Vector2(0f, 0.5f);
            btnR.anchoredPosition = new Vector3(0f, 0f, 0f);
            btnR.sizeDelta = new Vector2(30f, 30f);

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.85f, 1f, 0.3f);

            btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.2f, 0.85f, 1f, 0.3f);
            cb.highlightedColor = new Color(0.2f, 0.85f, 1f, 0.6f);
            cb.pressedColor = new Color(0.2f, 0.85f, 1f, 0.8f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
            btn.colors = cb;

            GameObject btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            RectTransform btRect = btnTextGo.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            Text btnTxt = btnTextGo.AddComponent<Text>();
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (btnTxt.font == null) btnTxt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnTxt.text = "+";
            btnTxt.fontSize = 20;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;

            return row;
        }

        private static void CreateCollectible(Vector3 position, CollectibleType type, int qty, string name)
        {
            GameObject itemGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            itemGo.name = name;
            itemGo.transform.position = position;
            itemGo.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Collider defaultCol = itemGo.GetComponent<Collider>();
            if (defaultCol != null) UnityEngine.Object.DestroyImmediate(defaultCol);

            CollectibleItem collectible = itemGo.AddComponent<CollectibleItem>();
            
            SerializedObject soCol = new SerializedObject(collectible);
            soCol.FindProperty("itemType").enumValueIndex = (int)type;
            soCol.FindProperty("quantity").intValue = qty;
            soCol.ApplyModifiedProperties();

            Material itemMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CollectibleMaterial.mat");
            if (itemMat == null)
            {
                itemMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                itemMat.name = "CollectibleMaterial";
                itemMat.SetFloat("_Surface", 1f); // Transparent
                itemMat.SetFloat("_Blend", 0f); // Alpha
                itemMat.SetColor("_BaseColor", new Color(0.2f, 0.85f, 1f, 0.8f)); // Glowing blue-cyan
                itemMat.SetOverrideTag("RenderType", "Transparent");
                itemMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                itemMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                itemMat.SetInt("_ZWrite", 0);
                itemMat.EnableKeyword("_EMISSION");
                itemMat.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1f) * 1.5f);

                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(itemMat, "Assets/Materials/CollectibleMaterial.mat");
            }
            itemGo.GetComponent<Renderer>().sharedMaterial = itemMat;
        }
    }
}