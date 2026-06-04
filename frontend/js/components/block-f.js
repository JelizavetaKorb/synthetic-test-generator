import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';

export function render(container) {
  const block = createCollapsibleBlock('F. Timing & Scheduling', 'block-f', content => {
    [
      createField({ label: 'Test Duration', path: 'timing.test_duration_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 10000 }),
      createField({ label: 'Time Resolution', path: 'timing.time_resolution_ms', type: 'number', min: 1, step: 1, unit: 'ms', defaultValue: 10 }),
      createField({ label: 'Repetitions', path: 'timing.repetitions', type: 'number', min: 1, step: 1, defaultValue: 1 }),
      createField({ label: 'Inter Test Delay', path: 'timing.inter_test_delay_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 0 }),
    ].forEach(el => content.appendChild(el));
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
