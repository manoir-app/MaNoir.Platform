import { getCoreApiBaseUrl } from '../runtimeConfig';

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
  mainEmail?: string;
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

export interface CoreServerHealthInfo {
  meshName: string;
  domainName?: string | null;
  adminUiVersion: string;
  startedAtUtc: string;
  uptimeSeconds: number;
}

export interface AutomationMeshLocalSettings {
  meshId: string;
  publicId?: string | null;
  publicBaseDomain?: string | null;
  languageId?: string | null;
  timeZoneId?: string | null;
  countryId?: string | null;
}

export interface AdminNavigationDomainSummary {
  id: string;
  label: string;
  icon: string;
  href?: string | null;
}

export interface AdminNavigationDomainsResponse {
  domains: AdminNavigationDomainSummary[];
}

export interface AdminNavigationPage {
  id: string;
  contributionId: string;
  pluginId: string;
  category: string;
  name: string;
  label: string;
  href: string;
}

export interface AdminNavigationSection {
  id: string;
  label: string;
  pages: AdminNavigationPage[];
}

export interface AdminDomainNavigationResponse {
  domain: AdminNavigationDomainSummary;
  sections: AdminNavigationSection[];
}

export type AgentState = 'unknown' | 'starting' | 'ready' | 'degraded' | 'stopping' | 'stopped';

export interface RegisteredAgentModel {
  id: string;
  agentId: string;
  displayName?: string | null;
  meshId: string;
  version?: string | null;
  capabilities: string[];
  state: AgentState;
  statusMessage?: string | null;
  registeredAtUtc: string;
  lastHeartbeatUtc: string;
  updatedAtUtc: string;
}

export class ApiProblemError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}.`);
  }
}

const apiBaseUrl = getCoreApiBaseUrl();

export function loginUser(request: UserAuthenticationRequest, isInteractive = true) {
  return requestJson<UserAuthenticationResponse>(`/auth/users/login?isInteractive=${isInteractive ? 'true' : 'false'}`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getCurrentUser() {
  return requestJson<UserModel>('/auth/users/me');
}

export function getServerInfo() {
  return requestJson<CoreServerHealthInfo>('/health/server-info');
}

export function getLocalMeshSettings() {
  return requestJson<AutomationMeshLocalSettings>('/system/mesh/local/settings');
}

export function getAdminNavigationDomains() {
  return requestJson<AdminNavigationDomainsResponse>('/system/admin-navigation');
}

export function getAdminNavigationDomain(domainId: string) {
  return requestJson<AdminDomainNavigationResponse>(`/system/admin-navigation/domains/${encodeURIComponent(domainId)}`);
}

export function getRegisteredAgents(meshId?: string) {
  const search = meshId ? `?meshId=${encodeURIComponent(meshId)}` : '';
  return requestJson<RegisteredAgentModel[]>(`/system/agents${search}`);
}

export function getRegisteredAgent(agentId: string, meshId = 'local') {
  const search = meshId ? `?meshId=${encodeURIComponent(meshId)}` : '';
  return requestJson<RegisteredAgentModel>(`/system/agents/${encodeURIComponent(agentId)}${search}`);
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