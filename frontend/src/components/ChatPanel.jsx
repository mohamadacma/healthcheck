import React, { useState } from "react";
import { askChat } from "../api/chat";

export default function ChatPanel({ onClose }) {
  const [messages, setMessages] = useState([
    { role: "system", content: "Ask about inventory, health checks, or auth." }
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);

  async function send() {
    const text = input.trim();
    if (!text || loading) return;

    setMessages((m) => [...m, { role: "user", content: text }]);
    setInput("");
    setLoading(true);

    try {
      const data = await askChat(text); 
      setMessages((m) => [...m, { role: "assistant", content: data.reply }]);
    } catch (e) {
      setMessages((m) => [...m, { role: "assistant", content: `Error: ${e.message}` }]);
    } finally {
      setLoading(false);
    }
  }

  function onKeyDown(e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send();
    }
  }

  return (
    <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, background: "#fff" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h3 style={{ margin: 0 }}>Inventory Chat</h3>
        {onClose && <button onClick={onClose}>×</button>}
      </div>

      <div style={{ height: 320, overflowY: "auto", marginTop: 8 }}>
        {messages.map((m, i) => (
          <div key={i} style={{
            textAlign: m.role === "user" ? "right" :
                       m.role === "assistant" ? "left" : "center",
            margin: "6px 0"
          }}>
            <span style={{
              display: "inline-block", padding: "8px 12px", borderRadius: 12,
              background: m.role === "user" ? "#e6f0ff" :
                         m.role === "assistant" ? "#f3f4f6" : "transparent"
            }}>
              {m.content}
            </span>
          </div>
        ))}
        {loading && <div style={{ color: "#666", fontSize: 12 }}>Thinking…</div>}
      </div>

      <textarea
        rows={3}
        placeholder="Ask: How do I check stock of gauze pads?"
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onKeyDown={onKeyDown}
        style={{ width: "100%", marginTop: 8, padding: 8 }}
        disabled={loading}
      />
      <div style={{ display: "flex", gap: 8, marginTop: 6 }}>
        <button onClick={send} disabled={loading || !input.trim()}>Send</button>
        <button onClick={() =>
          setMessages([{ role: "system", content: "Ask about inventory, health checks, or auth." }])
        }>
          Clear
        </button>
      </div>
    </div>
  );
}
