import { createCollapsibleBlock } from '../utils/dom.js';
import { createField, createDynamicList } from '../utils/form.js';

function renderMsgItem(item, idx, updateFn, removeFn) {
  const row = document.createElement('div'); row.className = 'list-item';
  [['ID', 'id', 'text', ''], ['Name', 'name', 'text', ''], ['Period', 'period_ms', 'number', 'ms']].forEach(([lbl, key, type, unit]) => {
    const w = document.createElement('div'); w.className = 'field-wrapper';
    const l = document.createElement('label'); l.textContent = lbl; w.appendChild(l);
    const inp = document.createElement('input'); inp.type = type === 'number' ? 'number' : 'text'; inp.value = item[key] ?? '';
    if (type === 'number') { inp.min = 0; inp.addEventListener('input', () => updateFn(key, inp.value === '' ? null : parseFloat(inp.value))); }
    else inp.addEventListener('input', () => updateFn(key, inp.value));
    inp.setAttribute('data-field', `bus_config.message_defs.${idx}.${key}`);
    if (unit) {
      const ir = document.createElement('div'); ir.className = 'input-row'; ir.appendChild(inp);
      ir.appendChild(Object.assign(document.createElement('span'), { className: 'field-unit', textContent: unit }));
      w.appendChild(ir);
    } else {
      w.appendChild(inp);
    }
    row.appendChild(w);
  });
  const paW = document.createElement('div'); paW.className = 'field-wrapper';
  const paL = document.createElement('label'); paL.textContent = 'Payload Schema'; paW.appendChild(paL);
  const ta = document.createElement('textarea'); ta.value = item.payload_schema ?? '';
  ta.setAttribute('data-field', `bus_config.message_defs.${idx}.payload_schema`);
  ta.addEventListener('input', () => updateFn('payload_schema', ta.value));
  paW.appendChild(ta); row.appendChild(paW);
  const btn = document.createElement('button'); btn.type = 'button'; btn.textContent = 'Remove'; btn.addEventListener('click', removeFn); row.appendChild(btn);
  return row;
}

export function render(container) {
  const block = createCollapsibleBlock('D. Bus & Network Config', 'block-d', content => {
    [
      createField({ label: 'Bus Name', path: 'bus_config.bus_name', type: 'text', defaultValue: '' }),
      createField({ label: 'Bus Type', path: 'bus_config.bus_type', type: 'select', options: ['CAN', 'CAN-FD', 'LIN', 'FlexRay', 'Ethernet'], defaultValue: 'CAN' }),
      createField({ label: 'Bitrate', path: 'bus_config.bitrate_kbps', type: 'number', min: 1, unit: 'kbps', defaultValue: 500 }),
      createField({ label: 'Bus Load', path: 'bus_config.bus_load_pct', type: 'number', min: 0, max: 100, unit: '%', defaultValue: 10 }),
      createField({ label: 'Message Jitter', path: 'bus_config.message_jitter_ms', type: 'number', min: 0, max: 100, unit: 'ms', defaultValue: 0 }),
    ].forEach(el => content.appendChild(el));
    content.appendChild(createDynamicList({
      path: 'bus_config.message_defs', label: 'Message Definitions', addLabel: 'Add Message',
      defaultItem: { id: '', name: '', period_ms: null, payload_schema: '' },
      renderItem: renderMsgItem
    }));
    const errDiv = document.createElement('div'); errDiv.className = 'block-errors'; content.appendChild(errDiv);
  });
  container.appendChild(block);
}
