'use strict';

const API   = 'http://localhost:5000';
const token = localStorage.getItem('bg_token');

if (!token) window.location.href = 'login.html';

function sair() {
  localStorage.clear();
  window.location.href = 'login.html';
}

// ── Formatação ──────────────────────────────────────────────────────────────
const FMT_BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const FMT_PCT = new Intl.NumberFormat('pt-BR', { style: 'percent', minimumFractionDigits: 2, maximumFractionDigits: 2 });

const brl = v => FMT_BRL.format(v);
const pct = v => FMT_PCT.format(v);

// ── Leitura do formulário ───────────────────────────────────────────────────
const n = id => parseFloat(document.getElementById(id).value) || 0;

function lerRequest() {
  return {
    fatProduto:  n('fat-produto'),
    fatServico:  n('fat-servico'),
    pctCMV:        n('pct-cmv'),
    pctCSV:        n('pct-csv'),
    pctDespesas:   n('pct-despesas'),
    pctDirCredito: n('pct-dir-credito'),
    aliqIbsCbs:           n('aliq-ibs-cbs'),
    aliqExtintosServDesp: n('aliq-extintos-serv-desp'),
    aliqExtitosCmv:       n('aliq-extitos-cmv'),
    LR_AliqExtintosReceita: n('lr-aliq-extintos-receita'),
    LR_AliqIrpjCsll:        n('lr-aliq-irpj-csll'),
    LP_AliqExtintosReceita: n('lp-aliq-extintos-receita'),
    LP_PctPresuncaoProd:    n('lp-pct-presuncao-prod'),
    LP_PctPresuncaoServ:    n('lp-pct-presuncao-serv'),
    LP_AliqIrpj:            n('lp-aliq-irpj'),
    LP_AliqCsll:            n('lp-aliq-csll'),
    SN_AliqExtintosReceita: n('sn-aliq-extintos-receita'),
    SN_AliqIrpjDas:         n('sn-aliq-irpj-das'),
    SN_AliqCsllDas:         n('sn-aliq-csll-das'),
    SN_AliqCppDas:          n('sn-aliq-cpp-das'),
    SNS_AliqExtintosReceita: n('sns-aliq-extintos-receita'),
    SNS_AliqIbsCbsDas:       n('sns-aliq-ibs-cbs-das'),
    SNS_AliqIbsCbsCompras:   n('sns-aliq-ibs-cbs-compras'),
    SNS_AliqIrpjDas:         n('sns-aliq-irpj-das'),
    SNS_AliqCsllDas:         n('sns-aliq-csll-das'),
  };
}

// ── Renderização DRE ────────────────────────────────────────────────────────
function linhas(r) {
  const a = r.antes, d = r.depois;
  const dedLabel  = r.isSimples ? '(-) DAS (ISS/PIS/COFINS → IBS/CBS)' : '(-) Deduções (ICMS/ISS/PIS/COFINS)';
  const despLabel = r.isSimples ? '(-) Despesas + CPP-INSS' : '(-) Despesas';

  return [
    { label: 'Receita Bruta',           a: a.receitaBruta,       d: d.receitaBruta },
    { label: dedLabel,                   a: a.deducoes,           d: d.deducoes },
    { label: 'Receita Líquida',          a: a.receitaLiquida,     d: d.receitaLiquida,     bold: true },
    { sep: true },
    { label: '(-) CMV + CSV',            a: a.cmvCsv,             d: d.cmvCsv },
    { label: 'Margem de Contribuição',   a: a.margemContribuicao, d: d.margemContribuicao, bold: true },
    { sep: true },
    { label: despLabel,                  a: a.despesas + a.cppDas, d: d.despesas + d.cppDas },
    { label: 'Lucro Antes do IRPJ/CSLL', a: a.lucroAntesIR,      d: d.lucroAntesIR,       bold: true },
    { sep: true },
    { label: '(-) IRPJ e CSLL',          a: a.irpjCsll,          d: d.irpjCsll },
    { sep: true },
    { label: 'LUCRO LÍQUIDO',            a: a.lucroLiquido,       d: d.lucroLiquido,       destaque: true },
  ];
}

function varClass(v) {
  if (v > 0.005)  return 'pos';
  if (v < -0.005) return 'neg';
  return 'neu';
}

function varStr(v) {
  if (Math.abs(v) < 0.005) return '<span class="neu">—</span>';
  const cls = varClass(v);
  return `<span class="${cls}">${v > 0 ? '+' : ''}${brl(v)}</span>`;
}

function buildDRETable(r) {
  const rows = linhas(r).map(l => {
    if (l.sep) return `<tr class="sep"><td colspan="4"></td></tr>`;
    const var_ = l.d - l.a;
    const cls  = l.destaque ? 'destaque' : l.bold ? 'bold' : '';
    return `<tr class="${cls}">
      <td>${l.label}</td>
      <td>${brl(l.a)}</td>
      <td>${brl(l.d)}</td>
      <td>${l.destaque
        ? `<span class="${varClass(var_)}">${var_ > 0 ? '+' : ''}${brl(var_)}</span>`
        : varStr(var_)}</td>
    </tr>`;
  }).join('');

  return `
    <table class="dre-table">
      <thead>
        <tr>
          <th style="width:40%">Descrição</th>
          <th style="width:20%">Antes</th>
          <th style="width:20%">Depois</th>
          <th style="width:20%">Variação</th>
        </tr>
      </thead>
      <tbody>${rows}</tbody>
    </table>`;
}

