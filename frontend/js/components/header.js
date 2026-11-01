import { createElement } from '../utils/dom.js';
import { BACKEND_TO_STATE } from '../utils/profile-map.js';
import { getEcuCatalog, getProfiles } from '../api.js';
import { resetState, resetSetupFields, resetTestTypeParams, getFullState, setState, setLockedFields, clearLockedFields } from '../state.js';
import { clearIssues } from './validation-panel.js';
import { hideResults } from './results-panel.js';

let profilesCache = [];

// fetches the ECU catalog and populates the select, with retry on click after failure
function loadEcuCatalog(selectEl) {

  const loadingOpt = document.createElement('option');
  loadingOpt.disabled = true;
  loadingOpt.value = '';
  loadingOpt.textContent = 'Loading ECU catalog';
  selectEl.appendChild(loadingOpt);
  selectEl.value = '';

  getEcuCatalog()
    .then(ecus => {
      loadingOpt.remove();
      if (!ecus || ecus.length === 0) {
        const opt = document.createElement('option');
        opt.disabled = true;
        opt.textContent = 'No ECUs in catalog';
        selectEl.appendChild(opt);
        return;
      }
      ecus.forEach(ecu => {
        const opt = document.createElement('option');
        opt.value = ecu.ecuId;
        opt.textContent = `${ecu.ecuId} \u00B7 ${ecu.ecuName}`;
        opt.dataset.entry = JSON.stringify(ecu);
        selectEl.appendChild(opt);
      });
      const current = getFullState().ecu_id;
      if (current && ecus.some(e => e.ecuId === current)) {
        selectEl.value = current;
        handleEcuChange(selectEl);
    // auto-select when there is exactly one ECU in the catalog
      } else if (ecus.length === 1) {

        selectEl.value = ecus[0].ecuId;
        handleEcuChange(selectEl);
      }
    })
    .catch(err => {
      loadingOpt.remove();
      const reason = err && err.isNetworkError
        ? 'cannot reach the API server'
        : (err && err.status ? `server returned HTTP ${err.status}` : 'unknown error');
      const opt = document.createElement('option');
      opt.disabled = true;
      opt.textContent = `ECU catalog unavailable (${reason}). Type the ECU ID in block B, or click to retry.`;
      selectEl.appendChild(opt);

      setState('ecu_catalog_unavailable', true);
      document.dispatchEvent(new CustomEvent('rerender-editor'));

      const retry = () => {
        selectEl.removeEventListener('mousedown', retry);
        Array.from(selectEl.querySelectorAll('option')).forEach(o => {
          if (o.value !== '') o.remove();
        });
        setState('ecu_catalog_unavailable', false);
        loadEcuCatalog(selectEl);
      };
      selectEl.addEventListener('mousedown', retry, { once: true });
    });
}

