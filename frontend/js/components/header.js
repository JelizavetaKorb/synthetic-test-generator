import { createElement } from '../utils/dom.js';
import { BACKEND_TO_STATE } from '../utils/profile-map.js';
import { generate, getProfiles } from '../api.js';
import { resetState, getFullState, setState, setLockedFields, clearLockedFields, flattenStateToOverrides } from '../state.js';
import { toggle as toggleJsonPreview } from './json-preview.js';
import { showIssues, clearIssues } from './validation-panel.js';
import { showResults, hideResults } from './results-panel.js';

let profilesCache = [];

function handleReset(profileSelect) {
  resetState();
  profileSelect.value = '';
  clearLockedFields();
  setState('profile', null);
  clearIssues();
  hideResults();
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

async function handleGenerate(btn) {
  btn.disabled = true;
  btn.textContent = 'Generating...';
  clearIssues();
  hideResults();
  const s = getFullState();
  const profileId = document.getElementById('profile-select')?.value || null;
  const body = {
    profileId,
    overrides: flattenStateToOverrides(),
    sweepParameters: s.sweep_parameters,
    strategy: s.combinatorial_strategy,
    maxScenarios: s.max_generated_scenarios
  };
  try {
    const result = await generate(body);
    const errors = (result.issues || []).filter(i => i.severity === 'error');
    if (errors.length > 0) {
      showIssues(result.issues);
    } else {
      showResults(result, s.name);
    }
  } catch {
    showIssues([{ severity: 'error', field: '', message: 'Something went wrong. Check your connection and try again.' }]);
  } finally {
    btn.disabled = false;
    btn.textContent = 'Generate';
  }
}

function loadProfiles(selectEl) {
  getProfiles()
    .then(profiles => {
      profilesCache = profiles || [];
      profilesCache.forEach(p => {
        const opt = document.createElement('option');
        opt.value = p.profileId;
        opt.textContent = p.name;
        selectEl.appendChild(opt);
      });
    })
    .catch(() => {
      const opt = document.createElement('option');
      opt.disabled = true;
      opt.textContent = 'Profiles unavailable';
      selectEl.appendChild(opt);
    });
}

function handleProfileChange(selectEl) {
  const id = selectEl.value;
  if (!id) {
    clearLockedFields();
    setState('profile', null);
    document.dispatchEvent(new CustomEvent('rerender-editor'));
    return;
  }
  const profile = profilesCache.find(p => p.profileId === id);
  if (!profile) return;
  Object.entries(profile.defaultParameters || {}).forEach(([key, val]) => {
    const path = BACKEND_TO_STATE[key];
    if (path) setState(path, val);
  });
  setLockedFields(
    (profile.lockedParameters || []).map(k => BACKEND_TO_STATE[k]).filter(Boolean)
  );
  if (profile.recommendedAssertions) setState('recommended_assertions', profile.recommendedAssertions);
  setState('profile', profile.name || '');
  document.dispatchEvent(new CustomEvent('rerender-editor'));
}

export function render(container) {
  const header = createElement('header', { class: 'app-header' });
  const title = createElement('h1', { class: 'app-title' }, ['ECU Test Scenario Builder']);
  const profileLabel = createElement('label', { for: 'profile-select' }, ['Profile:']);
  const profileSelect = createElement('select', { id: 'profile-select', class: 'profile-select' });
  profileSelect.appendChild(createElement('option', { value: '' }, ['No profile']));
  profileSelect.addEventListener('change', () => handleProfileChange(profileSelect));
  loadProfiles(profileSelect);
  const resetBtn = createElement('button', { class: 'btn' }, ['Reset']);
  resetBtn.addEventListener('click', () => handleReset(profileSelect));
  const generateBtn = createElement('button', { class: 'btn btn-primary' }, ['Generate']);
  generateBtn.addEventListener('click', () => handleGenerate(generateBtn));
  const previewBtn = createElement('button', { class: 'btn' }, ['JSON Preview']);
  previewBtn.addEventListener('click', toggleJsonPreview);
  const controls = createElement('div', { class: 'header-controls' });
  [profileLabel, profileSelect, resetBtn, generateBtn, previewBtn].forEach(el => controls.appendChild(el));
  header.appendChild(title);
  header.appendChild(controls);
  container.appendChild(header);
}
