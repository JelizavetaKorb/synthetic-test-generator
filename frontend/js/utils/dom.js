export function createElement(tag, attributes = {}, children = []) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attributes)) {
    if (key === 'class') {
      el.className = value;
    } else {
      el.setAttribute(key, value);
    }
  }
  for (const child of children) {
    if (child == null) continue;
    if (typeof child === 'string') {
      el.appendChild(document.createTextNode(child));
    } else if (child instanceof Node) {
      el.appendChild(child);
    }
  }
  return el;
}

export function clearElement(el) {
  while (el.lastChild) {
    el.removeChild(el.lastChild);
  }
}

export function show(el) {
  el.classList.remove('hidden');
}

export function hide(el) {
  el.classList.add('hidden');
}

export function createCollapsibleBlock(title, id, contentRenderFn) {
  const section = createElement('section', { class: 'collapsible-block', id });
  const header = createElement('div', { class: 'block-header' });
  const indicator = createElement('span', { class: 'toggle-indicator' }, ['\u25B6']);
  const titleEl = createElement('span', { class: 'block-title' }, [title]);
  header.appendChild(indicator);
  header.appendChild(titleEl);

  const content = createElement('div', { class: 'block-content collapsed' });
  contentRenderFn(content);

  header.addEventListener('click', () => {
    const isCollapsed = content.classList.contains('collapsed');
    content.classList.toggle('collapsed');
    indicator.textContent = isCollapsed ? '\u25BC' : '\u25B6';
  });

  section.appendChild(header);
  section.appendChild(content);
  return section;
}
