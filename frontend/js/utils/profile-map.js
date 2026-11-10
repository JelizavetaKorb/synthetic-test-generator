// maps snake_case UI state paths to camelCase backend parameter keys
export const STATE_TO_BACKEND = {
  'ecu_id':                                         'ecuId',
  'ecu_firmware_version':                           'ecuFirmwareVersion',
  'ecu_mode':                                       'ecuMode',
  'ecu_power_state':                                'ecuPowerState',
  'ecu_temperature_C':                              'ecuTemperatureC',
  'hw_interface':                                   'hwInterface',
  'diagnostic_session':                             'diagnosticSession',
  'target_execution':                               'targetExecution',
  'priority':                                       'priority',
  'author':                                         'author',
  'project':                                        'project',
  'version':                                        'version',
  'test_type':                                      'testType',
  'initial_conditions.ambient_temperature_C':       'initialConditions.ambientTemperatureC',
  'initial_conditions.battery_voltage_V':           'initialConditions.batteryVoltageV',
  'initial_conditions.engine_state':                'initialConditions.engineState',
  'initial_conditions.fuel_level_percent':          'initialConditions.fuelLevelPercent',
  'initial_conditions.vehicle_speed_mps':           'initialConditions.vehicleSpeedMps',
  'initial_conditions.gear_position':               'initialConditions.gearPosition',
  'initial_conditions.ignition_key':                'initialConditions.ignitionKey',
  'initial_conditions.ambient_humidity_pct':        'initialConditions.ambientHumidityPct',
  'initial_conditions.gps_fix_quality':             'initialConditions.gpsFixQuality',
  'initial_conditions.localization_error_m':        'initialConditions.localizationErrorM',

  'timing.test_duration_ms':                        'testDurationMs',
  'timing.time_resolution_ms':                      'timeResolutionMs',
  'timing.repetitions':                             'timing.repetitions',
  'timing.inter_test_delay_ms':                     'timing.interTestDelayMs',
  'reporting.log_level':                            'reporting.logLevel',
  'reporting.telemetry_frequency_hz':               'reporting.telemetryFrequencyHz',
  'reporting.trace_capture':                        'reporting.traceCapture',
  'reporting.report_format':                        'reporting.format',
  'reporting.save_artifacts':                       'reporting.saveArtifacts',
  'bus_config.bus_name':                            'bus.busName',
  'bus_config.bus_type':                            'bus.busType',
  'bus_config.bitrate_kbps':                        'bus.bitrateKbps',
  'bus_config.bus_load_pct':                        'bus.busLoadPct',
  'bus_config.message_jitter_ms':                   'bus.messageJitterMs',
  'safety.ASIL_level':                              'asilLevel',
  'safety.hazard_id':                               'hazardId',
  'safety.safety_goal':                             'safetyGoal',
  'safety.risk_weight':                             'riskWeight',
  'safety.recovery_behaviour_expected':             'recoveryBehaviourExpected',
  'execution.target_runner':                        'execution.targetRunner',
  'execution.ci_webhook_url':                       'execution.ciWebhookUrl',
  'execution.docker_image':                         'execution.dockerImage',
  'execution.timeout_ms':                           'execution.timeoutMs',
};

// inverted map used when applying profile defaults back into state
export const BACKEND_TO_STATE = Object.fromEntries(
  Object.entries(STATE_TO_BACKEND).map(([statePath, backendKey]) => [backendKey, statePath])
);

const BACKEND_LIST_TO_STATE = {
  faults: 'faults',
  assertions: 'assertions',
  signalList: 'signal_list',
  messageDefs: 'bus_config.message_defs',
};

const SWEEP_ISSUE_CODES = new Set(['INVALID_SWEEP_SPEC', 'SWEEP_MISSING_RANGE', 'UNSUPPORTED_SWEEP_TYPE']);

const BACKEND_FIELD_ALIASES = { sweepParameters: 'combinatorial_strategy' };

function camelToSnake(s) {
  return s.replace(/[A-Z]/g, c => '_' + c.toLowerCase());
}

export function backendFieldToState(field, code) {
  if (!field) return field;
  if (SWEEP_ISSUE_CODES.has(code)) return `sweep_parameters.${BACKEND_TO_STATE[field] ?? field}`;
  if (BACKEND_FIELD_ALIASES[field]) return BACKEND_FIELD_ALIASES[field];
  if (BACKEND_TO_STATE[field]) return BACKEND_TO_STATE[field];
  const item = field.match(/^(\w+)\[(\d+)\]\.(.+)$/);
  if (item && BACKEND_LIST_TO_STATE[item[1]]) {
    const rest = item[3].split('.').map(camelToSnake).join('.');
    return `${BACKEND_LIST_TO_STATE[item[1]]}.${item[2]}.${rest}`;
  }
  return field;
}

export function mapIssueFieldsToState(issues) {
  return issues.map(issue => ({ ...issue, field: backendFieldToState(issue.field, issue.code) }));
}
