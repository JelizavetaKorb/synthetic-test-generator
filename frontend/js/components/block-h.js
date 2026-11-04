import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';
import { humanizeValue } from '../utils/field-labels.js';

const TYPE_DEFS = {
  fixed: { value: null },
  linear_range: { from: null, to: null, step: null },
  geometric_range: { from: null, to: null, step: null },
  enum_list: { values: [] },
  random_uniform: { from: null, to: null, count: 5 },
  gaussian: { mean: null, std: null, count: 5 },
  latin_hypercube: { from: null, to: null, count: 5 }
};

function subFieldMeta(key) {
  const labels = { value: 'Value', from: 'From', to: 'To', step: 'Step', values: 'Values (comma-sep)', mean: 'Mean', std: 'Std Dev', count: 'Count' };
  const isText = key === 'value' || key === 'values';
  return [labels[key] || key, isText ? 'text' : 'number'];
}

function renderSubFields(row, name, cfg, type) {
  row.querySelectorAll('.sub-field').forEach(el => el.remove());
  Object.keys(TYPE_DEFS[type] || {}).forEach(key => {
    const [lbl, inputType] = subFieldMeta(key);
    const w = document.createElement('div'); w.className = 'field-wrapper sub-field';
    w.appendChild(Object.assign(document.createElement('label'), { textContent: lbl }));
    const inp = document.createElement('input'); inp.type = inputType;
    inp.setAttribute('data-field', `sweep_parameters.${name}.${key}`);
    inp.value = inputType === 'text'
      ? (Array.isArray(cfg[key]) ? cfg[key].join(',') : (cfg[key] || ''))
      : (cfg[key] != null ? cfg[key] : '');
    inp.addEventListener('input', () => {
      const cur = getState('sweep_parameters') || {};
      const val = inputType === 'number' ? (inp.value === '' ? null : parseFloat(inp.value))
        : (key === 'values' ? inp.value.split(',').map(s => s.trim()).filter(Boolean) : inp.value);
      setState('sweep_parameters', { ...cur, [name]: { ...cur[name], [key]: val } });
    });
    w.appendChild(inp); row.appendChild(w);
  });
}

function renderSweepEntry(name, cfg, mapEl) {
  const row = document.createElement('div'); row.className = 'list-item';
  const nameW = document.createElement('div'); nameW.className = 'field-wrapper';
  nameW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Parameter' }));
  const nameI = document.createElement('input'); nameI.type = 'text'; nameI.value = name;
  nameI.setAttribute('data-field', `sweep_parameters.${name}`); nameW.appendChild(nameI); row.appendChild(nameW);
  nameI.addEventListener('change', () => {
    const newName = nameI.value.trim(); if (!newName || newName === name) return;
    const cur = { ...(getState('sweep_parameters') || {}) };
    if (cur[newName]) { nameI.value = name; return; }
    const cfg = cur[name]; delete cur[name]; cur[newName] = cfg;
    setState('sweep_parameters', cur);

    row.querySelectorAll('[data-field]').forEach(el => {
      el.setAttribute('data-field', el.getAttribute('data-field').replace(`sweep_parameters.${name}`, `sweep_parameters.${newName}`));
    });
    name = newName;
  });
  const typeW = document.createElement('div'); typeW.className = 'field-wrapper';
  typeW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Strategy' }));
  const typeSel = document.createElement('select'); typeSel.setAttribute('data-field', `sweep_parameters.${name}.type`);
  Object.keys(TYPE_DEFS).forEach(v => { const o = document.createElement('option'); o.value = v; o.textContent = humanizeValue(v); if (v === cfg.type) o.selected = true; typeSel.appendChild(o); });
  typeSel.addEventListener('change', () => {
    const t = typeSel.value; const cur = getState('sweep_parameters') || {};
    const newCfg = { type: t, ...TYPE_DEFS[t] };
    setState('sweep_parameters', { ...cur, [name]: newCfg });
    renderSubFields(row, name, newCfg, t);
  });
  typeW.appendChild(typeSel); row.appendChild(typeW);
  renderSubFields(row, name, cfg, cfg.type || 'fixed');
  const rm = document.createElement('button'); rm.type = 'button'; rm.textContent = 'Remove';
  rm.addEventListener('click', () => { const cur = { ...(getState('sweep_parameters') || {}) }; delete cur[name]; setState('sweep_parameters', cur); row.remove(); });
  row.appendChild(rm); mapEl.appendChild(row);
}

export function render(container) {
  const block = createCollapsibleBlock('H. Parameter Sweep / Sampling Strategy', 'block-h', content => {
    const mapEl = document.createElement('div'); mapEl.className = 'dynamic-list-items';
    Object.entries(getState('sweep_parameters') || {}).forEach(([n, c]) => renderSweepEntry(n, c, mapEl));
    const addRow = document.createElement('div'); addRow.className = 'sweep-add-row';
    const nameInp = document.createElement('input'); nameInp.type = 'text'; nameInp.placeholder = 'Parameter name (e.g. initial_conditions.battery_voltage_V)';
    const nameErr = document.createElement('span'); nameErr.className = 'sweep-name-error'; nameErr.style.display = 'none';
    const addBtn = document.createElement('button'); addBtn.type = 'button'; addBtn.className = 'btn btn-primary'; addBtn.textContent = 'Add Sweep';
    addBtn.addEventListener('click', () => {
      let name = nameInp.value.trim();
      if (!name) {

        const cur = getState('sweep_parameters') || {};
        let n = 1; while (cur[`param_${n}`]) n++;
        name = `param_${n}`;
      }
      const cur = getState('sweep_parameters') || {};
      if (cur[name]) {
        nameErr.textContent = `"${name}" already exists.`; nameErr.style.display = '';
        nameInp.focus(); return;
      }
      nameErr.style.display = 'none';
      const newCfg = { type: 'fixed', ...TYPE_DEFS.fixed };
      setState('sweep_parameters', { ...cur, [name]: newCfg });
      renderSweepEntry(name, newCfg, mapEl); nameInp.value = '';
    });
    nameInp.addEventListener('input', () => { nameErr.style.display = 'none'; });
    addRow.appendChild(nameInp); addRow.appendChild(addBtn); addRow.appendChild(nameErr);
    content.appendChild(addRow); content.appendChild(mapEl);
    [
      createField({ label: 'Combinatorial Strategy', path: 'combinatorial_strategy', type: 'select', options: ['cartesian','pairwise','random_sample'], defaultValue: 'pairwise' }),
      createField({ label: 'Max Generated Scenarios', path: 'max_generated_scenarios', type: 'number', min: 1, step: 1, defaultValue: 100 }),
    ].forEach(el => content.appendChild(el));
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
