import { Role } from './role.model';

export interface AuthUser {
  id: string;
  email: string;
  full_name: string;
  roles: Role[];
  tenant_id?: string;
  entity_type?: string;
  entity_id?: string;
  email_verified_at?: string;
}

export interface MeResponse extends AuthUser {
  permissions: string[];
}

export interface AuthTokens {
  access_token: string;
  refresh_token: string;
  expires_in: number;
}

export interface LoginResponse extends AuthTokens {
  user: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface MfaVerifyRequest {
  code: string;
}

export interface PublicTenant {
  id: string;
  name: string;
  slug: string;
  logo_url?: string;
}
