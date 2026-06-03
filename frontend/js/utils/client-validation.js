// Client-side preflight validation for ECU test scenarios. Runs against the
// local state shape exposed by state.js and produces issue objects compatible
// with the validation panel:
//   { code, severity, field, message, suggestedFix? }
//
// Severities:
//   'error'   — must be fixed before generation (cannot be overridden).
//   'warning' — user may proceed anyway via the "Generate anyway" button.
//
// suggestedFix is either:
//   { path: 'state.path', value: <new value>, label: 'Human-readable action' }
//   or an array of those.

import { labelFor } from './field-labels.js';

// ---------- helpers ----------
const FLEXRAY_ALLOWED_KBPS = [2500, 5000, 10000];

const BUS_RATE_RANGE = {
  CAN: [10, 1000],
  'CAN-FD': [10, 8000],
  LIN: [1, 20],
  FlexRay: null, // discrete set
  Ethernet: [10, 10_000_000],
};

const FIELD_RANGES = {
  'initial_conditions.ambient_temperature_C': [-40, 85, '°C'],
  'initial_conditions.battery_voltage_V': [6.0, 16.0, 'V'],
  'initial_conditions.fuel_level_percent': [0, 100, '%'],
  'initial_conditions.vehicle_speed_mps': [0, 70, 'm/s'],
  'initial_conditions.ambient_humidity_pct': [0, 100, '%'],
  'initial_conditions.localization_error_m': [0, 10, 'm'],
  'ecu_temperature_C': [-40, 125, '°C'],
  'bus_config.bus_load_pct': [0, 100, '%'],
  'bus_config.message_jitter_ms': [0, 100, 'ms'],
  'safety.risk_weight': [0, 1, ''],
};

const REQUIRED_TEXT_FIELDS = [
  ['name', 'Give the scenario a name so it can be saved and recognised later.'],
  ['ecu_id', 'Select the ECU under test from the catalog.'],
  ['target_execution', 'Choose where the scenario will run (simulation, SIL, HIL, bench, lab).'],
];

const CARTESIAN_BLOCK_THRESHOLD = 10_000;

// ---------- field range/required checks ----------
function checkRequiredFields(s, issues) {
  REQUIRED_TEXT_FIELDS.forEach(([path, hint]) => {
    const v = getByPath(s, path);
    if (v === null || v === undefined || (typeof v === 'string' && v.trim() === '')) {
      issues.push({
        code: 'REQ_FIELD',
        severity: 'error',
        field: path,
        message: `${labelFor(path)} is required. ${hint}`,
      });
    }
  });
}

function checkRanges(s, issues) {
  Object.entries(FIELD_RANGES).forEach(([path, [min, max, unit]]) => {
    const v = getByPath(s, path);
    if (v === null || v === undefined || v === '') return;
    const n = Number(v);
    if (Number.isNaN(n)) {
      issues.push({
        code: 'TYPE_NUMBER',
        severity: 'error',
        field: path,
        message: `${labelFor(path)} must be a number.`,
      });
      return;
    }
    if (n < min || n > max) {
      const unitStr = unit ? ' ' + unit : '';
      issues.push({
        code: 'OUT_OF_RANGE',
        severity: 'error',
        field: path,
        message: `${labelFor(path)} must be between ${min}${unitStr} and ${max}${unitStr} (got ${n}${unitStr}).`,
        suggestedFix: { path, value: clamp(n, min, max), label: `Clamp to ${clamp(n, min, max)}${unitStr}` },
      });
    }
  });
}

