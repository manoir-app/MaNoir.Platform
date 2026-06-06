import { create } from 'zustand';
import {
  getAdminNavigationDomain,
  getAdminNavigationDomains,
  type AdminDomainNavigationResponse,
  type AdminNavigationDomainSummary,
} from '../lib/api';

type LoadState = 'idle' | 'loading' | 'ready' | 'error';

interface AdminNavigationStoreState {
  domains: AdminNavigationDomainSummary[];
  domainsState: LoadState;
  domainMenus: Record<string, AdminDomainNavigationResponse>;
  domainStates: Record<string, LoadState>;
  loadDomains: () => Promise<void>;
  loadDomain: (domainId: string) => Promise<void>;
  clear: () => void;
}

export const useAdminNavigationStore = create<AdminNavigationStoreState>((set, get) => ({
  domains: [],
  domainsState: 'idle',
  domainMenus: {},
  domainStates: {},
  loadDomains: async () => {
    const { domainsState } = get();
    if (domainsState === 'loading' || domainsState === 'ready') {
      return;
    }

    set({ domainsState: 'loading' });

    try {
      const response = await getAdminNavigationDomains();
      set({
        domains: response.domains ?? [],
        domainsState: 'ready',
      });
    } catch {
      set({ domainsState: 'error' });
    }
  },
  loadDomain: async (domainId: string) => {
    if (!domainId) {
      return;
    }

    const currentState = get().domainStates[domainId];
    if (currentState === 'loading' || currentState === 'ready') {
      return;
    }

    set((state) => ({
      domainStates: {
        ...state.domainStates,
        [domainId]: 'loading',
      },
    }));

    try {
      const response = await getAdminNavigationDomain(domainId);
      set((state) => ({
        domainMenus: {
          ...state.domainMenus,
          [domainId]: response,
        },
        domainStates: {
          ...state.domainStates,
          [domainId]: 'ready',
        },
      }));
    } catch {
      set((state) => ({
        domainStates: {
          ...state.domainStates,
          [domainId]: 'error',
        },
      }));
    }
  },
  clear: () => {
    set({
      domains: [],
      domainsState: 'idle',
      domainMenus: {},
      domainStates: {},
    });
  },
}));