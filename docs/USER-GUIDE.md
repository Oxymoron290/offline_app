# User Guide — Field Service Application

This guide is for **field workers and case workers** who use the BlazorPWA application to conduct facility inspections, capture data, and submit reports.

---

## Table of Contents

- [Installing the App](#installing-the-app)
- [Using the Application](#using-the-application)
- [Capturing Media](#capturing-media)
- [Working Offline](#working-offline)
- [Syncing Data](#syncing-data)
- [Handling Conflicts](#handling-conflicts)
- [Tips for Field Workers](#tips-for-field-workers)
- [Frequently Asked Questions](#frequently-asked-questions)

---

## Installing the App

The BlazorPWA application is a **Progressive Web App (PWA)** — it installs directly from your web browser. No app store download is required.

### Step-by-Step Installation

#### On a Laptop or Desktop (Chrome or Edge)

1. Open **Google Chrome** or **Microsoft Edge**.
2. Navigate to your organization's application URL (provided by your IT department).
3. Look for the **install icon** in the browser address bar:
   - **Chrome:** A small computer-with-arrow icon appears on the right side of the address bar.
   - **Edge:** A small `+` icon or "App available" prompt appears.
4. Click the install icon and select **Install**.
5. The app opens in its own window and a shortcut is added to your desktop and Start menu.

#### On an Android Device (Chrome)

1. Open **Chrome** on your Android device.
2. Navigate to the application URL.
3. A banner appears at the bottom: **"Add to Home Screen"**. Tap it.
   - If no banner appears: tap the **three-dot menu** (⋮) → **Install app** or **Add to Home screen**.
4. Tap **Install**.
5. The app icon appears on your home screen like a native app.

#### On an iPhone or iPad (Safari)

1. Open **Safari** (other browsers do not support PWA install on iOS).
2. Navigate to the application URL.
3. Tap the **Share button** (square with an arrow pointing up).
4. Scroll down and tap **Add to Home Screen**.
5. Tap **Add**.
6. The app icon appears on your home screen.

### Verifying Installation

After installation:
- The app should open in its own window (not inside a browser tab).
- You should see the app icon on your home screen, desktop, or Start menu.
- The app loads quickly, even with a slow connection, because assets are cached locally.

> **Note:** If the app does not install, ensure you are using a supported browser (Chrome, Edge, or Safari on iOS). Contact your IT department if you continue to have issues.

---

## Using the Application

### Dashboard Overview

When you open the app, you see the **Dashboard** with:

- **My Reports** — A list of your inspection reports, sorted by most recent.
- **Status Summary** — Count of reports by status: Draft, In Progress, Completed, Synced.
- **Quick Actions** — Buttons for common tasks:
  - **+ New Report** — Start a new inspection report.
  - **Sync Now** — Manually trigger a data sync.
- **Connectivity Indicator** — Shows whether you are online (🟢) or offline (🔴).
- **Sync Status** — Shows the number of pending sync items.

### Creating a New Inspection Report

1. Tap **+ New Report** on the dashboard (or from the navigation menu).
2. Fill in the **Report Details**:
   - **Facility Name** (required) — Name of the facility being inspected.
   - **Facility Address** — Street address of the facility.
   - **Inspection Date** (required) — Date of the inspection (defaults to today).
   - **Inspector Name** — Your name (auto-filled from your profile).
   - **Report Type** — Select from the dropdown (e.g., Routine, Follow-up, Complaint).
3. Complete the **Inspection Checklist**:
   - Check off each item as you inspect it.
   - Tap a checklist item to expand it and add specific observations.
4. Tap **Save** to save the report locally.

> **Tip:** You can save a report at any time and come back to it later. Reports in "Draft" status can be edited freely.

### Filling Out Form Fields

- **Required fields** are marked with a red asterisk (*).
- **Date fields** open a date picker. You can also type the date directly.
- **Dropdown fields** show a list of options. Tap to select.
- **Text areas** allow multi-line input for detailed observations.
- **Checkboxes** are tapped to toggle.
- All form data is **saved locally** immediately when you tap Save.

### Adding Notes

1. Open an existing report.
2. Scroll to the **Notes** section.
3. Tap **+ Add Note**.
4. Type your observation or comment.
5. Tap **Save Note**.
6. Notes are timestamped and associated with the report.

You can add as many notes as needed. Notes are useful for recording observations, follow-up items, or explanations.

---

## Capturing Media

You can attach photos, videos, and documents to any inspection report.

### Taking Photos

1. Open the report you want to add a photo to.
2. Tap **Add Photo**.
3. Choose one of:
   - **Take Photo** — Opens your device camera. Aim and tap the shutter button.
   - **Choose from Gallery** — Select an existing photo from your device.
4. The photo appears as a thumbnail in the report.
5. (Optional) Add a caption describing the photo.
6. Tap **Save**.

**Photo guidelines:**
- Take clear, well-lit photos.
- Include enough context so the photo is meaningful on its own.
- Photos are stored locally until synced.

### Recording Videos

1. Open the report.
2. Tap **Add Video**.
3. Choose one of:
   - **Record Video** — Opens your device camera in video mode. Tap to start/stop recording.
   - **Choose from Gallery** — Select an existing video.
4. The video appears as a thumbnail with a play icon.
5. (Optional) Add a description.
6. Tap **Save**.

**Video guidelines:**
- Keep videos short and focused (under 2 minutes is ideal).
- Narrate what you're showing if possible.
- Videos take longer to sync than photos due to file size.

### Attaching Documents

1. Open the report.
2. Tap **Add Document**.
3. Browse and select a file from your device.
4. Supported formats: **PDF**, **Word (.docx)**, **Excel (.xlsx)**, **Images (.jpg, .png)**.
5. The document name appears in the attachments list.
6. Tap **Save**.

### File Size Limits

| Media Type | Maximum File Size |
|------------|-------------------|
| Photos | 10 MB |
| Videos | 100 MB |
| Documents | 25 MB |

If a file exceeds the limit, the app displays an error message. Reduce the file size or quality and try again.

> **Tip:** For photos, the app automatically compresses images to reduce size while maintaining quality.

---

## Working Offline

The app is designed to work **without an internet connection**. You can conduct an entire facility inspection offline and sync later.

### How to Know When You're Offline

- The **connectivity indicator** in the top bar changes:
  - 🟢 **Online** — You have an internet connection.
  - 🔴 **Offline** — No internet connection detected.
- An **"Offline Mode"** banner may appear at the top of the screen.

### What Works Offline

Everything you need for an inspection works offline:

- ✅ Creating new reports
- ✅ Editing existing reports
- ✅ Adding notes
- ✅ Taking photos
- ✅ Recording videos
- ✅ Attaching documents
- ✅ Completing checklist items
- ✅ Viewing previously synced reports

### How Offline Storage Works

- All data you enter is **saved locally on your device** in the browser's database (IndexedDB).
- Media files (photos, videos, documents) are also stored locally.
- Records show a **"Pending Sync"** status badge until they are synced.
- Your data is safe as long as you **do not clear your browser data**.

### Important Offline Rules

> ⚠️ **Do NOT clear browser data, site data, or cache while you have unsynced records.** This will permanently delete your local data.

> ⚠️ **Do NOT uninstall the app while you have unsynced records.** Uninstalling removes local data.

> ✅ It's fine to close the app, restart your device, or switch to other apps. Your data is persisted.

---

## Syncing Data

Syncing uploads your locally-stored data to the organization's servers so it is backed up and accessible to supervisors and other team members.

### Automatic Sync

When your device detects an internet connection:
1. The sync engine **automatically starts** uploading pending records.
2. You'll see the sync status indicator animate (spinning arrows).
3. Records change from "Pending" to "Synced" as they are processed.
4. Media files (photos, videos, documents) are uploaded in the background.

**You do not need to do anything** — sync happens automatically.

### Manual Sync

If you want to force a sync (for example, to ensure everything is uploaded before leaving):

1. Tap the **Sync Now** button on the dashboard.
2. Or tap the **sync icon** in the top navigation bar.
3. The app attempts to sync all pending records immediately.

### Sync Status Indicators

Each record displays a status icon:

| Icon | Status | Meaning |
|------|--------|---------|
| ✓ (green) | **Synced** | Record and all media are safely stored on the server. |
| ⟳ (yellow) | **Pending Sync** | Record is saved locally and waiting to be uploaded. |
| ↑ (blue) | **Syncing** | Record is currently being uploaded. |
| ✗ (red) | **Failed** | Sync failed after multiple attempts. Check your connection. |
| ⚠ (orange) | **Conflict** | Someone else edited this record. See [Handling Conflicts](#handling-conflicts). |

### Viewing Sync Progress

1. Tap the **sync status indicator** in the top bar to open the Sync Details panel.
2. You'll see:
   - Total pending items (records + media).
   - Items currently syncing.
   - Items successfully synced.
   - Any failed items with error details.
3. For media uploads, a progress bar shows the upload percentage.

### Sync Priority

The sync engine uploads data in this order:
1. **Completed reports** — highest priority.
2. **Report updates** — edits to existing reports.
3. **Notes** — text notes attached to reports.
4. **Photos** — smaller media files.
5. **Documents** — document attachments.
6. **Videos** — largest files, synced last.

This ensures that the most important data (report information) is synced first, even if large video files take a long time.

---

## Handling Conflicts

Conflicts are rare but can occur when two people edit the same record at the same time (or when you edit a record offline that someone else also edits).

### When Conflicts Occur

A conflict happens when:
- You edited a report offline.
- While you were offline, another user (or you from another device) also edited the same report on the server.
- When you sync, the server detects that both versions have changed.

### Reviewing Conflicts

1. Records with conflicts show an **orange ⚠ Conflict** badge.
2. Tap the record to open it.
3. A **Conflict Resolution** panel appears showing:
   - **Your Version** (left side) — What you entered on this device.
   - **Server Version** (right side) — What is currently on the server.
   - Changed fields are **highlighted** for easy comparison.

### Resolving Conflicts

You have three options:

1. **Keep My Version** — Your local changes overwrite the server version.
2. **Keep Server Version** — The server version overwrites your local changes.
3. **Merge Manually** — Edit the fields to combine both versions, then save.

After selecting an option, tap **Resolve**. The record syncs with your chosen resolution.

### When You Don't Need to Worry

Conflicts do **not** occur for:
- New reports (only you created it — no conflict possible).
- Adding photos, videos, or documents (additions don't conflict).
- Adding notes (each note is a separate item).

Conflicts only occur when **the same field** of **the same report** is edited by multiple people.

---

## Tips for Field Workers

### Before Leaving the Office

- ✅ **Open the app and sync** before heading out. This ensures you have the latest data.
- ✅ **Verify** your pending items count is zero (all synced).
- ✅ **Charge your device** — the app uses battery for camera and storage.

### During Facility Visits

- ✅ **Save frequently** — tap Save after completing each section of a report.
- ✅ **Take photos as you go** — don't wait until the end.
- ✅ **Keep videos short** — under 2 minutes each.
- ✅ **Add notes** for anything that needs explanation beyond the checklist.

### After Facility Visits

- ✅ **Find a Wi-Fi or cellular connection** as soon as practical.
- ✅ **Check sync status** — all items should show ✓ (green).
- ✅ **Review any failed items** — tap the red ✗ for details.
- ✅ **Don't clear browser data** until everything is synced.

### General Best Practices

- **Keep the app installed** — don't uninstall between visits. The cached assets make the app load faster.
- **If sync fails repeatedly**, check your internet connection. Try connecting to a different Wi-Fi network.
- **Large video files take longer to sync** — be patient, or sync on Wi-Fi rather than cellular.
- **Contact IT support** if you see repeated sync failures, persistent red ✗ icons, or error messages you don't understand.

### Troubleshooting Quick Fixes

| Problem | Quick Fix |
|---------|-----------|
| App won't load | Check internet connection; close and reopen the app |
| Camera doesn't open | Check browser permissions for camera access |
| "Storage Full" error | Clear old synced data (Settings → Clear Synced Data) or free up device storage |
| Sync stuck on one item | Tap the item to see the error; try **Sync Now** again |
| App seems outdated | Look for "Update Available" banner; refresh the app |

---

## Frequently Asked Questions

### General

**Q: Do I need an internet connection to use the app?**
A: No. The app is designed to work completely offline. You only need an internet connection to sync data to the server.

**Q: How much storage does the app use on my device?**
A: The app itself is small (under 20 MB). Storage usage depends on how many photos, videos, and documents you capture before syncing. A typical day of inspections with 20-30 photos might use 200-300 MB.

**Q: Can I use the app on multiple devices?**
A: Yes. Your data syncs to the server and is accessible from any device where you're logged in. However, avoid editing the same report on two devices simultaneously to prevent conflicts.

**Q: What browsers are supported?**
A: Google Chrome (recommended), Microsoft Edge, and Safari (iOS). Firefox has limited PWA support and is not recommended.

### Offline Usage

**Q: How long can I work offline?**
A: As long as your device has battery and storage space. There is no time limit. Some field workers have worked offline for entire multi-day trips.

**Q: What happens if my device runs out of battery while offline?**
A: Your data is saved to persistent storage (IndexedDB) immediately when you tap Save. When you recharge and reopen the app, all your data will still be there.

**Q: Can I see reports created by other team members while I'm offline?**
A: You can see reports that were synced to your device before you went offline. New reports created by others will appear after your next sync.

### Syncing

**Q: How long does syncing take?**
A: Text data (reports, notes, checklists) syncs in seconds. Photos typically take a few seconds each. Videos depend on size and connection speed — a 1-minute video (about 50 MB) might take 1-2 minutes on a good connection.

**Q: Can I continue working while the app is syncing?**
A: Yes! Syncing happens in the background. You can create new reports, add photos, and continue working normally while the sync runs.

**Q: What if I lose connection during a sync?**
A: The app handles this gracefully. Any items that didn't finish syncing are marked as "Pending" and will be retried automatically when connectivity is restored. No data is lost.

**Q: Why do some items show "Failed" sync status?**
A: This usually means the sync was attempted multiple times and failed each time. Common causes: very poor internet connection, server maintenance, or a temporary server error. Try again later with a better connection. If the problem persists, contact IT support.

### Media

**Q: Can I delete a photo or video after attaching it?**
A: Yes. Open the report, tap the media item, and select **Remove**. If the media was already synced, it is also removed from the server.

**Q: What if a photo is blurry? Can I retake it?**
A: Yes. Remove the blurry photo and take a new one. You can also view the photo before saving to check quality.

**Q: Why are my photos being compressed?**
A: The app compresses photos to reduce storage usage and speed up syncing. The quality is maintained at a level suitable for inspection documentation. Original resolution photos can be configured by your IT department if needed.

### Conflicts and Errors

**Q: I see a "Conflict" warning. What should I do?**
A: Open the affected report, review both versions (yours and the server's), and choose which version to keep. If unsure, contact your supervisor. See [Handling Conflicts](#handling-conflicts) for detailed steps.

**Q: The app says "Update Available." What do I do?**
A: Tap the **Update** button or banner. The app will reload with the latest version. This only takes a few seconds. Your data is preserved during updates.

**Q: I accidentally cleared my browser data. Is my data lost?**
A: If the data was synced (green ✓ status), it is safely stored on the server and will reappear after your next sync. If the data was pending sync (yellow ⟳), it may be lost. Contact IT support for assistance.

### Getting Help

**Q: Who do I contact for technical support?**
A: Contact your IT help desk. Provide:
- Your device type and browser version.
- A screenshot of any error message.
- The number of items showing "Failed" sync status.
- When the problem started.

**Q: Where can I request new features or report bugs?**
A: Contact your supervisor or IT department. They can submit feature requests or bug reports to the development team.

---

## Quick Reference Card

| Action | How |
|--------|-----|
| Create a report | Dashboard → **+ New Report** |
| Take a photo | Open report → **Add Photo** → **Take Photo** |
| Record a video | Open report → **Add Video** → **Record Video** |
| Attach a document | Open report → **Add Document** → Browse file |
| Add a note | Open report → Notes → **+ Add Note** |
| Check sync status | Look at the top bar indicator or tap it for details |
| Manual sync | Dashboard → **Sync Now** |
| Resolve a conflict | Tap the ⚠ record → Review versions → **Resolve** |
| Install the app | Browser address bar → **Install** icon |

---

*For technical documentation, see the [Architecture Guide](ARCHITECTURE.md), [Deployment Guide](DEPLOYMENT.md), or [Operations Guide](OPERATIONS.md).*
