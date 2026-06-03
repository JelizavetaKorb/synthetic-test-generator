import { createElement } from '../utils/dom.js';

export function render(container) {
  const panel = createElement('div', { id: 'results-panel', class: 'results-panel hidden' });
  container.appendChild(panel);
}

export function hideResults() {
  const panel = document.getElementById('results-panel');
  if (panel) { panel.innerHTML = ''; panel.classList.add('hidden'); }
  document.getElementById('main-content')?.classList.remove('hidden');
}

export function showResults(response, scenarioName) {
  document.getElementById('main-content')?.classList.add('hidden');
  const panel = document.getElementById('results-panel');
  if (!panel) return;
  panel.innerHTML = '';
  panel.classList.remove('hidden');
  panel.appendChild(createElement('h2', { class: 'results-heading' }, [
    `Generated ${response.generatedCount} scenario(s)`
  ]));
  const warnings = (response.issues || []).filter(i => i.severity === 'warning');
  if (warnings.length > 0) {
    const warnBox = createElement('div', { class: 'results-warnings' });
    warnings.forEach(w => {
      warnBox.appendChild(createElement('p', { class: 'results-warning' }, [
        w.field ? `${w.field}: ${w.message}` : w.message
      ]));
    });
    panel.appendChild(warnBox);
  }
  const jsonStr = JSON.stringify(response.scenarios, null, 2);
  const pre = createElement('pre', { class: 'results-json' });
  pre.textContent = jsonStr;
  panel.appendChild(pre);
  const actions = createElement('div', { class: 'results-actions' });
  const backBtn = createElement('button', { class: 'btn btn-primary' }, ['Edit Parameters']);
  backBtn.addEventListener('click', hideResults);
  const downloadBtn = createElement('button', { class: 'btn' }, ['Download JSON']);
  downloadBtn.addEventListener('click', () => {
    const name = (scenarioName && scenarioName.trim()) ? scenarioName.trim() : 'scenarios';
    const blob = new Blob([jsonStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = `${name}.json`; a.click();
    URL.revokeObjectURL(url);
  });
  const copyBtn = createElement('button', { class: 'btn' }, ['Copy to Clipboard']);
  copyBtn.addEventListener('click', () => {
    navigator.clipboard.writeText(jsonStr).then(() => {
      copyBtn.textContent = 'Copied';
      setTimeout(() => { copyBtn.textContent = 'Copy to Clipboard'; }, 2000);
    });
  });
  actions.appendChild(backBtn);
  actions.appendChild(downloadBtn);
  actions.appendChild(copyBtn);
  panel.appendChild(actions);
}
