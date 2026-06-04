import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';
import { humanizeValue } from '../utils/field-labels.js';

function renderSuggestions(suggestEl, listEl, usedExprs) {
  suggestEl.innerHTML = '';
  const recs = getState('recommended_assertions') || [];
  if (!recs.length) return;
  suggestEl.appendChild(Object.assign(document.createElement('div'), { className: 'sub-section-title', textContent: 'Suggested Assertions' }));
  recs.forEach(expr => {
    const btn = document.createElement('button'); btn.type = 'button';
    btn.className = 'assertion-suggestion' + (usedExprs.has(expr) ? ' used' : '');
    btn.textContent = expr; btn.disabled = usedExprs.has(expr);
    btn.addEventListener('click', () => {
      if (usedExprs.has(expr)) return;
      usedExprs.add(expr);
      const a = [...(getState('assertions') || [])];
      a.push({ id: 'asr-' + Date.now(), type: 'boolean', expr, severity: 'warning', time_window_ms: { from: 0, to: 0 } });
      setState('assertions', a);
      renderAssertionList(listEl, suggestEl, usedExprs);
      renderSuggestions(suggestEl, listEl, usedExprs);
    });
    suggestEl.appendChild(btn);
  });
}

function renderAssertionList(listEl, suggestEl, usedExprs) {
  listEl.innerHTML = '';
  (getState('assertions') || []).forEach((item, idx) => {
    const row = document.createElement('div'); row.className = 'list-item';
    const upd = (f, v) => { const a = [...(getState('assertions') || [])]; a[idx] = { ...a[idx], [f]: v }; setState('assertions', a); };
    const updTW = (f, v) => { const a = [...(getState('assertions') || [])]; a[idx] = { ...a[idx], time_window_ms: { ...a[idx].time_window_ms, [f]: v } }; setState('assertions', a); };
    const mkSel = (lbl, key, opts) => {
      const w = document.createElement('div'); w.className = 'field-wrapper';
      w.appendChild(Object.assign(document.createElement('label'), { textContent: lbl }));
      const sel = document.createElement('select');
      opts.forEach(v => { const o = document.createElement('option'); o.value = v; o.textContent = humanizeValue(v); if (v === item[key]) o.selected = true; sel.appendChild(o); });
      sel.setAttribute('data-field', `assertions.${idx}.${key}`); sel.addEventListener('change', () => upd(key, sel.value)); w.appendChild(sel); return w;
    };
    const idW = document.createElement('div'); idW.className = 'field-wrapper';
    idW.appendChild(Object.assign(document.createElement('label'), { textContent: 'ID' }));
    const idI = document.createElement('input'); idI.type = 'text'; idI.value = item.id || '';
    idI.setAttribute('data-field', `assertions.${idx}.id`); idI.addEventListener('input', () => upd('id', idI.value)); idW.appendChild(idI); row.appendChild(idW);
    row.appendChild(mkSel('Type', 'type', ['STL', 'LTL', 'metric', 'boolean']));
    const exW = document.createElement('div'); exW.className = 'field-wrapper';
    exW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Expression' }));
    const ta = document.createElement('textarea'); ta.value = item.expr || '';
    ta.setAttribute('data-field', `assertions.${idx}.expr`); ta.addEventListener('input', () => upd('expr', ta.value)); exW.appendChild(ta); row.appendChild(exW);
    row.appendChild(mkSel('Severity', 'severity', ['warning', 'minor', 'major', 'critical']));
    const twfW = document.createElement('div'); twfW.className = 'field-wrapper';
    twfW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Window From' }));
    const twfI = document.createElement('input'); twfI.type = 'number'; twfI.min = 0; twfI.value = item.time_window_ms?.from ?? 0;
    twfI.setAttribute('data-field', `assertions.${idx}.time_window_ms.from`);
    twfI.addEventListener('input', () => updTW('from', twfI.value === '' ? 0 : parseFloat(twfI.value)));
    twfW.appendChild(twfI); twfW.appendChild(Object.assign(document.createElement('span'), { textContent: ' ms' })); row.appendChild(twfW);
    const twtW = document.createElement('div'); twtW.className = 'field-wrapper';
    twtW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Window To' }));
    const twtI = document.createElement('input'); twtI.type = 'number'; twtI.min = 0; twtI.value = item.time_window_ms?.to ?? 0;
    twtI.setAttribute('data-field', `assertions.${idx}.time_window_ms.to`);
    twtI.addEventListener('input', () => updTW('to', twtI.value === '' ? 0 : parseFloat(twtI.value)));
    twtW.appendChild(twtI); twtW.appendChild(Object.assign(document.createElement('span'), { textContent: ' ms' })); row.appendChild(twtW);
    const rm = document.createElement('button'); rm.type = 'button'; rm.textContent = 'Remove';
    rm.addEventListener('click', () => { const a = [...(getState('assertions') || [])]; a.splice(idx, 1); setState('assertions', a); renderAssertionList(listEl, suggestEl, usedExprs); });
    row.appendChild(rm); listEl.appendChild(row);
  });
}

export function render(container) {
  const block = createCollapsibleBlock('G. Safety Metadata & Formal Assertions', 'block-g', content => {
    [
      createField({ label: 'Hazard ID', path: 'safety.hazard_id', type: 'text', defaultValue: '' }),
      createField({ label: 'Safety Goal', path: 'safety.safety_goal', type: 'textarea', defaultValue: '' }),
      createField({ label: 'Risk Weight', path: 'safety.risk_weight', type: 'number', min: 0, max: 1, step: 0.01, defaultValue: 0 }),
      createField({ label: 'Recovery Expected', path: 'safety.recovery_behaviour_expected', type: 'checkbox', defaultValue: false }),
    ].forEach(el => content.appendChild(el));
    const asilW = document.createElement('div'); asilW.className = 'field-wrapper';
    asilW.appendChild(Object.assign(document.createElement('label'), { textContent: 'ASIL Level' }));
    const asilSel = document.createElement('select'); asilSel.setAttribute('data-field', 'safety.ASIL_level');
    [{ v: '', t: '(none)' }, ...['QM', 'A', 'B', 'C', 'D'].map(v => ({ v, t: v }))].forEach(({ v, t }) => {
      const o = document.createElement('option'); o.value = v; o.textContent = t;
      if ((getState('safety.ASIL_level') ?? '') === v) o.selected = true; asilSel.appendChild(o);
    });
    asilSel.addEventListener('change', () => setState('safety.ASIL_level', asilSel.value || null));
    asilW.appendChild(asilSel); content.appendChild(asilW);
    const suggestEl = document.createElement('div'); suggestEl.className = 'assertion-suggestions';
    const usedExprs = new Set();
    const listLabel = document.createElement('div'); listLabel.className = 'dynamic-list-label'; listLabel.textContent = 'Assertions';
    const listEl = document.createElement('div'); listEl.className = 'dynamic-list-items';
    renderSuggestions(suggestEl, listEl, usedExprs);
    renderAssertionList(listEl, suggestEl, usedExprs);
    const addBtn = document.createElement('button'); addBtn.type = 'button'; addBtn.textContent = 'Add Assertion';
    addBtn.addEventListener('click', () => {
      const a = [...(getState('assertions') || [])];
      a.push({ id: 'asr-' + Date.now(), type: 'boolean', expr: '', severity: 'warning', time_window_ms: { from: 0, to: 0 } });
      setState('assertions', a);
      renderAssertionList(listEl, suggestEl, usedExprs);
    });
    content.appendChild(suggestEl); content.appendChild(listLabel); content.appendChild(listEl); content.appendChild(addBtn);
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