// ---------- semantic rules (section 6.2) ----------
function checkInitialConditions(s, issues) {
  const eng = s.initial_conditions?.engine_state;
  const speed = Number(s.initial_conditions?.vehicle_speed_mps ?? 0);
  const gear = s.initial_conditions?.gear_position;
  const ign = s.initial_conditions?.ignition_key;
  const power = s.ecu_power_state;
  const exec = s.target_execution;
  const mode = s.ecu_mode;

  if (eng === 'OFF' && speed > 0) {
    issues.push({
      code: 'ENG_OFF_MOVING',
      severity: 'error',
      field: 'initial_conditions.vehicle_speed_mps',
      message: `Engine is OFF but vehicle speed is ${speed} m/s. A stopped engine cannot move the vehicle.`,
      suggestedFix: [
        { path: 'initial_conditions.engine_state', value: 'RUNNING', label: 'Set engine state to RUNNING' },
        { path: 'initial_conditions.vehicle_speed_mps', value: 0, label: 'Set vehicle speed to 0 m/s' },
      ],
    });
  }
  if (gear === 'P' && speed > 0) {
    issues.push({
      code: 'PARK_MOVING',
      severity: 'error',
      field: 'initial_conditions.vehicle_speed_mps',
      message: `Gear is in Park (P) but vehicle speed is ${speed} m/s. Vehicle in P cannot move.`,
      suggestedFix: [
        { path: 'initial_conditions.gear_position', value: 'D', label: 'Change gear to D' },
        { path: 'initial_conditions.vehicle_speed_mps', value: 0, label: 'Set vehicle speed to 0 m/s' },
      ],
    });
  }
  if (ign === 'OFF' && power === 'ON' && exec !== 'bench') {
    issues.push({
      code: 'IGN_OFF_POWER_ON',
      severity: 'warning',
      field: 'ecu_power_state',
      message: `Ignition is OFF but ECU power state is ON. This is only realistic on a bench setup — change target execution to "bench" if intentional.`,
      suggestedFix: { path: 'initial_conditions.ignition_key', value: 'ON', label: 'Turn ignition ON' },
    });
  }
  if (mode === 'CALIBRATION' && ign === 'OFF' && exec !== 'bench') {
    issues.push({
      code: 'CAL_REQUIRES_IGN',
      severity: 'error',
      field: 'ecu_mode',
      message: `Calibration mode requires the ignition to be ON (except in bench mode).`,
      suggestedFix: { path: 'initial_conditions.ignition_key', value: 'ON', label: 'Turn ignition ON' },
    });
  }
}

function checkBattery(s, issues) {
  const v = Number(s.initial_conditions?.battery_voltage_V ?? NaN);
  if (Number.isNaN(v)) return;
  const catalog = s.ecu_catalog_entry;
  const minOp = catalog?.minOperationalVoltage ?? catalog?.minOperationalVoltageV;
  if (minOp != null && v < minOp) {
    issues.push({
      code: 'BAT_BELOW_MIN',
      severity: 'error',
      field: 'initial_conditions.battery_voltage_V',
      message: `Battery voltage ${v} V is below the ECU's minimum operational voltage (${minOp} V). The ECU may not power up.`,
      suggestedFix: { path: 'initial_conditions.battery_voltage_V', value: minOp, label: `Raise to ${minOp} V` },
    });
  }
}

function checkBus(s, issues) {
  const bus = s.bus_config || {};
  const type = bus.bus_type;
  const rate = bus.bitrate_kbps;
  if (rate == null) return;
  if (type === 'FlexRay') {
    if (!FLEXRAY_ALLOWED_KBPS.includes(Number(rate))) {
      issues.push({
        code: 'BUS_BITRATE_FLEXRAY',
        severity: 'error',
        field: 'bus_config.bitrate_kbps',
        message: `FlexRay only supports ${FLEXRAY_ALLOWED_KBPS.join(', ')} kbps (got ${rate}).`,
        suggestedFix: { path: 'bus_config.bitrate_kbps', value: 10000, label: 'Set bitrate to 10000 kbps' },
      });
    }
  } else if (BUS_RATE_RANGE[type]) {
    const [lo, hi] = BUS_RATE_RANGE[type];
    if (rate < lo || rate > hi) {
      issues.push({
        code: 'BUS_BITRATE_RANGE',
        severity: 'error',
        field: 'bus_config.bitrate_kbps',
        message: `${type} bitrate must be between ${lo} and ${hi} kbps (got ${rate}).`,
        suggestedFix: { path: 'bus_config.bitrate_kbps', value: clamp(rate, lo, hi), label: `Clamp to ${clamp(rate, lo, hi)} kbps` },
      });
    }
  }

  // Unique message IDs
  const seenMsgIds = new Map();
  (bus.message_defs || []).forEach((m, i) => {
    if (!m.id) {
      issues.push({
        code: 'MSG_ID_REQUIRED', severity: 'error',
        field: `bus_config.message_defs.${i}.id`,
        message: `Message #${i + 1} is missing an ID.`,
      });
    } else if (seenMsgIds.has(m.id)) {
      issues.push({
        code: 'MSG_ID_DUPLICATE', severity: 'error',
        field: `bus_config.message_defs.${i}.id`,
        message: `Message ID "${m.id}" is duplicated (also used in message #${seenMsgIds.get(m.id) + 1}).`,
      });
    } else {
      seenMsgIds.set(m.id, i);
    }
    if (!m.name) {
      issues.push({
        code: 'MSG_NAME_REQUIRED', severity: 'error',
        field: `bus_config.message_defs.${i}.name`,
        message: `Message #${i + 1} is missing a name.`,
      });
    }
  });
}

