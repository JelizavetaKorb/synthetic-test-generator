import { createCollapsibleBlock } from '../utils/dom.js';
import { createField, createDynamicList } from '../utils/form.js';
import { getState, setState, subscribe } from '../state.js';
import { humanizeValue } from '../utils/field-labels.js';

function buildTargetOptions(sel, cur) {
  sel.innerHTML = '';
  const sigs = getState('signal_list') || [];
  const msgs = getState('bus_config.message_defs') || [];
  if (!sigs.length && !msgs.length) {
    const o = document.createElement('option'); o.value = ''; o.textContent = 'Define signals or messages first'; o.disabled = true; sel.appendChild(o); return;
  }
  const blank = document.createElement('option'); blank.value = ''; blank.textContent = '-- select target --'; sel.appendChild(blank);
  if (sigs.length) {
    const g = document.createElement('optgroup'); g.label = 'Signals';
    sigs.forEach(s => { const o = document.createElement('option'); o.value = s.name; o.textContent = s.name; g.appendChild(o); });
    sel.appendChild(g);
  }
  if (msgs.length) {
    const g = document.createElement('optgroup'); g.label = 'Messages';
    msgs.forEach(m => { const o = document.createElement('option'); o.value = m.name; o.textContent = m.name; g.appendChild(o); });
    sel.appendChild(g);
  }
  sel.value = cur || '';
}

function renderFaultItem(item, idx, updateFn, removeFn) {
  if (!item.fault_id) {
    const id = 'flt-' + crypto.randomUUID().slice(0, 8);
    updateFn('fault_id', id);
    item = { ...item, fault_id: id };
  }
  const row = document.createElement('div'); row.className = 'list-item';
  const mkF = (lbl, key, type, attrs = {}) => {
    const w = document.createElement('div'); w.className = 'field-wrapper';
    w.appendChild(Object.assign(document.createElement('label'), { textContent: lbl }));
    const inp = document.createElement('input'); inp.type = type;
    Object.entries(attrs).forEach(([k, v]) => { inp[k] = v; });
    inp.value = item[key] ?? '';
    inp.setAttribute('data-field', `faults.${idx}.${key}`);
    inp.addEventListener('input', () => updateFn(key, type === 'number' ? (inp.value === '' ? null : parseFloat(inp.value)) : inp.value));
    w.appendChild(inp); return w;
  };
  row.appendChild(mkF('Fault ID', 'fault_id', 'text'));
  const typeW = document.createElement('div'); typeW.className = 'field-wrapper';
  typeW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Type' }));
  const typeSel = document.createElement('select');
  ['message_loss','tx_error','rx_error','bit_flip','bus_noise','short_circuit','sensor_spoof','sensor_dropout','actuator_stuck','soft_reboot','high_latency','packet_delay','packet_duplication'].forEach(v => {
    const o = document.createElement('option'); o.value = v; o.textContent = humanizeValue(v); if (v === item.type) o.selected = true; typeSel.appendChild(o);
  });
  typeSel.setAttribute('data-field', `faults.${idx}.type`); typeSel.addEventListener('change', () => updateFn('type', typeSel.value));
  typeW.appendChild(typeSel); row.appendChild(typeW);
  const tgtW = document.createElement('div'); tgtW.className = 'field-wrapper';
  tgtW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Target' }));
  const tgtSel = document.createElement('select'); tgtSel.className = 'fault-target-sel';
  tgtSel.setAttribute('data-field', `faults.${idx}.target`);
  buildTargetOptions(tgtSel, item.target); tgtSel.addEventListener('change', () => updateFn('target', tgtSel.value));
  tgtW.appendChild(tgtSel); row.appendChild(tgtW);
  [['Start Time', 'start_time_ms', 'ms', 0], ['Duration', 'duration_ms', 'ms', 0], ['Intensity', 'intensity', '', 0]].forEach(([lbl, key, unit, min]) => {
    const w = mkF(lbl, key, 'number', key === 'intensity' ? { min: 0, max: 1, step: 0.01 } : { min });
    if (unit) w.appendChild(Object.assign(document.createElement('span'), { textContent: ' ' + unit }));
    row.appendChild(w);
  });
  const patW = document.createElement('div'); patW.className = 'field-wrapper';
  patW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Pattern' }));
  const patSel = document.createElement('select');
  ['continuous','burst','periodic','random'].forEach(v => { const o = document.createElement('option'); o.value = v; o.textContent = humanizeValue(v); if (v === item.pattern) o.selected = true; patSel.appendChild(o); });
  patSel.setAttribute('data-field', `faults.${idx}.pattern`); patSel.addEventListener('change', () => updateFn('pattern', patSel.value));
  patW.appendChild(patSel); row.appendChild(patW);
  const repW = document.createElement('div'); repW.className = 'field-wrapper';
  repW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Repeat' }));
  const repI = document.createElement('input'); repI.type = 'number'; repI.min = 0; repI.step = 1; repI.value = item.repeat ?? 0;
  repI.setAttribute('data-field', `faults.${idx}.repeat`);
  repI.addEventListener('input', () => updateFn('repeat', repI.value === '' ? 0 : parseInt(repI.value, 10)));
  repW.appendChild(repI); row.appendChild(repW);
  const rm = document.createElement('button'); rm.type = 'button'; rm.textContent = 'Remove'; rm.addEventListener('click', removeFn); row.appendChild(rm);
  return row;
}

