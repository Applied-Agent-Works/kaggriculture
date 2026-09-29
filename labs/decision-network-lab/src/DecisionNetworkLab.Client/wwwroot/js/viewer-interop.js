window.kaggricultureViewer = {
    openAndSend: function (viewerUrl, replayJson, metadata) {
        const viewer = window.open(viewerUrl, "kaggriculture-replay-viewer");
        if (!viewer) {
            return;
        }

        const message = {
            environment: JSON.parse(replayJson),
            agents: metadata.agents,
            playerLabels: metadata.playerLabels,
            playerMeta: metadata.playerMeta,
            opponentMeta: metadata.opponentMeta
        };

        let attempts = 0;
        const timer = window.setInterval(() => {
            if (viewer.closed || attempts++ >= 20) {
                window.clearInterval(timer);
                return;
            }

            viewer.postMessage(message, "*");
        }, 150);
    }
};
