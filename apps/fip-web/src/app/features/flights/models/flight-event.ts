export type FlightEventType =
  | 'Takeoff'
  | 'Landing'
  | 'TopOfClimb'
  | 'TopOfDescent'
  | 'TelemetryGap'
  | string;

export interface FlightEvent {
  id: string;
  flightId: string;
  type: FlightEventType;
  timestamp: string;
  latitude: number | null;
  longitude: number | null;
  altitudeFeet: number | null;
  description: string | null;
}
