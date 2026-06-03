import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState, subscribe } from '../state.js';

function renderSignalsCheckboxes(cbGroup) {
  cbGroup.innerHTML = '';
  const signals = (getState('signal_list') || []).map(s => s.name).filter(Boolean);
  if (!signals.length) {
    cbGroup.appendChild(Object.assign(document.createElement('span'), { textContent: 'No signals defined yet' })); return;
  }
  const current = getState('reporting.signals_to_record') || [];
  signals.forEach(name => {
    const lbl = document.createElement('label'); lbl.className = 'checkbox-item';
    const cb = document.createElement('input'); cb.type = 'checkbox'; cb.value = name;
    cb.checked = current.includes(name); cb.setAttribute('data-field', 'reporting.signals_to_record');
    cb.addEventListener('change', () => {
      const all = Array.from(cbGroup.querySelectorAll('input[type="checkbox"]')).filter(c => c.checked).map(c => c.value);
      setState('reporting.signals_to_record', all);
    });
    lbl.appendChild(cb); lbl.appendChild(document.createTextNode(' ' + name)); cbGroup.appendChild(lbl);
  });
}

export function render(container) {
  let cbGroupRef = null;
  const block = createCollapsibleBlock('I. Reporting & Logging', 'block-i', content => {
    [
      createField({ label: 'Log Level', path: 'reporting.log_level', type: 'select', options: ['ERROR','WARN','INFO','DEBUG'], defaultValue: 'INFO' }),
      createField({ label: 'Telemetry Frequency', path: 'reporting.telemetry_frequency_hz', type: 'number', min: 1, unit: 'Hz', defaultValue: 10 }),
      createField({ label: 'Trace Capture', path: 'reporting.trace_capture', type: 'checkbox', defaultValue: false }),
      createField({ label: 'Report Format', path: 'reporting.report_format', type: 'select', options: ['HTML','JSON','CSV','PDF'], defaultValue: 'JSON' }),
      createField({ label: 'Save Artifacts', path: 'reporting.save_artifacts', type: 'checkbox', defaultValue: false }),
    ].forEach(el => content.appendChild(el));
    const sigW = document.createElement('div'); sigW.className = 'field-wrapper';
    sigW.appendChild(Object.assign(document.createElement('label'), { textContent: 'Signals to Record' }));
    const cbGroup = document.createElement('div'); cbGroup.className = 'checkbox-group-options';
    cbGroupRef = cbGroup;
    renderSignalsCheckboxes(cbGroup); sigW.appendChild(cbGroup); content.appendChild(sigW);
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
  let prevSigs = '';
  subscribe(s => {
    const names = (s.signal_list || []).map(x => x.name).join(',');
    if (names === prevSigs) return;
    prevSigs = names;
    const sigNames = (s.signal_list || []).map(x => x.name).filter(Boolean);
    const cur = (s.reporting || {}).signals_to_record || [];
    const cleaned = cur.filter(n => sigNames.includes(n));
    if (cleaned.length !== cur.length) setState('reporting.signals_to_record', cleaned);
    if (cbGroupRef) renderSignalsCheckboxes(cbGroupRef);
  });
}
