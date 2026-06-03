import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';

function unitTestFields(content) {
  [
    createField({ label: 'Measurement Window', path: 'unit_test_params.measurement_window_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 0 }),
    createField({ label: 'Steady-state Tolerance', path: 'unit_test_params.steady_state_tolerance', type: 'number', step: 0.01, defaultValue: 0 }),
    createField({ label: 'Overshoot Limit', path: 'unit_test_params.overshoot_limit', type: 'number', step: 0.01, defaultValue: 0 }),
  ].forEach(el => content.appendChild(el));
}

function integrationFields(content) {
  [
    createField({ label: 'Connectivity Topology', path: 'integration_test_params.connectivity_topology', type: 'text', defaultValue: '' }),
    createField({ label: 'Gateway Behaviour', path: 'integration_test_params.gateway_behaviour', type: 'text', defaultValue: '' }),
  ].forEach(el => content.appendChild(el));
}

function stressFields(content) {
  [
    createField({ label: 'Duration', path: 'stress_endurance_params.duration_hours', type: 'number', min: 0, step: 0.1, unit: 'h', defaultValue: 0 }),
    createField({ label: 'High Temperature Profile', path: 'stress_endurance_params.high_temperature_profile', type: 'text', defaultValue: '' }),
    createField({ label: 'Battery Drain Model', path: 'stress_endurance_params.battery_drain_model', type: 'text', defaultValue: '' }),
    createField({ label: 'Background Load Pattern', path: 'stress_endurance_params.background_load_pattern', type: 'text', defaultValue: '' }),
  ].forEach(el => content.appendChild(el));
}

function securityFields(content) {
  if (!getState('security')) setState('security', { attack_type: 'spoofing', attack_vector: '', privilege_level: '', start_time_ms: 0, duration_ms: 0 });
  [
    createField({ label: 'Attack Type', path: 'security.attack_type', type: 'select', options: ['spoofing', 'replay', 'DoS', 'fuzzing', 'man_in_middle'], defaultValue: 'spoofing' }),
    createField({ label: 'Attack Vector', path: 'security.attack_vector', type: 'text', defaultValue: '' }),
    createField({ label: 'Privilege Level', path: 'security.privilege_level', type: 'text', defaultValue: '' }),
    createField({ label: 'Start Time', path: 'security.start_time_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 0 }),
    createField({ label: 'Duration', path: 'security.duration_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 0 }),
  ].forEach(el => content.appendChild(el));
}

const RENDERERS = {
  unit_test: ['Unit Test Parameters', unitTestFields],
  integration_test: ['Integration Test Parameters', integrationFields],
  stress_endurance: ['Stress / Endurance Parameters', stressFields],
  security_cyberattack: ['Security / Cyberattack Parameters', securityFields]
};

export function render(container) {
  const tt = getState('test_type');
  const entry = RENDERERS[tt];
  if (!entry) return;
  const [title, fn] = entry;
  const block = createCollapsibleBlock(title, 'block-type-params', content => {
    fn(content);
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
