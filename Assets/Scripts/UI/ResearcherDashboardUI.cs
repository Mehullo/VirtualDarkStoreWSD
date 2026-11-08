using System;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using VirtualDarkStore.Domain.Participants;
using VirtualDarkStore.WorkStudy;

namespace VirtualDarkStore.UI
{
    /// <summary>
    /// Researcher management interface for configuring 10 unique participant slots (P01–P10),
    /// reviewing completed session counts, selecting active participants, and triggering CSV exports.
    /// Toggleable via [F2] or on-screen button.
    /// </summary>
    public class ResearcherDashboardUI : MonoBehaviour
    {
        private static ResearcherDashboardUI instance;
        public static ResearcherDashboardUI Instance => instance != null ? instance : (instance = FindAnyObjectByType<ResearcherDashboardUI>());

        [Header("Display State")]
        [SerializeField] private bool isVisible = false;

        private Vector2 scrollPos = Vector2.zero;
        private string editingSlotId = null;
        private string editingNameText = string.Empty;
        private string statusMessage = string.Empty;
        private float statusTimer = 0f;
        private Color statusColor = Color.white;

        private GUIStyle titleStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle tableHeaderStyle;
        private GUIStyle tableRowStyle;
        private GUIStyle activeSlotStyle;
        private GUIStyle buttonStyle;
        private Texture2D windowBgTexture;
        private Texture2D cardBgTexture;
        private Texture2D activeRowTexture;
        private bool stylesInitialized = false;

        public bool IsVisible => isVisible;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Update()
        {
            if (statusTimer > 0f)
            {
                statusTimer -= Time.deltaTime;
                if (statusTimer <= 0f) statusMessage = string.Empty;
            }

            HandleToggleInput();
        }

