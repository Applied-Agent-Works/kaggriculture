let mermaid;
let renderNumber = 0;

async function getMermaid() {
    if (!mermaid) {
        const module = await import("https://cdn.jsdelivr.net/npm/mermaid@12.0.0/dist/mermaid.esm.min.mjs");
        mermaid = module.default;
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: "loose",
            theme: "base",
            flowchart: {
                useMaxWidth: true,
                htmlLabels: true,
                curve: "basis"
            },
            themeVariables: {
                background: "#17191b",
                primaryColor: "#183444",
                primaryTextColor: "#e7edf2",
                primaryBorderColor: "#5fa4c9",
                lineColor: "#2f91bc",
                secondaryColor: "#40351f",
                tertiaryColor: "#1d3b2d",
                fontFamily: "Segoe UI, system-ui, sans-serif"
            }
        });
    }

    return mermaid;
}

export async function renderDecisionNetwork(host, definition, nodeIds, edges, selectedNodeId, dotNetReference) {
    const renderer = await getMermaid();

    window.mermaidDecisionNetworkNodeClick = (nodeId) =>
        dotNetReference.invokeMethodAsync("SelectNodeFromMermaid", nodeId);

    const renderId = `decision-network-${++renderNumber}`;
    const result = await renderer.render(renderId, definition);
    host.innerHTML = result.svg;
    result.bindFunctions?.(host);

    const nodes = host.querySelectorAll(".node");
    for (const node of nodes) {
        const nodeId = nodeIds.find(candidate =>
            node.id.includes(`-${candidate}-`) || node.id.endsWith(`-${candidate}`));

        if (!nodeId) {
            continue;
        }

        node.dataset.networkNodeId = nodeId;
        node.setAttribute("role", "button");
        node.setAttribute("tabindex", "0");
        node.setAttribute("aria-label", `Select ${nodeId}`);
        node.addEventListener("keydown", event => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                window.mermaidDecisionNetworkNodeClick(nodeId);
            }
        });
    }

    highlightDecisionNetworkPath(host, selectedNodeId, edges);
}

export function highlightDecisionNetworkPath(host, selectedNodeId, edges) {
    const path = new Set();
    const incoming = new Map();
    const outgoing = new Map();

    for (const edge of edges ?? []) {
        const from = edge.fromNodeId ?? edge.FromNodeId;
        const to = edge.toNodeId ?? edge.ToNodeId;
        if (!incoming.has(to)) incoming.set(to, []);
        if (!outgoing.has(from)) outgoing.set(from, []);
        incoming.get(to).push(from);
        outgoing.get(from).push(to);
    }

    if (selectedNodeId) {
        path.add(selectedNodeId);
        for (const upstream of incoming.get(selectedNodeId) ?? []) path.add(upstream);
        for (const downstream of outgoing.get(selectedNodeId) ?? []) path.add(downstream);
    }

    for (const node of host.querySelectorAll(".node")) {
        const nodeId = node.dataset.networkNodeId;
        node.classList.toggle("mermaid-node-selected", nodeId === selectedNodeId);
        node.classList.toggle("mermaid-node-path", Boolean(nodeId && path.has(nodeId) && nodeId !== selectedNodeId));
    }
}

export function disposeDecisionNetwork(host) {
    host.replaceChildren();
}
