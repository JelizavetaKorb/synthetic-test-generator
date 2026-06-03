// Maps frontend snake_case state paths → backend camelCase parameter keys.
// This table mirrors the backend's ScenarioFormatter.KeyMap (inverse direction).
export const STATE_TO_BACKEND = {
  // ECU
  'ecu_id':                                         'ecuId',
  'ecu_firmware_version':                           'ecuFirmwareVersion',
  'ecu_mode':                                       'ecuMode',
  'ecu_power_state':                                'ecuPowerState',
  'ecu_temperature_C':                              'ecuTemperatureC',
  'hw_interface':                                   'hwInterface',
  'diagnostic_session':                             'diagnosticSession',
  // Metadata scalars
  'target_execution':                               'targetExecution',
  'priority':                                       'priority',
  'author':                                         'author',
  'project':                                        'project',
  'version':                                        'version',
  // Initial conditions
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
  // Timing
  'timing.test_duration_ms':                        'timing.testDurationMs',
  'timing.time_resolution_ms':                      'timing.timeResolutionMs',
  'timing.repetitions':                             'timing.repetitions',
  'timing.inter_test_delay_ms':                     'timing.interTestDelayMs',
  // Bus config
  'bus_config.bus_type':                            'busType',
  'bus_config.bitrate_kbps':                        'bitrateKbps',
  'bus_config.bus_load_pct':                        'busLoadPct',
  'bus_config.message_jitter_ms':                   'messageJitterMs',
  // Reporting
  'reporting.log_level':                            'reporting.logLevel',
  'reporting.telemetry_frequency_hz':               'reporting.telemetryFrequencyHz',
  'reporting.trace_capture':                        'reporting.traceCapture',
  'reporting.report_format':                        'reporting.format',
  'reporting.save_artifacts':                       'reporting.saveArtifacts',
  // Safety (flat keys in backend KeyMap)
  'safety.ASIL_level':                              'asilLevel',
  'safety.hazard_id':                               'hazardId',
  'safety.safety_goal':                             'safetyGoal',
  'safety.risk_weight':                             'riskWeight',
  'safety.recovery_behaviour_expected':             'recoveryBehaviourExpected',
  // Execution
  'execution.target_runner':                        'execution.targetRunner',
  'execution.ci_webhook_url':                       'execution.ciWebhookUrl',
  'execution.docker_image':                         'execution.dockerImage',
  'execution.timeout_ms':                           'execution.timeoutMs',
};

// Inverse map: backend camelCase → frontend state path
export const BACKEND_TO_STATE = Object.fromEntries(
  Object.entries(STATE_TO_BACKEND).map(([statePath, backendKey]) => [backendKey, statePath])
);
