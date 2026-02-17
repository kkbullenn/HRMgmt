// Shared shift color helpers for MVP pages.
(function () {
    const defaultPalette = [
        "#FFE066", "#B2F2BB", "#A5D8FF", "#FFD8A8", "#C5F6FA",
        "#EEBEFA", "#D3F9D8", "#FFC9C9", "#FFD6A5", "#FDFFB6",
        "#CAFFBF", "#9BF6FF", "#A0C4FF", "#BDB2FF", "#FFC6FF",
        "#F8B4D9", "#B8E1FF", "#CDEAC0", "#E2C2FF", "#FFD4B8"
    ];

    function buildMap(shiftIds, palette) {
        const colors = palette && palette.length ? palette : defaultPalette;
        const map = {};
        (shiftIds || []).forEach((id, i) => {
            map[id] = colors[i % colors.length];
        });
        return map;
    }

    function getColor(shiftId, colorMap, fallback) {
        const fallbackColor = fallback || "#dee2e6";
        if (!shiftId) return fallbackColor;
        return (colorMap && colorMap[shiftId]) || fallbackColor;
    }

    window.ShiftColorUtils = {
        buildMap,
        getColor,
        defaultPalette
    };
})();
