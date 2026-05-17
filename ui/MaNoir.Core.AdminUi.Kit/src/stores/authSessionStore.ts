import { create, type StoreApi, type UseBoundStore } from 'zustand';

export type ServerInfoState = 'loading' | 'ready' | 'offline';

export interface AuthSessionService<TUser, TServerInfo, TCredentials> {
  getCurrentUser(): Promise<TUser>;
  getServerInfo(): Promise<TServerInfo>;
  login(credentials: TCredentials, options?: { isInteractive?: boolean }): Promise<void>;
  logout(): Promise<void>;
  isUnauthorizedError(error: unknown): boolean;
  getRestoreSessionErrorMessage?(error: unknown): string | null | undefined;
  getLoginErrorMessage?(error: unknown): string | null | undefined;
  getLogoutErrorMessage?(error: unknown): string | null | undefined;
}

export interface AuthSessionState<TUser, TServerInfo> {
  currentUser: TUser | null;
  serverInfo: TServerInfo | null;
  serverInfoState: ServerInfoState;
  isRestoringSession: boolean;
  isSubmitting: boolean;
  hasRestoredSession: boolean;
  errorMessage: string | null;
}

export interface AuthSessionActions<TUser, TCredentials> {
  restoreSession(force?: boolean): Promise<void>;
  login(credentials: TCredentials, options?: { isInteractive?: boolean }): Promise<TUser | null>;
  logout(): Promise<void>;
  clearError(): void;
}

export type AuthSessionStore<TUser, TServerInfo, TCredentials> = AuthSessionState<TUser, TServerInfo> & AuthSessionActions<TUser, TCredentials>;

export function createAuthSessionStore<TUser, TServerInfo, TCredentials>(
  service: AuthSessionService<TUser, TServerInfo, TCredentials>,
): UseBoundStore<StoreApi<AuthSessionStore<TUser, TServerInfo, TCredentials>>> {
  return create<AuthSessionStore<TUser, TServerInfo, TCredentials>>()((set, get) => ({
    currentUser: null,
    serverInfo: null,
    serverInfoState: 'loading',
    isRestoringSession: false,
    isSubmitting: false,
    hasRestoredSession: false,
    errorMessage: null,

    restoreSession: async (force = false) => {
      const state = get();
      if (state.isRestoringSession) {
        return;
      }

      if (state.hasRestoredSession && !force) {
        return;
      }

      set({
        isRestoringSession: true,
        errorMessage: null,
        serverInfoState: 'loading',
      });

      let serverInfoFailed = false;

      try {
        const serverInfo = await service.getServerInfo();
        set({
          serverInfo,
          serverInfoState: 'ready',
        });
      } catch {
        serverInfoFailed = true;
        set({
          serverInfo: null,
          serverInfoState: 'offline',
        });
      }

      try {
        const currentUser = await service.getCurrentUser();
        set({ currentUser });
      } catch (error) {
        if (service.isUnauthorizedError(error)) {
          set({ currentUser: null });
        } else if (!serverInfoFailed) {
          const errorMessage = service.getRestoreSessionErrorMessage?.(error) ?? 'Impossible de verifier la session en cours.';
          if (errorMessage) {
            set({ errorMessage });
          }
        }
      } finally {
        set({
          hasRestoredSession: true,
          isRestoringSession: false,
        });
      }
    },

    login: async (credentials, options) => {
      set({
        errorMessage: null,
        isSubmitting: true,
      });

      try {
        await service.login(credentials, options);
        const currentUser = await service.getCurrentUser();
        set({
          currentUser,
          hasRestoredSession: true,
        });

        return currentUser;
      } catch (error) {
        set({
          errorMessage: service.getLoginErrorMessage?.(error) ?? 'La connexion a echoue.',
        });
        throw error;
      } finally {
        set({ isSubmitting: false });
      }
    },

    logout: async () => {
      set({
        errorMessage: null,
        isSubmitting: true,
      });

      try {
        await service.logout();
        set({
          currentUser: null,
          hasRestoredSession: true,
        });
      } catch (error) {
        set({
          errorMessage: service.getLogoutErrorMessage?.(error) ?? 'La deconnexion a echoue.',
        });
        throw error;
      } finally {
        set({ isSubmitting: false });
      }
    },

    clearError: () => {
      set({ errorMessage: null });
    },
  }));
}