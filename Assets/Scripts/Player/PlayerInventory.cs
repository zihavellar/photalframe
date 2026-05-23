using UnityEngine;
using System.Collections.Generic;
using System;
using PhotalFrame.Input;
using System;
using PhotalFrame.Input;

namespace PhotalFrame.Player
{
    public enum FilmType
    {
        Type14 = 0, // Infinite, standard damage
        Type61 = 1, // Limited, medium damage
        Type90 = 2  // Limited, high damage
    }

    public class PlayerInventory : MonoBehaviour
    {
        [Header("Starting Quantities")]
        [SerializeField] private int startingType61 = 0;
        [SerializeField] private int startingType90 = 0;
        [SerializeField] private int startingHerbalMedicine = 0;

        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerHealth playerHealth;

        private int type61Count;
        private int type90Count;
        private int herbalMedicineCount;
        private FilmType activeFilm = FilmType.Type14;

        public event Action OnInventoryChanged;

        public FilmType ActiveFilm => activeFilm;
        public int HerbalMedicineCount => herbalMedicineCount;

        private void Start()
        {
            type61Count = startingType61;
            type90Count = startingType90;
            herbalMedicineCount = startingHerbalMedicine;

            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (playerHealth == null)
                playerHealth = GetComponent<PlayerHealth>();

            OnInventoryChanged?.Invoke();
        }

        private void Update()
        {
            if (inputReader == null) return;

            // Handle cycling film
            if (inputReader.CycleNext)
            {
                CycleFilm(1);
            }
            else if (inputReader.CyclePrevious)
            {
                CycleFilm(-1);
            }

            // Handle quick healing
            if (inputReader.Heal)
            {
                UseHerbalMedicine();
            }
        }

        /// <summary>
        /// Cycles equipped film.
        /// </summary>
        /// <param name="direction">1 for next, -1 for previous.</param>
        private void CycleFilm(int direction)
        {
            int index = (int)activeFilm;
            index = (index + direction + 3) % 3;
            activeFilm = (FilmType)index;

            OnInventoryChanged?.Invoke();
            Debug.Log($"Equipped film cycled: {activeFilm}");
        }

        /// <summary>
        /// Returns the count of a specific film type (-1 for infinite).
        /// </summary>
        public int GetFilmCount(FilmType type)
        {
            switch (type)
            {
                case FilmType.Type14:
                    return -1; // Infinite
                case FilmType.Type61:
                    return type61Count;
                case FilmType.Type90:
                    return type90Count;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Consumes a roll of the active film. Returns false if out of ammo and switches back to Type-14.
        /// </summary>
        public bool ConsumeFilm()
        {
            if (activeFilm == FilmType.Type14) return true; // Infinite

            if (activeFilm == FilmType.Type61)
            {
                if (type61Count > 0)
                {
                    type61Count--;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            else if (activeFilm == FilmType.Type90)
            {
                if (type90Count > 0)
                {
                    type90Count--;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // If we ran out of this film type, auto-switch to infinite Type-14
            activeFilm = FilmType.Type14;
            OnInventoryChanged?.Invoke();
            Debug.Log("Out of special film! Auto-switched to Type-14");
            return false;
        }

        /// <summary>
        /// Adds a quantity of a specific film type to inventory.
        /// </summary>
        public void AddFilm(FilmType type, int count)
        {
            if (type == FilmType.Type61)
            {
                type61Count += count;
            }
            else if (type == FilmType.Type90)
            {
                type90Count += count;
            }

            OnInventoryChanged?.Invoke();
            Debug.Log($"+{count} Film {type} added to inventory.");
        }

        /// <summary>
        /// Adds a quantity of herbal medicines to inventory.
        /// </summary>
        public void AddMedicine(int count)
        {
            herbalMedicineCount += count;
            OnInventoryChanged?.Invoke();
            Debug.Log($"+{count} Herbal Medicine added to inventory.");
        }

        /// <summary>
        /// Uses a medicine item to heal the player.
        /// </summary>
        public void UseHerbalMedicine()
        {
            if (playerHealth == null || playerHealth.IsDead) return;

            if (herbalMedicineCount > 0 && playerHealth.CurrentHealth < playerHealth.MaxHealth)
            {
                herbalMedicineCount--;
                playerHealth.Heal(40f); // Restores 40 HP
                OnInventoryChanged?.Invoke();
            }
            else
            {
                if (herbalMedicineCount <= 0)
                {
                    Debug.Log("No Herbal Medicine left!");
                }
                else
                {
                    Debug.Log("Health is already full.");
                }
            }
        }
    

private int virginTapeCount;
    private List<string> collectedItemIds = new List<string>();

    public int VirginTapeCount => virginTapeCount;
    public IReadOnlyList<string> CollectedItemIds => collectedItemIds;

    public void AddVirginTape(int amount = 1)
    {
        virginTapeCount += amount;
        OnInventoryChanged?.Invoke();
        Debug.Log($"+{amount} Virgin Tape added! Total: {virginTapeCount}");
    }

    public bool ConsumeVirginTape()
    {
        if (virginTapeCount > 0)
        {
            virginTapeCount--;
            OnInventoryChanged?.Invoke();
            Debug.Log($"Consumed 1 Virgin Tape. Remaining: {virginTapeCount}");
            return true;
        }
        return false;
    }

    public void AddFilmType14(int count)
    {
        // Type-14 is infinite, no count needed
        Debug.Log("Type-14 film collected (infinite).");
    }

    public void AddFilmType61(int count)
    {
        AddFilm(FilmType.Type61, count);
    }

    public void AddHealthItem(int count)
    {
        AddMedicine(count);
    }

    public void RegisterCollectedItem(string itemId)
    {
        if (!collectedItemIds.Contains(itemId))
            collectedItemIds.Add(itemId);
    }

    public bool WasItemCollected(string itemId)
    {
        return collectedItemIds.Contains(itemId);
    }

    public void AddKeyItem(string itemId)
    {
        Debug.Log($"Key item collected: {itemId}");
        OnInventoryChanged?.Invoke();
    }

        public void SetVirginTapeState(int count)
    {
        virginTapeCount = count;
    }

    public void SetCollectedItems(List<string> ids)
    {
        collectedItemIds = new List<string>(ids);
    }
}
}
