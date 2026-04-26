export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
}

export interface UserModel {
  id: string;
  isGuest: boolean;
  isMain: boolean;
  name?: string;
  firstName?: string;
  commonName?: string;
  ssmlTaggedName?: string;
  mainEmail?: string;
  mainPhoneNumber?: string;
}

export interface InitialSetupStatus {
  canInitialize: boolean;
  hasMesh: boolean;
  hasUsers: boolean;
}

export interface InitialSetupRequest {
  adminUserId: string;
  adminFirstName: string;
  adminName: string;
  adminCommonName: string;
  adminEmail: string;
  adminPassword: string;
  languageId: string;
  timeZoneId: string;
  countryId: string;
}

export interface InitialSetupResponse {
  mesh: {
    id: string;
    publicId?: string;
    languageId?: string;
    timeZoneId?: string;
    countryId?: string;
  };
  user: UserModel;
}

export interface UserAuthenticationRequest {
  userId: string;
  password: string;
}

export interface UserAuthenticationResponse {
  tokenType: string;
  accessToken?: string | null;
  expiresAtUtc?: string;
  user: UserModel;
}

export class ApiProblemError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}.`);
  }
}

const apiBaseUrl = import.meta.env.VITE_CORE_API_BASE_URL?.replace(/\/$/, '') ?? '/api/core';

export function getSetupStatus() {
  return requestJson<InitialSetupStatus>('/setup/status');
}

export function initializeSetup(request: InitialSetupRequest) {
  return requestJson<InitialSetupResponse>('/setup/initialize', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function loginUser(request: UserAuthenticationRequest, isInteractive: boolean) {
  return requestJson<UserAuthenticationResponse>(`/auth/users/login?isInteractive=${isInteractive ? 'true' : 'false'}`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getCurrentUser() {
  return requestJson<UserModel>('/auth/users/me');
}

export function logoutUser() {
  return requestVoid('/auth/users/logout', {
    method: 'POST',
  });
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(buildUrl(path), withDefaults(init));
  if (!response.ok) {
    throw await createProblemError(response);
  }

  return (await response.json()) as T;
}

async function requestVoid(path: string, init?: RequestInit): Promise<void> {
  const response = await fetch(buildUrl(path), withDefaults(init));
  if (!response.ok) {
    throw await createProblemError(response);
  }
}

function withDefaults(init?: RequestInit): RequestInit {
  const headers = new Headers(init?.headers);
  if (init?.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json, application/problem+json');
  }

  return {
    credentials: 'include',
    ...init,
    headers,
  };
}

function buildUrl(path: string) {
  return `${apiBaseUrl}${path}`;
}

async function createProblemError(response: Response) {
  let problem: ProblemDetails | null = null;

  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    problem = null;
  }

  return new ApiProblemError(response.status, problem ?? { status: response.status, title: 'Request failed' });
}