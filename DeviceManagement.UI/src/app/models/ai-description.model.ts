export interface GenerateDescriptionRequest {
  brand: string;
  model: string;
  type: string;
  cpu?: string;
  ramGb?: number;
  storageGb?: number;
  operatingSystem?: string;
  notes?: string;
}

export interface GenerateDescriptionResponse {
  description: string;
}
