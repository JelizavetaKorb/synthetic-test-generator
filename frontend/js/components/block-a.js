import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';

export function render(container) {
  const block = createCollapsibleBlock('A. Initial Conditions / Environment', 'block-a', content => {
    [
      createField({ label: 'Ambient Temperature', path: 'initial_conditions.ambient_temperature_C', type: 'number', min: -40, max: 85, step: 0.1, unit: '°C', defaultValue: 20 }),
      createField({ label: 'Battery Voltage', path: 'initial_conditions.battery_voltage_V', type: 'number', min: 6.0, max: 16.0, step: 0.1, unit: 'V', defaultValue: 12.6 }),
      createField({ label: 'Engine State', path: 'initial_conditions.engine_state', type: 'select', options: ['OFF', 'IDLE', 'RUNNING'], defaultValue: 'IDLE' }),
      createField({ label: 'Fuel Level', path: 'initial_conditions.fuel_level_percent', type: 'number', min: 0, max: 100, unit: '%', defaultValue: 100 }),
      createField({ label: 'Vehicle Speed', path: 'initial_conditions.vehicle_speed_mps', type: 'number', min: 0, max: 70, step: 0.1, unit: 'm/s', defaultValue: 0 }),
      createField({ label: 'Gear Position', path: 'initial_conditions.gear_position', type: 'select', options: ['P', 'R', 'N', 'D', '1', '2', '3'], defaultValue: 'N' }),
      createField({ label: 'Ignition Key', path: 'initial_conditions.ignition_key', type: 'select', options: ['OFF', 'ON'], defaultValue: 'ON' }),
      createField({ label: 'Ambient Humidity', path: 'initial_conditions.ambient_humidity_pct', type: 'number', min: 0, max: 100, unit: '%', defaultValue: 50 }),
      createField({ label: 'GPS Fix Quality', path: 'initial_conditions.gps_fix_quality', type: 'select', options: ['NONE', 'POOR', 'GOOD', 'EXCELLENT'], defaultValue: 'GOOD' }),
      createField({ label: 'Localization Error', path: 'initial_conditions.localization_error_m', type: 'number', min: 0, max: 10, step: 0.01, unit: 'm', defaultValue: 0.1 }),
    ].forEach(el => content.appendChild(el));
    const errDiv = document.createElement('div');
    errDiv.className = 'block-errors';
    content.appendChild(errDiv);
  });
  container.appendChild(block);
}

