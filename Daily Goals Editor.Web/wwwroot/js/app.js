// Browser helpers called from Blazor through IJSRuntime as "dailyGoals.<name>".
window.dailyGoals = {
    getItem: key => localStorage.getItem(key),
    setItem: (key, value) => localStorage.setItem(key, value),

    copyText: text => navigator.clipboard.writeText(text),

    /** Focuses the first element matching the selector; selectAll also selects its text, as for a placeholder title. */
    focus: (selector, selectAll) => {
        const element = document.querySelector(selector);
        if (!element) {
            return;
        }
        element.focus();
        if (selectAll && typeof element.select === 'function') {
            element.select();
        }
    },

    setTextScale: scale => document.documentElement.style.setProperty('--text-scale', scale)
};
