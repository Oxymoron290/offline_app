# User Guide

A guide for case workers using the BlazorWASM_PWA application in the field.

---

## Installing the App

The app is a Progressive Web App (PWA) — it runs in your browser but can be installed on your device for a native app-like experience. **You do not need to visit an app store.**

### Install on Android (Chrome)

1. Open Chrome and navigate to the application URL
2. Wait for the page to fully load
3. Tap the **"Install App"** banner at the bottom of the screen, or:
   - Tap the **three-dot menu** (⋮) in the top-right corner
   - Tap **"Install app"** or **"Add to Home screen"**
4. Tap **"Install"** in the confirmation dialog
5. The app icon will appear on your home screen

### Install on iPhone/iPad (Safari)

1. Open **Safari** (must be Safari — other browsers on iOS do not support PWA install)
2. Navigate to the application URL
3. Wait for the page to fully load
4. Tap the **Share** button (the square with an upward arrow) at the bottom of the screen
5. Scroll down and tap **"Add to Home Screen"**
6. Tap **"Add"** in the top-right corner
7. The app icon will appear on your home screen

> **Important:** On iOS, always use Safari for installation. Chrome and Firefox on iOS do not support installing PWAs.

### Install on Desktop (Windows/Mac)

**Microsoft Edge:**
1. Navigate to the application URL
2. Click the **install icon** (monitor with a down arrow) in the address bar
3. Click **"Install"**

**Google Chrome:**
1. Navigate to the application URL
2. Click the **install icon** in the address bar, or:
   - Click the **three-dot menu** → **"Install [App Name]"**
3. Click **"Install"**

The app opens in its own window without the browser address bar.

---

## Signing In

1. Open the app (from your home screen icon or browser)
2. Tap **"Sign In"**
3. Enter your organizational email and password on the Microsoft login page
4. Complete any multi-factor authentication (MFA) prompts
5. You will be redirected back to the app, now signed in

> **Tip:** Your login session stays active for extended periods. You should not need to sign in frequently.

---

## Navigating the App

### Home Dashboard

After signing in, the home dashboard shows:

- **Pending sync count** — Number of records waiting to be sent to the server
- **Last sync time** — When data was last successfully synced
- **Connectivity status** — Whether the app can reach the server
- **Quick actions** — Create a new record, view recent records

### Entity List

View all records (entities) that you have created or that have been synced to your device:

- **Search** — Filter records by name or type
- **Sort** — Order by date created, date modified, or name
- **Tap a record** to view details or edit it

### Creating a New Record

