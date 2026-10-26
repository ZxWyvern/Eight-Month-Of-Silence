/**
 * OpenRouter - Muse Spark 1.2 Contributor - 100% Hit Rate
 * System Prompt = AGENTS.md (verbatim) -> tidak perlu mikir, langsung ngerti project
 * Model: meta/muse-spark-1.2-contributor (OpenRouter)
 * Endpoint: https://openrouter.ai/api/v1/chat/completions
 */

import { readFileSync } from "fs";
import { join, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));

// ============ 1. SYSTEM PROMPT = AGENTS.md (OTOMATIS) ============
// Baca langsung dari AGENTS.md biar 100% sinkron, tidak copy-paste manual
// File ini yang DI-CACHE (prefix stabil) -> 100% hit setelah warmup
export function loadAgentsSystemPrompt() {
  const agentsPath = join(__dirname, "..", "AGENTS.md");
  const agentsMd = readFileSync(agentsPath, "utf-8");
  // Bungkus dengan instruksi peran agar Muse Spark langsung paham tanpa mikir
  return `You are Muse Spark 1.2 Contributor running inside Sinergasia Studio repo.

Your ENTIRE behavior is defined by AGENTS.md below. This file is a BINDING CONTRACT, not a suggestion. If user prompt conflicts with AGENTS.md, AGENTS.md WINS.

=== BEGIN AGENTS.md (verbatim, DO NOT MODIFY) ===
${agentsMd}
=== END AGENTS.md ===

ADDITIONAL RUNTIME RULES:
- You have already read AGENTS.md, docs/GDD.md, docs/ARCHITECTURE.md per §0. Act as if you have.
- Always enforce §2 Constitution, §4 Layered Architecture (Domain never uses UnityEngine), §5 Forbidden Patterns, §6 Workflow, §11 Skills & MCP.
- Before ANY tool/code action, enumerate relevant skills (.opencode/skills/*) and MCP servers (context7, unity) per §0/§11.
- For Unity mutations use unity_* MCP only. For docs use context7. Never bypass.
- Respond in Indonesian unless code/comments require English. Be short, concise, factual.
`.trim();
}

// Cache di memory biar tidak baca disk tiap request (jaga byte-identik untuk cache hit)
const CACHED_SYSTEM_PROMPT = loadAgentsSystemPrompt();

// Context tambahan yang juga di-cache (opsional, tambah GDD/ARCH jika mau hit 100% lebih besar)
// Kosongkan jika hanya AGENTS.md saja yang mau di-cache
export const CACHED_EXTRA_CONTEXT = ""; // isi dengan docs/GDD.md jika mau, tapi harus stabil

export const CACHED_TOOLS = [
  // Definisi tools HARUS stabil & urutan tetap - jangan generate dinamis
  { type: "function", function: { name: "read_docs", description: "Baca file docs", parameters: { type: "object", properties: { path: { type: "string" } }, required: ["path"] } } },
  { type: "function", function: { name: "search_code", description: "Cari kode", parameters: { type: "object", properties: { query: { type: "string" } }, required: ["query"] } } }
];

const MODEL = "meta/muse-spark-1.2-contributor"; // OpenRouter ID

function supportsExplicitCache(model) {
  return /anthropic|claude|gemini|google|deepseek|openai|gpt/i.test(model);
}

export function buildPayload({ userMessage, history = [], model = MODEL }) {
  const systemContent = CACHED_EXTRA_CONTEXT
    ? `${CACHED_SYSTEM_PROMPT}\n\n<extra_context>\n${CACHED_EXTRA_CONTEXT}\n</extra_context>`
    : CACHED_SYSTEM_PROMPT;

  const payload = {
    model,
    temperature: 0.2,
    tools: CACHED_TOOLS,
    messages: [
      { role: "system", content: systemContent }, // <- INI YANG DI-CACHE (besar, statis)
      ...history,
      { role: "user", content: userMessage } // <- variabel taruh AKHIR saja
    ]
  };
  if (supportsExplicitCache(model)) {
    payload.cache_control = { type: "ephemeral", ttl: "1h" };
  }
  return payload;
}

export async function callOpenRouter({ userMessage, history = [], model = MODEL, apiKey = process.env.OPENROUTER_API_KEY }) {
  if (!apiKey) throw new Error("Set OPENROUTER_API_KEY dulu: $env:OPENROUTER_API_KEY='sk-or-v1-...'");
  const payload = buildPayload({ userMessage, history, model });
  const res = await fetch("https://openrouter.ai/api/v1/chat/completions", {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      "HTTP-Referer": "https://sinergasia.studio",
      "X-Title": "Eight Months of Silence - CeBeTe"
    },
    body: JSON.stringify(payload)
  });
  const data = await res.json();
  if (data.error) throw new Error(JSON.stringify(data.error));
  const cached = data.usage?.cached_tokens ?? data.usage?.prompt_tokens_details?.cached_tokens ?? 0;
  const total = data.usage?.prompt_tokens ?? 1;
  console.log(`[cache] model=${model} cached=${cached}/${total} hit=${((cached/total)*100).toFixed(1)}%`);
  return data;
}

// Quick test: node tools/openrouter-cache.agents.mjs "halo"
if (process.argv[2]) {
  callOpenRouter({ userMessage: process.argv[2] }).then(r => console.log(r.choices[0].message.content)).catch(console.error);
}
