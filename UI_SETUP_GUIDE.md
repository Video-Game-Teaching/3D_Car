# Nitro Charges UI Setup Guide for Unity Editor

This guide will walk you through setting up the Nitro Charges UI display in Unity Editor.

## Prerequisites
- You have a Scene with a CarController component attached to a car GameObject
- You have TextMeshPro installed (already in your project)

---

## Step 1: Create or Locate the Canvas

### Option A: If you already have a Canvas in your scene:
1. Open your scene in Unity Editor
2. In the Hierarchy window, find your Canvas GameObject
3. If you don't see one, create one (see Option B)

### Option B: Create a new Canvas:
1. In the Hierarchy window, right-click in empty space
2. Select **UI > Canvas**
3. This creates a Canvas with an EventSystem (keep the EventSystem)

---

## Step 2: Create the Nitro Charges Text Display

1. Right-click on the **Canvas** in Hierarchy
2. Select **UI > Text - TextMeshPro**
   - If prompted to import TMP Essentials, click "Import TMP Essentials"
3. Rename the new GameObject to **"NitroChargesText"**

---

## Step 3: Position and Style the Text

1. Select **NitroChargesText** in Hierarchy
2. In the Inspector window, you'll see the TextMeshProUGUI component

### Position the Text:
- In the Rect Transform component:
  - Set **Anchor Presets**: Click the anchor icon and choose a corner (e.g., Top-Left)
  - Adjust **Pos X** and **Pos Y** to position it (e.g., 50, -50 for top-left corner)
  - Set **Width**: 200
  - Set **Height**: 50

### Style the Text:
- In the **TextMeshProUGUI** component:
  - **Text**: Set to "Nitro Level: 0" (this is just a placeholder)
  - **Font Size**: Set to 36 (or your preferred size)
  - **Alignment**: Choose center or left alignment
  - **Color**: Set to a visible color (e.g., white or orange)

---

## Step 4: Add the HUDNitroCharges Script

1. Select the **NitroChargesText** GameObject in Hierarchy
2. In the Inspector, click **"Add Component"**
3. Search for "HUDNitroCharges"
4. Click to add the **HUDNitroCharges** component

---

## Step 5: Connect the References

1. With **NitroChargesText** still selected, look at the **HUDNitroCharges** component in Inspector

### Connect the Car Controller:
2. Find your car GameObject in the Hierarchy (the one with CarController component)
3. Drag and drop the car GameObject from Hierarchy into the **"Car Controller"** field in HUDNitroCharges component

### Connect the Text Field:
4. The **"Nitro Charges Text"** field should already be connected automatically (since the script is on the same GameObject as the text)
   - If it's not connected: Drag the **NitroChargesText** GameObject from Hierarchy into this field

### Optional - Visual Indicators:
5. If you want to use visual indicators (icons/bars for each charge):
   - Create 3 Image GameObjects as children of NitroChargesText
   - Drag each Image GameObject into the **"Charge Indicators"** array
   - The array should have 3 elements (for 3 charges)

---

## Step 6: Configure Display Options (Optional)

In the **HUDNitroCharges** component, you can adjust:

- **Show Text**: Keep this checked (to show "Nitro Level: X")
- **Prefix Text**: "Nitro Level: " (you can change this)
- **Active Color**: Color when charges > 0 (default: orange/red)
- **Inactive Color**: Color when charges = 0 (default: gray)
- **Auto Hide When Zero**: If checked, hides text when no charges

---

## Step 7: Test It!

1. Press **Play** in Unity Editor
2. The text should show "Nitro Level: 0" initially
3. Hold **Space** and drift to earn nitro charges
4. The text should update to "Nitro Level: 1", "Nitro Level: 2", or "Nitro Level: 3"
5. Press **Shift** to activate nitro - charges should go back to 0

---

## Troubleshooting

### If the text doesn't update:
1. Check that the CarController reference is connected in HUDNitroCharges
2. Make sure the CarController has a PlayerDrivingInput component attached
3. Verify the CarController GameObject is active in the scene
4. Check the Console for any errors

### If you see "Nitro Level: 0" but it doesn't change:
- Make sure you're drifting correctly (hold Space while steering)
- Check that drift duration is reaching the thresholds (1s, 2s, 4s)
- Verify the CarController is awarding charges correctly

### If the text is not visible:
- Check the Canvas Render Mode (should be Screen Space - Overlay)
- Make sure the text color contrasts with the background
- Check the Rect Transform position and size

---

## Alternative: Simpler Setup (Just Text)

If you want the simplest setup:

1. Create Canvas (if needed)
2. Create Text - TextMeshPro child
3. Add HUDNitroCharges script to the Text GameObject
4. Drag CarController GameObject to "Car Controller" field
5. Done!

The script will auto-find the TextMeshProUGUI component on the same GameObject.

