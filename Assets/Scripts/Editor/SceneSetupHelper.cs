using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PhotalFrame.Player;
using PhotalFrame.Camera;
using PhotalFrame.Input;
using PhotalFrame.UI;

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

            // Ensure capsule collider exists and is properly sized
            CapsuleCollider col = player.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = player.AddComponent<CapsuleCollider>();
            }
            col.height = 2f;
            col.center = Vector3.zero;

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
                lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0.4f); // relative chest level (assuming local pos y=0 is center)
                lightGo.transform.localRotation = Quaternion.identity;
                
                flashlight = lightGo.AddComponent<Light>();
                flashlight.type = LightType.Spot;
                flashlight.range = 18f;
                flashlight.spotAngle = 40f;
                flashlight.intensity = 3f;
                flashlight.color = new Color(0.95f, 0.95f, 0.85f); // slightly warm light
            }

            // 6. Setup Camera
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

            // 7. Setup UI Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
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

                // Add circular reticle outline (placeholder)
                GameObject reticle = new GameObject("ReticleCircle");
                reticle.transform.SetParent(viewfinderPanel.transform, false);
                Image reticleImg = reticle.AddComponent<Image>();
                reticleImg.color = new Color(0.9f, 0.9f, 0.9f, 0.4f);
                
                Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (knobSprite != null) reticleImg.sprite = knobSprite;
                
                RectTransform reticleRect = reticle.GetComponent<RectTransform>();
                reticleRect.sizeDelta = new Vector2(40f, 40f);
            }
            else
            {
                viewfinderPanel = viewfinderTrans.gameObject;
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

                // Add Slider
                staminaSlider = staminaPanel.AddComponent<Slider>();
                
                // Background
                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(staminaPanel.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                // Fill Area
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(staminaPanel.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                // Fill
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.2f, 0.85f, 0.4f, 0.75f); // Neon green
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
            soUI.FindProperty("viewfinderPanel").objectReferenceValue = viewfinderPanel;
            soUI.FindProperty("staminaPanel").objectReferenceValue = staminaPanel;
            soUI.FindProperty("staminaSlider").objectReferenceValue = staminaSlider;
            soUI.FindProperty("staminaCanvasGroup").objectReferenceValue = staminaCanvasGroup;
            soUI.ApplyModifiedProperties();

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
                
                // Ensure Assets/Materials folder exists
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

            // 12. Adjust Directional Light
            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.intensity = 0.12f;
                dirLight.color = new Color(0.55f, 0.65f, 0.85f);
            }

            Debug.Log("Photal Frame: Setup concluído com sucesso! Abra o menu 'Tools > Photal Frame > Setup Scene' na sua Unity para configurar a cena.");
        }
    }
}
