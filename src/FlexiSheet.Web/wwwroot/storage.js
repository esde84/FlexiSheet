(() => {
    "use strict";
    const key = "flexisheet.workspace.v1";
    window.flexiSheetStorage = {
        read: () => localStorage.getItem(key),
        write: (expected, value) => {
            // Detect changes made since the last read before replacing the document.
            if (localStorage.getItem(key) !== expected) return false;
            localStorage.setItem(key, value);
            return true;
        },
        requestPersistence: async () => {
            if (!navigator.storage?.persist) return false;
            return await navigator.storage.persisted() || await navigator.storage.persist();
        }
    };
})();
