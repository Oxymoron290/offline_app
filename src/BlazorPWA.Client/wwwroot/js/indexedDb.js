import Dexie from 'https://cdn.jsdelivr.net/npm/dexie@3.2.7/dist/dexie.mjs';

const db = new Dexie('BlazorPWA');

db.version(1).stores({
    entities: 'id, type, syncStatus, updatedAt',
    syncQueue: 'id, operationType, entityId, status, createdAt',
    mediaBlobs: 'id, reportId, syncStatus',
    config: 'key'
});

window.indexedDb = {
    // Entity operations
    putEntity: async (entity) => {
        await db.entities.put(entity);
    },
    getEntity: async (id) => {
        return await db.entities.get(id);
    },
    getAllEntities: async (type) => {
        if (type) {
            return await db.entities.where('type').equals(type).toArray();
        }
        return await db.entities.toArray();
    },
    deleteEntity: async (id) => {
        await db.entities.delete(id);
    },

    // Sync queue operations
    addToSyncQueue: async (operation) => {
        await db.syncQueue.put(operation);
    },
    getPendingSyncOperations: async () => {
        return await db.syncQueue.where('status').equals('Pending').sortBy('createdAt');
    },
    updateSyncOperationStatus: async (id, status, errorMessage) => {
        await db.syncQueue.update(id, { status, errorMessage, updatedAt: new Date().toISOString() });
    },
    removeSyncOperation: async (id) => {
        await db.syncQueue.delete(id);
    },
    getSyncQueueCount: async () => {
        return await db.syncQueue.where('status').equals('Pending').count();
    },

    // Media blob operations
    putMediaBlob: async (media) => {
        await db.mediaBlobs.put(media);
    },
    getMediaBlob: async (id) => {
        return await db.mediaBlobs.get(id);
    },
    getMediaBlobsByReport: async (reportId) => {
        return await db.mediaBlobs.where('reportId').equals(reportId).toArray();
    },
    deleteMediaBlob: async (id) => {
        await db.mediaBlobs.delete(id);
    },
    getPendingMediaBlobs: async () => {
        return await db.mediaBlobs.where('syncStatus').equals('Pending').toArray();
    },

    // Config operations
    getConfig: async (key) => {
        const result = await db.config.get(key);
        return result ? result.value : null;
    },
    setConfig: async (key, value) => {
        await db.config.put({ key, value });
    },

    // Utility
    clearAll: async () => {
        await db.entities.clear();
        await db.syncQueue.clear();
        await db.mediaBlobs.clear();
        await db.config.clear();
    }
};
