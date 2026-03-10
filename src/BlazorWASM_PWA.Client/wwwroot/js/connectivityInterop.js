/**
 * Connectivity Interop Module for BlazorWASM_PWA
 * Detects online/offline status and notifies Blazor via DotNetObjectReference.
 * Exposed under window.connectivity for Blazor IJSRuntime calls.
 */

(function () {
    'use strict';

    let dotNetHelper = null;
    let onlineHandler = null;
    let offlineHandler = null;

    function initialize(helper) {
        try {
            if (dotNetHelper) {
                dispose();
            }

            dotNetHelper = helper;

            onlineHandler = function () {
                console.log('[Connectivity] Browser went online.');
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnConnectivityChanged', true)
                        .catch(err => console.error('[Connectivity] Failed to notify .NET of online status:', err));
                }
            };

            offlineHandler = function () {
                console.log('[Connectivity] Browser went offline.');
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnConnectivityChanged', false)
                        .catch(err => console.error('[Connectivity] Failed to notify .NET of offline status:', err));
                }
            };

            window.addEventListener('online', onlineHandler);
            window.addEventListener('offline', offlineHandler);

            console.log('[Connectivity] Initialized. Current status:', navigator.onLine ? 'online' : 'offline');
            return navigator.onLine;
        } catch (error) {
            console.error('[Connectivity] Failed to initialize:', error);
            throw error;
        }
    }

    function isOnline() {
        return navigator.onLine;
    }

    function dispose() {
        try {
            if (onlineHandler) {
                window.removeEventListener('online', onlineHandler);
                onlineHandler = null;
            }
            if (offlineHandler) {
                window.removeEventListener('offline', offlineHandler);
                offlineHandler = null;
            }
            dotNetHelper = null;
            console.log('[Connectivity] Disposed.');
        } catch (error) {
            console.error('[Connectivity] Failed to dispose:', error);
        }
    }

    window.connectivity = {
        initialize,
        isOnline,
        dispose
    };

    console.log('[Connectivity] Interop module loaded.');
})();
