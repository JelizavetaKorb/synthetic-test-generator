import { createCollapsibleBlock } from '../utils/dom.js';
import { createField, createCheckboxGroup } from '../utils/form.js';
import { getState, setState } from '../state.js';

function renderTagsInput() {
  const wrapper = document.createElement('div');
  wrapper.className = 'field-wrapper';
  const lbl = document.createElement('label');
  lbl.textContent = 'Tags';
  wrapper.appendChild(lbl);
  const tagList = document.createElement('div');
  tagList.className = 'tag-list';
  function refresh() {
    tagList.innerHTML = '';
    (getState('tags') || []).forEach((tag, i) => {
      const span = document.createElement('span');
      span.className = 'tag';
      span.textContent = tag + ' ';
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.textContent = '✕';
      btn.addEventListener('click', () => {
        const arr = [...(getState('tags') || [])];
        arr.splice(i, 1);
        setState('tags', arr);
        refresh();
      });
      span.appendChild(btn);
      tagList.appendChild(span);
    });
  }
  refresh();
  const input = document.createElement('input');
  input.type = 'text';
  input.placeholder = 'Type tag and press Enter';
  input.setAttribute('data-field', 'tags');
  input.addEventListener('keydown', e => {
    if (e.key !== 'Enter') return;
    e.preventDefault();
    const val = input.value.trim();
    if (!val) return;
    const arr = [...(getState('tags') || [])];
    arr.push(val);
    setState('tags', arr);
    input.value = '';
    refresh();
  });
  wrapper.appendChild(tagList);
  wrapper.appendChild(input);
  return wrapper;
}

export function render(container) {
  const block = createCollapsibleBlock('Scenario Metadata', 'block-metadata', content => {
    [
      createField({ label: 'Name', path: 'name', type: 'text', required: true, defaultValue: '' }),
      createField({ label: 'Description', path: 'description', type: 'textarea', defaultValue: '' }),
      createField({ label: 'Author', path: 'author', type: 'text', defaultValue: '' }),
      createField({ label: 'Project', path: 'project', type: 'text', defaultValue: '' }),
      createField({ label: 'Priority', path: 'priority', type: 'select', options: ['low', 'medium', 'high', 'critical'], defaultValue: 'medium' }),
      renderTagsInput(),
      createField({ label: 'Target Execution', path: 'target_execution', type: 'select', options: ['simulation', 'SIL', 'HIL', 'bench', 'lab'], defaultValue: 'simulation' }),
    ].forEach(el => content.appendChild(el));
    const errDiv = document.createElement('div');
    errDiv.className = 'block-errors';
    content.appendChild(errDiv);
  });
  container.appendChild(block);
}

