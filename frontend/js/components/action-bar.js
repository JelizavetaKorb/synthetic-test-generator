import { createElement } from '../utils/dom.js';
import { getFullState, flattenStateToOverrides, setState } from '../state.js';
import { generate, exportScenarios } from '../api.js';
import { toggle as togglePreview } from './json-preview.js';
import { showIssues, clearIssues, clearFieldErrors } from './validation-panel.js';
import { showResults, hideResults } from './results-panel.js';
import { runClientValidation } from '../utils/client-validation.js';

function applySuggestedFix(fix) {
  if (!fix) return;
  const fixes = Array.isArray(fix) ? fix : [fix];
  fixes.forEach(f => {
    if (f && typeof f === 'object' && f.path !== undefined) {
      setState(f.path, f.value);
    }
  });
}

function buildGenerateBody() {
  const s = getFullState();
  return {
    profileId: document.getElementById('profile-select')?.value || null,
    overrides: flattenStateToOverrides(),
    sweepParameters: s.sweep_parameters,
    strategy: s.combinatorial_strategy,
    maxScenarios: s.max_generated_scenarios
    // NOTE: backend GenerateRequestDto has no `allowWarnings` field.
    // Warning suppression is a client-side UX gate only.
  };
}

async function handleGenerate(btn, allowWarnings) {
  clearFieldErrors();
  btn.disabled = true;
  btn.textContent = 'Generating...';
  clearIssues();
  hideResults();

  // 1. Client-side preflight — fast, friendly errors before contacting the backend.
  const clientIssues = runClientValidation(getFullState());
  const clientErrors = clientIssues.filter(i => i.severity === 'error');
  const clientWarnings = clientIssues.filter(i => i.severity === 'warning');
  if (clientErrors.length > 0) {
    showIssues(clientIssues, {
      onApplyFix: applySuggestedFix,
      onRetry: () => handleGenerate(btn, true),
    });
    btn.disabled = false;
    btn.textContent = 'Generate';
    return;
  }
  if (clientWarnings.length > 0 && !allowWarnings) {
    showIssues(clientIssues, {
      onApplyFix: applySuggestedFix,
      onRetry: () => handleGenerate(btn, true),
    });
    btn.disabled = false;
    btn.textContent = 'Generate';
    return;
  }

  try {
    const result = await generate(buildGenerateBody());
    const issues = result.issues || [];
    const errors = issues.filter(i => i.severity === 'error');
    const warnings = issues.filter(i => i.severity === 'warning');
    if (errors.length > 0) {
      showIssues(issues, { onApplyFix: applySuggestedFix, onRetry: () => handleGenerate(btn, true) });
    } else if (warnings.length > 0 && !allowWarnings) {
      showIssues(issues, { onApplyFix: applySuggestedFix, onRetry: () => handleGenerate(btn, true), hasResult: true, result });
    } else {
      showResults(result, getFullState().name);
    }
  } catch (e) {
    const issues = e.issues && e.issues.length
      ? e.issues
      : [{
          severity: 'error',
          field: '',
          code: e.isNetworkError ? 'NETWORK' : 'SERVER',
          message: e.isNetworkError
            ? 'Could not reach the generation server. Check your connection and try again.'
            : (e.message || 'Generation failed on the server.'),
        }];
    showIssues(issues, { onApplyFix: applySuggestedFix, onRetry: () => handleGenerate(btn, true) });
  } finally {
    btn.disabled = false;
    btn.textContent = 'Generate';
  }
}

async function handleExportJson(btn) {
  clearFieldErrors();
  btn.disabled = true;
  btn.textContent = 'Exporting...';
  try {
    const s = getFullState();
    const name = (s.name && s.name.trim()) ? s.name.trim() : 'scenario-config';
    let blob;
    try {
      blob = await exportScenarios([s], 'json');
    } catch {
      blob = new Blob([JSON.stringify(s, null, 2)], { type: 'application/json' });
    }
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${name}.json`;
    a.click();
    URL.revokeObjectURL(url);
  } finally {
    btn.disabled = false;
    btn.textContent = 'Export JSON';
  }
}

export function render(container) {
  const bar = createElement('div', { class: 'action-bar' });

  const generateBtn = createElement('button', { class: 'btn btn-primary' }, ['Generate']);
  generateBtn.addEventListener('click', () => handleGenerate(generateBtn, false));

  const previewBtn = createElement('button', { class: 'btn' }, ['Preview']);
  previewBtn.addEventListener('click', togglePreview);

  const exportBtn = createElement('button', { class: 'btn' }, ['Export JSON']);
  exportBtn.addEventListener('click', () => handleExportJson(exportBtn));

  [generateBtn, previewBtn, exportBtn].forEach(el => bar.appendChild(el));
  container.appendChild(bar);
}
