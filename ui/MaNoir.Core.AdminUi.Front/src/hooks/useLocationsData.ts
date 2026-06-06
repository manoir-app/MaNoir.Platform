import * as React from 'react';
import { getLocations, upsertLocation, type LocationModel } from '../lib/api';

function sortLocationsByName(locations: LocationModel[]) {
  return [...locations].sort((left, right) => (left.name ?? '').localeCompare(right.name ?? '', undefined, { sensitivity: 'base' }));
}

function replaceLocation(currentLocations: LocationModel[], previousId: string, savedLocation: LocationModel) {
  const filteredLocations = currentLocations.filter((location) => location.id !== previousId && location.id !== savedLocation.id);
  return sortLocationsByName([...filteredLocations, savedLocation]);
}

export function useLocationsData() {
  const [locations, setLocations] = React.useState<LocationModel[]>([]);
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [isRefreshing, setIsRefreshing] = React.useState(false);
  const [isSaving, setIsSaving] = React.useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = React.useState<Date | null>(null);

  const loadLocations = React.useCallback(async (isManualRefresh: boolean) => {
    if (isManualRefresh) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    setErrorMessage(null);

    try {
      const loadedLocations = await getLocations();
      setLocations(sortLocationsByName(loadedLocations));
      setLastUpdatedAt(new Date());
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Unable to load locations.');
    } finally {
      if (isManualRefresh) {
        setIsRefreshing(false);
      } else {
        setIsLoading(false);
      }
    }
  }, []);

  React.useEffect(() => {
    void loadLocations(false);
  }, [loadLocations]);

  const saveLocation = React.useCallback(async (location: LocationModel) => {
    const previousId = location.id;

    setIsSaving(true);
    setErrorMessage(null);

    try {
      const savedLocation = await upsertLocation(previousId, location);
      setLocations((currentLocations) => replaceLocation(currentLocations, previousId, savedLocation));
      setLastUpdatedAt(new Date());
      return savedLocation;
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Unable to save location changes.');
      throw error;
    } finally {
      setIsSaving(false);
    }
  }, []);

  return {
    errorMessage,
    isLoading,
    isRefreshing,
    isSaving,
    lastUpdatedAt,
    locations,
    refreshLocations: async () => {
      await loadLocations(true);
    },
    saveLocation,
  };
}