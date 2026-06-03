import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';

const FALLBACK_INTERFACES = ['CAN', 'CAN-FD', 'LIN', 'FlexRay', 'Ethernet', 'UART', 'SPI', 'I2C'];
const FALLBACK_MODES = ['NORMAL', 'DIAGNOSTIC', 'SAFE', 'CALIBRATION'];

export function render(container) {
  const block = createCollapsibleBlock('B. ECU Selection & HW Config', 'block-b', content => {
    content.appendChild(createField({ label: 'ECU ID', path: 'ecu_id', type: 'text', defaultValue: '' }));
    content.appendChild(createField({ label: 'Firmware Version', path: 'ecu_firmware_version', type: 'text', defaultValue: '' }));

    const modeWrapper = document.createElement('div');
    modeWrapper.className = 'field-wrapper';
    const modeLbl = document.createElement('label');
    modeLbl.textContent = 'ECU Mode';
    const modeSel = document.createElement('select');
    modeSel.setAttribute('data-field', 'ecu_mode');
    modeSel.addEventListener('change', () => setState('ecu_mode', modeSel.value));
    modeWrapper.appendChild(modeLbl);
    modeWrapper.appendChild(modeSel);
    content.appendChild(modeWrapper);

    const cur = getState('ecu_mode');
    FALLBACK_MODES.forEach(m => {
      const o = document.createElement('option');
      o.value = m; o.textContent = m;
      if (m === cur) o.selected = true;
      modeSel.appendChild(o);
    });

    content.appendChild(createField({ label: 'Power State', path: 'ecu_power_state', type: 'select', options: ['ON', 'OFF', 'SLEEP'], defaultValue: 'ON' }));
    content.appendChild(createField({ label: 'ECU Temperature', path: 'ecu_temperature_C', type: 'number', min: -40, max: 125, step: 0.1, unit: '°C', defaultValue: 25 }));

    const hwWrapper = document.createElement('div');
    hwWrapper.className = 'field-wrapper checkbox-group';
    const hwLbl = document.createElement('label');
    hwLbl.textContent = 'HW Interfaces';
    const hwGroup = document.createElement('div');
    hwGroup.className = 'checkbox-group-options';
    hwWrapper.appendChild(hwLbl);
    hwWrapper.appendChild(hwGroup);
    content.appendChild(hwWrapper);

    const curHw = getState('hw_interface') || [];
    FALLBACK_INTERFACES.forEach(opt => {
      const lbl = document.createElement('label');
      lbl.className = 'checkbox-item';
      const cb = document.createElement('input');
      cb.type = 'checkbox'; cb.value = opt; cb.checked = curHw.includes(opt);
      cb.setAttribute('data-field', 'hw_interface');
      cb.addEventListener('change', () => {
        setState('hw_interface', Array.from(hwGroup.querySelectorAll('input:checked')).map(c => c.value));
      });
      lbl.appendChild(cb);
      lbl.appendChild(document.createTextNode(' ' + opt));
      hwGroup.appendChild(lbl);
    });

    content.appendChild(createField({ label: 'Diagnostic Session', path: 'diagnostic_session', type: 'select', options: ['NONE', 'DEFAULT', 'EXTENDED'], defaultValue: 'NONE' }));
  });
  container.appendChild(block);
}

