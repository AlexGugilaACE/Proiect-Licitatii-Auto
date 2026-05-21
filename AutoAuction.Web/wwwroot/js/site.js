// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("click", (event) => {
    const card = event.target.closest("[data-card-url]");

    if (!card) {
        return;
    }

    const interactiveElement = event.target.closest("a, button, input, select, textarea, label, form");

    if (interactiveElement) {
        return;
    }

    window.location.href = card.dataset.cardUrl;
});

document.querySelectorAll("[data-image-preview-input]").forEach((input) => {
    const preview = input.closest(".auction-form-field")?.querySelector("[data-image-preview]");
    let selectedFiles = [];

    if (!preview) {
        return;
    }

    function syncInputFiles() {
        const dataTransfer = new DataTransfer();
        selectedFiles.forEach((file) => dataTransfer.items.add(file));
        input.files = dataTransfer.files;
    }

    function renderPreview() {
        preview.innerHTML = "";

        if (selectedFiles.length === 0) {
            return;
        }

        const toolbar = document.createElement("div");
        toolbar.className = "image-upload-preview__toolbar";

        const count = document.createElement("span");
        count.textContent = `${selectedFiles.length} poze selectate`;

        const clearButton = document.createElement("button");
        clearButton.type = "button";
        clearButton.textContent = "Șterge toate";
        clearButton.addEventListener("click", () => {
            selectedFiles = [];
            syncInputFiles();
            renderPreview();
        });

        toolbar.append(count, clearButton);
        preview.appendChild(toolbar);

        const grid = document.createElement("div");
        grid.className = "image-upload-preview__grid";
        preview.appendChild(grid);

        selectedFiles.forEach((file, index) => {
            if (!file.type.startsWith("image/")) {
                return;
            }

            const item = document.createElement("div");
            item.className = "image-upload-preview__item";

            const removeButton = document.createElement("button");
            removeButton.type = "button";
            removeButton.className = "image-upload-preview__remove";
            removeButton.setAttribute("aria-label", `Șterge ${file.name}`);
            removeButton.textContent = "×";
            removeButton.addEventListener("click", () => {
                selectedFiles.splice(index, 1);
                syncInputFiles();
                renderPreview();
            });

            const image = document.createElement("img");
            image.src = URL.createObjectURL(file);
            image.alt = file.name;
            image.addEventListener("load", () => URL.revokeObjectURL(image.src), { once: true });

            const name = document.createElement("span");
            name.textContent = file.name;

            item.append(removeButton, image, name);
            grid.appendChild(item);
        });
    }

    input.addEventListener("change", () => {
        selectedFiles = Array.from(input.files || []);
        renderPreview();
    });
});
