/**
 * IndexedDB Interop Module for BlazorWASM_PWA
 * Uses Dexie.js to manage offline data storage for case workers.
 * All functions are exposed under window.indexedDb for Blazor IJSRuntime calls.
 */

(function () {
    'use strict';

    let db = null;

    const DB_NAME = 'BlazorWASM_PWA_DB';
    const DB_VERSION = 1;

    function ensureDb() {
        if (!db) {
            throw new Error('Database not initialized. Call initDatabase() first.');
        }
    }

    // ── Database Initialization ──────────────────────────────────────────

    async function initDatabase() {
        try {
            if (db) {
                console.log('[IndexedDB] Database already initialized.');
                return true;
            }

            db = new Dexie(DB_NAME);

            db.version(DB_VERSION).stores({
                entities: 'id, entityType, isSynced, isDeleted, [entityType+isSynced], [entityType+isDeleted]',
                blobs: 'id, entityId, blobType, isSynced, [entityId+blobType], [entityId+isSynced]',
                operations: 'id, status, createdAt, [status+createdAt]'
            });

            await db.open();
            console.log('[IndexedDB] Database initialized successfully.');
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to initialize database:', error);
            throw error;
        }
    }

    // ── Entity Operations ────────────────────────────────────────────────

    async function addEntity(entity) {
        try {
            ensureDb();
            const now = new Date().toISOString();
            entity.createdAt = entity.createdAt || now;
            entity.updatedAt = entity.updatedAt || now;
            entity.isDeleted = entity.isDeleted ?? false;
            entity.isSynced = entity.isSynced ?? false;

            await db.entities.add(entity);
            console.log('[IndexedDB] Entity added:', entity.id);
            return entity.id;
        } catch (error) {
            console.error('[IndexedDB] Failed to add entity:', error);
            throw error;
        }
    }

    async function getEntity(id) {
        try {
            ensureDb();
            const entity = await db.entities.get(id);
            return entity || null;
        } catch (error) {
            console.error('[IndexedDB] Failed to get entity:', error);
            throw error;
        }
    }

    async function getAllEntities(entityType) {
        try {
            ensureDb();
            let collection;
            if (entityType) {
                collection = db.entities.where('entityType').equals(entityType);
            } else {
                collection = db.entities.toCollection();
            }
            const entities = await collection.filter(e => !e.isDeleted).toArray();
            console.log(`[IndexedDB] Retrieved ${entities.length} entities` +
                (entityType ? ` of type '${entityType}'` : '') + '.');
            return entities;
        } catch (error) {
            console.error('[IndexedDB] Failed to get entities:', error);
            throw error;
        }
    }

    async function updateEntity(entity) {
        try {
            ensureDb();
            entity.updatedAt = new Date().toISOString();
            await db.entities.put(entity);
            console.log('[IndexedDB] Entity updated:', entity.id);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to update entity:', error);
            throw error;
        }
    }

    async function deleteEntity(id) {
        try {
            ensureDb();
            const entity = await db.entities.get(id);
            if (!entity) {
                console.warn('[IndexedDB] Entity not found for soft delete:', id);
                return false;
            }
            entity.isDeleted = true;
            entity.isSynced = false;
            entity.updatedAt = new Date().toISOString();
            await db.entities.put(entity);
            console.log('[IndexedDB] Entity soft-deleted:', id);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to soft-delete entity:', error);
            throw error;
        }
    }

    async function getUnsyncedEntities() {
        try {
            ensureDb();
            const entities = await db.entities
                .where('isSynced').equals(0)
                .filter(e => !e.isDeleted)
                .toArray();
            console.log(`[IndexedDB] Found ${entities.length} unsynced entities.`);
            return entities;
        } catch (error) {
            console.error('[IndexedDB] Failed to get unsynced entities:', error);
            throw error;
        }
    }

    async function markEntitiesSynced(ids) {
        try {
            ensureDb();
            await db.transaction('rw', db.entities, async () => {
                for (const id of ids) {
                    await db.entities.update(id, {
                        isSynced: true,
                        updatedAt: new Date().toISOString()
                    });
                }
            });
            console.log(`[IndexedDB] Marked ${ids.length} entities as synced.`);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to mark entities as synced:', error);
            throw error;
        }
    }

    // ── Blob Operations ──────────────────────────────────────────────────

    async function addBlob(blob) {
        try {
            ensureDb();
            blob.createdAt = blob.createdAt || new Date().toISOString();
            blob.isSynced = blob.isSynced ?? false;

            // Convert base64 data from Blazor to Uint8Array if needed
            if (blob.data && typeof blob.data === 'string') {
                const binaryString = atob(blob.data);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                blob.data = bytes;
            }

            await db.blobs.add(blob);
            console.log('[IndexedDB] Blob added:', blob.id, `(${blob.fileName})`);
            return blob.id;
        } catch (error) {
            console.error('[IndexedDB] Failed to add blob:', error);
            throw error;
        }
    }

    async function getBlob(id) {
        try {
            ensureDb();
            const blob = await db.blobs.get(id);
            if (blob && blob.data instanceof Uint8Array) {
                // Convert Uint8Array to base64 for Blazor interop
                let binary = '';
                for (let i = 0; i < blob.data.length; i++) {
                    binary += String.fromCharCode(blob.data[i]);
                }
                blob.data = btoa(binary);
            }
            return blob || null;
        } catch (error) {
            console.error('[IndexedDB] Failed to get blob:', error);
            throw error;
        }
    }

    async function getBlobsByEntity(entityId) {
        try {
            ensureDb();
            const blobs = await db.blobs.where('entityId').equals(entityId).toArray();
            // Convert binary data to base64 for Blazor interop
            for (const blob of blobs) {
                if (blob.data instanceof Uint8Array) {
                    let binary = '';
                    for (let i = 0; i < blob.data.length; i++) {
                        binary += String.fromCharCode(blob.data[i]);
                    }
                    blob.data = btoa(binary);
                }
            }
            console.log(`[IndexedDB] Retrieved ${blobs.length} blobs for entity '${entityId}'.`);
            return blobs;
        } catch (error) {
            console.error('[IndexedDB] Failed to get blobs by entity:', error);
            throw error;
        }
    }

    async function deleteBlob(id) {
        try {
            ensureDb();
            await db.blobs.delete(id);
            console.log('[IndexedDB] Blob deleted:', id);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to delete blob:', error);
            throw error;
        }
    }

    async function markBlobsSynced(ids) {
        try {
            ensureDb();
            await db.transaction('rw', db.blobs, async () => {
                for (const id of ids) {
                    await db.blobs.update(id, {
                        isSynced: true
                    });
                }
            });
            console.log(`[IndexedDB] Marked ${ids.length} blobs as synced.`);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to mark blobs as synced:', error);
            throw error;
        }
    }

    // ── Sync Operation Queue ─────────────────────────────────────────────

    async function addOperation(operation) {
        try {
            ensureDb();
            operation.status = operation.status || 'Pending';
            operation.createdAt = operation.createdAt || new Date().toISOString();
            operation.retryCount = operation.retryCount ?? 0;
            operation.lastError = operation.lastError || null;

            await db.operations.add(operation);
            console.log('[IndexedDB] Operation added:', operation.id, `(${operation.operationType})`);
            return operation.id;
        } catch (error) {
            console.error('[IndexedDB] Failed to add operation:', error);
            throw error;
        }
    }

    async function getPendingOperations() {
        try {
            ensureDb();
            const operations = await db.operations
                .where('[status+createdAt]')
                .between(['Pending', Dexie.minKey], ['Pending', Dexie.maxKey])
                .toArray();
            console.log(`[IndexedDB] Found ${operations.length} pending operations.`);
            return operations;
        } catch (error) {
            console.error('[IndexedDB] Failed to get pending operations:', error);
            throw error;
        }
    }

    async function updateOperationStatus(id, status, errorMessage) {
        try {
            ensureDb();
            const updates = { status: status };
            if (errorMessage !== undefined && errorMessage !== null) {
                updates.lastError = errorMessage;
            }
            if (status === 'Failed') {
                const op = await db.operations.get(id);
                if (op) {
                    updates.retryCount = (op.retryCount || 0) + 1;
                }
            }
            await db.operations.update(id, updates);
            console.log(`[IndexedDB] Operation ${id} status updated to '${status}'.`);
            return true;
        } catch (error) {
            console.error('[IndexedDB] Failed to update operation status:', error);
            throw error;
        }
    }

    async function getOperationCount(status) {
        try {
            ensureDb();
            let count;
            if (status) {
                count = await db.operations.where('status').equals(status).count();
            } else {
                count = await db.operations.count();
            }
            return count;
        } catch (error) {
            console.error('[IndexedDB] Failed to get operation count:', error);
            throw error;
        }
    }

    async function clearCompletedOperations() {
        try {
            ensureDb();
            const count = await db.operations.where('status').equals('Completed').delete();
            console.log(`[IndexedDB] Cleared ${count} completed operations.`);
            return count;
        } catch (error) {
            console.error('[IndexedDB] Failed to clear completed operations:', error);
            throw error;
        }
    }

    // ── Expose API on window ─────────────────────────────────────────────

    window.indexedDb = {
        initDatabase,
        addEntity,
        getEntity,
        getAllEntities,
        updateEntity,
        deleteEntity,
        addBlob,
        getBlob,
        getBlobsByEntity,
        deleteBlob,
        addOperation,
        getPendingOperations,
        updateOperationStatus,
        getOperationCount,
        clearCompletedOperations,
        getUnsyncedEntities,
        markEntitiesSynced,
        markBlobsSynced
    };

    console.log('[IndexedDB] Interop module loaded.');
})();
