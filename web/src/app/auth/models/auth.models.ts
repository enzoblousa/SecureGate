export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface RegisterResult {
  id: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface TokenResult {
  token: string;
  expiresAt: string;
}

export interface DecodedUser {
  sub: string;
  email: string;
  name: string;
}

export interface ApiValidationError {
  errors: string[];
}

export interface ApiError {
  error: string;
}
