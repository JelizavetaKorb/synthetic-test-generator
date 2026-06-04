import { createElement } from '../utils/dom.js';
import { getFullState, flattenStateToOverrides, setState } from '../state.js';
import { generate } from '../api.js';
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

  };
}

async function handleGenerate(btn, allowWarnings) {
  clearFieldErrors();
  btn.disabled = true;
  btn.textContent = 'Generating...';
  clearIssues();
  hideResults();

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

export function render(container) {
  const bar = createElement('div', { class: 'action-bar' });

  const generateBtn = createElement('button', { class: 'btn btn-primary' }, ['Generate']);
  generateBtn.addEventListener('click', () => handleGenerate(generateBtn, false));

  const previewBtn = createElement('button', { class: 'btn' }, ['Preview']);
  previewBtn.addEventListener('click', togglePreview);

  [generateBtn, previewBtn].forEach(el => bar.appendChild(el));
  container.appendChild(bar);
}
