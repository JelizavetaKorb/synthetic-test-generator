import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';
import { getState, setState } from '../state.js';

function renderConstraintList(listEl) {
  listEl.innerHTML = '';
  (getState('advanced.constraint_expressions') || []).forEach((expr, idx) => {
    const row = document.createElement('div'); row.className = 'list-item';
    const inp = document.createElement('input'); inp.type = 'text'; inp.value = expr;
    inp.setAttribute('data-field', `advanced.constraint_expressions.${idx}`);
    inp.addEventListener('input', () => {
      const arr = [...(getState('advanced.constraint_expressions') || [])]; arr[idx] = inp.value;
      setState('advanced.constraint_expressions', arr);
    });
    const rm = document.createElement('button'); rm.type = 'button'; rm.textContent = 'Remove';
    rm.addEventListener('click', () => {
      const arr = [...(getState('advanced.constraint_expressions') || [])]; arr.splice(idx, 1);
      setState('advanced.constraint_expressions', arr); renderConstraintList(listEl);
    });
    row.appendChild(inp); row.appendChild(rm); listEl.appendChild(row);
  });
}

function makeJsonTA(lbl, path, fallback) {
  const w = document.createElement('div'); w.className = 'field-wrapper';
  w.appendChild(Object.assign(document.createElement('label'), { textContent: lbl }));
  const ta = document.createElement('textarea'); ta.setAttribute('data-field', path);
  const cur = getState(path);
  ta.value = (cur && typeof cur === 'object') ? JSON.stringify(cur, null, 2) : (cur || fallback);
  ta.addEventListener('input', () => { try { setState(path, JSON.parse(ta.value)); } catch { setState(path, ta.value); } });
  w.appendChild(ta); return w;
}

export function render(container) {
  const block = createCollapsibleBlock('K. Advanced / Expert Options', 'block-k', content => {
    const listLbl = document.createElement('div'); listLbl.className = 'dynamic-list-label'; listLbl.textContent = 'Constraint Expressions';
    const listEl = document.createElement('div'); listEl.className = 'dynamic-list-items';
    renderConstraintList(listEl);
    const addBtn = document.createElement('button'); addBtn.type = 'button'; addBtn.textContent = 'Add Expression';
    addBtn.addEventListener('click', () => {
      const arr = [...(getState('advanced.constraint_expressions') || [])]; arr.push('');
      setState('advanced.constraint_expressions', arr); renderConstraintList(listEl);
    });
    content.appendChild(listLbl); content.appendChild(listEl); content.appendChild(addBtn);
    [
      makeJsonTA('Dependency Graph (JSON)', 'advanced.dependency_graph', '{}'),
      createField({ label: 'Seed Dataset Link', path: 'advanced.seed_dataset_link', type: 'text', defaultValue: '' }),
      makeJsonTA('Provenance (JSON)', 'advanced.provenance', '{}'),
    ].forEach(el => content.appendChild(el));
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
