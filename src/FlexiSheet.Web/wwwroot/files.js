(() => {
    "use strict";
    window.flexiSheetFiles = {
        download: (fileName, json) => {
            const url = URL.createObjectURL(new Blob([json], { type: "application/json;charset=utf-8" }));
            const link = document.createElement("a");
            try {
                link.href = url;
                link.download = fileName;
                document.body.appendChild(link);
                link.click();
            } finally {
                link.remove();
                setTimeout(() => URL.revokeObjectURL(url), 1000);
            }
        },
        email: mailto => {
            if (!mailto.startsWith("mailto:?")) throw new Error("Expected an email composition link.");
            window.location.assign(mailto);
        }
    };
})();
