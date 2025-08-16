<template>
  <div class="chat-shell">
    <header class="topbar">
      <h1>AdventureWorks Chat</h1>

      <div class="config-section">
        <button class="config-toggle" type="button" @click="showConfig = !showConfig" :aria-expanded="showConfig">
          <span class="config-icon" :class="{ expanded: showConfig }">▶</span>
          Configuration
        </button>

        <div class="config-content" :class="{ expanded: showConfig }">
          <div class="key-row">
            <label class="key-label" for="apiKey">OpenAI API Key</label>
            <div class="key-input-wrap">
              <input id="apiKey" :type="showKey ? 'text' : 'password'" v-model="openAiKey" placeholder="sk-..."
                autocomplete="off" spellcheck="false" aria-label="OpenAI API Key" />
              <button class="ghost" type="button" @click="showKey = !showKey" :aria-pressed="showKey">
                {{ showKey ? 'Hide' : 'Show' }}
              </button>
            </div>
          </div>

          <div class="key-row">
            <label class="key-label" for="agentEndpoint">Agent Endpoint</label>
            <div class="key-input-wrap">
              <input id="agentEndpoint" type="text" v-model="agentEndpoint"
                placeholder="https://api.example.com/endpoint" autocomplete="off" spellcheck="false"
                aria-label="Agent Endpoint" />
            </div>
          </div>
        </div>
      </div>
    </header>

    <main class="chat-area">
      <div class="messages" ref="messagesRef">
        <div v-if="messages.length === 0" class="empty">
          <p>Ask anything about products. Example: <em>“Show me the different colors of touring bikes.”</em></p>
        </div>

        <div v-for="m in messages" :key="m.id" class="msg" :class="m.role">
          <div class="bubble">
            <pre class="content">{{ m.content }}</pre>
          </div>
        </div>
      </div>
    </main>

    <footer class="composer">
      <form @submit.prevent="send">
        <textarea v-model="userInput" placeholder="Type your question... (Enter to send, Shift+Enter for newline)"
          rows="2" @keydown.enter.exact.prevent="send" @keydown.enter.shift.stop aria-label="Message input" />
        <div class="actions">
          <button class="secondary" type="button" @click="clearChat"
            :disabled="loading || messages.length === 0">Clear</button>
          <button class="primary" type="submit" :disabled="!canSend || loading">
            <span v-if="!loading">Send</span>
            <span v-else>Sending…</span>
          </button>
        </div>
      </form>
      <p class="disclaimer">Key is used only for this request and not stored.</p>
    </footer>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, nextTick } from 'vue'

/**
 * Minimal, dependency-free chat UI that calls a fixed backend endpoint.
 * Drop this file in a Vue 3 + Vite (TypeScript) project as App.vue and run.
 *
 * Endpoint (POST): https://i7j8qc5q08.execute-api.us-east-1.amazonaws.com/prod/api/query
 * Body shape: { "OpenAiApiKey": string, "Query": string }
 * Response shape: { "answer": string }
 *
 * No persistence: the API key is read directly from the password input.
 */

type Role = 'user' | 'assistant' | 'error'

interface ChatMessage {
  id: number
  role: Role
  content: string
}

const API_URL = 'https://i7j8qc5q08.execute-api.us-east-1.amazonaws.com/prod/api/query'

const openAiKey = ref<string>('')
const agentEndpoint = ref<string>(API_URL)
const userInput = ref<string>('')
const messages = ref<ChatMessage[]>([])
const loading = ref(false)
const showKey = ref(false)
const showConfig = ref(false)
const messagesRef = ref<HTMLElement | null>(null)
let counter = 0

const canSend = computed(() => !!openAiKey.value.trim() && !!userInput.value.trim() && !!agentEndpoint.value.trim() && !loading.value)

function addMessage(role: Role, content: string) {
  messages.value.push({ id: ++counter, role, content })
  // Auto-scroll to bottom after DOM updates
  nextTick(() => {
    const el = messagesRef.value
    if (el) el.scrollTop = el.scrollHeight
  })
}

