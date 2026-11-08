# Meta Quest 3 PC VR (Windows Profile) Setup & Deployment Guide
## Virtual Dark Store Work-Study & Simulation Platform

This step-by-step guide explains how to configure, build, and run the **Virtual Dark Store** simulation on your laptop using **Meta Quest 3** connected via **Meta Horizon Link App (Quest Link / USB Cable)** under the Windows Standalone profile.

---

## 1. System Architecture & Interaction Modes

The dark-store simulation provides two synchronized interaction modes that run on the exact same industrial engineering and inventory core:
- **Desktop Mode**: First-person keyboard/mouse interaction (`WASD` locomotion, mouse look, `[E]` / Left-Click pick).
- **VR Mode (Meta Quest 3 via Link)**: 6-DoF physical head tracking, level horizon camera alignment, dual Touch Plus spatial controllers, dual Pick/Grab interaction (Index Trigger & Hand Grip), and sprint locomotion (Thumbstick Click / Grip / Shift).

You can switch between Desktop and VR modes at any time by pressing **`[V]`** on your laptop keyboard or clicking the mode buttons in the HUD.

---

## 2. Hardware & Software Requirements

1. **Hardware**:
   - **Meta Quest 3** headset with Touch Plus controllers.
   - **High-Speed USB-C 3.0 Link Cable** (or USB-C to USB-A 3.0 adapter) connected directly to a high-speed USB 3.0+ port on your laptop (marked with blue plastic or SS icon).
   - Dedicated GPU: NVIDIA RTX-series or compatible desktop/laptop GPU.
