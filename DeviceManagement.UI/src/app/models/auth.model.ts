export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  location: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  id: string;
  email: string;
  name: string;
  token: string;
}

export interface AuthUser {
  id: string;
  email: string;
  name: string;
}
