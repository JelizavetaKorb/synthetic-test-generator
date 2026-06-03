import { render as renderHeader } from './components/header.js';
import { render as renderJsonPreview } from './components/json-preview.js';
import { render as renderIssuesPanel } from './components/validation-panel.js';
import { render as renderResultsPanel } from './components/results-panel.js';
import { render as renderMetadata } from './components/metadata.js';
import { render as renderBlockA } from './components/block-a.js';
import { render as renderBlockB } from './components/block-b.js';
import { render as renderBlockC } from './components/block-c.js';
import { render as renderBlockD } from './components/block-d.js';
import { render as renderBlockE } from './components/block-e.js';
import { render as renderBlockF } from './components/block-f.js';
import { render as renderBlockG } from './components/block-g.js';
import { render as renderBlockH } from './components/block-h.js';
import { render as renderBlockI } from './components/block-i.js';
import { render as renderBlockJ } from './components/block-j.js';
import { render as renderBlockK } from './components/block-k.js';
import { render as renderBlockL } from './components/block-l.js';
import { clearElement } from './utils/dom.js';

function renderEditorView() {
  const main = document.getElementById('main-content');
  clearElement(main);
  renderMetadata(main);
  renderBlockA(main);
  renderBlockB(main);
  renderBlockC(main);
  renderBlockD(main);
  renderBlockE(main);
  renderBlockF(main);
  renderBlockG(main);
  renderBlockH(main);
  renderBlockI(main);
  renderBlockJ(main);
  renderBlockK(main);
  renderBlockL(main);
}

document.addEventListener('DOMContentLoaded', () => {
  renderHeader(document.getElementById('header'));
  renderJsonPreview(document.getElementById('json-preview-container'));
  renderIssuesPanel(document.getElementById('validation-container'));
  renderResultsPanel(document.getElementById('results-container'));
  renderEditorView();
  document.addEventListener('rerender-editor', renderEditorView);
});
