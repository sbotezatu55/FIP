import { FlightEventType } from '../models/flight-event';

const eventLabels: Record<string, string> = {
  Takeoff: 'Takeoff',
  Landing: 'Landing',
  TopOfClimb: 'Top of Climb',
  TopOfDescent: 'Top of Descent',
  TelemetryGap: 'Telemetry Gap'
};

export function flightEventLabel(type: FlightEventType): string {
  return eventLabels[type] ?? type;
}

export function flightEventClass(type: FlightEventType): string {
  return type
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}
