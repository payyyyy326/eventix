/**
 * event-seat-map.js
 *
 * Seat map editor for the EventWizard (Step 5).
 * Blocks are ALWAYS generated from the current session ticket types.
 * Saved venue layouts are used only to restore previous positions/sizes
 * of matching sections — they never add or remove blocks.
 *
 * Canvas: 900 × 620 px (matches .seat-map-canvas CSS)
 */

'use strict';

/* ── State ───────────────────────────────────────────────────────────────── */
let zones = [];           // { section, seatCount, isSeatRequired, x, y, width, height, color }
let isLayoutSaved = false;
let selectedSection = null; // section name of currently selected zone

/* ── Bootstrap ───────────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', function () {
    initSeatMap();

    document.getElementById('saveSeatMapBtn')
        ?.addEventListener('click', saveSeatMap);

    document.getElementById('resetLayoutBtn')
        ?.addEventListener('click', resetLayout);

    document.getElementById('seatMapContinueForm')
        ?.addEventListener('submit', function (e) {
            if (!isLayoutSaved) {
                e.preventDefault();
                showStatus('Vui lòng lưu sơ đồ trước khi tiếp tục.', 'error');
            }
        });

    // Deselect on canvas background click
    document.getElementById('seatMapCanvas')
        ?.addEventListener('mousedown', function (e) {
            if (e.target === this) deselectAll();
        });
});

/* ── Init ────────────────────────────────────────────────────────────────── */
function initSeatMap() {
    const { sections = [], layouts = [], isSaved = false } = window.eventixSeatMap ?? {};

    isLayoutSaved = isSaved === true;

    const CANVAS_W = document.getElementById('seatMapCanvas')?.offsetWidth  || 900;
    const CANVAS_H = document.getElementById('seatMapCanvas')?.offsetHeight || 620;

    // Build one zone per ticket type from session
    zones = sections.map((s, i) => {
        const name   = s.Name    ?? s.name    ?? s.Section ?? s.section ?? `Khu ${i + 1}`;
        const count  = s.SeatCount ?? s.seatCount ?? 0;
        const seated = s.IsSeatRequired ?? s.isSeatRequired ?? false;
        const color  = s.Color ?? s.color ?? getDefaultColor(i);

        // Check if this section has a saved layout position
        const saved = layouts.find(l =>
            (l.Section ?? l.section ?? '').toLowerCase() === name.toLowerCase()
        );

        if (saved) {
            return {
                section: name,
                seatCount: count,
                isSeatRequired: seated,
                x: saved.X ?? saved.x ?? 40,
                y: saved.Y ?? saved.y ?? 40,
                width:  saved.Width  ?? saved.width  ?? defaultWidth(count),
                height: saved.Height ?? saved.height ?? 120,
                color: saved.Color ?? saved.color ?? color
            };
        }

        // Auto-layout: arrange in rows with padding
        const { x, y, w, h } = autoPosition(i, sections.length, CANVAS_W, CANVAS_H, count);
        return {
            section: name,
            seatCount: count,
            isSeatRequired: seated,
            x, y,
            width: w,
            height: h,
            color
        };
    });

    renderZones();
    updateStats();

    if (isLayoutSaved) {
        showStatus('Sơ đồ đã được lưu.', 'success');
    }
}

/* ── Auto-position algorithm ─────────────────────────────────────────────── */
/**
 * Arrange blocks in a grid from bottom → top (stage is at top).
 * Blocks for larger ticket types get more width proportionally.
 */
function autoPosition(index, total, canvasW, canvasH, seatCount) {
    const COLS      = Math.min(total, 3);
    const ROWS      = Math.ceil(total / COLS);
    const PAD       = 20;          // padding from edge
    const STAGE_H   = 60;          // reserved space at top for stage label area
    const GAP       = 16;

    const col = index % COLS;
    const row = Math.floor(index / COLS);

    const cellW = (canvasW - PAD * 2 - GAP * (COLS - 1)) / COLS;
    const cellH = (canvasH - STAGE_H - PAD - GAP * (ROWS - 1)) / ROWS;

    const w = Math.max(Math.floor(cellW), 120);
    const h = Math.max(Math.floor(cellH * 0.85), 100);

    const x = PAD + col * (cellW + GAP);
    // Place rows from bottom upward so stage area is clear
    const y = STAGE_H + (ROWS - 1 - row) * (cellH + GAP) + (cellH - h) / 2;

    return { x: Math.round(x), y: Math.round(y), w, h };
}

/* ── Default dimensions based on seat count ─────────────────────────────── */
function defaultWidth(seatCount) {
    if (seatCount > 1000) return 280;
    if (seatCount > 500)  return 240;
    if (seatCount > 100)  return 200;
    return 170;
}

/* ── Colors ──────────────────────────────────────────────────────────────── */
function getDefaultColor(index) {
    const colors = [
        '#7c3aed', '#2563eb', '#16a34a', '#ea580c',
        '#db2777', '#0891b2', '#d97706', '#be123c',
        '#0e7490', '#4d7c0f', '#7e22ce', '#1d4ed8'
    ];
    return colors[index % colors.length];
}

