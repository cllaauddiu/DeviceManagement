export interface Device {
  id?: string;
  name: string;
  manufacturer: string;
  type: string;
  os: string;
  osVersion: string;
  processor: string;
  ramAmount: number;
  description: string;
  userId?: string;
}
