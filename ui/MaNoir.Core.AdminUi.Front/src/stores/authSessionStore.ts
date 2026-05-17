import { createAuthSessionStore, type AuthSessionService } from '@manoir-app/core-admin-ui-kit/auth-session-store';
import i18n from '../i18n';
import {
  ApiProblemError,
  getCurrentUser,
  getServerInfo,
  loginUser,
  logoutUser,
  type CoreServerHealthInfo,
  type UserAuthenticationRequest,
  type UserModel,
} from '../lib/api';

const authSessionService: AuthSessionService<UserModel, CoreServerHealthInfo, UserAuthenticationRequest> = {
  getCurrentUser,
  getServerInfo,
  login: async (credentials, options) => {
    await loginUser(credentials, options?.isInteractive ?? true);
  },
  logout: logoutUser,
  isUnauthorizedError: (error) => error instanceof ApiProblemError && error.status === 401,
  getRestoreSessionErrorMessage: () => i18n.t('errors.restoreSession'),
  getLoginErrorMessage: (error) => {
    if (error instanceof ApiProblemError) {
      if (error.status === 401) {
        return i18n.t('errors.invalidCredentials');
      }

      if (error.status === 400) {
        return error.problem.detail ?? error.problem.title ?? i18n.t('errors.incompleteLoginRequest');
      }

      return error.problem.detail ?? error.problem.title ?? i18n.t('errors.loginFailed');
    }

    return i18n.t('errors.loginFailed');
  },
  getLogoutErrorMessage: (error) => {
    if (error instanceof ApiProblemError) {
      return error.problem.detail ?? error.problem.title ?? i18n.t('errors.logoutFailed');
    }

    return i18n.t('errors.logoutFailed');
  },
};

export const useAuthSessionStore = createAuthSessionStore(authSessionService);