'use strict';

(function () {
  const API   = 'http://localhost:5000';
  const token = localStorage.getItem('bg_token');
  if (!token) return; // só mostra se estiver logado

  // ── Histórico da conversa (para contexto) ───────────────
  const historico = [];

  // ── HTML do widget ──────────────────────────────────────
  const widget = document.createElement('div');
  widget.id = 'chat-widget';
  widget.innerHTML = `
    <button id="chat-toggle" title="Assistente IA — Reforma Tributária">
      <span id="chat-icon">💬</span>
      <span id="chat-badge" hidden>!</span>
    </button>

    <div id="chat-panel" style="display:none">
      <div id="chat-header">
        <div class="chat-header-info">
          <span class="chat-avatar">🤖</span>
          <div>
            <strong>Baruch IA</strong>
            <span class="chat-sub">Especialista em Reforma Tributária</span>
          </div>
        </div>
        <button id="chat-close" title="Fechar">✕</button>
      </div>

      <div id="chat-messages">
        <div class="msg-ia">
          <div class="msg-bubble">
            Olá! Sou o <strong>Baruch IA</strong> 👋<br>
            Posso responder suas dúvidas sobre a <strong>Reforma Tributária</strong>,
            <strong>LC 214/2025</strong>, IBS, CBS, split payment e muito mais.<br><br>
            Como posso ajudar?
          </div>
        </div>
      </div>

      <div id="chat-form">
        <textarea id="chat-input" placeholder="Digite sua dúvida sobre a reforma..." rows="1"></textarea>
        <button id="chat-send">➤</button>
      </div>
    </div>
  `;
  document.body.appendChild(widget);

  // ── CSS do widget ───────────────────────────────────────
  const style = document.createElement('style');
  style.textContent = `
    #chat-widget {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      font-family: 'Segoe UI', system-ui, sans-serif;
    }

    #chat-toggle {
      width: 56px; height: 56px;
      border-radius: 50%;
      background: #10B981;
      border: none;
      cursor: pointer;
      box-shadow: 0 4px 16px rgba(16,185,129,.45);
      font-size: 1.5rem;
      display: flex; align-items: center; justify-content: center;
      position: relative;
      transition: transform .2s, box-shadow .2s;
    }
    #chat-toggle:hover { transform: scale(1.08); box-shadow: 0 6px 20px rgba(16,185,129,.55); }

    #chat-badge {
      position: absolute; top: -4px; right: -4px;
      background: #dc2626; color: white;
      font-size: .65rem; font-weight: 800;
      width: 18px; height: 18px;
      border-radius: 50%; display: flex; align-items: center; justify-content: center;
      border: 2px solid white;
    }

    #chat-panel {
      position: absolute;
      bottom: 68px; right: 0;
      width: 360px;
      background: white;
      border-radius: 16px;
      box-shadow: 0 8px 40px rgba(0,0,0,.18);
      display: flex; flex-direction: column;
      overflow: hidden;
      max-height: 520px;
    }

    #chat-header {
      background: linear-gradient(135deg, #0F172A 0%, #1a3252 100%);
      border-bottom: 2px solid #10B981;
      padding: 12px 14px;
      display: flex; align-items: center; justify-content: space-between;
    }
    .chat-header-info { display: flex; align-items: center; gap: 10px; }
    .chat-avatar { font-size: 1.4rem; }
    .chat-header-info strong { color: white; font-size: .92rem; display: block; }
    .chat-sub { color: rgba(255,255,255,.55); font-size: .72rem; }

    #chat-close {
      background: none; border: none; color: rgba(255,255,255,.6);
      cursor: pointer; font-size: 1rem; padding: 4px 7px; border-radius: 4px;
      transition: background .15s;
    }
    #chat-close:hover { background: rgba(255,255,255,.12); color: white; }

    #chat-messages {
      flex: 1; overflow-y: auto; padding: 16px 12px;
      display: flex; flex-direction: column; gap: 10px;
      background: #f8fafc;
    }
    #chat-messages::-webkit-scrollbar { width: 4px; }
    #chat-messages::-webkit-scrollbar-thumb { background: #cbd5e1; border-radius: 4px; }

    .msg-ia, .msg-user { display: flex; }
    .msg-user { justify-content: flex-end; }

    .msg-bubble {
      max-width: 82%;
      padding: 10px 13px;
      border-radius: 12px;
      font-size: .85rem;
      line-height: 1.55;
      white-space: pre-wrap;
    }
    .msg-ia   .msg-bubble { background: white; border: 1px solid #e2e8f0; color: #1e293b; border-radius: 4px 12px 12px 12px; }
    .msg-user .msg-bubble { background: #0F172A; color: white; border-radius: 12px 4px 12px 12px; }

    .msg-thinking .msg-bubble {
      background: white; border: 1px solid #e2e8f0; color: #94a3b8;
      font-style: italic;
    }

    #chat-form {
      display: flex; align-items: flex-end; gap: 8px;
      padding: 10px 12px;
      border-top: 1px solid #e2e8f0;
      background: white;
    }
    #chat-input {
      flex: 1;
      border: 1.5px solid #e2e8f0;
      border-radius: 10px;
      padding: 8px 12px;
      font-size: .88rem;
      resize: none;
      font-family: inherit;
      max-height: 90px;
      transition: border-color .15s;
    }
    #chat-input:focus { outline: none; border-color: #10B981; }
    #chat-send {
      width: 38px; height: 38px;
      background: #10B981; color: white;
      border: none; border-radius: 10px;
      cursor: pointer; font-size: 1rem;
      flex-shrink: 0;
      transition: background .15s;
    }
    #chat-send:hover { background: #059669; }
    #chat-send:disabled { background: #cbd5e1; cursor: not-allowed; }

    @media (max-width: 420px) {
      #chat-panel { width: calc(100vw - 48px); right: 0; }
    }
  `;
  document.head.appendChild(style);

  // ── Lógica ──────────────────────────────────────────────
  const panel    = document.getElementById('chat-panel');
  const toggle   = document.getElementById('chat-toggle');
  const closeBtn = document.getElementById('chat-close');
  const input    = document.getElementById('chat-input');
  const sendBtn  = document.getElementById('chat-send');
  const messages = document.getElementById('chat-messages');
  const badge    = document.getElementById('chat-badge');
  let   isOpen   = false;

  toggle.addEventListener('click', () => {
    isOpen = !isOpen;
    panel.style.display = isOpen ? 'flex' : 'none';
    badge.hidden = true;
    if (isOpen) { input.focus(); scrollBottom(); }
  });

  closeBtn.addEventListener('click', () => {
    isOpen = false;
    panel.style.display = 'none';
  });

  // Auto-resize textarea
  input.addEventListener('input', () => {
    input.style.height = 'auto';
    input.style.height = Math.min(input.scrollHeight, 90) + 'px';
  });

  // Enter envia (Shift+Enter quebra linha)
  input.addEventListener('keydown', e => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); enviar(); }
  });
  sendBtn.addEventListener('click', enviar);

  function scrollBottom() {
    setTimeout(() => messages.scrollTop = messages.scrollHeight, 50);
  }

  function addMsg(texto, tipo) {
    const div = document.createElement('div');
    div.className = `msg-${tipo}`;
    div.innerHTML = `<div class="msg-bubble">${texto}</div>`;
    messages.appendChild(div);
    scrollBottom();
    return div;
  }

  async function enviar() {
    const texto = input.value.trim();
    if (!texto) return;

    addMsg(escHtml(texto), 'user');
    input.value = '';
    input.style.height = 'auto';
    sendBtn.disabled = true;

    const thinking = addMsg('Digitando...', 'thinking');

    try {
      const res = await fetch(`${API}/api/chat`, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify({
          mensagem: texto,
          historico: historico.slice(-10), // últimas 10 mensagens
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        thinking.querySelector('.msg-bubble').textContent = data.erro || 'Erro ao consultar IA.';
        thinking.className = 'msg-ia';
        return;
      }

      // Salva no histórico
      historico.push({ role: 'user',  texto });
      historico.push({ role: 'model', texto: data.resposta });

      thinking.className = 'msg-ia';
      thinking.querySelector('.msg-bubble').innerHTML = formatarResposta(data.resposta);

      // Badge quando painel fechado
      if (!isOpen) { badge.hidden = false; }

    } catch {
      thinking.querySelector('.msg-bubble').textContent = 'Erro de conexão. Verifique se a API está rodando.';
      thinking.className = 'msg-ia';
    } finally {
      sendBtn.disabled = false;
      scrollBottom();
    }
  }

  function escHtml(str) {
    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
  }

  function formatarResposta(texto) {
    // Markdown básico: **negrito**, \n → <br>
    return escHtml(texto)
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }
})();
