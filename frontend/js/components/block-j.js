import { createCollapsibleBlock } from '../utils/dom.js';
import { createField } from '../utils/form.js';

export function render(container) {
  const block = createCollapsibleBlock('J. Execution Settings / CI Hooks', 'block-j', content => {
    [
      createField({ label: 'Target Runner', path: 'execution.target_runner', type: 'select', options: ['CARLA','SVL','SIL_runner','HIL_bench','pytest_harness'], defaultValue: 'SIL_runner' }),
      createField({ label: 'CI Webhook URL', path: 'execution.ci_webhook_url', type: 'text', defaultValue: '' }),
      createField({ label: 'Docker Image', path: 'execution.docker_image', type: 'text', defaultValue: '' }),
      createField({ label: 'Timeout', path: 'execution.timeout_ms', type: 'number', min: 0, unit: 'ms', defaultValue: 60000 }),
    ].forEach(el => content.appendChild(el));
    content.appendChild(Object.assign(document.createElement('div'), { className: 'block-errors' }));
  });
  container.appendChild(block);
}
