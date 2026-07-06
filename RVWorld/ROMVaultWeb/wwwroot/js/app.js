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