async function send() {
  if (!canSend.value) return

  const key = openAiKey.value.trim()
  const query = userInput.value.trim()

  addMessage('user', query)
  loading.value = true

  try {
    const res = await fetch(agentEndpoint.value.trim(), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ OpenAiApiKey: key, Query: query }),
    })

    if (!res.ok) {
      const text = await res.text().catch(() => '')
      throw new Error(text || `Request failed with status ${res.status}`)
    }

    const data = (await res.json()) as { answer?: string }
    const answer = data?.answer ?? '(No answer returned)'
    addMessage('assistant', answer)

    userInput.value = ''
  } catch (err: any) {
    const message = err?.message ?? String(err)
    addMessage('error', `Error: ${message}`)
  } finally {
    loading.value = false
  }
}

function clearChat() {
  messages.value = []
  counter = 0
}
</script>

<style scoped>
:root {
  --bg: #f8f9fa;
  --panel: #ffffff;
  --muted: #6c757d;
  --accent: #0b5ed7;
  --accent-strong: #0b5ed7;
  --danger: #dc3545;
  --text: #212529;
  --shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
}

* {
  box-sizing: border-box;
}

.chat-shell {
  display: grid;
  grid-template-rows: auto 1fr auto;
  height: 100vh;
  background: linear-gradient(180deg, #f8f9fa 0%, #e9ecef 100%);
  color: var(--text);
  font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica, Arial, "Apple Color Emoji", "Segoe UI Emoji";
}

.topbar {
  display: grid;
  gap: 10px;
  padding: 16px clamp(16px, 4vw, 28px);
  background: var(--panel);
  box-shadow: var(--shadow);
}

.topbar h1 {
  margin: 0 0 4px;
  font-size: clamp(16px, 3vw, 18px);
  font-weight: 600;
}

.config-section {
  display: grid;
  gap: 8px;
}

.config-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  background: transparent;
  border: 1px solid #dee2e6;
  color: var(--text);
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  justify-self: start;
}

.config-toggle:hover {
  background: #f8f9fa;
}

.config-icon {
  transition: transform 0.2s ease;
  font-size: 12px;
}

.config-icon.expanded {
  transform: rotate(90deg);
}

.config-content {
  display: grid;
  gap: 12px;
  max-height: 0;
  overflow: hidden;
  transition: max-height 0.3s ease;
}

.config-content.expanded {
  max-height: 200px;
}

.key-row {
  display: grid;
  gap: 6px;
}

.key-label {
  font-size: 12px;
  color: var(--muted);
}

.key-input-wrap {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 8px;
}

.key-input-wrap input {
  background: #ffffff;
  border: 1px solid #dee2e6;
  color: var(--text);
  padding: 10px 12px;
  border-radius: 10px;
  outline: none;
}

.key-input-wrap input:focus {
  border-color: var(--accent);
}

button {
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
}

button.primary {
  background: var(--accent);
  color: #000000 !important;
}

button.primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

button.secondary {
  background: #e9ecef;
  color: var(--text);
}

button.ghost {
  background: transparent;
  color: var(--muted);
  border: 1px solid #dee2e6;
}

.chat-area {
  overflow: hidden;
}

.messages {
  height: 100%;
  overflow-y: auto;
  padding: 18px clamp(12px, 4vw, 28px);
  display: grid;
  align-content: start;
  gap: 12px;
}

.empty {
  opacity: 0.7;
  text-align: center;
  margin-top: 10vh;
}

.msg {
  display: grid;
}

.bubble {
  max-width: 900px;
  width: fit-content;
  background: #f8f9fa;
  border: 1px solid #dee2e6;
  box-shadow: var(--shadow);
  padding: 12px 14px;
  border-radius: 16px;
}

.msg.user .bubble {
  background: #e3f2fd;
  border-color: #bbdefb;
}

.msg.assistant .bubble {
  background: #f1f8e9;
  border-color: #c8e6c9;
}

.msg.error .bubble {
  background: #ffebee;
  border-color: #ffcdd2;
}

.content {
  margin: 0;
  white-space: pre-wrap;
  /* preserve newlines and bullets */
  font: 14px/1.5 ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
}

.composer {
  padding: 12px clamp(12px, 4vw, 28px);
  background: var(--panel);
  box-shadow: var(--shadow);
}

.composer form {
  display: grid;
  gap: 10px;
}

.composer textarea {
  width: 100%;
  background: #ffffff;
  border: 1px solid #dee2e6;
  color: var(--text);
  padding: 10px 12px;
  border-radius: 10px;
  outline: none;
  resize: vertical;
}

.composer textarea:focus {
  border-color: var(--accent-strong);
}

.actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}

.disclaimer {
  margin: 8px 0 0 0;
  font-size: 12px;
  color: var(--muted);
}
</style>
