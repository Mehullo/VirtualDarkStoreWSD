# Meta Quest 3 VR Setup, Build & Deployment Guide
## Virtual Dark Store Work-Study & Simulation Platform

This step-by-step guide walks you through connecting your **Meta Quest 3** headset via USB, configuring the environment, building/deploying the application, and retrieving exported participant work-study CSV files.

---

## 1. Prerequisites Checklist

Ensure you have the following ready before starting:

1. **Hardware**:
   - Meta Quest 3 headset with standard Touch Plus controllers.
   - High-speed USB-C to USB-A or USB-C 3.0 cable connected directly to a high-speed USB 3.0+ port on your PC.
2. **Software**:
   - **Unity 6000.6.7f1** (already installed at `C:\Program Files\Unity\Hub\Editor\6000.6.7f1`).
   - **Android Build Support** with OpenJDK, SDK, and NDK (already configured in the Unity Editor).
   - **Meta Quest Mobile App** (installed on your smartphone to toggle Developer Mode).
   - **Meta Horizon OS Developer Account** (free registration at [developer.oculus.com](https://developer.oculus.com)).

---

## 2. Step 1: Enable Developer Mode on Meta Quest 3

1. Turn on your Meta Quest 3 headset.
2. Open the **Meta Quest App** on your paired smartphone.
3. Tap **Menu** (bottom right) $\to$ **Devices**.
4. Select your **Meta Quest 3** headset.
5. Tap **Headset Settings** $\to$ **Developer Mode**.
6. Switch the **Developer Mode** toggle to **ON**.
7. Restart your Meta Quest 3 headset.

---

## 3. Step 2: Connect Quest 3 to PC via USB Cable

1. Plug the USB-C cable into the side port of the Quest 3 and plug the other end into your PC.
2. Put on the Quest 3 headset.
3. You will see an Android system prompt: **"Allow USB Debugging?"**:
   - Check the box: **"Always allow from this computer"**.
   - Click **Allow**.
4. If a prompt appears asking **"Allow access to data?"**, click **Allow** (this enables Windows File Explorer to browse files via MTP).

---

## 4. Step 3: Verify USB Device Connection in Terminal

Open PowerShell on your PC and run:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" devices
```

**Expected Output**:
```text
List of devices attached
1WMHH812XXXXXX    device
```
*(If it says `unauthorized`, put on your headset and accept the "Allow USB Debugging" dialog).*

---

## 5. Step 4: Build and Deploy the VR Application

You can deploy to your headset in two ways:

### Option A: Direct Build & Run from Unity Editor (Recommended)
1. Open the project in **Unity 6000.6.7f1**.
2. Go to **File $\to$ Build Settings** (or press `Ctrl + Shift + B`).
3. Ensure **Platform** is set to **Android**:
   - If not, select **Android** and click **Switch Platform**.
4. Ensure `Assets/Scenes/DarkStore_Phase1.unity` is checked in **Scenes In Build**.
5. Under **Run Device**, select your connected **Meta Quest 3** (or `Default Device`).
6. Click **Build and Run**.
7. Choose a destination filename (e.g. `Builds/DarkStore_Quest3.apk`).
8. Unity will compile via IL2CPP ARM64, package the APK, automatically install it to your Quest 3 via USB, and launch the application directly in your headset!

### Option B: Installing Pre-built APK via ADB
If you have a built `.apk` file (such as `warehouse_picking.apk` in the project root):
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r warehouse_picking.apk
```
Once installed, put on the Quest 3:
- Open the App Library $\to$ Search filter (top right) $\to$ Select **Unknown Sources**.
- Click **WSD VR Project** to launch.

---

## 6. Step 5: Playing in VR (Controls & Navigation)

Once inside the dark store in VR:

| Control | Controller / Input | Function |
|---|---|---|
| **Head Movement** | Physical Headset | 6-DoF natural head tracking (look in any direction). |
| **Locomotion** | **Left Controller Thumbstick** | Smooth walking locomotion through corridors and aisles. |
| **Aiming / Reticle** | **Right Controller** | Spatial pointing ray projecting from the controller. |
| **Pick Product** | **Right Index Trigger** | Point at target shelf item and pull trigger to pick SKU. |
| **Complete Packing**| **Right Index Trigger** | Stand at Dispatch Packing Bench (`PACK-01`) and pull trigger when all items are picked. |
| **Target Guide** | Floating Beacon | Luminous marker hovering directly over the next SKU slot to guide picking sequence. |
| **Researcher Dashboard**| **Left Controller Y Button** | Opens the 10-participant management dashboard in VR. |

---

## 7. Step 6: Step-by-Step Work-Study Session Workflow

1. **Select Participant**:
   - Open Researcher Dashboard (`[F2]` on desktop or `Y` button in VR).
   - Enter/select participant slot (e.g. `P01: Rahul`).
2. **Select Scenario & Order**:
   - Choose Scenario (e.g. `Scenario A`) and Order (e.g. `Standard Order` or Generated Batch).
3. **Select Routing Mode**:
   - Choose **Baseline** (original order sequence) or **Optimized** (C# TSP solver route).
4. **Execute Fulfillment**:
   - Walk to Aisles `A01`–`A06`, follow target beacons, point and pull trigger on shelf SKU boxes.
   - Proceed to `PACK-01` desk to finalize packing.
5. **Save Work-Study Report**:
   - Pull up results UI (`[Tab]`).
   - Click **Save Participant CSVs**.
   - The session summary and granular event logs are automatically generated and saved.

---

## 8. Step 7: How to Retrieve Participant CSV Data from Quest 3

All participant CSVs on the Meta Quest 3 are written to:
```text
/sdcard/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/WorkStudyData/
```

### Method 1: Instant ADB Pull Command (Fastest)
Run this single command in PowerShell on your PC while the Quest 3 is connected via USB:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" pull /sdcard/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/WorkStudyData/ ./WorkStudyData_Quest/
```
All participant session folders and combined files will be pulled straight into a `WorkStudyData_Quest` folder on your PC!

### Method 2: Windows File Explorer (MTP)
1. Keep the headset connected via USB.
2. Put on the headset and click **Allow** if prompted for data access.
3. Open **Windows File Explorer** $\to$ Click **Quest 3** on the left sidebar.
4. Navigate to:
   ```text
   Internal shared storage > Android > data > com.UnityTechnologies.com.unity.template.urpblank > files > WorkStudyData
   ```
5. Copy the `Participants` and `Combined` folders to your PC.

---

## 9. Alternative: Playing via Meta Quest Link / Air Link (PC VR Mode)

If you prefer to run the simulation inside the Unity Editor with live telemetry without compiling an APK each time:

1. Install the official **Meta Quest PC App** on your Windows PC.
2. Connect Quest 3 to PC via USB-C link cable.
3. Inside the Quest 3 headset, select **Quest Link** from the Quick Settings menu.
4. In Unity Editor, open `Assets/Scenes/DarkStore_Phase1.unity`.
5. Press the **Play** button in Unity.
6. The simulation will render directly inside your headset with real-time head and controller tracking, while writing CSVs directly to `WorkStudyData/` on your PC hard drive!
