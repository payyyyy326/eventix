let zones = [];

let isLayoutSaved = false;

document.addEventListener("DOMContentLoaded", function () {
    initSeatMap();

    const saveButton = document.getElementById("saveSeatMapBtn");

    if (saveButton) {
        saveButton.addEventListener("click", saveSeatMap);
    }

    document.getElementById("seatMapContinueForm")?.addEventListener("submit", function (event) {
        if (isLayoutSaved) return;

        event.preventDefault();
        const status = document.getElementById("saveMapStatus");
        if (status) {
            status.textContent = "Please save the layout before continuing.";
            status.style.color = "#ef4444";
        }
    });
});

function initSeatMap() {
    const sections = window.eventixSeatMap.sections || [];
    const layouts = window.eventixSeatMap.layouts || [];
    isLayoutSaved = window.eventixSeatMap.isSaved === true;

    console.log("Seat map sections:", sections);
    console.log("Seat map layouts:", layouts);

    zones = sections.map((section, index) => {
        const sectionName =
            section.Name ||
            section.name ||
            section.Section ||
            section.section;

        const seatCount =
            section.SeatCount ||
            section.seatCount ||
            section.Capacity ||
            section.capacity ||
            0;

        const saved = layouts.find(x =>
            (x.Section || x.section) === sectionName
        );

        if (saved) {
            return {
                section: sectionName,
                seatCount: seatCount,
                x: saved.X ?? saved.x,
                y: saved.Y ?? saved.y,
                width: saved.Width ?? saved.width,
                height: saved.Height ?? saved.height,
                color: saved.Color ?? saved.color
            };
        }

        return {
            section: sectionName,
            seatCount: seatCount,
            x: 40 + index * 35,
            y: 40 + index * 35,
            width: 210,
            height: 120,
            color: section.Color || section.color || getDefaultColor(index)
        };
    });

    console.log("Seat map zones payload:", zones);

    renderZones();
}

function getDefaultColor(index) {
    const colors = [
        "#7c3aed",
        "#2563eb",
        "#16a34a",
        "#ea580c",
        "#db2777",
        "#0891b2"
    ];

    return colors[index % colors.length];
}

function renderZones() {
    const canvas = document.getElementById("seatMapCanvas");

    if (!canvas) return;

    canvas.innerHTML = "";

    zones.forEach(zone => {
        const div = document.createElement("div");

        div.className = "seat-map-zone";
        div.dataset.section = zone.section;

        div.style.left = `${zone.x}px`;
        div.style.top = `${zone.y}px`;
        div.style.width = `${zone.width}px`;
        div.style.height = `${zone.height}px`;
        div.style.background = zone.color;

        div.innerHTML = `
            <div class="seat-map-zone-content">
                <strong>${zone.section}</strong>
                <span>${zone.seatCount} seats</span>
            </div>
        `;

        canvas.appendChild(div);
    });

    enableDragResize();
}

function enableDragResize() {
    interact(".seat-map-zone")
        .draggable({
            modifiers: [
                interact.modifiers.restrictRect({
                    restriction: "parent",
                    endOnly: true
                })
            ],
            listeners: {
                move(event) {
                    const target = event.target;
                    const section = target.dataset.section;
                    const zone = zones.find(x => x.section === section);

                    if (!zone) return;

                    zone.x += event.dx;
                    zone.y += event.dy;
                    markLayoutDirty();

                    target.style.left = `${zone.x}px`;
                    target.style.top = `${zone.y}px`;
                }
            }
        })
        .resizable({
            edges: {
                left: true,
                right: true,
                bottom: true,
                top: true
            },
            modifiers: [
                interact.modifiers.restrictEdges({
                    outer: "parent"
                }),
                interact.modifiers.restrictSize({
                    min: {
                        width: 120,
                        height: 70
                    }
                })
            ],
            listeners: {
                move(event) {
                    const target = event.target;
                    const section = target.dataset.section;
                    const zone = zones.find(x => x.section === section);

                    if (!zone) return;

                    zone.width = event.rect.width;
                    zone.height = event.rect.height;

                    zone.x += event.deltaRect.left;
                    zone.y += event.deltaRect.top;
                    markLayoutDirty();

                    target.style.width = `${zone.width}px`;
                    target.style.height = `${zone.height}px`;
                    target.style.left = `${zone.x}px`;
                    target.style.top = `${zone.y}px`;
                }
            }
        });
}

async function saveSeatMap() {
    const status = document.getElementById("saveMapStatus");

    const payload = zones.map(zone => ({
        section: zone.section,
        x: Math.round(zone.x),
        y: Math.round(zone.y),
        width: Math.round(zone.width),
        height: Math.round(zone.height),
        color: zone.color
    }));

    if (status) {
        status.textContent = "Saving...";
        status.style.color = "#facc15";
    }

    const response = await fetch("/EventWizard/SaveSeatMap", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        const message = await response.text();

        if (status) {
            status.textContent = "Save failed.";
            status.style.color = "#ef4444";
        }

        alert(message || "Cannot save seat map.");
        return;
    }

    if (status) {
        status.textContent = "Layout saved successfully.";
        status.style.color = "#22c55e";
    }

    isLayoutSaved = true;
}

function markLayoutDirty() {
    if (!isLayoutSaved) return;

    isLayoutSaved = false;
    const status = document.getElementById("saveMapStatus");
    if (status) {
        status.textContent = "Layout has changed. Save it before continuing.";
        status.style.color = "#facc15";
    }
}
