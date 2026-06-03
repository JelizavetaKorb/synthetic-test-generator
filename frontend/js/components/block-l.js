import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';

const SECURITY_DEFAULTS = { attack_type: 'spoofing', attack_vector: '', privilege_level: 'none', start_time_ms: 0, duration_ms: 1000 };

function isSecurityProfile() {
  return /cyber|security|attack/i.test(getState('profile') || '');
}

function renderSecurityFields(fieldsEl) {
  fieldsEl.innerHTML = '';
  [
    createField({ label: 'Attack Type', path: 'security.attack_type', type: 'select', options: ['spoofing','replay','DoS','fuzzing','man_in_middle'], defaultValue: 'spoofing' }),
    createField({ label: 'Attack Vector', path: 'security.attack_vector', type: 'text', defaultValue: '' }),
    createField({ label: 'Privilege Level', path: 'security.privilege_level', type: 'select', options: ['none','user','root','physical'], defaultValue: 'none' }),
    createField({ label: 'Start Time', path: 'security.start_time_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 0 }),
    createField({ label: 'Duration', path: 'security.duration_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 1000 }),
  ].forEach(el => fieldsEl.appendChild(el));
}

export function render(container) {
  const block = createCollapsibleBlock('L. Security / Cyberattack Test', 'block-l', content => {
    const toggleW = document.createElement('div'); toggleW.className = 'field-wrapper';
    const toggleLbl = document.createElement('label'); toggleLbl.className = 'checkbox-item';
    const toggleCb = document.createElement('input'); toggleCb.type = 'checkbox';
    toggleCb.setAttribute('data-field', 'security');
    const fieldsEl = document.createElement('div');
    function setEnabled(enabled) {
      toggleCb.checked = enabled;
      fieldsEl.style.display = enabled ? '' : 'none';
      if (enabled && getState('security') === null) setState('security', { ...SECURITY_DEFAULTS });
      if (!enabled) setState('security', null);
    }
    toggleCb.addEventListener('change', () => setEnabled(toggleCb.checked));
    toggleLbl.appendChild(toggleCb); toggleLbl.appendChild(document.createTextNode(' Enable Security Testing'));
    toggleW.appendChild(toggleLbl); content.appendChild(toggleW);
    renderSecurityFields(fieldsEl);
    setEnabled(getState('security') !== null || isSecurityProfile());
    content.appendChild(fieldsEl);
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