        private void HandleToggleInput()
        {
            bool f2Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                f2Pressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.F2))
            {
                f2Pressed = true;
            }
#endif
            if (f2Pressed)
            {
                ToggleVisibility();
            }
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
        }

        public void ShowDashboard()
        {
            isVisible = true;
        }

        public void HideDashboard()
        {
            isVisible = false;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            windowBgTexture = MakeColorTexture(new Color(0.06f, 0.08f, 0.12f, 0.96f));
            cardBgTexture = MakeColorTexture(new Color(0.12f, 0.16f, 0.24f, 0.85f));
            activeRowTexture = MakeColorTexture(new Color(0.10f, 0.35f, 0.45f, 0.85f));

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = new Color(0.0f, 0.9f, 1.0f); // Cyan

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            subHeaderStyle.normal.textColor = new Color(1.0f, 0.8f, 0.2f); // Gold

            tableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            tableHeaderStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);

            tableRowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            tableRowStyle.normal.textColor = Color.white;

            activeSlotStyle = new GUIStyle(tableRowStyle)
            {
                fontStyle = FontStyle.Bold
            };
            activeSlotStyle.normal.textColor = new Color(0.3f, 1.0f, 0.5f); // Bright Green

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (Camera.current != null && Camera.current.stereoEnabled) return;
            if (!isVisible) return;
            InitStyles();

            float width = 840f;
            float height = 540f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.DrawTexture(new Rect(x, y, width, height), windowBgTexture);
            GUILayout.BeginArea(new Rect(x + 18f, y + 14f, width - 36f, height - 28f));

            // Header Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("RESEARCHER DASHBOARD: 10 PARTICIPANT SLOTS [F2]", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕ Close", GUILayout.Width(80f), GUILayout.Height(24f)))
            {
                isVisible = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Configure participants, select active subject, and export independent CSV work-study datasets.", tableHeaderStyle);
            GUILayout.Space(6);

            // Status message banner
            if (!string.IsNullOrEmpty(statusMessage))
            {
                var prevColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label($"▶ {statusMessage}", subHeaderStyle);
                GUI.color = prevColor;
                GUILayout.Space(4);
            }

            var roster = ParticipantRoster.Instance;

            // Slots Table Header
            GUILayout.BeginHorizontal(cardBgTexture != null ? GUI.skin.box : GUIStyle.none);
            GUILayout.Label("Slot", tableHeaderStyle, GUILayout.Width(45f));
            GUILayout.Label("Participant Name", tableHeaderStyle, GUILayout.Width(160f));
            GUILayout.Label("Sessions", tableHeaderStyle, GUILayout.Width(65f));
            GUILayout.Label("Scenarios Completed", tableHeaderStyle, GUILayout.Width(170f));
            GUILayout.Label("Routing Modes", tableHeaderStyle, GUILayout.Width(120f));
            GUILayout.Label("Last Session", tableHeaderStyle, GUILayout.Width(110f));
            GUILayout.Label("Actions", tableHeaderStyle, GUILayout.Width(100f));
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(330f));

            for (int i = 0; i < roster.Slots.Count; i++)
            {
                var slot = roster.Slots[i];
                bool isActive = (roster.ActiveSlotIndex == i);

                Rect rowRect = GUILayoutUtility.GetRect(width - 50f, 28f);
                if (isActive)
                {
                    GUI.DrawTexture(rowRect, activeRowTexture);
                }
                else if (i % 2 == 1)
                {
                    GUI.DrawTexture(rowRect, cardBgTexture);
                }

                GUILayout.BeginArea(rowRect);
                GUILayout.BeginHorizontal();

                // Slot ID
                GUILayout.Label(slot.SlotId, isActive ? activeSlotStyle : tableRowStyle, GUILayout.Width(45f));

                // Name or Edit Mode
                if (editingSlotId == slot.SlotId)
                {
                    editingNameText = GUILayout.TextField(editingNameText, GUILayout.Width(110f));
                    if (GUILayout.Button("Save", GUILayout.Width(45f)))
                    {
                        if (roster.TryRegisterParticipant(editingNameText, out _, out string err, i))
                        {
                            SetStatus($"Registered slot {slot.SlotId} as '{editingNameText}'.", Color.green);
                            editingSlotId = null;
                        }
                        else
                        {
                            SetStatus($"Error: {err}", Color.red);
                        }
                    }
                }
                else
                {
                    string displayName = slot.IsRegistered ? slot.ParticipantName : "<Unassigned>";
                    GUILayout.Label(displayName, isActive ? activeSlotStyle : tableRowStyle, GUILayout.Width(160f));
                }

                // Completed Sessions Count
                GUILayout.Label(slot.CompletedSessionsCount.ToString(), tableRowStyle, GUILayout.Width(65f));

                // Scenarios list
                string scns = slot.CompletedScenarios.Count > 0 ? string.Join(", ", slot.CompletedScenarios) : "-";
                GUILayout.Label(scns, tableRowStyle, GUILayout.Width(170f));

                // Routing modes
                string modes = slot.CompletedRoutingModes.Count > 0 ? string.Join(", ", slot.CompletedRoutingModes) : "-";
                GUILayout.Label(modes, tableRowStyle, GUILayout.Width(120f));

                // Last session date
                string lastDate = !string.IsNullOrEmpty(slot.LastSessionAtIso) ? slot.LastSessionAtIso.Substring(0, Math.Min(10, slot.LastSessionAtIso.Length)) : "-";
                GUILayout.Label(lastDate, tableRowStyle, GUILayout.Width(110f));

                // Actions: Edit / Select
                if (editingSlotId != slot.SlotId)
                {
                    if (GUILayout.Button(slot.IsRegistered ? "Edit" : "Register", GUILayout.Width(52f)))
                    {
                        editingSlotId = slot.SlotId;
                        editingNameText = slot.ParticipantName;
                    }
                    if (slot.IsRegistered && !isActive)
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(50f)))
                        {
                            if (roster.SelectParticipant(i, out string err))
                            {
                                SetStatus($"Active participant set to [{slot.SlotId}] {slot.ParticipantName}", Color.cyan);
                            }
                            else
                            {
                                SetStatus($"Error: {err}", Color.red);
                            }
                        }
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            GUILayout.EndScrollView();
            GUILayout.Space(8);

            // Bottom Actions Bar
            GUILayout.BeginHorizontal();
            var active = roster.ActiveParticipant;
            string activeLabel = active != null ? $"ACTIVE: [{active.SlotId}] {active.ParticipantName}" : "NO PARTICIPANT SELECTED";
            GUILayout.Label(activeLabel, subHeaderStyle);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Data Folder", buttonStyle, GUILayout.Width(150f), GUILayout.Height(28f)))
            {
                OpenDataFolder();
            }

            if (GUILayout.Button("Export Combined Data", buttonStyle, GUILayout.Width(170f), GUILayout.Height(28f)))
            {
                SetStatus($"Combined exports are updated automatically on every session save at {roster.GetStorageDirectory()}/Combined/", Color.cyan);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void OpenDataFolder()
        {
            string dir = ParticipantRoster.Instance.GetStorageDirectory();
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                System.Diagnostics.Process.Start("explorer.exe", dir);
                SetStatus($"Opened explorer at: {dir}", Color.green);
#else
                SetStatus($"Data folder: {dir}", Color.white);
#endif
            }
            catch (Exception ex)
            {
                SetStatus($"Could not open explorer: {ex.Message}", Color.red);
            }
        }

        private void SetStatus(string message, Color color)
        {
            statusMessage = message;
            statusColor = color;
            statusTimer = 5f;
        }

        private Texture2D MakeColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void OnDestroy()
        {
            if (windowBgTexture != null) Destroy(windowBgTexture);
            if (cardBgTexture != null) Destroy(cardBgTexture);
            if (activeRowTexture != null) Destroy(activeRowTexture);
        }
    }
}
