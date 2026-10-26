/**
 * OpenRouter Prompt Caching - 100% Hit Rate Wrapper (Otomatis)
 * Works for: Anthropic Claude, Google Gemini, DeepSeek, OpenAI (auto prefix)
 * Docs: https://openrouter.ai/docs/guides/best-practices/prompt-caching
 *
 * Aturan 100% Hit:
 * 1. Prefix byte-identik (system + tools + context) tidak boleh berubah
 * 2. Variable (user query, timestamp, id) TARUH PALING AKHIR
 * 3. Model string HARUS pinned (jangan :free / :nitro gonta-ganti)
 * 4. Gunakan top-level cache_control untuk auto-advance (multi-turn)
 * 5. TTL 5m default, 1h untuk long session
 */

// ============ CONFIG STATIS - INI YANG DI-CACHE ============
export const CACHED_SYSTEM_PROMPT = `Kamu adalah asisten ahli. Jawab akurat, ringkas.
# Aturan:
- Jangan halusinasi
- Selalu pakai bahasa Indonesia kecuali diminta lain
`.trim();

// Jika ada RAG / GDD / knowledge besar, taruh di sini (akan di-cache)
export const CACHED_CONTEXT = `... isi dokumen besar / RAG / CSV / book chapter ...`;

// Tools harus urutan & definisi stabil - jangan generate dinamis
export const CACHED_TOOLS = [
  {
    type: "function",
    function: {
      name: "search_docs",
      description: "Cari dokumen",
      parameters: { type: "object", properties: { q: { type: "string" } }, required: ["q"] }
    }
  }
];

// ============ WRAPPER OTOMATIS ============
const MUSE_SPARK_MODEL = "meta/muse-spark-1.2-contributor"; // OpenRouter ID (bukan opencode). Alternatif: meta/muse-spark-1.2

function supportsExplicitCache(model) {
  // Anthropic/Google/DeepSeek support cache_control, Muse Spark/Meta/Llama = implicit prefix cache
  return /anthropic|claude|gemini|google|deepseek|openai|gpt/i.test(model);
}

export function buildOpenRouterPayload({ userMessage, history = [], model = MUSE_SPARK_MODEL, use1hTTL = false }) {
  const payload = {
    model, // Muse Spark 1.2 Contributor - pinned, jangan gonta-ganti :free
    tools: CACHED_TOOLS,
    temperature: 0.2,
    messages: [
      {
        role: "system",
        content: `${CACHED_SYSTEM_PROMPT}\n\n<context>\n${CACHED_CONTEXT}\n</context>`
      },
      ...history,
      { role: "user", content: userMessage }
    ]
  };
  // Muse Spark = implicit prefix cache (otomatis jika prefix identik & >512 token), tidak perlu cache_control
  // Anthropic/Google tetap pakai top-level cache_control untuk 100% hit
  if (supportsExplicitCache(model)) {
    payload.cache_control = use1hTTL ? { type: "ephemeral", ttl: "1h" } : { type: "ephemeral" };
  }
  return payload;
}

// Alternatif: Explicit breakpoint (hanya untuk Anthropic/Google, TIDAK untuk Muse Spark)
export function buildExplicitPayload({ userMessage, model = MUSE_SPARK_MODEL }) {
  if (!supportsExplicitCache(model)) {
    console.warn(`[cache] ${model} pakai implicit prefix cache, tidak perlu explicit breakpoint. Pakai buildOpenRouterPayload saja.`);
    return buildOpenRouterPayload({ userMessage, model });
  }
  return {
    model,
    messages: [
      {
        role: "system",
        content: [
          { type: "text", text: CACHED_SYSTEM_PROMPT },
          { type: "text", text: CACHED_CONTEXT, cache_control: { type: "ephemeral", ttl: "1h" } }
        ]
      },
      { role: "user", content: userMessage }
    ]
  };
}

// ============ CONTOH PAKAI ============
export async function callOpenRouter({ userMessage, history, model = MUSE_SPARK_MODEL, apiKey = process.env.OPENROUTER_API_KEY }) {
  if (!apiKey) throw new Error("OPENROUTER_API_KEY belum di-set. Export dulu: $env:OPENROUTER_API_KEY='sk-or-v1-...'");
  const payload = buildOpenRouterPayload({ userMessage, history, model, use1hTTL: true });

  const res = await fetch("https://openrouter.ai/api/v1/chat/completions", {
    method: "POST",
    headers: {
      "Authorization": `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      "HTTP-Referer": "https://your-app.com",
      "X-Title": "Your App"
    },
    body: JSON.stringify(payload)
  });

  const data = await res.json();
  // Cek hit: data.usage.cached_tokens > 0 atau data.usage.prompt_tokens_details.cached_tokens
  console.log("cached_tokens:", data.usage?.cached_tokens ?? data.usage?.prompt_tokens_details?.cached_tokens ?? 0);
  console.log("hit rate:", (data.usage?.cached_tokens / data.usage?.prompt_tokens) * 100 + "%");
  return data;
}

// ============ ANTI-PATTERN YANG BIKIN MISS ============
// JANGAN:
// - `system: "Hari ini ${new Date().toISOString()}"` -> cache bust tiap detik
// - `model: "anthropic/claude-sonnet-4:free"` kadang route beda provider -> miss
// - tools di-generate random order / description beda spasi -> miss
// - taruh user id / timestamp di system prompt -> miss
// SOLUSI: taruh semua variable di user message terakhir saja
