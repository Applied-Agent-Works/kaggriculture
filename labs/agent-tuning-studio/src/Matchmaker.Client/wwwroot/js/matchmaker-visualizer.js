(() => {
    const windowName = "kaggriculture-full-match-visualizer";
    let reservedWindow = null;

    window.matchmakerVisualizer = {
        reserve() {
            reservedWindow = window.open("about:blank", windowName);
            if (!reservedWindow) {
                throw new Error("The browser blocked the visualizer window. Allow pop-ups for Matchmaker and try again.");
            }
        },

        closeReserved() {
            if (reservedWindow && !reservedWindow.closed) {
                try {
                    if (reservedWindow.location.href === "about:blank") reservedWindow.close();
                } catch { }
            }
            reservedWindow = null;
        },

        open(url, parentOrigin, matchId, replay) {
            const target = reservedWindow && !reservedWindow.closed
                ? reservedWindow
                : window.open("about:blank", windowName);
            reservedWindow = null;
            if (!target) {
                throw new Error("The browser blocked the visualizer window. Allow pop-ups for Matchmaker and try again.");
            }

            const visualizerUrl = new URL(url);
            visualizerUrl.searchParams.set("parentOrigin", parentOrigin);
            visualizerUrl.searchParams.set("matchId", matchId);
            visualizerUrl.searchParams.set("launch", Date.now().toString());

            return new Promise((resolve, reject) => {
                const timeout = window.setTimeout(() => {
                    window.removeEventListener("message", onMessage);
                    reject(new Error("The visualizer did not finish loading the match."));
                }, 30000);

                const onMessage = (event) => {
                    if (event.source !== target || event.origin !== visualizerUrl.origin) return;

                    if (event.data?.type === "matchmaker:ready" && event.data.matchId === matchId) {
                        target.postMessage({ type: "matchmaker:load-replay", matchId, replay }, visualizerUrl.origin);
                    } else if (event.data?.type === "matchmaker:replay-loaded" && event.data.matchId === matchId) {
                        window.clearTimeout(timeout);
                        window.removeEventListener("message", onMessage);
                        target.focus();
                        resolve();
                    }
                };

                window.addEventListener("message", onMessage);
                target.location.href = visualizerUrl.href;
            });
        }
    };
})();