/* ── Render ──────────────────────────────────────────────────────────────── */
function renderZones() {
    const canvas = document.getElementById('seatMapCanvas');
    if (!canvas) return;

    canvas.innerHTML = '';

    zones.forEach(zone => {
        const div = document.createElement('div');
        div.className = 'seat-map-zone';
        div.dataset.section = zone.section;

        div.style.cssText = `
            left:   ${zone.x}px;
            top:    ${zone.y}px;
            width:  ${zone.width}px;
            height: ${zone.height}px;
            background: ${zone.color};
        `;

        const typeLabel = zone.isSeatRequired
            ? `<span class="zone-badge seated">Có ghế</span>`
            : `<span class="zone-badge standing">Tự do</span>`;

        const countLabel = zone.seatCount > 0
            ? `<span class="zone-count">${zone.seatCount.toLocaleString()} ${zone.isSeatRequired ? 'ghế' : 'vé'}</span>`
            : '';

        div.innerHTML = `
            <div class="seat-map-zone-content">
                <strong>${zone.section}</strong>
                ${countLabel}
                ${typeLabel}
            </div>
            <div class="zone-resize-handle"></div>
        `;

        div.addEventListener('mousedown', () => selectZone(zone.section));

        canvas.appendChild(div);
    });

    enableDragResize();
}

/* ── Selection ───────────────────────────────────────────────────────────── */
function selectZone(sectionName) {
    selectedSection = sectionName;
    document.querySelectorAll('.seat-map-zone').forEach(el => {
        el.classList.toggle('selected', el.dataset.section === sectionName);
    });
}

function deselectAll() {
    selectedSection = null;
    document.querySelectorAll('.seat-map-zone').forEach(el => el.classList.remove('selected'));
}

/* ── Drag & Resize (interact.js) ─────────────────────────────────────────── */
function enableDragResize() {
    interact('.seat-map-zone')
        .draggable({
            modifiers: [
                interact.modifiers.restrictRect({
                    restriction: 'parent',
                    endOnly: false
                })
            ],
            listeners: {
                start(e) { selectZone(e.target.dataset.section); },
                move(e) {
                    const zone = zones.find(z => z.section === e.target.dataset.section);
                    if (!zone) return;
                    zone.x += e.dx;
                    zone.y += e.dy;
                    e.target.style.left = `${zone.x}px`;
                    e.target.style.top  = `${zone.y}px`;
                    markLayoutDirty();
                }
            }
        })
        .resizable({
            edges: { left: true, right: true, bottom: true, top: true },
            modifiers: [
                interact.modifiers.restrictEdges({ outer: 'parent' }),
                interact.modifiers.restrictSize({ min: { width: 120, height: 80 } })
            ],
            listeners: {
                start(e) { selectZone(e.target.dataset.section); },
                move(e) {
                    const zone = zones.find(z => z.section === e.target.dataset.section);
                    if (!zone) return;
                    zone.width  = e.rect.width;
                    zone.height = e.rect.height;
                    zone.x += e.deltaRect.left;
                    zone.y += e.deltaRect.top;
                    e.target.style.width  = `${zone.width}px`;
                    e.target.style.height = `${zone.height}px`;
                    e.target.style.left   = `${zone.x}px`;
                    e.target.style.top    = `${zone.y}px`;
                    markLayoutDirty();
                }
            }
        });
}

/* ── Save ────────────────────────────────────────────────────────────────── */
async function saveSeatMap() {
    showStatus('Đang lưu…', 'loading');

    const payload = zones.map(zone => ({
        section: zone.section,
        x:       Math.round(zone.x),
        y:       Math.round(zone.y),
        width:   Math.round(zone.width),
        height:  Math.round(zone.height),
        color:   zone.color
    }));

    try {
        const res = await fetch('/EventWizard/SaveSeatMap', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify(payload)
        });

        if (!res.ok) {
            const msg = await res.text();
            showStatus('Lưu thất bại: ' + (msg || res.status), 'error');
            return;
        }

        isLayoutSaved = true;
        showStatus('Đã lưu sơ đồ thành công ✓', 'success');
    } catch (err) {
        showStatus('Lỗi kết nối: ' + err.message, 'error');
    }
}

/* ── Reset layout ────────────────────────────────────────────────────────── */
function resetLayout() {
    const CANVAS_W = document.getElementById('seatMapCanvas')?.offsetWidth  || 900;
    const CANVAS_H = document.getElementById('seatMapCanvas')?.offsetHeight || 620;

    zones.forEach((zone, i) => {
        const { x, y, w, h } = autoPosition(i, zones.length, CANVAS_W, CANVAS_H, zone.seatCount);
        zone.x = x; zone.y = y; zone.width = w; zone.height = h;
    });

    renderZones();
    markLayoutDirty();
    showStatus('Đã đặt lại bố cục — nhớ lưu lại.', 'warning');
}

/* ── Stats panel ─────────────────────────────────────────────────────────── */
function updateStats() {
    const totalEl   = document.getElementById('mapStatTotal');
    const seatedEl  = document.getElementById('mapStatSeated');
    const standingEl = document.getElementById('mapStatStanding');

    if (totalEl)    totalEl.textContent    = zones.reduce((s, z) => s + z.seatCount, 0).toLocaleString();
    if (seatedEl)   seatedEl.textContent   = zones.filter(z => z.isSeatRequired).length;
    if (standingEl) standingEl.textContent = zones.filter(z => !z.isSeatRequired).length;
}

/* ── Status message ──────────────────────────────────────────────────────── */
function showStatus(msg, type) {
    const el = document.getElementById('saveMapStatus');
    if (!el) return;
    el.textContent = msg;
    el.className = 'map-status map-status--' + type;
}

/* ── Dirty flag ──────────────────────────────────────────────────────────── */
function markLayoutDirty() {
    if (!isLayoutSaved) return;
    isLayoutSaved = false;
    showStatus('Bố cục đã thay đổi — nhớ lưu lại trước khi tiếp tục.', 'warning');
}
