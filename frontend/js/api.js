import { ENV } from '../env.js';

async function request(method, path, body) {
  const options = { method, headers: { 'Content-Type': 'application/json' } };
  if (body !== undefined) options.body = JSON.stringify(body);
  const res = await fetch(`${ENV.API_BASE_URL}${path}`, options);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HTTP ${res.status}: ${text}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

export function getProfiles() {
  return request('GET', '/profiles');
}

export function generate(data) {
  return request('POST', '/scenarios/generate', data);
}
