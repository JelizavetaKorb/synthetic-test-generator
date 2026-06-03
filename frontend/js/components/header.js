import { createElement } from '../utils/dom.js';
import { BACKEND_TO_STATE } from '../utils/profile-map.js';
import { getEcuCatalog, getProfiles } from '../api.js';
import { resetState, getFullState, setState, setLockedFields, clearLockedFields } from '../state.js';
import { clearIssues } from './validation-panel.js';
import { hideResults } from './results-panel.js';

let profilesCache = [];

function loadEcuCatalog(selectEl) {
  // Show a visible loading state — the configured backend (Render.com free tier)
  // can take 30+ seconds to wake from sleep on the first request.
  const loadingOpt = document.createElement('option');
  loadingOpt.disabled = true;
  loadingOpt.value = '';
  loadingOpt.textContent = 'Loading ECU catalog…';
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
        opt.textContent = `${ecu.ecuId} — ${ecu.ecuName}`;
        opt.dataset.entry = JSON.stringify(ecu);
        selectEl.appendChild(opt);
      });
      const current = getFullState().ecu_id;
      if (current && ecus.some(e => e.ecuId === current)) {
        selectEl.value = current;
        handleEcuChange(selectEl);
      } else if (ecus.length === 1) {
        // Single-choice catalogs: pick it for the user so they don't have to
        // open a dropdown that has only one real option.
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
      opt.textContent = `ECU catalog unavailable (${reason}) — type the ECU ID in block B, or click to retry`;
      selectEl.appendChild(opt);
      // Flag the catalog as unavailable so block-b switches to manual entry.
      setState('ecu_catalog_unavailable', true);
      document.dispatchEvent(new CustomEvent('rerender-editor'));
      // Single-click retry: clear options and try again.
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
  setState('test_type', selectEl.value);
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

function loadProfiles(selectEl) {
  const loadingOpt = document.createElement('option');
  loadingOpt.disabled = true;
  loadingOpt.textContent = 'Loading profiles…';
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
      opt.textContent = `Profiles unavailable (${reason}) — click to retry`;
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

function handleProfileChange(selectEl) {
  const id = selectEl.value;
  if (!id) {
    clearLockedFields();
    setState('profile', null);
    setState('recommended_assertions', []);
    document.dispatchEvent(new CustomEvent('rerender-editor'));
    return;
  }
  const profile = profilesCache.find(p => p.profileId === id);
  if (!profile) return;
  clearLockedFields();
  Object.entries(profile.defaultParameters || {}).forEach(([key, val]) => {
    const path = BACKEND_TO_STATE[key];
    if (path) setState(path, val);
  });
  setLockedFields(
    (profile.lockedParameters || []).map(k => BACKEND_TO_STATE[k]).filter(Boolean)
  );
  setState('recommended_assertions', profile.recommendedAssertions || []);
  setState('profile', profile.name || '');
  document.dispatchEvent(new CustomEvent('rerender-editor'));
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
  [ecuLabel, ecuSelect, testTypeLabel, testTypeSelect, profileLabel, profileSelect, resetBtn].forEach(el => controls.appendChild(el));

  header.appendChild(title);
  header.appendChild(controls);
  container.appendChild(header);
}

