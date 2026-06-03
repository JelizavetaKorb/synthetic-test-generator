import { createElement } from '../utils/dom.js';

export function render(container) {
  const panel = createElement('div', { id: 'issues-panel', class: 'issues-panel hidden' });
  container.appendChild(panel);
}

export function clearIssues() {
  const panel = document.getElementById('issues-panel');
  if (panel) { panel.innerHTML = ''; panel.classList.add('hidden'); }
}

export function showIssues(issues) {
  const panel = document.getElementById('issues-panel');
  if (!panel) return;
  panel.innerHTML = '';
  panel.classList.remove('hidden');
  const hasErrors = issues.some(i => i.severity === 'error');
  panel.appendChild(createElement('h3', { class: 'issues-heading' }, [
    hasErrors ? 'Please fix these issues before generating' : 'Warnings'
  ]));
  issues.forEach(issue => {
    const row = createElement('div', { class: `issue-row issue-${issue.severity || 'error'}` });
    row.appendChild(createElement('span', { class: 'issue-badge' }, [issue.severity || 'error']));
    row.appendChild(createElement('span', { class: 'issue-message' }, [
      issue.field ? `${issue.field}: ${issue.message}` : issue.message
    ]));
    if (issue.field) {
      row.style.cursor = 'pointer';
      row.addEventListener('click', () => {
        const el = document.querySelector(`[data-field="${issue.field}"]`);
        if (el) {
          el.scrollIntoView({ behavior: 'smooth', block: 'center' });
          el.classList.add('field-highlight');
          setTimeout(() => el.classList.remove('field-highlight'), 1500);
        }
      });
    }
    panel.appendChild(row);
  });
}
