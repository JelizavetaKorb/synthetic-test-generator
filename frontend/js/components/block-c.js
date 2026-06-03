import { createCollapsibleBlock } from '../utils/dom.js';
import { createDynamicList } from '../utils/form.js';

function makeInp(lbl, key, type, opts, idx, listPath) {
  const w = document.createElement('div'); w.className = 'field-wrapper';
  const l = document.createElement('label'); l.textContent = lbl; w.appendChild(l);
  let el;
  if (type === 'select') {
    el = document.createElement('select');
    (opts || []).forEach(o => { const op = document.createElement('option'); op.value = o; op.textContent = o; el.appendChild(op); });
  } else {
    el = document.createElement('input'); el.type = type === 'number' ? 'number' : 'text';
  }
  el.setAttribute('data-field', `${listPath}.${idx}.${key}`);
  w.appendChild(el);
  return { w, el };
}

function renderSignalItem(item, idx, updateFn, removeFn) {
  const row = document.createElement('div'); row.className = 'list-item';
  const fields = [['Name', 'name', 'text'], ['Type', 'type', 'select', ['analog', 'digital']],
    ['Protocol ID', 'protocol_id', 'text'], ['Unit', 'unit', 'text'],
    ['Min', 'min', 'number'], ['Max', 'max', 'number'], ['Initial', 'initial', 'text']];
  fields.forEach(([lbl, key, type, opts]) => {
    const { w, el } = makeInp(lbl, key, type, opts, idx, 'signal_list');
    if (type === 'select') { el.value = item[key] ?? ''; el.addEventListener('change', () => updateFn(key, el.value)); }
    else { el.value = item[key] ?? ''; el.addEventListener('input', () => updateFn(key, type === 'number' ? (el.value === '' ? null : parseFloat(el.value)) : el.value)); }
    row.appendChild(w);
  });
  const { w: bW, el: bSel } = makeInp('Behavior', 'behavior', 'select', ['static', 'step', 'ramp', 'sine', 'noise', 'random_walk', 'scripted'], idx, 'signal_list');
  bSel.value = item.behavior ?? 'static';
  bSel.addEventListener('change', () => updateFn('behavior', bSel.value));
  row.appendChild(bW);
  const stepW = document.createElement('div'); stepW.className = 'field-wrapper';
  const stepL = document.createElement('label'); stepL.textContent = 'Step Time'; stepW.appendChild(stepL);
  const stepInp = document.createElement('input'); stepInp.type = 'number'; stepInp.min = 0; stepInp.value = item.step_time_ms ?? '';
  stepInp.setAttribute('data-field', `signal_list.${idx}.step_time_ms`);
  stepInp.addEventListener('input', () => updateFn('step_time_ms', stepInp.value === '' ? null : parseFloat(stepInp.value)));
  stepW.appendChild(stepInp); stepW.appendChild(Object.assign(document.createElement('span'), { textContent: ' ms' })); row.appendChild(stepW);
  const riseW = document.createElement('div'); riseW.className = 'field-wrapper';
  const riseL = document.createElement('label'); riseL.textContent = 'Rise Time'; riseW.appendChild(riseL);
  const riseInp = document.createElement('input'); riseInp.type = 'number'; riseInp.min = 0; riseInp.value = item.rise_time_ms ?? '';
  riseInp.setAttribute('data-field', `signal_list.${idx}.rise_time_ms`);
  riseInp.addEventListener('input', () => updateFn('rise_time_ms', riseInp.value === '' ? null : parseFloat(riseInp.value)));
  riseW.appendChild(riseInp); riseW.appendChild(Object.assign(document.createElement('span'), { textContent: ' ms' })); row.appendChild(riseW);
  const fallW = document.createElement('div'); fallW.className = 'field-wrapper';
  const fallL = document.createElement('label'); fallL.textContent = 'Fall Time'; fallW.appendChild(fallL);
  const fallInp = document.createElement('input'); fallInp.type = 'number'; fallInp.min = 0; fallInp.value = item.fall_time_ms ?? '';
  fallInp.setAttribute('data-field', `signal_list.${idx}.fall_time_ms`);
  fallInp.addEventListener('input', () => updateFn('fall_time_ms', fallInp.value === '' ? null : parseFloat(fallInp.value)));
  fallW.appendChild(fallInp); fallW.appendChild(Object.assign(document.createElement('span'), { textContent: ' ms' })); row.appendChild(fallW);
  function syncVisibility() {
    const b = bSel.value;
    stepW.style.display = b === 'step' ? '' : 'none';
    riseW.style.display = b === 'ramp' ? '' : 'none';
    fallW.style.display = b === 'ramp' ? '' : 'none';
  }
  bSel.addEventListener('change', () => {
    const b = bSel.value;
    if (b !== 'step') updateFn('step_time_ms', null);
    if (b !== 'ramp') { updateFn('rise_time_ms', null); updateFn('fall_time_ms', null); }
    syncVisibility();
  });
  syncVisibility();
  const rm = document.createElement('button'); rm.type = 'button'; rm.textContent = 'Remove'; rm.addEventListener('click', removeFn); row.appendChild(rm);
  return row;
}

