import { ENV } from '../env.js';

async function request(method, path, body) {
  const options = { method, headers: { 'Content-Type': 'application/json' } };
  if (body !== undefined) options.body = JSON.stringify(body);
  let res;
  try {
    res = await fetch(`${ENV.API_BASE_URL}${path}`, options);
  } catch {
    const err = new Error('Network error: could not reach the server.');
    err.isNetworkError = true;
    throw err;
  }
  const contentType = res.headers.get('content-type') || '';
  const isJson = contentType.includes('application/json');
  if (!res.ok) {
    const payload = isJson ? await res.json().catch(() => null) : await res.text().catch(() => '');
    const err = new Error(`HTTP ${res.status}`);
    err.status = res.status;
    err.payload = payload;
    err.issues = Array.isArray(payload?.issues) ? payload.issues
      : Array.isArray(payload?.errors) ? payload.errors
      : null;
    throw err;
  }
  if (res.status === 204) return null;
  return isJson ? res.json() : res.text();
}

export function getEcuCatalog() {
  return request('GET', '/ecus');
}

export function getProfiles() {
  return request('GET', '/profiles');
}

export function generate(data) {
  return request('POST', '/scenarios/generate', data);
}

export function exportScenarios(scenarios, format) {
  return request('POST', '/scenarios/export', { scenarios, format });
}