2. **Software**:
   - **Meta Horizon PC App** (formerly the Oculus PC App). Download from [meta.com/quest/setup](https://www.meta.com/quest/setup/).
   - **Unity 6 Editor** (`6000.6.7f1`) with Universal Render Pipeline (URP).
   - Windows 10/11 64-bit.

---

## 3. Step 1: Configure the Meta Horizon PC App

Before launching Unity, you must configure the Meta Quest Link runtime:

1. Open the **Meta Horizon PC App** on your Windows laptop.
2. Go to **Settings** (gear icon on the left sidebar) $\to$ **General** tab:
   - Find **OpenXR Runtime**.
   - If it does not say *"Meta Quest Link is currently set as the active OpenXR runtime"*, click **Set as active**.
   > [!IMPORTANT]
   > Meta Quest Link **must** be the active OpenXR runtime on Windows so Unity connects directly to the headset without third-party SteamVR layers.
3. In the **Beta** tab:
   - Ensure **Developer Runtime Features** is toggled **ON** if you wish to inspect spatial telemetry.

---

## 4. Step 2: Connect the Quest 3 via USB Cable

1. Power on your Meta Quest 3 headset.
2. Plug the USB-C link cable into the side port of the Quest 3, and connect the other end to your laptop's USB 3.0 port.
3. Put on the headset:
   - When prompted **"Allow USB Debugging?"**, select **Always allow** and click **Allow**.
   - When prompted **"Allow access to data?"**, select **Allow**.
4. In the headset's **Quick Settings** panel (clock / battery icon on the universal menu bar):
   - Click **Quest Link** (or Horizon Link).
   - Ensure the connection type is set to **Use USB Cable**.
   - Click **Launch**.
5. You will enter the Meta Quest Link Dash (the dark grey curved grid environment). You are now connected to your laptop!

---

## 5. Step 3: XR Rig Hierarchy, Camera & Height Configuration

The XR Rig has been configured as follows:

| Property | Value | Explanation |
|---|---|---|
| **Rig Root Name** | `XR Origin Hands (XR Rig)` | Wraps standard `Unity.XR.CoreUtils.XROrigin`. Restored official rig name in Scene Hierarchy. |
| **Operational Starting Point** | `(0.0m, 0.0m, -8.5m)` | Spawns in the front cross-aisle facing North (`Quaternion.identity`) looking into Aisles `A01`–`A06`. Prevents spawning at the world center `(0, 0, 0)`. |
| **Camera Offset** | `Camera Floor Offset` | Zero pitch and zero roll (`Quaternion.identity`), ensuring the virtual warehouse floor is level with your physical room floor. |
| **Main Camera Height** | `1.68m` eye height | Floor-origin mode (`RequestedTrackingOriginMode = Floor`). The physical headset tracks your true standing height above floor level. |
| **Camera Tag & Data** | `MainCamera` | Equipped with URP `UniversalAdditionalCameraData` and `VRHeadTracking`. |
| **AudioListener Handling** | Automatic | When VR Rig is active, its camera's `AudioListener` is enabled and desktop listeners are muted, preventing duplicate listener warnings. |

---

## 6. Step 4: Controls, Hand Visuals & In-VR UI

### 6.1 Sleek Ringless Controller Visuals & Ergonomic 50° Forward Tilt
Both hands are fully visible in the VR environment with dedicated visual models and calibrated ergonomics:
- **Quest 3 Touch Plus Profile**: Features a sleek, ringless controller profile matching official Meta Quest 3 hardware. The legacy 9cm circular tracking ring has been removed, eliminating any oversized blue disc in your view.
- **50° Angle Tilt Away From User**: Both controller visual models (grip, face, and trigger) and the right-hand pointing ray are pitched **50 degrees forward / away from the user** (`tiltAngleAwayFromUser = 50°`). When holding the controllers naturally, the pointer fires straight ahead into shelves instead of pointing upwards/backwards.
- **Left Hand**: Soft cyan accents with a dorsal wrist mounting anchor.
- **Right Hand**: Safety amber accents with a directional laser pointer ray (`LineRenderer`) that targets products across shelves.
- **Physics-Safe**: All collision components are completely stripped from hand meshes to eliminate any snagging on shelves, racks, or the floor.

### 6.2 Dorsal Wrist Next Order Checklist (Clean Multi-Column Layout, Zero Obstruction)
In desktop mode, the next order checklist was rendered via 2D screen-space GUI. In VR, it is rendered in full stereoscopic 3D via a World Space Canvas:
- **Mounted on the Dorsal Wrist / Forearm**: Positioned at `(x = -0.02m, y = +0.15m, z = -0.07m)` relative to the controller root, tilted 38° towards your face. Because it is positioned 7 cm behind the grip and elevated 15 cm, the controller body and thumbpad sit completely below and in front of the display. There is **zero overlap** between controller geometry and text.
- **Structured 3-Column Table**:
  - `PRODUCT`: Dedicated wide column rendering full SKU names (e.g. `Orange Juice Fresh 1L`, `Tortilla Chips Jalapeno 200g`, `Basmati Rice Royal 1kg`) without aggressive truncation.
  - `QTY`: Center-aligned column for required units.
  - `AISLE`: Right-aligned bold golden amber column (`#FFD54F`) showing aisle designations (`A01`, `A02`, `A03`, etc.).
- **Row Styling**:
  - Active target row: Highlighted in bright golden yellow with an indicator arrow `▶`.
  - Upcoming pending rows: Marked with `○` in light slate.
  - Picked rows: Marked with `✔` in soft green.
- **Static Floating / Pin Mode (`[T]` Shortcut)**:
  - If you prefer the Next Order card to stay static in your field of view instead of having to raise your arm, press **`[T]`** on the laptop keyboard.
  - The card detaches from the wrist and floats gently in front of your eyes (billboarded toward your head).
  - Pressing **`[T]`** again snaps it back onto your left wrist.

### 6.3 3D World-Space Grab Prompts & Wrist Gaze Suppression
- **World-Space Anchored**: When aiming at or looking at a shelf SKU, the 3D interaction badge (`[Trigger / Grip] Grab: <Product>`) hovers directly in 3D world space above the product on the shelf. It does not fly into your face or overlap your wrist.
- **Smart Wrist Gaze Suppression**: When you raise your left arm to look down at your Next Order checklist, gaze raycasts onto distant shelves in the background are automatically suppressed so background product badges never obstruct your checklist.

### 6.4 Clean Stereoscopic VR Views (2D Desktop OnGUI Suppressed in Headset)
- When wearing the Quest 3, 2D screen-space desktop IMGUI windows (such as the "SIMULATION SETUP" menu or 2D reticles) are automatically suppressed on stereo VR camera passes (`Camera.current.stereoEnabled`).
- The researcher on the laptop screen can still see the desktop GUI and click simulation setup buttons on PC, while the VR participant inside the headset enjoys an unobstructed, fully immersive 3D simulation!

### 6.3 In-VR 3D Gaze Grab Prompt & Reticle
When exploring the aisles, the VR rig continuously projects a central gaze ray from the HMD camera and a pointer ray from your right controller:
- **Look to Inspect**: Looking directly at any stocked product box immediately highlights the item and surfaces a floating 3D badge:
  `[Trigger / Grip] Grab: <Product Name> (<SkuId>) [<LocationSlot>]`
- **Dynamic Reticle**: The center gaze dot dynamically expands and changes from translucent white to glowing amber when aimed at an interactable object.
- **Looking Away**: The badge automatically dismisses when looking away to keep your field of view clean.

### 6.4 Locomotion, Right Stick Turning & Sprint
- **Normal Walking (1.4 m/s)**: Move the **Left Controller Thumbstick** in any direction. Locomotion is oriented relative to your headset's gaze forward direction.
- **Character Rotation (Right Controller Thumbstick - VR Best Practice)**:
  - Flick the **Right Controller Thumbstick** left or right to rotate your character in 45° snap increments (`snapTurnAngle = 45°`).
  - Smooth turning is also supported (`smoothTurnSpeed = 90°/s`).
  - In-place turning does not alter cumulative travel displacement, preserving 100% faithful industrial work-study distance measurements.
  - **Keyboard Fallback**: The **Left Arrow** and **Right Arrow** keys on the laptop can also be used to turn the VR character.
- **Sprint Transit (3.5 m/s)**:
  - **Option 1**: Click the **Left Controller Thumbstick** down while moving.
  - **Option 2**: Squeeze and hold the **Left Controller Grip Button** while moving forward.
  - **Option 3**: Hold the **Left Shift** key on your laptop keyboard.
- **Continuous Travel Telemetry**: Every centimeter of displacement is recorded by `ActivityClassifier` and `WorkEventLogger` for industrial time-and-motion analysis.

### 6.5 Full Desktop-to-VR Interaction Mapping
All desktop grabbing, picking, and dispatch interactions are 100% mapped to VR controls:

| Industrial Action | Desktop Control | VR Controller Control |
|---|---|---|
| **Pick / Grab Shelf SKU** | Press `[E]` or Left-Click mouse | Pull **Right Index Trigger** or squeeze **Right Grip (Grab)** |
| **Gaze Prompt** | Center screen crosshair text | 3D in-world floating badge & amber gaze reticle |
| **Rotate Character** | Mouse Look / Arrow keys | Flick **Right Controller Thumbstick** Left/Right (45° Snap Turn) |
| **Collect Pick Cart & Activate Order** | Press `[E]` at Collection Desk | Aim / look at Desk + pull Trigger or Grip |
| **Deliver to Staging & Finalize Order** | Press `[E]` at Packing Bench (`PACK-01`) | Aim / look at Bench + pull Trigger or Grip |
| **Sprint Locomotion** | Hold `Left Shift` | Click Left Thumbstick or hold Left Grip |
| **Toggle Order UI Pin / Wrist** | N/A (Desktop HUD always on screen) | Press `[T]` on keyboard (or wrist view) |
| **Researcher Dashboard** | Press `[F2]` on keyboard | Press **Left Controller 'Y' Button** or `[F2]` |
| **Toggle Desktop $\longleftrightarrow$ VR** | Press `[V]` on keyboard | Press `[V]` on laptop keyboard |
| **Researcher Dashboard** | Press `[F2]` on keyboard | Press **Left Controller 'Y' Button** or `[F2]` |
| **Toggle Desktop $\longleftrightarrow$ VR** | Press `[V]` on keyboard | Press `[V]` on laptop keyboard |

---

## 7. Step 5: How to Run the Simulation

You can run the simulation through two supported methods:

### Method A: Play Directly inside Unity Editor (Fastest for Iteration)
1. Verify the Meta Horizon PC App is running and Quest Link is active inside your headset.
2. Open the project in Unity Editor (`6000.6.7f1`).
3. Open `Assets/Scenes/DarkStore_Phase1.unity`.
4. Click the Unity **Play** button (`Ctrl + P`).
5. In the on-screen HUD (or top-left panel), click **Start VR Rig** (or press **`[V]`** on the keyboard).
6. Put on your Quest 3 headset:
   - You will see the entire quick-commerce dark store in stereo 3D.
   - Look around to inspect the shelves and overhead aisle labels.
   - Use the thumbsticks and controllers to pick items and fulfill orders!

### Method B: Build Windows Standalone .exe (Optimized Full-Screen Performance)
1. In Unity Editor, go to **File $\to$ Build Settings** (`Ctrl + Shift + B`).
2. Select **Windows, Mac, Linux** platform:
   - **Target Platform**: `Windows`
   - **Architecture**: `Intel 64-bit` (`x86_64`)
   - Ensure `Assets/Scenes/DarkStore_Phase1.unity` is checked.
3. Click **Build and Run**.
4. Choose an output folder (e.g. `Builds/Windows/VirtualDarkStore.exe`).
5. Launch `VirtualDarkStore.exe` while Quest Link is connected:
   - The application starts in full VR inside your headset at native framerates with zero editor overhead.

---

## 8. Step 6: Exporting & Analyzing Participant CSV Data

1. Once a picking run is completed, open the Work-Study Results panel (press **`[Tab]`** or use the controller prompt).
2. Click **Save Participant CSVs**.
3. All telemetry is written directly to the Windows hard drive:
   - `WorkStudyData/Participants/P01_<Name>/Summary/` (RFC-4180 summary metrics).
   - `WorkStudyData/Participants/P01_<Name>/Events/` (millisecond-accurate elemental event logs).
   - `WorkStudyData/Combined/All_Session_Summaries.csv` (aggregated multi-session analysis table).

---

## 9. Troubleshooting & FAQ

| Symptom | Cause | Solution |
|---|---|---|
| **Headset shows blank white grid / doesn't launch VR** | OpenXR runtime not set to Meta Quest Link | Open Meta Horizon PC App $\to$ Settings $\to$ General $\to$ Click *Set as active* under OpenXR Runtime. Restart Unity. |
| **Player spawns inside shelves or aisle racks** | Starting coordinates set to world origin `(0, 0, 0)` | Rig auto-corrects to `(0, 0, -8.5m)` upon start. You can also press **`[V]`** or select *Start VR Rig* in the HUD to teleport directly to the front cross-aisle. |
| **Camera horizon is tilted or angled** | Non-zero parent pitch/roll | The Camera Offset rotation has been locked to `Quaternion.identity`. If your physical floor calibration shifted, hold the **Oculus button** on the right controller for 2 seconds to recenter your headset orientation. |
| **Desktop and VR cameras fight / duplicate AudioListener warning** | Both cameras enabled | The `VRPlayerRig` automatically mutes desktop listeners when enabled and re-enables them when disabled. |
| **USB Link disconnects or drops frames** | Cable connected to USB 2.0 port | Plug cable into a dedicated USB 3.0+ port on your laptop. In Meta Horizon PC App, go to *Devices* $\to$ *Test USB Speed* to verify $> 1.8\text{ Gbps}$ throughput. |
