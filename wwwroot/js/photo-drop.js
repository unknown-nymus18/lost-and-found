window.photoDrop = {
    attach: (zoneId, inputId) => {
        const zone = document.getElementById(zoneId);
        const input = document.getElementById(inputId);
        if (!zone || !input || zone.dataset.photoDropAttached === "true") return;

        zone.dataset.photoDropAttached = "true";

        zone.addEventListener("dragover", event => {
            event.preventDefault();
            event.dataTransfer.dropEffect = "copy";
            zone.classList.add("is-dragging");
        });

        zone.addEventListener("dragleave", event => {
            if (!zone.contains(event.relatedTarget))
                zone.classList.remove("is-dragging");
        });

        zone.addEventListener("drop", event => {
            event.preventDefault();
            zone.classList.remove("is-dragging");

            const files = Array.from(event.dataTransfer.files).filter(file => file.type.startsWith("image/"));
            if (files.length === 0) return;

            const transfer = new DataTransfer();
            transfer.items.add(files[0]);
            input.files = transfer.files;
            input.dispatchEvent(new Event("change", { bubbles: true }));
        });
    }
};