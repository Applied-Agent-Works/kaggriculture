import { createReplayVisualizer, ReplayAdapter } from '@kaggle-environments/core';
import { renderer } from './renderer';
import { getKaggricultureStepRenderTime } from './timing';
import './style.css';

const app = document.getElementById('app');
if (!app) {
  throw new Error('Could not find app element');
}

if (import.meta.env?.DEV && import.meta.hot) {
  import.meta.hot.accept();
}

createReplayVisualizer(
  app,
  new ReplayAdapter({
    gameName: 'kaggriculture',
    renderer: renderer as any,
    ui: 'inline',
    getStepRenderTime: (step, replayMode, speedModifier) =>
      getKaggricultureStepRenderTime(step, replayMode, speedModifier),
  })
);

const matchmakerQuery = new URLSearchParams(window.location.search);
const matchmakerOrigin = matchmakerQuery.get('parentOrigin');
const matchmakerMatchId = matchmakerQuery.get('matchId');
const matchmakerOpener = window.opener;

if (matchmakerOpener && matchmakerOrigin && matchmakerMatchId) {
  window.addEventListener('message', (event) => {
    if (
      event.source !== matchmakerOpener ||
      event.origin !== matchmakerOrigin ||
      event.data?.type !== 'matchmaker:load-replay' ||
      event.data?.matchId !== matchmakerMatchId ||
      !event.data?.replay
    ) {
      return;
    }

    // ReplayVisualizer listens for the same message shape from host pages.
    // Re-dispatch locally so it receives the payload through its public handoff.
    window.postMessage({ replay: event.data.replay }, window.location.origin);
    matchmakerOpener.postMessage(
      { type: 'matchmaker:replay-loaded', matchId: matchmakerMatchId },
      matchmakerOrigin
    );
  });

  matchmakerOpener.postMessage(
    { type: 'matchmaker:ready', matchId: matchmakerMatchId },
    matchmakerOrigin
  );
} else if (matchmakerMatchId) {
  // Direct links (including a tab opened by an IDE/browser integration) do
  // not have an opener to send the replay. The Matchmaker Vite host proxies
  // this same-origin request to the local catalog API.
  void fetch(`/api/matchmaker/matches/${encodeURIComponent(matchmakerMatchId)}/replay`)
    .then(async (response) => {
      if (!response.ok) {
        throw new Error(`Match ${matchmakerMatchId} replay request failed (${response.status}).`);
      }
      return await response.json();
    })
    .then((replay) => {
      window.postMessage({ replay }, window.location.origin);
    })
    .catch((error: unknown) => {
      const message = error instanceof Error ? error.message : 'The match replay could not be loaded.';
      app.textContent = message;
      console.error(message, error);
    });
}
