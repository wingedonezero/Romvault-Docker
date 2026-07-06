// F-key shortcuts routed to Blazor (same set as the desktop menu strip)
window.rvHotkeys = (dotnet) => {
    window.addEventListener('keydown', (e) => {
        const keys = ['F1', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12'];
        if (!keys.includes(e.key)) return;
        e.preventDefault();
        dotnet.invokeMethodAsync('OnHotKey', e.key, e.shiftKey, e.ctrlKey);
    });
};

// Clipboard helper (works because the page is a normal web page)
window.rvCopyText = (text) => {
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text);
        return;
    }
    // http fallback
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    document.execCommand('copy');
    document.body.removeChild(ta);
};

// open a URL in a new tab (game web-page links)
window.rvOpenUrl = (url) => { window.open(url, '_blank', 'noopener'); };

// Draggable pane splitters. Sizes stored as CSS vars + localStorage so the
// layout survives reloads (the web equivalent of screenpos.xml).
(() => {
    for (let i = 0; i < localStorage.length; i++) {
        const k = localStorage.key(i);
        if (k && k.startsWith('rv--'))
            document.documentElement.style.setProperty(k.substring(2), localStorage.getItem(k));
    }
    let drag = null;
    document.addEventListener('pointerdown', (e) => {
        const s = e.target.closest('.splitter, .col-resize');
        if (!s) return;
        const target = s.classList.contains('col-resize') ? s.parentElement : document.querySelector(s.dataset.target);
        if (!target) return;
        const axis = s.dataset.axis;
        const rect = target.getBoundingClientRect();
        drag = {
            el: s,
            varName: s.dataset.var,
            axis,
            invert: s.dataset.invert === '1',
            start: axis === 'x' ? e.clientX : e.clientY,
            startVal: axis === 'x' ? rect.width : rect.height,
        };
        s.classList.add('dragging');
        document.body.style.userSelect = 'none';
        e.preventDefault();
    });
    document.addEventListener('pointermove', (e) => {
        if (!drag) return;
        let delta = (drag.axis === 'x' ? e.clientX : e.clientY) - drag.start;
        if (drag.invert) delta = -delta;
        const val = Math.max(drag.el.classList.contains('col-resize') ? 30 : 80, drag.startVal + delta) + 'px';
        document.documentElement.style.setProperty(drag.varName, val);
        localStorage.setItem('rv' + drag.varName, val);
    });
    document.addEventListener('pointerup', () => {
        if (drag) drag.el.classList.remove('dragging');
        document.body.style.userSelect = '';
        drag = null;
    });
})();