function checkSignals(s, issues) {
  const seen = new Map();
  (s.signal_list || []).forEach((sig, i) => {
    if (!sig.name) {
      issues.push({
        code: 'SIG_NAME_REQUIRED', severity: 'error',
        field: `signal_list.${i}.name`,
        message: `Signal #${i + 1} is missing a name.`,
      });
    } else if (seen.has(sig.name)) {
      issues.push({
        code: 'SIG_NAME_DUPLICATE', severity: 'error',
        field: `signal_list.${i}.name`,
        message: `Signal name "${sig.name}" is used more than once (also signal #${seen.get(sig.name) + 1}).`,
      });
    } else {
      seen.set(sig.name, i);
    }
    if (sig.min != null && sig.max != null && Number(sig.min) > Number(sig.max)) {
      issues.push({
        code: 'SIG_MIN_GT_MAX', severity: 'error',
        field: `signal_list.${i}.min`,
        message: `Signal "${sig.name || '#' + (i + 1)}": Min (${sig.min}) is greater than Max (${sig.max}).`,
      });
    }
  });
}

function checkFaults(s, issues) {
  const duration = Number(s.timing?.test_duration_ms ?? 0);
  const signalNames = new Set((s.signal_list || []).map(x => x.name).filter(Boolean));
  const msgIds = new Set();
  (s.bus_config?.message_defs || []).forEach(m => {
    if (m.id) msgIds.add(m.id);
    if (m.name) msgIds.add(m.name);
  });

  (s.faults || []).forEach((f, i) => {
    if (!f.target) {
      issues.push({
        code: 'FAULT_TARGET_REQUIRED', severity: 'error',
        field: `faults.${i}.target`,
        message: `Fault #${i + 1} has no target. Pick a signal or message ID to inject the fault into.`,
      });
    } else if (!signalNames.has(f.target) && !msgIds.has(f.target)) {
      issues.push({
        code: 'FAULT_TARGET_UNKNOWN', severity: 'error',
        field: `faults.${i}.target`,
        message: `Fault #${i + 1} targets "${f.target}", which is not defined in the signal list or bus messages.`,
      });
    }
    const start = Number(f.start_time_ms ?? 0);
    const dur = Number(f.duration_ms ?? 0);
    if (duration > 0 && start + dur > duration) {
      issues.push({
        code: 'FAULT_OUTSIDE_TEST', severity: 'error',
        field: `faults.${i}.duration_ms`,
        message: `Fault #${i + 1} ends at ${start + dur} ms, but the test only runs for ${duration} ms.`,
        suggestedFix: { path: `faults.${i}.duration_ms`, value: Math.max(0, duration - start), label: `Trim duration to ${Math.max(0, duration - start)} ms` },
      });
    }
    if (f.intensity != null) {
      const ix = Number(f.intensity);
      if (ix < 0 || ix > 1) {
        issues.push({
          code: 'FAULT_INTENSITY_RANGE', severity: 'error',
          field: `faults.${i}.intensity`,
          message: `Fault #${i + 1} intensity must be between 0 and 1 (got ${ix}).`,
          suggestedFix: { path: `faults.${i}.intensity`, value: clamp(ix, 0, 1), label: `Clamp to ${clamp(ix, 0, 1)}` },
        });
      }
    }
    if (dur < 0) {
      issues.push({
        code: 'FAULT_NEG_DURATION', severity: 'error',
        field: `faults.${i}.duration_ms`,
        message: `Fault #${i + 1} duration cannot be negative.`,
      });
    }
  });
}

