import { STATE_TO_BACKEND } from './utils/profile-map.js';

function createDefaultState() {
  return {
    scenario_id: null,
    name: "",
    description: "",
    author: "",
    project: "",
    tags: [],
    created_at: null,
    version: "",
    priority: "medium",
    target_execution: "simulation",
    export_formats: [],
    profile: null,
    initial_conditions: {
      ambient_temperature_C: 20,
      battery_voltage_V: 12.6,
      engine_state: "IDLE",
      fuel_level_percent: 100,
      vehicle_speed_mps: 0,
      gear_position: "N",
      ignition_key: "ON",
      ambient_humidity_pct: 50,
      gps_fix_quality: "GOOD",
      localization_error_m: 0.1
    },
    ecu_id: "",
    ecu_firmware_version: "",
    ecu_mode: "NORMAL",
    ecu_power_state: "ON",
    ecu_temperature_C: 25,
    hw_interface: [],
    diagnostic_session: "NONE",
    signal_list: [],
    sequence_script: [],
    event_triggers: [],
    bus_config: {
      bus_name: "",
      bus_type: "CAN",
      bitrate_kbps: 500,
      message_defs: [],
      bus_load_pct: 10,
      message_jitter_ms: 0
    },
    faults: [],
    environmental_perturbations: {
      EMI_level: "none",
      temperature_ramp: null
    },
    timing: {
      test_duration_ms: 10000,
      time_resolution_ms: 10,
      repetitions: 1,
      inter_test_delay_ms: 0
    },
    safety: {
      hazard_id: "",
      safety_goal: "",
      ASIL_level: null,
      risk_weight: 0,
      recovery_behaviour_expected: false
    },
    assertions: [],
    sweep_parameters: {},
    combinatorial_strategy: "pairwise",
    max_generated_scenarios: 100,
    selection_strategy: "all",
    search_algorithm: {
      name: "none",
      params: {}
    },
    reporting: {
      log_level: "INFO",
      signals_to_record: [],
      telemetry_frequency_hz: 10,
      trace_capture: false,
      report_format: "JSON",
      save_artifacts: false
    },
    execution: {
      target_runner: "SIL_runner",
      ci_webhook_url: "",
      docker_image: "",
      timeout_ms: 60000
    },
    advanced: {
      constraint_expressions: [],
      dependency_graph: {},
      seed_dataset_link: "",
      provenance: {}
    },
    security: null,
    ecu_catalog_entry: null
  };
}

const defaultState = createDefaultState();
export const state = createDefaultState();
const subscribers = [];

export function resetState() {
  Object.keys(state).forEach(key => delete state[key]);
  Object.assign(state, JSON.parse(JSON.stringify(defaultState)));
  notify();
}

export function setState(path, value) {
  const parts = path.split('.');
  let obj = state;
  for (let i = 0; i < parts.length - 1; i++) {
    obj = obj[parts[i]];
  }
  obj[parts[parts.length - 1]] = value;
  notify();
}

export function getState(path) {
  const parts = path.split('.');
  let obj = state;
  for (const part of parts) {
    if (obj === undefined || obj === null) return undefined;
    obj = obj[part];
  }
  return obj;
}

export function getFullState() {
  return JSON.parse(JSON.stringify(state));
}

export function subscribe(callback) {
  subscribers.push(callback);
}

export function notify() {
  subscribers.forEach(cb => cb(getFullState()));
}

let lockedFields = [];

export function setLockedFields(fields) {
  lockedFields = fields || [];
}

export function clearLockedFields() {
  lockedFields = [];
}

export function isFieldLocked(path) {
  return lockedFields.includes(path);
}

const FLATTEN_SKIP = new Set([
  'profile', 'recommended_assertions', 'scenario_id', 'created_at',
  'sweep_parameters', 'combinatorial_strategy', 'max_generated_scenarios',
  'selection_strategy', 'search_algorithm', 'ecu_catalog_entry'
]);

export function flattenStateToOverrides() {
  const result = {};
  function walk(val, path) {
    if (val === null || val === undefined || val === '') return;
    if (Array.isArray(val)) return;
    if (typeof val === 'object') {
      for (const [key, child] of Object.entries(val)) {
        walk(child, path ? `${path}.${key}` : key);
      }
      return;
    }
    const backendKey = STATE_TO_BACKEND[path] ?? path;
    result[backendKey] = String(val);
  }
  const full = getFullState();
  for (const [key, val] of Object.entries(full)) {
    if (FLATTEN_SKIP.has(key)) continue;
    walk(val, key);
  }
  return result;
}
