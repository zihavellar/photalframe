using UnityEngine;
using System.Collections;
using PhotalFrame.Player;

namespace PhotalFrame.Player
{
    public enum CollectibleType
    {
        Film61,
        Film90,
        Medicine,
        VirginTape
    }

    [RequireComponent(typeof(SphereCollider))]
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Item Settings")]
        [SerializeField] private CollectibleType itemType;
        [SerializeField] private int quantity = 1;

        [Header("Animation Settings")]
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float rotationSpeed = 50f;

        private Vector3 startPosition;
        private SphereCollider triggerCollider;
        private bool isCollected = false;

        private void Start()
        {
            startPosition = transform.position;
            triggerCollider = GetComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 1.0f; // Large enough detection radius
        }

        private void Update()
        {
            if (isCollected) return;

            // Simple bobbing and rotation animation (glowing point feel)
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPosition + Vector3.up * yOffset;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                PlayerInventory inventory = other.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    isCollected = true;
                    DeliverItem(inventory);
                    SpawnPickupText();
                    Destroy(gameObject, 0.05f); // Destroy pickup object
                }
            }
        }

        private void DeliverItem(PlayerInventory inventory)
        {
            switch (itemType)
            {
                case CollectibleType.Film61:
                    inventory.AddFilm(FilmType.Type61, quantity);
                    break;
                case CollectibleType.Film90:
                    inventory.AddFilm(FilmType.Type90, quantity);
                    break;
                case CollectibleType.Medicine:
                    inventory.AddMedicine(quantity);
                    break;
                case CollectibleType.VirginTape:
                    inventory.AddVirginTape(quantity);
                    break;
            }
        }

        private void SpawnPickupText()
        {
            GameObject textGo = new GameObject("PickupText");
            textGo.transform.position = transform.position + Vector3.up * 0.5f;
            textGo.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            TextMesh textMesh = textGo.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 44;
            textMesh.color = new Color(0.2f, 0.85f, 0.4f); // Glowing light green

            string nameStr = itemType == CollectibleType.VirginTape ? "Virgin Tape" :
                            itemType == CollectibleType.Medicine ? "Herbal Med" : 
                            (itemType == CollectibleType.Film61 ? "Film Type-61" : "Film Type-90");
            textMesh.text = $"+{quantity} {nameStr}";

            // Launch floating coroutine
            StartCoroutine(PickupTextRoutine(textGo, textMesh));
        }

        private IEnumerator PickupTextRoutine(GameObject obj, TextMesh mesh)
        {
            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.8f;

            UnityEngine.Camera mainCam = UnityEngine.Camera.main;

            while (elapsed < duration)
            {
                if (obj == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Move up
                obj.transform.position = Vector3.Lerp(startPos, endPos, t);

                // Face camera
                if (mainCam != null)
                {
                    obj.transform.rotation = Quaternion.LookRotation(obj.transform.position - mainCam.transform.position);
                }

                // Fade alpha
                Color c = mesh.color;
                c.a = 1f - t;
                mesh.color = c;

                yield return null;
            }

            Destroy(obj);
        }
    }
}