function checkAssertions(s, issues) {
  const duration = Number(s.timing?.test_duration_ms ?? 0);
  (s.assertions || []).forEach((a, i) => {
    if (!a.expr || !a.expr.trim()) {
      issues.push({
        code: 'ASSERT_EXPR_REQUIRED', severity: 'error',
        field: `assertions.${i}.expr`,
        message: `Assertion #${i + 1} has an empty expression.`,
      });
    }
    const to = Number(a.time_window_ms?.to ?? 0);
    const from = Number(a.time_window_ms?.from ?? 0);
    if (from > to && to > 0) {
      issues.push({
        code: 'ASSERT_WINDOW_BACKWARDS', severity: 'error',
        field: `assertions.${i}.time_window_ms.from`,
        message: `Assertion #${i + 1}: window starts at ${from} ms but ends earlier at ${to} ms.`,
      });
    }
    if (duration > 0 && to > duration) {
      issues.push({
        code: 'ASSERT_WINDOW_OVERFLOW', severity: 'warning',
        field: `assertions.${i}.time_window_ms.to`,
        message: `Assertion #${i + 1} window ends at ${to} ms, after the test duration of ${duration} ms. It will be auto-clipped.`,
        suggestedFix: { path: `assertions.${i}.time_window_ms`, value: { from, to: duration }, label: `Clip to ${duration} ms` },
      });
    }
  });
}

function checkTiming(s, issues) {
  const duration = Number(s.timing?.test_duration_ms ?? 0);
  const resolution = Number(s.timing?.time_resolution_ms ?? 0);
  if (duration <= 0) {
    issues.push({
      code: 'DURATION_REQUIRED', severity: 'error',
      field: 'timing.test_duration_ms',
      message: `Test duration must be greater than zero.`,
      suggestedFix: { path: 'timing.test_duration_ms', value: 10000, label: 'Set to 10 000 ms (10 s)' },
    });
  }
  if (resolution > 0 && duration > 0 && resolution > duration) {
    issues.push({
      code: 'RES_GT_DURATION', severity: 'error',
      field: 'timing.time_resolution_ms',
      message: `Time resolution (${resolution} ms) cannot be larger than the test duration (${duration} ms).`,
    });
  }
}

function checkSafety(s, issues) {
  const asil = s.safety?.ASIL_level;
  const exec = s.target_execution;
  const assertions = s.assertions || [];
  if (!asil) return;

  if ((asil === 'C' || asil === 'D') && exec === 'simulation') {
    issues.push({
      code: 'ASIL_NEEDS_HIL', severity: 'warning',
      field: 'target_execution',
      message: `ASIL ${asil} scenarios should run on HIL or lab hardware, not pure simulation.`,
      suggestedFix: { path: 'target_execution', value: 'HIL', label: 'Switch to HIL execution' },
    });
  }
  if (asil === 'D') {
    if (!s.safety?.hazard_id) {
      issues.push({
        code: 'ASIL_D_NEEDS_HAZARD', severity: 'error',
        field: 'safety.hazard_id',
        message: 'ASIL D requires a linked Hazard ID.',
      });
    }
    const formal = assertions.some(a => a.type === 'STL' || a.type === 'LTL');
    if (!formal) {
      issues.push({
        code: 'ASIL_D_NEEDS_FORMAL', severity: 'error',
        field: 'safety.ASIL_level',
        message: 'ASIL D requires at least one formal (STL or LTL) assertion.',
      });
    }
  }
  const hasCritical = assertions.some(a => a.severity === 'critical' || a.severity === 'major');
  if ((asil === 'C' || asil === 'D') && !hasCritical) {
    issues.push({
      code: 'ASIL_NEEDS_CRITICAL', severity: 'warning',
      field: 'safety.ASIL_level',
      message: `ASIL ${asil} scenarios should include at least one major/critical assertion.`,
    });
  }
}

