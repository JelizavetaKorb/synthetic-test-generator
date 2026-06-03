import { createElement } from '../utils/dom.js';
import { getFullState, subscribe } from '../state.js';

export function render(container) {
  const panel = createElement('div', { id: 'json-preview-panel', class: 'json-preview-panel hidden' });
  const pre = createElement('pre', { class: 'json-preview-content' });
  panel.appendChild(pre);
  container.appendChild(panel);
  subscribe(() => {
    if (!panel.classList.contains('hidden')) {
      pre.textContent = JSON.stringify(getFullState(), null, 2);
    }
  });
}

export function toggle() {
  const panel = document.getElementById('json-preview-panel');
  if (!panel) return;
  if (panel.classList.contains('hidden')) {
    panel.classList.remove('hidden');
    const pre = panel.querySelector('pre');
    if (pre) pre.textContent = JSON.stringify(getFullState(), null, 2);
  } else {
    panel.classList.add('hidden');
  }
}