function renderDREs(resultados) {
  const tabsEl = document.getElementById('tabs');
  const contEl = document.getElementById('dre-container');

  tabsEl.innerHTML = resultados.map((r, i) =>
    `<button class="tab-btn${i === 0 ? ' active' : ''}" data-idx="${i}">${r.regime}</button>`
  ).join('');

  contEl.innerHTML = resultados.map((r, i) =>
    `<div class="dre-panel${i === 0 ? ' active' : ''}" data-idx="${i}">
      <div class="dre-regime-title">${r.regime}</div>
      ${buildDRETable(r)}
    </div>`
  ).join('');

  tabsEl.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const idx = btn.dataset.idx;
      tabsEl.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      contEl.querySelectorAll('.dre-panel').forEach(p => p.classList.remove('active'));
      btn.classList.add('active');
      contEl.querySelector(`.dre-panel[data-idx="${idx}"]`).classList.add('active');
    });
  });
}

function renderComparativo(resultados) {
  const rows = resultados.map(r => {
    const a    = r.antes.lucroLiquido;
    const d    = r.depois.lucroLiquido;
    const var_ = d - a;
    const pv   = a !== 0 ? var_ / Math.abs(a) : 0;
    const cls  = var_ >= 0 ? 'pos' : 'neg';
    return `<tr>
      <td>${r.regime}</td>
      <td>${brl(a)}</td>
      <td>${brl(d)}</td>
      <td><span class="${cls}">${var_ > 0 ? '+' : ''}${brl(var_)}</span></td>
      <td><span class="badge ${cls}">${pv > 0 ? '+' : ''}${pct(pv)}</span></td>
    </tr>`;
  }).join('');

  document.getElementById('comparativo').innerHTML = `
    <table class="comp-table">
      <thead>
        <tr>
          <th>Regime</th>
          <th>Antes</th>
          <th>Depois</th>
          <th>Variação (R$)</th>
          <th>Variação (%)</th>
        </tr>
      </thead>
      <tbody>${rows}</tbody>
    </table>`;
}

// ── Atualizar total ao vivo ─────────────────────────────────────────────────
function atualizarTotal() {
  const total = n('fat-produto') + n('fat-servico');
  document.getElementById('fat-total-display').textContent = brl(total);
}

// ── Inicialização ───────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  // Mostrar nome do usuário e links de admin no header
  const nome    = localStorage.getItem('bg_nome') || '';
  const isAdmin = localStorage.getItem('bg_isAdmin') === 'true';
  const userEl  = document.getElementById('header-user-name');
  const adminEl = document.getElementById('header-admin-link');
  if (userEl) userEl.textContent = nome;
  if (adminEl) adminEl.hidden = !isAdmin;

  // Total ao vivo
  ['fat-produto', 'fat-servico'].forEach(id =>
    document.getElementById(id).addEventListener('input', atualizarTotal)
  );

  // Submit — chama a API
  document.getElementById('form').addEventListener('submit', async e => {
    e.preventDefault();

    const btnCalc = e.submitter || document.querySelector('.btn-calcular');
    btnCalc.disabled = true;
    btnCalc.textContent = '⏳ Calculando…';

    try {
      const req = lerRequest();
      const res = await fetch(`${API}/api/simular`, {
        method:  'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify(req),
      });

      if (res.status === 401) { sair(); return; }
      if (!res.ok) { alert('Erro na simulação. Tente novamente.'); return; }

      const resultados = await res.json();

      renderDREs(resultados);
      renderComparativo(resultados);

      // Nome da empresa
      const nomeEmpresa = document.getElementById('nome-empresa').value.trim();
      document.getElementById('empresa-nome-display').textContent =
        nomeEmpresa ? `📋 ${nomeEmpresa}` : '';

      // Cabeçalho de impressão
      const fatTotal = n('fat-produto') + n('fat-servico');
      const dataStr  = new Date().toLocaleDateString('pt-BR', { day: '2-digit', month: 'long', year: 'numeric' });
      document.getElementById('ph-empresa').textContent = nomeEmpresa || '';
      document.getElementById('ph-fat').textContent     = `Faturamento Total: ${brl(fatTotal)}`;
      document.getElementById('ph-data').textContent    = `Simulação em ${dataStr}`;

      const results = document.getElementById('results');
      results.hidden = false;
      results.scrollIntoView({ behavior: 'smooth', block: 'start' });

    } finally {
      btnCalc.disabled = false;
      btnCalc.textContent = '⚡ Calcular Impacto da Reforma';
    }
  });

  // Exportar PDF
  document.getElementById('btn-pdf').addEventListener('click', () => window.print());
});
