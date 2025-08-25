import React, { useState, useRef, useEffect } from "react";

import { askChat } from "../api/chat";

export default function ChatPanel({ onClose }) {
  const [messages, setMessages] = useState([
    { role: "system", content: "Ask about inventory, health checks, or auth." }
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef(null);
  const textareaRef = useRef(null);

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 120) + 'px';
    }
  }, [input]);

  async function send() {
    const text = input.trim();
    if (!text || loading) return;

    setMessages((m) => [...m, { role: "user", content: text }]);
    setInput("");
    setLoading(true);

    try {
      const data = await askChat(text);
      setMessages((m) => [...m, { 
        role: "assistant", 
        content: data.reply,
        meta: { source: data.source, error: data.error || null } 
      }]);
    } catch (e) {
      setMessages((m) => [...m, { 
        role: "assistant", 
        content: `Error: ${e.message}` 
      }]);
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

  const clearChat = () => {
    setMessages([{ role: "system", content: "Ask about inventory, health checks, or auth." }]);
  };

  return (
    <div className="chat-panel">
      {/* Header */}
      <div className="chat-header">
        <div className="header-content">
          <div className="header-icon">💬</div>
          <h3 className="header-title">Inventory Chat</h3>
          <div className="status-indicator online"></div>
        </div>
        {onClose && (
          <button className="close-button" onClick={onClose} aria-label="Close chat">
            ✕
          </button>
        )}
      </div>

      {/* Messages */}
      <div className="messages-container">
        {messages.map((m, i) => (
          <div 
            key={i} 
            className={`message ${m.role}`}
          >
            {m.role === "assistant" && (
              <div className="message-avatar">🤖</div>
            )}
            
            <div className="message-content">
              <div className="message-bubble">
                {m.content}
              </div>
              
              {/* Meta information */}
              {m.role === "assistant" && m.meta?.source === "fallback" && m.meta?.error && (
                <div className="message-meta">
                  Answered locally. 
                  <details className="error-details">
                    <summary>why?</summary>
                    <span>{m.meta.error}</span>
                  </details>
                </div>
              )}
            </div>
            
            {m.role === "user" && (
              <div className="message-avatar user-avatar">👤</div>
            )}
          </div>
        ))}
        
        {loading && (
          <div className="message assistant">
            <div className="message-avatar">🤖</div>
            <div className="message-content">
              <div className="typing-indicator">
                <span></span>
                <span></span>
                <span></span>
              </div>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input Area */}
      <div className="input-container">
        <div className="input-wrapper">
          <textarea
            ref={textareaRef}
            className="message-input"
            placeholder="Ask: How do I check stock of gauze pads?"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={onKeyDown}
            disabled={loading}
            rows={1}
          />
          <button 
            className="send-button"
            onClick={send} 
            disabled={loading || !input.trim()}
            aria-label="Send message"
          >
            {loading ? "⏳" : "📤"}
          </button>
        </div>
        
        <div className="action-buttons">
          <button className="action-button secondary" onClick={clearChat}>
            🗑️ Clear
          </button>
          <div className="input-hint">
            Press Enter to send, Shift+Enter for new line
          </div>
        </div>
      </div>

      <style jsx>{`
        .chat-panel {
          background: linear-gradient(145deg, #ffffff 0%, #f8fafc 100%);
          border: 1px solid #e2e8f0;
          border-radius: 16px;
          box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(0, 0, 0, 0.05);
          overflow: hidden;
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
          max-width: 420px;
          width: 100%;
        }

        .chat-header {
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          color: white;
          padding: 16px 20px;
          display: flex;
          justify-content: space-between;
          align-items: center;
          border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        .header-content {
          display: flex;
          align-items: center;
          gap: 10px;
        }

        .header-icon {
          font-size: 20px;
          animation: pulse 2s infinite;
        }

        .header-title {
          margin: 0;
          font-size: 18px;
          font-weight: 600;
          letter-spacing: -0.025em;
        }

        .status-indicator {
          width: 8px;
          height: 8px;
          border-radius: 50%;
          background: #10b981;
          box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
        }

        .status-indicator.online {
          animation: pulse-green 2s infinite;
        }

        .close-button {
          background: none;
          border: none;
          color: white;
          font-size: 20px;
          cursor: pointer;
          padding: 4px 8px;
          border-radius: 6px;
          transition: all 0.2s ease;
        }

        .close-button:hover {
          background: rgba(255, 255, 255, 0.2);
          transform: scale(1.1);
        }

        .messages-container {
          height: 350px;
          overflow-y: auto;
          padding: 16px;
          background: #fafbfc;
          scroll-behavior: smooth;
        }

        .messages-container::-webkit-scrollbar {
          width: 6px;
        }

        .messages-container::-webkit-scrollbar-track {
          background: #f1f1f1;
          border-radius: 3px;
        }

        .messages-container::-webkit-scrollbar-thumb {
          background: #c1c1c1;
          border-radius: 3px;
        }

        .messages-container::-webkit-scrollbar-thumb:hover {
          background: #a8a8a8;
        }

        .message {
          display: flex;
          margin: 12px 0;
          gap: 8px;
          animation: slideIn 0.3s ease-out;
        }

        .message.user {
          justify-content: flex-end;
        }

        .message.system {
          justify-content: center;
        }

        .message-avatar {
          width: 32px;
          height: 32px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          flex-shrink: 0;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        .user-avatar {
          background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
        }

        .message-content {
          max-width: 280px;
          display: flex;
          flex-direction: column;
        }

        .message-bubble {
          padding: 12px 16px;
          border-radius: 18px;
          line-height: 1.4;
          word-wrap: break-word;
          position: relative;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        }

        .message.user .message-bubble {
          background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
          color: white;
          border-bottom-right-radius: 6px;
        }

        .message.assistant .message-bubble {
          background: white;
          color: #374151;
          border: 1px solid #e5e7eb;
          border-bottom-left-radius: 6px;
        }

        .message.system .message-bubble {
          background: #f3f4f6;
          color: #6b7280;
          font-size: 14px;
          font-style: italic;
          text-align: center;
          border-radius: 12px;
        }

        .message-meta {
          font-size: 12px;
          color: #9ca3af;
          margin-top: 4px;
          padding-left: 4px;
        }

        .error-details {
          display: inline;
          margin-left: 4px;
        }

        .error-details summary {
          cursor: pointer;
          color: #ef4444;
          text-decoration: underline;
        }

        .typing-indicator {
          display: flex;
          gap: 4px;
          padding: 12px 16px;
          background: white;
          border: 1px solid #e5e7eb;
          border-radius: 18px;
          border-bottom-left-radius: 6px;
        }

        .typing-indicator span {
          width: 8px;
          height: 8px;
          background: #9ca3af;
          border-radius: 50%;
          animation: typing 1.4s infinite ease-in-out both;
        }

        .typing-indicator span:nth-child(1) { animation-delay: -0.32s; }
        .typing-indicator span:nth-child(2) { animation-delay: -0.16s; }

        .input-container {
          background: white;
          border-top: 1px solid #e5e7eb;
          padding: 16px;
        }

        .input-wrapper {
          display: flex;
          gap: 8px;
          align-items: flex-end;
        }

        .message-input {
          flex: 1;
          min-height: 44px;
          max-height: 120px;
          padding: 12px 16px;
          border: 2px solid #e5e7eb;
          border-radius: 22px;
          font-size: 14px;
          resize: none;
          outline: none;
          transition: all 0.2s ease;
          font-family: inherit;
          background: #f9fafb;
        }

        .message-input:focus {
          border-color: #667eea;
          box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
          background: white;
        }

        .message-input:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        .send-button {
          width: 44px;
          height: 44px;
          border: none;
          border-radius: 50%;
          background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
          color: white;
          font-size: 16px;
          cursor: pointer;
          transition: all 0.2s ease;
          flex-shrink: 0;
          box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
        }

        .send-button:hover:not(:disabled) {
          transform: scale(1.05);
          box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
        }

        .send-button:disabled {
          opacity: 0.5;
          cursor: not-allowed;
          transform: none;
        }

        .action-buttons {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-top: 12px;
        }

        .action-button {
          background: none;
          border: 1px solid #e5e7eb;
          color: #6b7280;
          padding: 6px 12px;
          border-radius: 16px;
          cursor: pointer;
          font-size: 12px;
          transition: all 0.2s ease;
        }

        .action-button:hover {
          background: #f3f4f6;
          border-color: #d1d5db;
        }

        .input-hint {
          font-size: 11px;
          color: #9ca3af;
          font-style: italic;
        }

        @keyframes slideIn {
          from {
            opacity: 0;
            transform: translateY(10px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }

        @keyframes typing {
          0%, 80%, 100% {
            transform: scale(0.8);
            opacity: 0.5;
          }
          40% {
            transform: scale(1);
            opacity: 1;
          }
        }

        @keyframes pulse {
          0%, 100% {
            transform: scale(1);
          }
          50% {
            transform: scale(1.05);
          }
        }

        @keyframes pulse-green {
          0%, 100% {
            box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
          }
          50% {
            box-shadow: 0 0 0 4px rgba(16, 185, 129, 0.4);
          }
        }

        @media (max-width: 480px) {
          .chat-panel {
            border-radius: 0;
            height: 100vh;
          }

          .messages-container {
            height: calc(100vh - 200px);
          }

          .message-content {
            max-width: calc(100vw - 120px);
          }
        }
      `}</style>
    </div>
  );
}