import { createElement } from '../utils/dom.js';
import { getFullState, subscribe } from '../state.js';

const HIDDEN_KEYS = new Set([
  'scenario_id', 'created_at', 'version', 'ecu_catalog_entry',
  'recommended_assertions', 'profile'
]);

const SECTION_TITLES = {
  initial_conditions: 'Initial Conditions',
  timing: 'Timing & Scheduling',
  safety: 'Safety Metadata',
  reporting: 'Reporting & Logging',
  execution: 'Execution Settings',
  advanced: 'Advanced Options',
  environmental_perturbations: 'Environmental Perturbations',
  security: 'Security / Cyberattack',
  unit_test_params: 'Unit Test Parameters',
  integration_test_params: 'Integration Test Parameters',
  stress_endurance_params: 'Stress & Endurance Parameters'
};

function isEmpty(v) {
  if (v === null || v === undefined || v === '') return true;
  if (Array.isArray(v) && v.length === 0) return true;
  if (typeof v === 'object' && !Array.isArray(v) && Object.keys(v).length === 0) return true;
  return false;
}

function fmtPrimitive(v) {
  if (v === null || v === undefined) return '(none)';
  if (typeof v === 'boolean') return v ? 'Yes' : 'No';
  return String(v);
}

function renderKv(parent, label, value) {
  const row = createElement('div', { class: 'preview-kv' });
  row.appendChild(createElement('span', { class: 'preview-key' }, [label + ': ']));
  row.appendChild(createElement('span', { class: 'preview-val' }, [fmtPrimitive(value)]));
  parent.appendChild(row);
}

function renderObject(parent, obj) {
  Object.entries(obj).forEach(([k, v]) => {
    if (isEmpty(v)) return;
    if (Array.isArray(v)) {
      const sub = createElement('div', { class: 'preview-sub' });
      sub.appendChild(createElement('div', { class: 'preview-subkey' }, [k + ` (${v.length})`]));
      v.forEach((item, i) => {
        if (typeof item === 'object' && item !== null) {
          const card = createElement('div', { class: 'preview-card' });
          card.appendChild(createElement('div', { class: 'preview-cardtitle' }, [`#${i + 1}`]));
          renderObject(card, item);
          sub.appendChild(card);
        } else {
          sub.appendChild(createElement('div', { class: 'preview-kv' }, [`• ${fmtPrimitive(item)}`]));
        }
      });
      parent.appendChild(sub);
    } else if (typeof v === 'object') {
      const sub = createElement('div', { class: 'preview-sub' });
      sub.appendChild(createElement('div', { class: 'preview-subkey' }, [k]));
      renderObject(sub, v);
      parent.appendChild(sub);
    } else {
      renderKv(parent, k, v);
    }
  });
}

function renderSummary(target) {
  target.innerHTML = '';
  const s = getFullState();

  const top = createElement('section', { class: 'preview-section' });
  top.appendChild(createElement('h3', { class: 'preview-section-title' }, ['Scenario']));
  ['name', 'description', 'author', 'project', 'priority', 'target_execution', 'test_type', 'profile']
    .forEach(k => { if (!isEmpty(s[k])) renderKv(top, k, s[k]); });
  if (!isEmpty(s.tags)) renderKv(top, 'tags', s.tags.join(', '));
  if (!isEmpty(s.ecu_id)) renderKv(top, 'ecu_id', s.ecu_id);
  if (!isEmpty(s.ecu_mode)) renderKv(top, 'ecu_mode', s.ecu_mode);
  if (!isEmpty(s.hw_interface)) renderKv(top, 'hw_interface', s.hw_interface.join(', '));
  target.appendChild(top);

  Object.entries(s).forEach(([key, val]) => {
    if (HIDDEN_KEYS.has(key)) return;
    if (typeof val !== 'object' || val === null || Array.isArray(val)) return;
    if (isEmpty(val)) return;
    if (key === 'sweep_parameters' && isEmpty(val)) return;
    const section = createElement('section', { class: 'preview-section' });
    section.appendChild(createElement('h3', { class: 'preview-section-title' }, [SECTION_TITLES[key] || key]));
    renderObject(section, val);
    target.appendChild(section);
  });

  const arrays = [
    ['signal_list', 'Input Signals'],
    ['sequence_script', 'Sequence Script'],
    ['event_triggers', 'Event Triggers'],
    ['faults', 'Fault Injections'],
    ['assertions', 'Assertions']
  ];
  arrays.forEach(([k, title]) => {
    const arr = s[k] || [];
    if (!arr.length) return;
    const section = createElement('section', { class: 'preview-section' });
    section.appendChild(createElement('h3', { class: 'preview-section-title' }, [`${title} (${arr.length})`]));
    arr.forEach((item, i) => {
      const card = createElement('div', { class: 'preview-card' });
      card.appendChild(createElement('div', { class: 'preview-cardtitle' }, [`#${i + 1}`]));
      renderObject(card, item);
      section.appendChild(card);
    });
    target.appendChild(section);
  });
}

export function render(container) {
  const panel = createElement('div', { id: 'preview-panel', class: 'preview-panel hidden' });

  const header = createElement('div', { class: 'preview-header' });
  header.appendChild(createElement('h2', { class: 'preview-title' }, ['Scenario Preview']));
  const closeBtn = createElement('button', {
    class: 'preview-close',
    type: 'button',
    'aria-label': 'Close preview',
    title: 'Close preview',
  }, ['\u00D7']);
  closeBtn.addEventListener('click', hide);
  header.appendChild(closeBtn);
  panel.appendChild(header);

  const body = createElement('div', { class: 'preview-body' });
  panel.appendChild(body);
  container.appendChild(panel);

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !panel.classList.contains('hidden')) hide();
  });

  subscribe(() => {
    if (!panel.classList.contains('hidden')) renderSummary(body);
  });
}

function hide() {
  const panel = document.getElementById('preview-panel');
  if (panel) panel.classList.add('hidden');
}

export function toggle() {
  const panel = document.getElementById('preview-panel');
  if (!panel) return;
  if (panel.classList.contains('hidden')) {
    panel.classList.remove('hidden');
    renderSummary(panel.querySelector('.preview-body'));
  } else {
    panel.classList.add('hidden');
  }
}
