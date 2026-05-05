import * as React from 'react';
import { getRegisteredAgents, type RegisteredAgentModel } from '../lib/api';

export const staleAgentThresholdMs = 5 * 60 * 1000;

export function isAgentStale(agent: RegisteredAgentModel, now = Date.now()) {
  const heartbeatTime = Date.parse(agent.lastHeartbeatUtc);
  if (Number.isNaN(heartbeatTime)) {
    return false;
  }

  return now - heartbeatTime > staleAgentThresholdMs;
}

function sortAgentsByAttention(agents: RegisteredAgentModel[]) {
  return [...agents].sort((left, right) => {
    const leftStale = isAgentStale(left) ? 1 : 0;
    const rightStale = isAgentStale(right) ? 1 : 0;
    if (leftStale !== rightStale) {
      return rightStale - leftStale;
    }

    const leftSeverity = left.state === 'degraded' ? 1 : left.state === 'starting' || left.state === 'stopping' ? 2 : 3;
    const rightSeverity = right.state === 'degraded' ? 1 : right.state === 'starting' || right.state === 'stopping' ? 2 : 3;
    if (leftSeverity !== rightSeverity) {
      return leftSeverity - rightSeverity;
    }

    return left.agentId.localeCompare(right.agentId);
  });
}

function summarizeAgents(agents: RegisteredAgentModel[]) {
  const now = Date.now();

  return agents.reduce(
    (summary, agent) => {
      summary.totalCount += 1;

      if (agent.state === 'ready') {
        summary.readyCount += 1;
      }

      if (agent.state === 'degraded') {
        summary.degradedCount += 1;
      }

      if (isAgentStale(agent, now)) {
        summary.staleCount += 1;
      }

      return summary;
    },
    {
      totalCount: 0,
      readyCount: 0,
      degradedCount: 0,
      staleCount: 0,
    },
  );
}

export function useRegisteredAgentsData() {
  const [agents, setAgents] = React.useState<RegisteredAgentModel[]>([]);
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [isRefreshing, setIsRefreshing] = React.useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = React.useState<Date | null>(null);

  const loadAgents = async (isManualRefresh: boolean) => {
    if (isManualRefresh) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    setErrorMessage(null);

    try {
      const loadedAgents = await getRegisteredAgents();
      setAgents(sortAgentsByAttention(loadedAgents));
      setLastUpdatedAt(new Date());
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Unable to load the current agent registry.');
    } finally {
      if (isManualRefresh) {
        setIsRefreshing(false);
      } else {
        setIsLoading(false);
      }
    }
  };

  React.useEffect(() => {
    let isDisposed = false;

    const loadInitialAgents = async () => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const loadedAgents = await getRegisteredAgents();
        if (isDisposed) {
          return;
        }

        setAgents(sortAgentsByAttention(loadedAgents));
        setLastUpdatedAt(new Date());
      } catch (error) {
        if (isDisposed) {
          return;
        }

        setErrorMessage(error instanceof Error ? error.message : 'Unable to load the current agent registry.');
      } finally {
        if (!isDisposed) {
          setIsLoading(false);
        }
      }
    };

    void loadInitialAgents();

    return () => {
      isDisposed = true;
    };
  }, []);

  return {
    agents,
    errorMessage,
    isLoading,
    isRefreshing,
    lastUpdatedAt,
    refreshAgents: async () => {
      await loadAgents(true);
    },
    summary: summarizeAgents(agents),
  };
}