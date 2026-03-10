# User Guide — Case Worker App

This guide is for case workers who use the Blazor Hybrid app to collect and manage field data.

## Installing the App

### Windows
1. Download the app installer from your organization's distribution channel
2. Run the installer and follow the prompts
3. The app will appear in your Start menu as **Case Worker App**

### Android
1. Download the APK from your organization's distribution portal (or install via Google Play if published)
2. Allow installation from unknown sources if prompted
3. Tap the APK to install
4. Find the app in your app drawer

### iOS / macOS
1. Install via your organization's MDM (Mobile Device Management) solution
2. Or download from the App Store if published

## Signing In

1. Launch the app
2. On the Home screen, tap **Sign In**
3. You'll be redirected to your organization's Microsoft login page
4. Enter your work email and password
5. Complete multi-factor authentication (MFA) if prompted
6. You'll be returned to the app, signed in

> **Tip**: Your sign-in stays active. You only need to re-authenticate periodically or after a long period of inactivity.

## Home Dashboard

The Home screen shows:
- **Connectivity status** — Whether you're currently online or offline
- **Entity count** — Total number of entities you've created
- **Pending sync count** — Number of operations waiting to be synced to the server

## Managing Entities

Entities are the core records you create during field visits (cases, inspections, assessments, etc.).

### Creating an Entity
1. Tap **Manage Entities** on the Home screen (or **Entities** in the navigation)
2. Tap **+ New Entity**
3. Fill in:
   - **Name** — A descriptive name for this record
   - **Type** — The category (e.g., "Inspection", "Case", "Assessment")
   - **Description** — Additional details
4. Add **Custom Fields** for any additional data:
   - Tap **+ Add Field**
   - Enter a field name and value
   - Repeat for as many fields as needed
5. Tap **Create**

The entity is saved **immediately to your device** — no internet required.

### Editing an Entity
1. Go to **Entities**
2. Tap on the entity you want to edit
3. Make your changes
4. Tap **Save**

### Deleting an Entity
1. Open the entity
2. Tap **Delete** (red button at bottom right)
3. The entity is soft-deleted and will be synced as a deletion

## Capturing Photos

1. Navigate to **Photos** in the sidebar
2. Tap **📷 Take Photo**
3. Your device camera will open
4. Take the photo
5. The photo is automatically:
   - Saved to your device
   - Tagged with GPS coordinates (if location access is granted)
   - Queued for sync

Photos appear in the gallery view with their capture date and sync status.

## Recording Videos

1. Navigate to **Videos** in the sidebar
2. Tap **🎥 Record Video**
3. Your device camera opens in video mode
4. Record your video
5. The video is saved and queued for sync

> **Note**: Large video files may take longer to sync. Ensure you have a stable connection for video uploads.

## Attaching Documents

1. Navigate to **Documents** in the sidebar
2. Tap **📎 Attach Document**
3. Select a file from your device (PDF, Word, images, etc.)
4. The document is saved locally and queued for sync

## Understanding Sync

### How It Works

The app uses a **store-and-forward** approach:
1. All data is saved **locally first** — you never lose work
2. Every 30 seconds, the app checks for internet connectivity
3. When online, pending operations are sent to the server automatically
4. You can see the status of all pending operations on the **Sync Status** page

### Sync Status Icons

| Icon | Meaning |
|------|---------|
| ✅ | Synced to server |
| ⏳ | Waiting to sync |

### Manual Sync

If you want to force an immediate sync:
1. Go to **Sync Status**
2. Tap **Sync Now**

### Sync Failures

If a sync operation fails (red row in the queue):
- The app will retry automatically (up to 5 times)
- Check the **Error** column for details
- Common causes: server temporarily unavailable, network timeout
- If failures persist, contact your IT support

## Working Offline

The app is designed to work fully offline:

- ✅ Create and edit entities
- ✅ Take photos and record videos
- ✅ Attach documents
- ✅ View all previously created data
- ⏳ Sync happens automatically when you reconnect

**No data is lost when offline.** Everything is stored on your device until it can be synced.

### Tips for Offline Usage

1. **Sign in before going into the field** — Authentication requires internet
2. **Check your pending sync count** before leaving a connected area
3. **Connect periodically** to sync your data and reduce the queue
4. **Don't uninstall the app** while there are pending sync operations

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Can't sign in | Ensure you have internet access. Check your credentials. |
| Camera not working | Check that the app has camera permissions in your device settings. |
| Photos not showing | Photos are stored locally. If you cleared app data, photos may be lost. |
| Sync always failing | Check internet connection. Try "Sync Now". Contact IT if persistent. |
| App is slow | Close and reopen the app. Ensure your device has free storage. |
| Location not captured on photos | Enable location permissions for the app in device settings. |

## Data Privacy

- All data is stored encrypted on your device
- Data is transmitted over HTTPS to secure Azure servers
- Only authenticated users with proper permissions can access the data
- Media files are stored in encrypted Azure Blob Storage
- Your organization controls data retention policies
