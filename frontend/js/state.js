import { STATE_TO_BACKEND } from './utils/profile-map.js';

function createDefaultState() {
  return {
    scenario_id: crypto.randomUUID(),
    name: "",
    description: "",
    author: "",
    project: "",
    tags: [],
    created_at: null,
    version: "",
    priority: "medium",
    target_execution: "simulation",
    profile: null,
    test_type: "",
    recommended_assertions: [],
    initial_conditions: {
      ambient_temperature_C: 20,
      ambient_temperature_sweep: null,
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
      bus_load_pct: 10,
      message_jitter_ms: 0,
      message_defs: []
    },
    faults_enabled: false,
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
    ecu_catalog_entry: null,
    unit_test_params: {
      input_sequence: [],
      measurement_window_ms: 0,
      steady_state_tolerance: 0,
      overshoot_limit: 0
    },
    integration_test_params: {
      connectivity_topology: "",
      gateway_behaviour: ""
    },
    stress_endurance_params: {
      duration_hours: 0,
      high_temperature_profile: "",
      battery_drain_model: "",
      background_load_pattern: ""
    }
  };
}

export const state = createDefaultState();
const subscribers = [];

export function resetState() {
  const fresh = createDefaultState();
  Object.keys(state).forEach(key => delete state[key]);
  Object.assign(state, fresh);
  notify();
}

// fields that describe the scenario itself rather than its test setup; kept when the profile changes
const PROFILE_SWITCH_KEEP = new Set([
  'scenario_id', 'name', 'description', 'author', 'project', 'tags',
  'test_type', 'security', 'unit_test_params', 'integration_test_params', 'stress_endurance_params',
  'ecu_id', 'ecu_catalog_entry',
]);

// restores every setup field to its default so values from the previous profile don't linger
export function resetSetupFields() {
  const fresh = createDefaultState();
  Object.keys(fresh).forEach(key => {
    if (!PROFILE_SWITCH_KEEP.has(key)) state[key] = fresh[key];
  });
  notify();
}

// restores test-type-specific params to defaults so values from a previous type don't linger
export function resetTestTypeParams() {
  const fresh = createDefaultState();
  ['security', 'unit_test_params', 'integration_test_params', 'stress_endurance_params']
    .forEach(key => { state[key] = fresh[key]; });
  notify();
}

// writes a value to a dot-separated path inside the state object
export function setState(path, value) {
  const parts = path.split('.');
  let obj = state;
  for (let i = 0; i < parts.length - 1; i++) {
    if (obj[parts[i]] === null || obj[parts[i]] === undefined) {
      obj[parts[i]] = {};
    }
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

// fires all subscribers with a deep copy of the current state
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

// keys that belong only to the UI and should never reach the backend
const FLATTEN_SKIP = new Set([
  'profile', 'recommended_assertions', 'scenario_id', 'created_at',
  'sweep_parameters', 'combinatorial_strategy', 'max_generated_scenarios',
  'selection_strategy', 'search_algorithm', 'ecu_catalog_entry',

  'faults_enabled', 'ecu_catalog_unavailable',
  'unit_test_params', 'stress_endurance_params',
  'integration_test_params', 'security',
]);

// maps snake_case state paths to camelCase backend keys for plain arrays
const LIST_KEY_MAP = {
  'sequence_script': 'sequenceScript',
  'event_triggers':  'eventTriggers',
  'bus_config.message_defs': 'messageDefs',
  'tags':            'tags',
  'hw_interface':    'hwInterface',
  'advanced.constraint_expressions': 'constraintExpressions',
  'reporting.signals_to_record': 'signalsToRecord'
};

// lists that get flattened using bracket notation like faults[0].field
const BRACKET_FLATTEN_LISTS = {
  'faults':      'faults',
  'assertions':  'assertions',
  'signal_list': 'signalList'
};

// converts snake_case keys to camelCase for the backend
function snakeToCamel(s) {
  return s.replace(/_([a-zA-Z])/g, (_, c) => c.toUpperCase());
}

// recursively walks an object and writes dot-path entries with bracket prefix
function flattenObjectIntoBracket(obj, prefix, out) {
  if (obj === null || obj === undefined) return;
  if (Array.isArray(obj)) {

    out[prefix] = obj;
    return;
  }
  if (typeof obj === 'object') {
    for (const [key, val] of Object.entries(obj)) {
      if (val === null || val === undefined || val === '') continue;
      flattenObjectIntoBracket(val, `${prefix}.${snakeToCamel(key)}`, out);
    }
    return;
  }
  out[prefix] = obj;
}

// produces a flat key-value dict of all non-skipped state fields for the backend
export function flattenStateToOverrides() {
  const result = {};
  function walk(val, path) {
    if (val === null || val === undefined || val === '') return;
    if (Array.isArray(val)) {
      if (BRACKET_FLATTEN_LISTS[path]) {
        const base = BRACKET_FLATTEN_LISTS[path];
        val.forEach((item, i) => {
          if (item === null || item === undefined) return;
          if (typeof item !== 'object' || Array.isArray(item)) {
            result[`${base}[${i}]`] = item;
            return;
          }
          for (const [k, v] of Object.entries(item)) {
            if (v === null || v === undefined || v === '') continue;
            flattenObjectIntoBracket(v, `${base}[${i}].${snakeToCamel(k)}`, result);
          }
        });
        return;
      }
      const backendKey = LIST_KEY_MAP[path] ?? STATE_TO_BACKEND[path] ?? path;
      if (val.length > 0) result[backendKey] = val;
      return;
    }
    if (typeof val === 'object') {
      for (const [key, child] of Object.entries(val)) {
        walk(child, path ? `${path}.${key}` : key);
      }
      return;
    }
    const backendKey = STATE_TO_BACKEND[path] ?? path;
    result[backendKey] = val;
  }
  const full = getFullState();
  for (const [key, val] of Object.entries(full)) {
    if (FLATTEN_SKIP.has(key)) continue;
    walk(val, key);
  }
  return result;
}