function renderScriptItem(item, idx, updateFn, removeFn) {
  const row = document.createElement('div'); row.className = 'list-item';
  [['Time', 'time_ms', 'number', 'ms'], ['Action', 'action', 'text', ''], ['Parameters', 'parameters', 'text', '']].forEach(([lbl, key, type, unit]) => {
    const w = document.createElement('div'); w.className = 'field-wrapper';
    const l = document.createElement('label'); l.textContent = lbl; w.appendChild(l);
    const inp = document.createElement('input'); inp.type = type === 'number' ? 'number' : 'text'; inp.value = item[key] ?? '';
    if (type === 'number') { inp.min = 0; inp.addEventListener('input', () => updateFn(key, inp.value === '' ? null : parseFloat(inp.value))); }
    else inp.addEventListener('input', () => updateFn(key, inp.value));
    inp.setAttribute('data-field', `sequence_script.${idx}.${key}`); w.appendChild(inp);
    if (unit) w.appendChild(Object.assign(document.createElement('span'), { textContent: ' ' + unit }));
    row.appendChild(w);
  });
  const btn = document.createElement('button'); btn.type = 'button'; btn.textContent = 'Remove'; btn.addEventListener('click', removeFn); row.appendChild(btn);
  return row;
}

function renderTriggerItem(item, idx, updateFn, removeFn) {
  const row = document.createElement('div'); row.className = 'list-item';
  [['Time', 'time_ms', 'number', 'ms'], ['Condition', 'condition', 'text', ''], ['Action', 'action', 'text', '']].forEach(([lbl, key, type, unit]) => {
    const w = document.createElement('div'); w.className = 'field-wrapper';
    const l = document.createElement('label'); l.textContent = lbl; w.appendChild(l);
    const inp = document.createElement('input'); inp.type = type === 'number' ? 'number' : 'text'; inp.value = item[key] ?? '';
    if (type === 'number') { inp.min = 0; inp.addEventListener('input', () => updateFn(key, inp.value === '' ? null : parseFloat(inp.value))); }
    else inp.addEventListener('input', () => updateFn(key, inp.value));
    inp.setAttribute('data-field', `event_triggers.${idx}.${key}`); w.appendChild(inp);
    if (unit) w.appendChild(Object.assign(document.createElement('span'), { textContent: ' ' + unit }));
    row.appendChild(w);
  });
  const btn = document.createElement('button'); btn.type = 'button'; btn.textContent = 'Remove'; btn.addEventListener('click', removeFn); row.appendChild(btn);
  return row;
}

export function render(container) {
  const block = createCollapsibleBlock('C. Input Signals & Stimuli', 'block-c', content => {
    content.appendChild(createDynamicList({
      path: 'signal_list', label: 'Signals', addLabel: 'Add Signal',
      defaultItem: { name: '', type: 'analog', protocol_id: '', unit: '', min: null, max: null, initial: null, behavior: 'static' },
      renderItem: renderSignalItem
    }));
    content.appendChild(createDynamicList({
      path: 'sequence_script', label: 'Sequence Script', addLabel: 'Add Step',
      defaultItem: { time_ms: 0, action: '', parameters: '' },
      renderItem: renderScriptItem
    }));
    content.appendChild(createDynamicList({
      path: 'event_triggers', label: 'Event Triggers', addLabel: 'Add Trigger',
      defaultItem: { time_ms: null, condition: '', action: '' },
      renderItem: renderTriggerItem
    }));
    const errDiv = document.createElement('div'); errDiv.className = 'block-errors'; content.appendChild(errDiv);
  });
  container.appendChild(block);
}

