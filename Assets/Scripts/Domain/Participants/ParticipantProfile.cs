using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualDarkStore.Domain.Participants
{
    /// <summary>
    /// Represents an individual registered research participant slot (P01 through P10).
    /// Holds immutable slot identity, researcher-assigned name, and completed session history.
    /// </summary>
    [Serializable]
    public class ParticipantProfile
    {
        [SerializeField] private string slotId; // Immutable: "P01", "P02", ... "P10"
        [SerializeField] private string participantName; // Researcher assigned: e.g. "Rahul", "Aman"
        [SerializeField] private string registeredAtIso;
        [SerializeField] private string lastSessionAtIso;
        [SerializeField] private int completedSessionsCount;
        [SerializeField] private List<string> completedScenarios = new List<string>();
        [SerializeField] private List<string> completedRoutingModes = new List<string>();

        public string SlotId => slotId;
        public string ParticipantName => participantName;
        public string RegisteredAtIso => registeredAtIso;
        public string LastSessionAtIso => lastSessionAtIso;
        public int CompletedSessionsCount => completedSessionsCount;
        public IReadOnlyList<string> CompletedScenarios => completedScenarios;
        public IReadOnlyList<string> CompletedRoutingModes => completedRoutingModes;

        public bool IsRegistered => !string.IsNullOrWhiteSpace(participantName);

        public ParticipantProfile() { }

        public ParticipantProfile(string slotId, string participantName, string registeredAtIso = null)
        {
            if (string.IsNullOrWhiteSpace(slotId))
                throw new ArgumentException("Slot ID cannot be null or empty.", nameof(slotId));

            this.slotId = slotId.Trim().ToUpperInvariant();
            this.participantName = participantName != null ? participantName.Trim() : string.Empty;
            this.registeredAtIso = !string.IsNullOrEmpty(registeredAtIso)
                ? registeredAtIso
                : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
            this.lastSessionAtIso = string.Empty;
            this.completedSessionsCount = 0;
            this.completedScenarios = new List<string>();
            this.completedRoutingModes = new List<string>();
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Participant name cannot be null, empty, or whitespace-only.", nameof(newName));

            participantName = newName.Trim();
        }

        public void RecordCompletedSession(string scenarioName, string routingMode, string sessionTimeIso = null)
        {
            completedSessionsCount++;
            lastSessionAtIso = !string.IsNullOrEmpty(sessionTimeIso)
                ? sessionTimeIso
                : DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

            if (!string.IsNullOrWhiteSpace(scenarioName) && !completedScenarios.Contains(scenarioName))
            {
                completedScenarios.Add(scenarioName);
            }

            if (!string.IsNullOrWhiteSpace(routingMode) && !completedRoutingModes.Contains(routingMode))
            {
                completedRoutingModes.Add(routingMode);
            }
        }

        public override string ToString() => $"[{SlotId}] {ParticipantName} (Sessions: {completedSessionsCount})";
    }
}
