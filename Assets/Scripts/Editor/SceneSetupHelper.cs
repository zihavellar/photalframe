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

            // 4. Setup Player Controller
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = player.AddComponent<PlayerController>();
            }
            
            // Link InputReader to PlayerController
            SerializedObject soPlayer = new SerializedObject(playerController);
            soPlayer.FindProperty("inputReader").objectReferenceValue = inputReader;
            soPlayer.ApplyModifiedProperties();

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
                RectTransform fillRect = fill.AddComponent<RectTransform>();
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
                RectTransform fillRect = fill.AddComponent<RectTransform>();
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
            soUI.FindProperty("viewfinderPanel").objectReferenceValue = viewfinderPanel;
            soUI.FindProperty("staminaPanel").objectReferenceValue = staminaPanel;
            soUI.FindProperty("staminaSlider").objectReferenceValue = staminaSlider;
            soUI.FindProperty("staminaCanvasGroup").objectReferenceValue = staminaCanvasGroup;
            soUI.FindProperty("reticleImage").objectReferenceValue = reticleImage;
            soUI.FindProperty("rechargeSlider").objectReferenceValue = rechargeSlider;
            soUI.FindProperty("flashCanvasGroup").objectReferenceValue = flashCanvasGroup;
            soUI.FindProperty("filmText").objectReferenceValue = filmText;
            soUI.FindProperty("fatalWarningText").objectReferenceValue = fatalGo;
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

            // Mark scene as dirty and save it so changes are not lost
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("Photal Frame: Setup concluído com sucesso! A cena foi configurada e salva.");
        }
    }
}
