using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VirtualDarkStore.Domain.Participants
{
    /// <summary>
    /// Central manager for the fixed 10-slot research participant roster (P01–P10).
    /// Enforces uniqueness, non-empty validation, persistent JSON storage, and safe name edits.
    /// </summary>
    public class ParticipantRoster
    {
        public const int MaxParticipants = 10;
        public const string DefaultStorageFileName = "participants_roster.json";

        private static ParticipantRoster instance;
        public static ParticipantRoster Instance => instance ?? (instance = new ParticipantRoster());

        private readonly List<ParticipantProfile> slots = new List<ParticipantProfile>(MaxParticipants);
        private int activeSlotIndex = -1; // -1 means none selected
        private string customStorageDirectory = null;

        public IReadOnlyList<ParticipantProfile> Slots => slots;
        public int RegisteredCount
        {
            get
            {
                int c = 0;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].IsRegistered) c++;
                }
                return c;
            }
        }

        public ParticipantProfile ActiveParticipant =>
            (activeSlotIndex >= 0 && activeSlotIndex < slots.Count && slots[activeSlotIndex].IsRegistered)
                ? slots[activeSlotIndex]
                : null;

        public int ActiveSlotIndex => activeSlotIndex;

        public ParticipantRoster(string customStorageDir = null)
        {
            this.customStorageDirectory = customStorageDir;
            InitializeSlots();
            LoadFromDisk();
        }

        /// <summary>
        /// Initializes the fixed 10 slots with stable IDs P01 to P10.
        /// </summary>
        public void InitializeSlots()
        {
            slots.Clear();
            for (int i = 1; i <= MaxParticipants; i++)
            {
                string id = $"P{i:D2}";
                slots.Add(new ParticipantProfile(id, string.Empty));
            }
            activeSlotIndex = -1;
        }

        /// <summary>
        /// Sets a custom storage directory path (useful for testing or platform overrides).
        /// </summary>
        public void SetStorageDirectory(string dirPath)
        {
            customStorageDirectory = dirPath;
        }

        /// <summary>
        /// Registers a participant in the next available slot or a specific slot index.
        /// Rejects empty names, duplicate names, and registrations beyond 10 participants.
        /// </summary>
        public bool TryRegisterParticipant(string name, out ParticipantProfile registeredProfile, out string errorMessage, int specificSlotIndex = -1)
        {
            registeredProfile = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Participant name cannot be null, empty, or whitespace-only.";
                return false;
            }

            string cleanName = name.Trim();

            // Duplicate check across registered slots (case-insensitive)
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsRegistered && string.Equals(slots[i].ParticipantName, cleanName, StringComparison.OrdinalIgnoreCase))
                {
                    if (specificSlotIndex != i)
                    {
                        errorMessage = $"A participant with the name '{cleanName}' is already registered in slot {slots[i].SlotId}.";
                        return false;
                    }
                }
            }

            // Target slot determination
            int targetIndex = specificSlotIndex;
            if (targetIndex < 0)
            {
                targetIndex = slots.FindIndex(s => !s.IsRegistered);
            }

            if (targetIndex < 0 || targetIndex >= MaxParticipants)
            {
                errorMessage = $"Cannot register more than {MaxParticipants} participants. All slots P01 to P10 are occupied.";
                return false;
            }

            var slot = slots[targetIndex];
            slot.UpdateName(cleanName);
            registeredProfile = slot;

            if (activeSlotIndex < 0)
            {
                activeSlotIndex = targetIndex;
            }

            SaveToDisk();
            return true;
        }

        /// <summary>
        /// Edits an existing participant's name without changing their stable ID or history.
        /// </summary>
        public bool TryEditParticipantName(string slotId, string newName, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                errorMessage = "Slot ID cannot be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                errorMessage = "Participant name cannot be null, empty, or whitespace-only.";
                return false;
            }

            string cleanName = newName.Trim();
            int slotIdx = slots.FindIndex(s => string.Equals(s.SlotId, slotId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (slotIdx < 0)
            {
                errorMessage = $"Slot ID '{slotId}' not found in roster.";
                return false;
            }

            // Check duplicate against other slots
            for (int i = 0; i < slots.Count; i++)
            {
                if (i != slotIdx && slots[i].IsRegistered && string.Equals(slots[i].ParticipantName, cleanName, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = $"A participant with the name '{cleanName}' is already registered in slot {slots[i].SlotId}.";
                    return false;
                }
            }

            slots[slotIdx].UpdateName(cleanName);
            SaveToDisk();
            return true;
        }

        /// <summary>
        /// Selects the active participant by slot index (0..9).
        /// </summary>
        public bool SelectParticipant(int slotIndex, out string error)
        {
            error = null;
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                error = $"Slot index {slotIndex} is outside range 0..{slots.Count - 1}.";
                return false;
            }

            if (!slots[slotIndex].IsRegistered)
            {
                error = $"Slot {slots[slotIndex].SlotId} is unassigned. Please register a participant name first.";
                return false;
            }

            activeSlotIndex = slotIndex;
            SaveToDisk();
            return true;
        }

        /// <summary>
        /// Selects the active participant by slot ID ("P01" .. "P10").
        /// </summary>
        public bool SelectParticipantById(string slotId, out string error)
        {
            error = null;
            int idx = slots.FindIndex(s => string.Equals(s.SlotId, slotId?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                error = $"Slot ID '{slotId}' not found in roster.";
                return false;
            }
            return SelectParticipant(idx, out error);
        }

        public string GetStorageDirectory()
        {
            if (!string.IsNullOrWhiteSpace(customStorageDirectory))
            {
                return customStorageDirectory;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, "WorkStudyData");
#else
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "WorkStudyData"));
#endif
        }

        public string GetStorageFilePath()
        {
            return Path.Combine(GetStorageDirectory(), DefaultStorageFileName);
        }

        public void SaveToDisk()
        {
            try
            {
                string dir = GetStorageDirectory();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var container = new RosterSerializationContainer { slots = this.slots, activeSlotIndex = this.activeSlotIndex };
                string json = JsonUtility.ToJson(container, true);
                File.WriteAllText(GetStorageFilePath(), json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParticipantRoster] Failed to save roster to disk: {ex.Message}");
            }
        }

        public void LoadFromDisk()
        {
            try
            {
                string path = GetStorageFilePath();
                if (!File.Exists(path)) return;

                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) return;

                var container = JsonUtility.FromJson<RosterSerializationContainer>(json);
                if (container != null && container.slots != null && container.slots.Count > 0)
                {
                    // Merge loaded slots into fixed 10 slots
                    for (int i = 0; i < Math.Min(container.slots.Count, MaxParticipants); i++)
                    {
                        var loaded = container.slots[i];
                        int matchIdx = slots.FindIndex(s => string.Equals(s.SlotId, loaded.SlotId, StringComparison.OrdinalIgnoreCase));
                        if (matchIdx >= 0)
                        {
                            slots[matchIdx] = loaded;
                        }
                    }
                    if (container.activeSlotIndex >= 0 && container.activeSlotIndex < slots.Count && slots[container.activeSlotIndex].IsRegistered)
                    {
                        activeSlotIndex = container.activeSlotIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParticipantRoster] Failed to load roster from disk: {ex.Message}");
            }
        }

        [Serializable]
        private class RosterSerializationContainer
        {
            public List<ParticipantProfile> slots;
            public int activeSlotIndex;
        }
    }
}