1. Tap the **"+ New Record"** button
2. Select the **entity type** (e.g., case report, inspection, assessment)
3. Fill in the required form fields
4. Attach any photos, videos, or documents (see [Capturing Photos and Documents](#capturing-photos-and-uploading-documents))
5. Tap **"Save"**

The record is saved to your device immediately. It will sync to the server when you have connectivity.

### Editing a Record

1. Navigate to the entity list and tap the record
2. Tap **"Edit"**
3. Make your changes
4. Tap **"Save"**

> **Note:** If you edit a record that was also modified on the server by someone else, the most recent change wins. Both versions are preserved on the server for audit purposes.

---

## Working Offline

The app is designed to work **without an internet connection**. Here is what you need to know.

### Connectivity Indicator

Look for the connectivity indicator in the app header:

| Indicator | Meaning |
|-----------|---------|
| 🟢 **Online** | Connected to the server. Data syncs automatically. |
| 🟡 **Syncing** | Currently sending data to the server. |
| 🔴 **Offline** | No server connection. Data is saved locally and will sync later. |

### What Works Offline

- ✅ Creating new records
- ✅ Editing existing records (that are already on your device)
- ✅ Taking and attaching photos
- ✅ Viewing previously synced records
- ✅ All form data entry
- ✅ Attaching documents and videos

### What Requires Connectivity

- ❌ Signing in for the first time
- ❌ Downloading records created by other users
- ❌ Sending data to the server (queued until online)
- ❌ Receiving updates from the server

### How Offline Storage Works

When you save a record offline:

1. The record is stored securely in your device's browser storage (IndexedDB)
2. A sync operation is queued
3. When connectivity returns, the app automatically sends all queued operations to the server
4. You do not need to take any action — syncing happens in the background

> **Important:** Always ensure you have loaded the app at least once while online before going into the field. The app needs to download and cache its files during the first visit.

---

## Capturing Photos and Uploading Documents

### Taking a Photo

1. Open a record (or create a new one)
2. Tap the **"Add Photo"** button
3. Your device's camera will open (or you can choose an existing photo from your gallery)
4. Take the photo or select it
5. The photo appears as a thumbnail in the record
6. Tap **"Save"** to save the record with the photo

The photo is stored locally on your device. It will upload to the server during the next sync.

### Uploading a Document

1. Open a record
2. Tap **"Attach Document"**
3. Select a file from your device (PDF, Word, Excel, etc.)
4. The document name appears in the attachments list
5. Tap **"Save"**

### Recording a Video

1. Open a record
2. Tap **"Add Video"**
3. Record or select a video
4. Tap **"Save"**

> **Storage note:** Videos use significantly more storage than photos or documents. Be mindful of your device's available storage when recording long videos (see [Storage Tips](#tips-for-the-best-experience)).

### Supported File Types

| Type | Formats |
|------|---------|
| Photos | JPEG, PNG, HEIC (iOS) |
| Videos | MP4, MOV, WebM |
| Documents | PDF, DOCX, XLSX, PPTX, TXT, CSV |

---

## Understanding Sync Status

### Pending Operations Count

The app shows the number of pending operations (records and files waiting to sync) in the header or dashboard. For example:

> **3 pending** — Three operations are waiting to be sent to the server.

### Sync Status Meanings

| Status | Description |
|--------|-------------|
| **Synced** | All data has been successfully sent to the server. |
| **Pending** | Data is saved locally and waiting for connectivity to sync. |
| **Syncing** | Data is currently being sent to the server. |
| **Error** | A sync operation failed. See below for resolution. |

### Sync Errors

If a sync error occurs:

1. The app will automatically retry the failed operation (up to 5 times with increasing delays)
2. If retries fail, the operation is marked with an error
3. You can view error details by tapping on the sync status indicator
4. Tap **"Retry"** to manually retry failed operations

**Common sync errors:**

| Error | What to Do |
|-------|------------|
| Network error | Move to an area with connectivity and retry. |
| File too large | Reduce video length or photo resolution and re-attach. |
| Authentication expired | Sign out and sign back in, then retry. |
| Server error | Wait a few minutes and retry. Contact support if persistent. |

### Manual Sync

While syncing happens automatically, you can force a sync:

1. Tap the **sync icon** (🔄) in the app header
2. The app will attempt to send all pending operations immediately

This is useful when you have just regained connectivity and want to ensure everything is sent.

---

## Tips for the Best Experience

### Before Going to the Field

- ✅ **Load the app while online** — Open the app and navigate through key screens to ensure all resources are cached
- ✅ **Verify you are signed in** — Check that your name appears in the app header
- ✅ **Check pending sync count** — Make sure all previous records have synced (pending count is 0)
- ✅ **Charge your device** — Offline data collection can use significant battery

### While in the Field

- ✅ **Save frequently** — Tap "Save" often to ensure data is stored locally
- ✅ **Check the connectivity indicator** — Be aware of your connection status
- ✅ **Take photos at reasonable resolution** — High-resolution photos use more storage
- ✅ **Keep videos short** — Videos consume storage quickly

### Storage Space Awareness

Your device has a limited amount of storage for offline data. Monitor usage:

- **Android/Chrome:** The app typically has access to several GB of storage
- **iPhone/iPad (Safari):** Limited to approximately **1 GB** of offline storage — this fills quickly with photos and videos

**If you see a "Storage full" warning:**

1. Return to an area with connectivity
2. Wait for all pending operations to sync
3. The app will clean up local copies of synced data
4. If the problem persists, clear old synced data from the app settings

### After Returning from the Field

- ✅ **Connect to Wi-Fi** — Syncing large files (photos, videos) is faster and more reliable on Wi-Fi
- ✅ **Keep the app open** — Leave the app open until all pending operations have synced
- ✅ **Verify sync completion** — Check that the pending count reaches 0
- ✅ **Do not clear browser data** — Clearing browser data will delete unsynced records

> ⚠️ **Warning:** Never clear your browser data or uninstall the app while there are pending sync operations. Unsynced data will be permanently lost.

---

## Frequently Asked Questions

### General

**Q: Do I need an internet connection to use the app?**
A: No. The app works fully offline for creating and editing records, taking photos, and attaching documents. You only need a connection to sync data to the server and to receive updates from other users.

**Q: Is my data safe when working offline?**
A: Yes. Data is stored securely in your device's browser storage. It persists even if you close the app or restart your device. However, clearing browser data or uninstalling the app will delete unsaved data.

**Q: How long can I work offline?**
A: There is no time limit. You can work offline for days or weeks. The limiting factor is your device's storage space. When you reconnect, all queued data will sync.

### Installation

**Q: Why can't I find the app in the App Store / Google Play?**
A: This is a Progressive Web App (PWA) that installs directly from the browser. Follow the installation instructions above for your device.

**Q: The install option doesn't appear. What should I do?**
A: Ensure:
- You are using Safari (on iOS) or Chrome/Edge (on Android/desktop)
- The page has fully loaded
- You have visited the app at least once before trying to install

**Q: Can I use the app in multiple browser tabs?**
A: It is recommended to use the app in a single tab or window to avoid sync conflicts.

### Offline and Sync

**Q: What happens if I lose power while working offline?**
A: Any data you have saved (by tapping "Save") is safely stored and will be available when you reopen the app. Unsaved form data (changes you have not yet saved) may be lost.

**Q: Can two people edit the same record offline?**
A: Yes, but the last person to sync their changes will overwrite the other's edits. The system keeps a history of all changes for audit purposes.

**Q: I see a "sync error" — what should I do?**
A: First, ensure you have an internet connection. Then tap "Retry" on the failed operation. If the error persists, try signing out and back in. Contact your administrator if the problem continues.

**Q: How do I know if all my data has synced?**
A: Check the pending operations count in the app header or dashboard. When it shows **0 pending**, all data has been sent to the server.

### Photos and Documents

**Q: Is there a file size limit for uploads?**
A: Individual files should be under 100 MB. For videos, keep recordings under 5 minutes for optimal sync performance.

**Q: Can I take photos with the app?**
A: Yes. The app uses your device's camera directly. You can also select existing photos from your gallery.

**Q: What if I accidentally attach the wrong file?**
A: Open the record, tap on the attachment, and select "Remove" before saving. If already synced, edit the record and remove the attachment.

### Troubleshooting

**Q: The app is showing an old version. How do I update?**
A: Close the app completely and reopen it while connected to the internet. The app updates automatically. If that does not work, on your device go to the browser settings and clear the cache for this specific site (not all browser data).

**Q: The app is running slowly. What can I do?**
A: Try:
1. Sync all pending data to free up local storage
2. Close other apps and browser tabs
3. Restart the app
4. If on a mobile device, restart the device

**Q: I was signed out unexpectedly. Why?**
A: Your authentication session may have expired. This typically happens after extended offline periods. Simply sign in again — your offline data is preserved.

---

## Getting Help

If you encounter an issue not covered in this guide:

1. **Check your connectivity** — Many issues resolve once you have a stable connection
2. **Note the error message** — Take a screenshot of any error messages
3. **Contact your administrator** — Provide details about what you were doing, the error message, and your device type

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Developer setup guide
- [Architecture](ARCHITECTURE.md) — Technical design (for developers)
