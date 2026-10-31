import { createElement } from '../utils/dom.js';
import { labelFor } from '../utils/field-labels.js';

export function render(container) {
  const panel = createElement('div', { id: 'issues-panel', class: 'issues-panel hidden' });
  container.appendChild(panel);
}

export function clearIssues() {
  const panel = document.getElementById('issues-panel');
  if (panel) { panel.innerHTML = ''; panel.classList.add('hidden'); }
  clearFieldErrors();
}

export function clearFieldErrors() {
  document.querySelectorAll('.inline-field-error').forEach(el => el.remove());
  document.querySelectorAll('.field-has-error').forEach(el => el.classList.remove('field-has-error'));
}

let currentInlineIssues = [];

function lineFor(target) {
  return target.closest('.checkbox-group-options') || target.closest('.input-row, .checkbox-item') || target;
}

function expandBlockOf(el) {
  const block = el.closest('.collapsible-block');
  const content = block && block.querySelector('.block-content');
  if (!content || !content.classList.contains('collapsed')) return;
  content.classList.remove('collapsed');
  const indicator = block.querySelector('.toggle-indicator');
  if (indicator) indicator.textContent = '\u25BC';
}

function buildInlineError(fieldIssues) {
  const severity = fieldIssues.some(i => (i.severity || 'error') === 'error') ? 'error' : 'warning';
  const box = document.createElement('div');
  box.className = `inline-field-error inline-field-error--${severity}`;
  box.setAttribute('role', severity === 'error' ? 'alert' : 'status');
  const icon = document.createElement('span');
  icon.className = 'inline-field-error__icon';
  icon.textContent = '!';
  const text = document.createElement('div');
  text.className = 'inline-field-error__text';
  fieldIssues.forEach(issue => {
    const line = document.createElement('div');
    line.textContent = issue.message;
    line.title = `${issue.severity || 'error'}${issue.code ? ' [' + issue.code + ']' : ''}`;
    text.appendChild(line);
  });
  box.appendChild(icon);
  box.appendChild(text);
  return box;
}

function injectFieldErrors(issues) {
  clearFieldErrors();
  const byField = new Map();
  issues.forEach(issue => {
    if (!issue.field) return;
    if (!byField.has(issue.field)) byField.set(issue.field, []);
    byField.get(issue.field).push(issue);
  });
  byField.forEach((fieldIssues, field) => {
    const target = document.querySelector(`[data-field="${CSS.escape(field)}"]`);
    if (!target) return;
    target.classList.add('field-has-error');
    const line = lineFor(target);
    line.parentNode.insertBefore(buildInlineError(fieldIssues), line);
    expandBlockOf(target);
  });
}

function scrollToFirstError() {
  const boxes = Array.from(document.querySelectorAll('.inline-field-error--error'));
  const first = boxes.sort((a, b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top)[0];
  const target = first || document.getElementById('issues-panel');
  if (!target) return;
  target.scrollIntoView({ behavior: 'smooth', block: 'start' });
  const input = first && first.parentNode.querySelector('.field-has-error');
  if (input && !input.disabled) input.focus({ preventScroll: true });
}

function setupInlineErrorPersistence() {
  if (window.__inlineErrorRerenderHooked) return;
  window.__inlineErrorRerenderHooked = true;
  document.addEventListener('rerender-editor', () => {
    if (currentInlineIssues.length) {
      requestAnimationFrame(() => injectFieldErrors(currentInlineIssues));
    }
  });
}

export function showIssues(issues, options = {}) {
  const panel = document.getElementById('issues-panel');
  if (!panel) return;
  panel.innerHTML = '';
  panel.classList.remove('hidden');
  currentInlineIssues = issues;
  setupInlineErrorPersistence();
  injectFieldErrors(issues);
  const errors = issues.filter(i => i.severity === 'error');
  const warnings = issues.filter(i => i.severity === 'warning');
  const info = issues.filter(i => i.severity !== 'error' && i.severity !== 'warning');

  if (errors.length) {
    panel.appendChild(createElement('h3', { class: 'issues-heading issues-heading--error' }, ['Fix these errors before generating']));
    errors.forEach(issue => panel.appendChild(buildIssueRow(issue, options)));
  }
  if (warnings.length) {
    panel.appendChild(createElement('h3', { class: 'issues-heading issues-heading--warning' }, ['Warnings']));
    warnings.forEach(issue => panel.appendChild(buildIssueRow(issue, options)));
    if (options.onRetry && !errors.length) {
      const overrideBar = createElement('div', { class: 'issues-actions' });
      const overrideBtn = createElement('button', { class: 'btn' }, ['Generate anyway (override warnings)']);
      overrideBtn.addEventListener('click', () => options.onRetry());
      overrideBar.appendChild(overrideBtn);
      panel.appendChild(overrideBar);
    }
  }
  if (info.length) {
    info.forEach(issue => panel.appendChild(buildIssueRow(issue, options)));
  }
  if (errors.length) requestAnimationFrame(scrollToFirstError);
}

function buildIssueRow(issue, options) {
  const severity = issue.severity || 'error';
  const row = createElement('div', { class: `issue-row issue-${severity}` });
  const badge = createElement('span', { class: 'issue-badge' }, [severity]);
  row.appendChild(badge);
  const friendlyField = issue.field ? labelFor(issue.field) : '';
  const text = friendlyField ? `${friendlyField}: ${issue.message}` : issue.message;
  row.appendChild(createElement('span', { class: 'issue-message' }, [text]));
  if (issue.code) {
    const codeBadge = createElement('span', { class: 'issue-code', title: 'Error code' }, [issue.code]);
    row.appendChild(codeBadge);
  }

  const dismissibleBtn = createElement('button', { class: 'btn btn-small issue-dismiss' }, ['Dismiss']);
  dismissibleBtn.addEventListener('click', e => {
    e.stopPropagation();
    row.remove();
  });

  if (issue.suggestedFix && options.onApplyFix) {
    const fixes = Array.isArray(issue.suggestedFix) ? issue.suggestedFix : [issue.suggestedFix];
    fixes.forEach(fix => {
      const label = (fix && fix.label) ? fix.label : 'Apply fix';
      const fixBtn = createElement('button', { class: 'btn btn-small btn-fix' }, [label]);
      fixBtn.addEventListener('click', e => {
        e.stopPropagation();
        options.onApplyFix(fix);
        document.dispatchEvent(new CustomEvent('rerender-editor'));
        row.remove();
      });
      row.appendChild(fixBtn);
    });
  }

  if (severity === 'warning') row.appendChild(dismissibleBtn);

  if (issue.field) {
    row.style.cursor = 'pointer';
    row.addEventListener('click', () => {
      const el = document.querySelector(`[data-field="${CSS.escape(issue.field)}"]`);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.classList.add('field-highlight');
        setTimeout(() => el.classList.remove('field-highlight'), 1500);
      }
    });
  }
  return row;
}
