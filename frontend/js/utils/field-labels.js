// Maps internal state paths (and a few backend keys) to human-readable labels
// shown to the user inside the issues panel.

export const FIELD_LABELS = {
  // Metadata
  'name': 'Scenario name',
  'description': 'Description',
  'author': 'Author',
  'project': 'Project',
  'priority': 'Priority',
  'tags': 'Tags',
  'target_execution': 'Target execution',
  'test_type': 'Test type',

  // ECU
  'ecu_id': 'ECU',
  'ecu_firmware_version': 'ECU firmware version',
  'ecu_mode': 'ECU mode',
  'ecu_power_state': 'ECU power state',
  'ecu_temperature_C': 'ECU temperature (°C)',
  'hw_interface': 'HW interfaces',
  'diagnostic_session': 'Diagnostic session',

  // Initial conditions
  'initial_conditions.ambient_temperature_C': 'Ambient temperature (°C)',
  'initial_conditions.battery_voltage_V': 'Battery voltage (V)',
  'initial_conditions.engine_state': 'Engine state',
  'initial_conditions.fuel_level_percent': 'Fuel level (%)',
  'initial_conditions.vehicle_speed_mps': 'Vehicle speed (m/s)',
  'initial_conditions.gear_position': 'Gear position',
  'initial_conditions.ignition_key': 'Ignition key',
  'initial_conditions.ambient_humidity_pct': 'Ambient humidity (%)',
  'initial_conditions.gps_fix_quality': 'GPS fix quality',
  'initial_conditions.localization_error_m': 'Localization error (m)',

  // Bus
  'bus_config.bus_name': 'Bus name',
  'bus_config.bus_type': 'Bus type',
  'bus_config.bitrate_kbps': 'Bitrate (kbps)',
  'bus_config.bus_load_pct': 'Bus load (%)',
  'bus_config.message_jitter_ms': 'Message jitter (ms)',

  // Timing
  'timing.test_duration_ms': 'Test duration (ms)',
  'timing.time_resolution_ms': 'Time resolution (ms)',
  'timing.repetitions': 'Repetitions',
  'timing.inter_test_delay_ms': 'Inter-test delay (ms)',

  // Safety
  'safety.hazard_id': 'Hazard ID',
  'safety.safety_goal': 'Safety goal',
  'safety.ASIL_level': 'ASIL level',
  'safety.risk_weight': 'Risk weight',
  'safety.recovery_behaviour_expected': 'Recovery behaviour expected',

  // Sweep
  'combinatorial_strategy': 'Combinatorial strategy',
  'max_generated_scenarios': 'Max generated scenarios',
  'selection_strategy': 'Selection strategy',

  // Reporting / execution
  'reporting.log_level': 'Log level',
  'reporting.telemetry_frequency_hz': 'Telemetry frequency (Hz)',
  'reporting.report_format': 'Report format',
  'execution.target_runner': 'Target runner',
  'execution.ci_webhook_url': 'CI webhook URL',
  'execution.docker_image': 'Docker image',
  'execution.timeout_ms': 'Timeout (ms)',
};

// Friendly label for indexed array paths like "faults.2.target" or
// "bus_config.message_defs.0.id".
const ARRAY_LABEL_PATTERNS = [
  [/^faults\.(\d+)\.(.+)$/, (m) => `Fault #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^signal_list\.(\d+)\.(.+)$/, (m) => `Signal #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^bus_config\.message_defs\.(\d+)\.(.+)$/, (m) => `Message #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^assertions\.(\d+)\.(.+)$/, (m) => `Assertion #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^sequence_script\.(\d+)\.(.+)$/, (m) => `Sequence step #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^event_triggers\.(\d+)\.(.+)$/, (m) => `Event trigger #${+m[1] + 1}: ${humanizeKey(m[2])}`],
  [/^sweep_parameters\.([^.]+)(?:\.(.+))?$/, (m) => `Sweep "${m[1]}"${m[2] ? ': ' + humanizeKey(m[2]) : ''}`],
  [/^advanced\.constraint_expressions\.(\d+)$/, (m) => `Constraint expression #${+m[1] + 1}`],
];

function humanizeKey(key) {
  return key
    .replace(/[_.]/g, ' ')
    .replace(/\b([a-z])/g, (_, c) => c.toUpperCase())
    .replace(/\bMs\b/g, '(ms)')
    .replace(/\bPct\b/g, '(%)');
}

// Convert an internal enum/identifier value into a readable label for display
// in dropdowns and checkbox groups. Splits on `_` and `-`, then on camelCase
// boundaries that follow a lowercase letter, and title-cases lowercase tokens.
// All-uppercase tokens (acronyms like SIL, HIL, CAN, GPS, ASIL, JSON) are kept
// intact, and the surrounding storage value (sent to the backend) is unchanged.
export function humanizeValue(v) {
  if (v === null || v === undefined) return '';
  const s = String(v);
  if (!s) return '';
  const parts = s
    .split(/[_-]/)
    .flatMap(piece => piece.replace(/([a-z])([A-Z])/g, '$1 $2').split(' '))
    .filter(Boolean);
  return parts
    .map(p => (/^[a-z]/.test(p) ? p.charAt(0).toUpperCase() + p.slice(1) : p))
    .join(' ');
}

export function labelFor(field) {
  if (!field) return '';
  if (FIELD_LABELS[field]) return FIELD_LABELS[field];
  for (const [pattern, render] of ARRAY_LABEL_PATTERNS) {
    const m = field.match(pattern);
    if (m) return render(m);
  }
  // Backend may send camelCase paths like initialConditions.batteryVoltageV — try
  // a simple snake_case conversion and look it up again.
  const snake = field
    .replace(/([A-Z])/g, '_$1')
    .replace(/\._/g, '.')
    .toLowerCase()
    .replace(/^_/, '');
  if (FIELD_LABELS[snake]) return FIELD_LABELS[snake];
  return humanizeKey(field);
}
