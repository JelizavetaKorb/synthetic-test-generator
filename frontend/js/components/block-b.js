import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';

const FALLBACK_INTERFACES = ['CAN', 'CAN-FD', 'LIN', 'FlexRay', 'Ethernet', 'UART', 'SPI', 'I2C'];
const FALLBACK_MODES = ['NORMAL', 'DIAGNOSTIC', 'SAFE', 'CALIBRATION'];

export function render(container) {
  const block = createCollapsibleBlock('B. ECU Selection & HW Config', 'block-b', content => {
    // ECU ID is normally driven by the header dropdown. If the catalog API
    // is unavailable (e.g. backend not redeployed), fall back to manual entry
    // so the user is not locked out.
    const catalogDown = !!getState('ecu_catalog_unavailable');
    const ecuWrapper = document.createElement('div');
    ecuWrapper.className = 'field-wrapper';
    const ecuLbl = document.createElement('label');
    ecuLbl.textContent = 'ECU ID *';
    const ecuInput = document.createElement('input');
    ecuInput.type = 'text';
    ecuInput.setAttribute('data-field', 'ecu_id');
    ecuInput.value = getState('ecu_id') || '';
    if (catalogDown) {
      ecuInput.placeholder = 'Catalog unavailable. Type the ECU ID manually.';
      ecuInput.title = 'ECU catalog API is offline; enter the ID by hand';
      ecuInput.addEventListener('input', () => setState('ecu_id', ecuInput.value));
    } else {
      ecuInput.readOnly = true;
      ecuInput.placeholder = 'Select an ECU in the header above';
      ecuInput.title = 'Selected in the header ECU dropdown';
    }
    ecuWrapper.appendChild(ecuLbl);
    ecuWrapper.appendChild(ecuInput);
    content.appendChild(ecuWrapper);

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

