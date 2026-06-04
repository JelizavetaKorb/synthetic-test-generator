import { getState, setState, isFieldLocked } from '../state.js';
import { humanizeValue } from './field-labels.js';

function normalizeOption(opt) {
  if (opt && typeof opt === 'object' && 'value' in opt) {
    return { value: opt.value, label: opt.label ?? humanizeValue(opt.value) };
  }
  return { value: opt, label: humanizeValue(opt) };
}

export function createField(config) {
  const { label, path, type, unit, min, max, step, options, required, defaultValue } = config;
  const wrapper = document.createElement('div');
  wrapper.className = 'field-wrapper';
  const lbl = document.createElement('label');
  lbl.textContent = label + (required ? ' *' : '');
  wrapper.appendChild(lbl);
  const currentValue = getState(path) ?? defaultValue;
  const locked = isFieldLocked(path);
  let input;
  if (type === 'textarea') {
    input = document.createElement('textarea');
    input.value = currentValue ?? '';
    input.addEventListener('input', () => setState(path, input.value));
  } else if (type === 'select') {
    input = document.createElement('select');
    (options || []).forEach(opt => {
      const { value, label } = normalizeOption(opt);
      const o = document.createElement('option');
      o.value = value;
      o.textContent = label;
      if (value === currentValue) o.selected = true;
      input.appendChild(o);
    });
    input.addEventListener('change', () => setState(path, input.value));
  } else if (type === 'checkbox') {
    input = document.createElement('input');
    input.type = 'checkbox';
    input.checked = !!currentValue;
    input.addEventListener('change', () => setState(path, input.checked));
  } else if (type === 'number') {
    input = document.createElement('input');
    input.type = 'number';
    if (min !== undefined) input.min = min;
    if (max !== undefined) input.max = max;
    if (step !== undefined) input.step = step;
    input.value = currentValue ?? '';
    input.addEventListener('input', () => {
      const v = input.value === '' ? null : parseFloat(input.value);
      setState(path, v);
    });
  } else {
    input = document.createElement('input');
    input.type = 'text';
    input.value = currentValue ?? '';
    input.addEventListener('input', () => setState(path, input.value));
  }
  input.setAttribute('data-field', path);
  if (required) input.required = true;
  if (locked) input.disabled = true;
  if (unit) {
    const inputRow = document.createElement('div');
    inputRow.className = 'input-row';
    inputRow.appendChild(input);
    const unitSpan = document.createElement('span');
    unitSpan.className = 'field-unit';
    unitSpan.textContent = unit;
    inputRow.appendChild(unitSpan);
    wrapper.appendChild(inputRow);
  } else {
    wrapper.appendChild(input);
  }
  return wrapper;
}

export function createCheckboxGroup(config) {
  const { label, path, options } = config;
  const wrapper = document.createElement('div');
  wrapper.className = 'field-wrapper checkbox-group';
  const lbl = document.createElement('label');
  lbl.textContent = label;
  wrapper.appendChild(lbl);
  const group = document.createElement('div');
  group.className = 'checkbox-group-options';
  const currentValues = getState(path) || [];
  (options || []).forEach(opt => {
    const { value, label } = normalizeOption(opt);
    const item = document.createElement('label');
    item.className = 'checkbox-item';
    const cb = document.createElement('input');
    cb.type = 'checkbox';
    cb.value = value;
    cb.checked = currentValues.includes(value);
    cb.setAttribute('data-field', path);
    if (isFieldLocked(path)) cb.disabled = true;
    cb.addEventListener('change', () => {
      const selected = Array.from(group.querySelectorAll('input[type="checkbox"]'))
        .filter(c => c.checked).map(c => c.value);
      setState(path, selected);
    });
    item.appendChild(cb);
    item.appendChild(document.createTextNode(' ' + label));
    group.appendChild(item);
  });
  wrapper.appendChild(group);
  return wrapper;
}

export function createDynamicList(config) {
  const { path, label, addLabel, defaultItem, renderItem } = config;
  const container = document.createElement('div');
  container.className = 'dynamic-list';
  const lbl = document.createElement('div');
  lbl.className = 'dynamic-list-label';
  lbl.textContent = label;
  container.appendChild(lbl);
  const listEl = document.createElement('div');
  listEl.className = 'dynamic-list-items';
  container.appendChild(listEl);
  function renderList() {
    listEl.innerHTML = '';
    const items = getState(path) || [];
    items.forEach((item, index) => {
      const updateFn = (field, value) => {
        const arr = [...(getState(path) || [])];
        arr[index] = { ...arr[index], [field]: value };
        setState(path, arr);
      };
      const removeFn = () => {
        const arr = [...(getState(path) || [])];
        arr.splice(index, 1);
        setState(path, arr);
        renderList();
      };
      listEl.appendChild(renderItem(item, index, updateFn, removeFn));
    });
  }
  renderList();
  const addBtn = document.createElement('button');
  addBtn.type = 'button';
  addBtn.textContent = addLabel || 'Add';
  addBtn.addEventListener('click', () => {
    const arr = [...(getState(path) || [])];
    arr.push(JSON.parse(JSON.stringify(defaultItem)));
    setState(path, arr);
    renderList();
  });
  container.appendChild(addBtn);
  return container;
}
