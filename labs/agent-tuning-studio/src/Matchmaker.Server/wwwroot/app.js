const state = { agents: [], matches: [], editingAgent: null, editingMatch: null };

const byId = (id) => document.getElementById(id);
const jsonHeaders = { "Content-Type": "application/json" };

function showNotice(message, error = false) {
  const notice = byId("notice");
  notice.textContent = message;
  notice.hidden = false;
  notice.classList.toggle("error", error);
}

function clearNotice() {
  const notice = byId("notice");
  notice.hidden = true;
  notice.classList.remove("error");
}

async function request(path, options = {}) {
  const response = await fetch(path, options);
  if (!response.ok) {
    let detail = `${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      if (body.error) detail = body.error;
    } catch {
      // Preserve the HTTP failure when the server did not return JSON.
    }
    throw new Error(detail);
  }
  return response.status === 204 ? null : response.json();
}

function renderAgents() {
  const list = byId("agent-list");
  list.replaceChildren();
  for (const agent of state.agents) {
    const record = document.createElement("article");
    record.className = "record";
    record.innerHTML = `
      <div class="record-header"><strong></strong><span class="badge"></span></div>
      <p></p><small></small>
      <div class="record-actions">
        <button class="secondary edit-agent" type="button">Edit</button>
        <button class="danger delete-agent" type="button">Remove record</button>
      </div>`;
    record.querySelector("strong").textContent = agent.label;
    record.querySelector(".badge").textContent = agent.kind;
    record.querySelector("p").textContent = agent.description || "No description.";
    record.querySelector("small").textContent = `${agent.id} · ${agent.entryPoint || agent.id} · ${(agent.traits || []).join(", ") || "no traits"}`;
    record.querySelector(".edit-agent").addEventListener("click", () => editAgent(agent));
    record.querySelector(".delete-agent").addEventListener("click", () => deleteAgent(agent.id));
    list.append(record);
  }
  if (!state.agents.length) list.textContent = "No agent records.";
  renderAgentOptions();
}

function renderMatches() {
  const list = byId("match-list");
  list.replaceChildren();
  for (const match of state.matches) {
    const record = document.createElement("article");
    record.className = "record";
    record.innerHTML = `
      <div class="record-header"><strong></strong><span class="badge"></span></div>
      <p></p><small></small>
      <div class="record-actions">
        <button class="secondary edit-match" type="button">Edit</button>
        <button class="danger delete-match" type="button">Remove record</button>
      </div>`;
    record.querySelector("strong").textContent = match.id;
    record.querySelector(".badge").textContent = match.status;
    record.querySelector("p").textContent = `${match.agent} vs ${match.opponent}`;
    record.querySelector("small").textContent = `Seed ${match.seed} · ${match.steps} steps · seat ${match.seat}${match.artifactPath ? ` · ${match.artifactPath}` : ""}`;
    record.querySelector(".edit-match").addEventListener("click", () => editMatch(match));
    record.querySelector(".delete-match").addEventListener("click", () => deleteMatch(match.id));
    list.append(record);
  }
  if (!state.matches.length) list.textContent = "No match records.";
}

function renderAgentOptions() {
  for (const id of ["match-agent", "match-opponent"]) {
    const select = byId(id);
    const previous = select.value;
    select.replaceChildren();
    for (const agent of state.agents.filter((item) => item.available)) {
      const option = new Option(`${agent.label} (${agent.id})`, agent.id);
      select.add(option);
    }
    if (state.agents.some((agent) => agent.id === previous)) select.value = previous;
  }
}

function resetAgentForm() {
  state.editingAgent = null;
  byId("agent-form").reset();
  byId("agent-available").checked = true;
  byId("agent-form-title").textContent = "Add agent";
  byId("agent-id").disabled = false;
  byId("cancel-agent").hidden = true;
}

function editAgent(agent) {
  state.editingAgent = agent.id;
  byId("agent-form-title").textContent = "Edit agent";
  byId("agent-id").value = agent.id;
  byId("agent-id").disabled = true;
  byId("agent-label").value = agent.label;
  byId("agent-kind").value = agent.kind;
  byId("agent-entry-point").value = agent.entryPoint || "";
  byId("agent-description").value = agent.description || "";
  byId("agent-traits").value = (agent.traits || []).join(", ");
  byId("agent-available").checked = agent.available;
  byId("cancel-agent").hidden = false;
  byId("agent-form").scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function resetMatchForm() {
  state.editingMatch = null;
  byId("match-form").reset();
  byId("match-form-title").textContent = "Add match record";
  byId("match-id").disabled = false;
  byId("match-seed").value = 42;
  byId("match-steps").value = 24;
  byId("match-seat").value = 0;
  byId("match-status").value = "planned";
  byId("cancel-match").hidden = true;
}

function editMatch(match) {
  state.editingMatch = match.id;
  byId("match-form-title").textContent = "Edit match record";
  byId("match-id").value = match.id;
  byId("match-id").disabled = true;
  byId("match-agent").value = match.agent;
  byId("match-opponent").value = match.opponent;
  byId("match-seed").value = match.seed;
  byId("match-steps").value = match.steps;
  byId("match-seat").value = match.seat;
  byId("match-status").value = match.status;
  byId("match-artifact").value = match.artifactPath || "";
  byId("cancel-match").hidden = false;
  byId("match-form").scrollIntoView({ behavior: "smooth", block: "nearest" });
}

async function saveAgent(event) {
  event.preventDefault();
  clearNotice();
  const id = byId("agent-id").value.trim();
  const body = {
    id,
    label: byId("agent-label").value.trim(),
    description: byId("agent-description").value.trim(),
    traits: byId("agent-traits").value.split(",").map((value) => value.trim()).filter(Boolean),
    kind: byId("agent-kind").value,
    entryPoint: byId("agent-entry-point").value.trim() || null,
    available: byId("agent-available").checked
  };
  try {
    await request(`/api/matchmaker/agents${state.editingAgent ? `/${encodeURIComponent(state.editingAgent)}` : ""}`, {
      method: state.editingAgent ? "PUT" : "POST", headers: jsonHeaders, body: JSON.stringify(body)
    });
    await load();
    resetAgentForm();
    showNotice(`Agent record '${id}' saved.`);
  } catch (error) {
    showNotice(`Could not save agent: ${error.message}`, true);
  }
}

async function deleteAgent(id) {
  if (!confirm(`Remove the catalog record for '${id}'? Source files are not deleted.`)) return;
  try {
    await request(`/api/matchmaker/agents/${encodeURIComponent(id)}`, { method: "DELETE" });
    await load();
    showNotice(`Agent record '${id}' removed.`);
  } catch (error) {
    showNotice(`Could not remove agent: ${error.message}`, true);
  }
}

async function saveMatch(event) {
  event.preventDefault();
  clearNotice();
  const id = byId("match-id").value.trim();
  const body = {
    id,
    agent: byId("match-agent").value,
    opponent: byId("match-opponent").value,
    seed: Number(byId("match-seed").value),
    steps: Number(byId("match-steps").value),
    seat: Number(byId("match-seat").value),
    status: byId("match-status").value,
    artifactPath: byId("match-artifact").value.trim() || null,
    configuration: {}
  };
  try {
    await request(`/api/matchmaker/matches${state.editingMatch ? `/${encodeURIComponent(state.editingMatch)}` : ""}`, {
      method: state.editingMatch ? "PUT" : "POST", headers: jsonHeaders, body: JSON.stringify(body)
    });
    await load();
    resetMatchForm();
    showNotice(`Match record '${id}' saved.`);
  } catch (error) {
    showNotice(`Could not save match: ${error.message}`, true);
  }
}

async function deleteMatch(id) {
  if (!confirm(`Remove the catalog record for '${id}'? Match artifacts are not deleted.`)) return;
  try {
    await request(`/api/matchmaker/matches/${encodeURIComponent(id)}`, { method: "DELETE" });
    await load();
    showNotice(`Match record '${id}' removed.`);
  } catch (error) {
    showNotice(`Could not remove match: ${error.message}`, true);
  }
}

async function load() {
  const catalog = await request("/api/matchmaker/catalog");
  state.agents = catalog.agents || [];
  state.matches = catalog.matches || [];
  renderAgents();
  renderMatches();
  byId("status-badge").textContent = `${state.agents.length} agents · ${state.matches.length} matches`;
}

byId("agent-form").addEventListener("submit", saveAgent);
byId("match-form").addEventListener("submit", saveMatch);
byId("new-agent").addEventListener("click", resetAgentForm);
byId("cancel-agent").addEventListener("click", resetAgentForm);
byId("new-match").addEventListener("click", resetMatchForm);
byId("cancel-match").addEventListener("click", resetMatchForm);

load().catch((error) => showNotice(`Could not load Matchmaker catalog: ${error.message}`, true));