// updates state with the selected ECU id and catalog entry
function handleEcuChange(selectEl) {
  const ecuId = selectEl.value;
  setState('ecu_id', ecuId);
  const selected = selectEl.options[selectEl.selectedIndex];
  if (selected && selected.dataset.entry) {
    setState('ecu_catalog_entry', JSON.parse(selected.dataset.entry));
  } else {
    setState('ecu_catalog_entry', null);
  }
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

function handleTestTypeChange(selectEl) {
  resetTestTypeParams();
  setState('test_type', selectEl.value);
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

// fetches available profiles and populates the select, with retry on click after failure
function loadProfiles(selectEl) {
  const loadingOpt = document.createElement('option');
  loadingOpt.disabled = true;
  loadingOpt.textContent = 'Loading profiles';
  selectEl.appendChild(loadingOpt);

  getProfiles()
    .then(profiles => {
      loadingOpt.remove();
      profilesCache = profiles || [];
      profilesCache.forEach(p => {
        const opt = document.createElement('option');
        opt.value = p.profileId;
        opt.textContent = p.name;
        selectEl.appendChild(opt);
      });
    })
    .catch(err => {
      loadingOpt.remove();
      const reason = err && err.isNetworkError
        ? 'cannot reach the API server'
        : (err && err.status ? `server returned HTTP ${err.status}` : 'unknown error');
      const opt = document.createElement('option');
      opt.disabled = true;
      opt.textContent = `Profiles unavailable (${reason}). Click to retry.`;
      selectEl.appendChild(opt);
      const retry = () => {
        selectEl.removeEventListener('mousedown', retry);
        Array.from(selectEl.querySelectorAll('option')).forEach(o => {
          if (o.value !== '') o.remove();
        });
        loadProfiles(selectEl);
      };
      selectEl.addEventListener('mousedown', retry, { once: true });
    });
}

// applies profile defaults, locks profile-locked fields and stores recommended assertions
function handleProfileChange(selectEl) {
  const id = selectEl.value;
  const profile = profilesCache.find(p => p.profileId === id);
  if (id && !profile) return;
  clearLockedFields();
  resetSetupFields();
  if (!profile) {
    document.dispatchEvent(new CustomEvent('rerender-editor'));
    return;
  }
  Object.entries(profile.defaultParameters || {}).forEach(([key, val]) => {
    const path = BACKEND_TO_STATE[key];
    if (path) setState(path, val);
  });
  setLockedFields(
    (profile.lockedParameters || []).map(k => BACKEND_TO_STATE[k]).filter(Boolean)
  );
  setState('recommended_assertions', profile.recommendedAssertions || []);
  setState('profile', profile.name || '');
  syncEcuSelect();
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

// keeps the header ECU dropdown and catalog entry in line with an ECU set by the profile
function syncEcuSelect() {
  const ecuSelect = document.getElementById('ecu-select');
  const ecuId = getFullState().ecu_id;
  if (!ecuSelect || !ecuId || ecuSelect.value === ecuId) return;
  if (!Array.from(ecuSelect.options).some(o => o.value === ecuId)) return;
  ecuSelect.value = ecuId;
  handleEcuChange(ecuSelect);
}

function handleReset(ecuSelect, testTypeSelect, profileSelect) {
  resetState();
  ecuSelect.value = '';
  testTypeSelect.value = '';
  profileSelect.value = '';
  clearLockedFields();
  clearIssues();
  hideResults();
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

export function render(container) {
  const header = createElement('header', { class: 'app-header' });
  const title = createElement('h1', { class: 'app-title' }, ['ECU Test Scenario Builder']);

  const ecuLabel = createElement('label', { for: 'ecu-select' }, ['ECU:']);
  const ecuSelect = createElement('select', { id: 'ecu-select', class: 'header-select' });
  ecuSelect.appendChild(createElement('option', { value: '' }, ['Select ECU']));
  ecuSelect.addEventListener('change', () => handleEcuChange(ecuSelect));
  loadEcuCatalog(ecuSelect);

  const testTypeLabel = createElement('label', { for: 'test-type-select' }, ['Test Type:']);
  const testTypeSelect = createElement('select', { id: 'test-type-select', class: 'header-select' });
  [
    ['', 'Select Test Type'],
    ['unit_test', 'Unit Test'],
    ['integration_test', 'Integration Test'],
    ['regression_sweep', 'Regression Sweep'],
    ['stress_endurance', 'Stress & Endurance'],
    ['fault_injection', 'Fault Injection'],
    ['safety_scenario', 'Safety Scenario'],
    ['security_cyberattack', 'Security / Cyberattack'],
  ].forEach(([val, label]) => {
    testTypeSelect.appendChild(createElement('option', { value: val }, [label]));
  });
  testTypeSelect.addEventListener('change', () => handleTestTypeChange(testTypeSelect));

  const profileLabel = createElement('label', { for: 'profile-select' }, ['Profile:']);
  const profileSelect = createElement('select', { id: 'profile-select', class: 'header-select' });
  profileSelect.appendChild(createElement('option', { value: '' }, ['No profile']));
  profileSelect.addEventListener('change', () => handleProfileChange(profileSelect));
  loadProfiles(profileSelect);

  const resetBtn = createElement('button', { class: 'btn' }, ['Reset']);
  resetBtn.addEventListener('click', () => handleReset(ecuSelect, testTypeSelect, profileSelect));

  const controls = createElement('div', { class: 'header-controls' });

  // groups a label and select into a single flex div so they never wrap apart
  function makePair(label, select) {
    const pair = createElement('div', { class: 'header-control-pair' });
    pair.appendChild(label);
    pair.appendChild(select);
    return pair;
  }

  controls.appendChild(makePair(ecuLabel, ecuSelect));
  controls.appendChild(makePair(testTypeLabel, testTypeSelect));
  controls.appendChild(makePair(profileLabel, profileSelect));
  controls.appendChild(resetBtn);

  header.appendChild(title);
  header.appendChild(controls);
  container.appendChild(header);
}