function checkSweep(s, issues) {
  const sweeps = Object.entries(s.sweep_parameters || {});
  if (!sweeps.length) return;
  const strategy = s.combinatorial_strategy;
  const max = Number(s.max_generated_scenarios ?? 0);

  // Validate per-sweep fields
  sweeps.forEach(([name, cfg]) => {
    const t = cfg?.type;
    const need = (k) => cfg?.[k] === null || cfg?.[k] === undefined || cfg?.[k] === '';
    if (!t) {
      issues.push({
        code: 'SWEEP_TYPE_REQUIRED', severity: 'error',
        field: `sweep_parameters.${name}.type`,
        message: `Sweep "${name}" needs a strategy.`,
      });
      return;
    }
    if (['linear_range', 'geometric_range', 'random_uniform', 'latin_hypercube'].includes(t)) {
      if (need('from') || need('to')) {
        issues.push({
          code: 'SWEEP_RANGE_REQUIRED', severity: 'error',
          field: `sweep_parameters.${name}.from`,
          message: `Sweep "${name}" (${t}) needs both "from" and "to".`,
        });
      } else if (Number(cfg.from) > Number(cfg.to)) {
        issues.push({
          code: 'SWEEP_FROM_GT_TO', severity: 'error',
          field: `sweep_parameters.${name}.from`,
          message: `Sweep "${name}": "from" (${cfg.from}) is greater than "to" (${cfg.to}).`,
        });
      }
    }
    if (t === 'gaussian' && (need('mean') || need('std'))) {
      issues.push({
        code: 'SWEEP_GAUSS_REQUIRED', severity: 'error',
        field: `sweep_parameters.${name}.mean`,
        message: `Sweep "${name}" (gaussian) needs both mean and std.`,
      });
    }
    if (t === 'enum_list' && (!Array.isArray(cfg.values) || cfg.values.length === 0)) {
      issues.push({
        code: 'SWEEP_ENUM_EMPTY', severity: 'error',
        field: `sweep_parameters.${name}.values`,
        message: `Sweep "${name}" (enum_list) needs at least one value.`,
      });
    }
  });

  // Combinatorial explosion estimate (cartesian only)
  if (strategy === 'cartesian') {
    let total = 1;
    sweeps.forEach(([, cfg]) => {
      total *= estimateSweepSize(cfg);
    });
    if (total > CARTESIAN_BLOCK_THRESHOLD) {
      issues.push({
        code: 'SWEEP_EXPLOSION', severity: 'error',
        field: 'combinatorial_strategy',
        message: `Cartesian product would generate ~${total.toLocaleString()} scenarios, over the safe limit of ${CARTESIAN_BLOCK_THRESHOLD.toLocaleString()}. Switch to pairwise/random sampling or reduce sweep ranges.`,
        suggestedFix: { path: 'combinatorial_strategy', value: 'pairwise', label: 'Switch to pairwise sampling' },
      });
    } else if (max > 0 && total > max) {
      issues.push({
        code: 'SWEEP_OVER_MAX', severity: 'warning',
        field: 'max_generated_scenarios',
        message: `Cartesian product (~${total.toLocaleString()}) exceeds Max Generated Scenarios (${max}). Only the first ${max} will be kept.`,
      });
    }
  }
}

function estimateSweepSize(cfg) {
  if (!cfg) return 1;
  switch (cfg.type) {
    case 'fixed': return 1;
    case 'enum_list': return Array.isArray(cfg.values) ? Math.max(1, cfg.values.length) : 1;
    case 'linear_range':
    case 'geometric_range': {
      const f = Number(cfg.from), t = Number(cfg.to), st = Number(cfg.step);
      if (!Number.isFinite(f) || !Number.isFinite(t) || !Number.isFinite(st) || st <= 0) return 1;
      return Math.max(1, Math.floor(Math.abs(t - f) / st) + 1);
    }
    case 'random_uniform':
    case 'gaussian':
    case 'latin_hypercube':
      return 10;
    default: return 1;
  }
}

// ---------- main entry ----------
export function runClientValidation(state) {
  const issues = [];
  checkRequiredFields(state, issues);
  checkRanges(state, issues);
  checkInitialConditions(state, issues);
  checkBattery(state, issues);
  checkBus(state, issues);
  checkSignals(state, issues);
  checkFaults(state, issues);
  checkAssertions(state, issues);
  checkTiming(state, issues);
  checkSafety(state, issues);
  checkSweep(state, issues);
  return issues;
}

// ---------- utilities ----------
function clamp(n, lo, hi) {
  return Math.min(hi, Math.max(lo, n));
}

function getByPath(obj, path) {
  const parts = path.split('.');
  let cur = obj;
  for (const p of parts) {
    if (cur === null || cur === undefined) return undefined;
    cur = cur[p];
  }
  return cur;
}