export function render(container) {
  const block = createCollapsibleBlock('E. Fault Injections / Perturbations', 'block-e', content => {
    content.appendChild(createDynamicList({
      path: 'faults', label: 'Fault Injections', addLabel: 'Add Fault',
      defaultItem: { fault_id: '', type: 'message_loss', target: '', start_time_ms: 0, duration_ms: 1000, intensity: 0.5, pattern: 'continuous', repeat: 0 },
      renderItem: renderFaultItem
    }));
    const env = document.createElement('div'); env.className = 'sub-section';
    env.appendChild(Object.assign(document.createElement('div'), { className: 'sub-section-title', textContent: 'Environmental Perturbations' }));
    env.appendChild(createField({ label: 'EMI Level', path: 'environmental_perturbations.EMI_level', type: 'select', options: ['none','low','medium','high'], defaultValue: 'none' }));
    ['start_C','end_C','rate_C_per_min'].forEach(key => {
      const labels = { start_C: 'Temp Ramp Start', end_C: 'Temp Ramp End', rate_C_per_min: 'Temp Ramp Rate' };
      const units = { start_C: '°C', end_C: '°C', rate_C_per_min: '°C/min' };
      const w = document.createElement('div'); w.className = 'field-wrapper';
      w.appendChild(Object.assign(document.createElement('label'), { textContent: labels[key] }));
      const inp = document.createElement('input'); inp.type = 'number';
      inp.setAttribute('data-field', `environmental_perturbations.temperature_ramp.${key}`);
      const ramp = getState('environmental_perturbations.temperature_ramp');
      inp.value = ramp ? (ramp[key] ?? '') : '';
      inp.addEventListener('input', () => {
        const val = inp.value === '' ? null : parseFloat(inp.value);
        const r = getState('environmental_perturbations.temperature_ramp') || {};
        const u = { ...r, [key]: val };
        setState('environmental_perturbations.temperature_ramp', (u.start_C == null && u.end_C == null && u.rate_C_per_min == null) ? null : u);
      });
      w.appendChild(inp); w.appendChild(Object.assign(document.createElement('span'), { textContent: ' ' + units[key] })); env.appendChild(w);
    });
    content.appendChild(env);
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
  let prevSig = '', prevMsg = '';
  subscribe(s => {
    const sn = (s.signal_list || []).map(x => x.name).join(',');
    const mn = ((s.bus_config || {}).message_defs || []).map(x => x.name).join(',');
    if (sn === prevSig && mn === prevMsg) return;
    prevSig = sn; prevMsg = mn;
    block.querySelectorAll('.fault-target-sel').forEach(sel => buildTargetOptions(sel, sel.value));
  });
}
