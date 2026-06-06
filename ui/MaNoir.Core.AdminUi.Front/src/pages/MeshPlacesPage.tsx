import { Dialog } from '@manoir-app/core-admin-ui-kit/dialog';
import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocationsData } from '../hooks/useLocationsData';
import type { LocationElementPropertiesModel, LocationModel, LocationRoomModel, LocationZoneModel } from '../lib/api';

type BuildingType = 'residential' | 'annex';
type RoomCategory = 'living' | 'wet' | 'circulation' | 'technical' | 'exterior';

interface RoomRecord {
  id: string;
  name: string;
  category: RoomCategory;
  areaM2: number;
  deviceCount: number;
  accent: 'sage' | 'sky' | 'sand';
}

interface LevelRecord {
  id: string;
  name: string;
  order: number;
  rooms: RoomRecord[];
}

interface BuildingRecord {
  id: string;
  name: string;
  type: BuildingType;
  surfaceM2: number;
  levels: LevelRecord[];
}

type ActiveDialog =
  | { kind: 'building'; mode: 'create' }
  | { kind: 'building'; mode: 'edit'; buildingId: string }
  | { kind: 'level'; mode: 'create'; buildingId: string }
  | { kind: 'level'; mode: 'edit'; buildingId: string; levelId: string }
  | { kind: 'room'; mode: 'create'; buildingId: string; levelId: string }
  | { kind: 'room'; mode: 'edit'; buildingId: string; levelId: string; roomId: string };

interface BuildingDraft {
  name: string;
  type: BuildingType;
  surfaceM2: string;
}

interface LevelDraft {
  name: string;
  order: string;
}

interface RoomDraft {
  name: string;
  category: RoomCategory;
  areaM2: string;
  deviceCount: string;
  accent: RoomRecord['accent'];
}

const roomKindByCategory: Record<RoomCategory, number> = {
  living: 5,
  wet: 3,
  circulation: 1,
  technical: 0,
  exterior: 9,
};

const metadataKeys = {
  buildingType: 'admin.buildingType',
  surfaceM2: 'admin.surfaceM2',
  levelOrder: 'admin.levelOrder',
  roomCategory: 'admin.roomCategory',
  roomAreaM2: 'admin.areaM2',
  roomAccent: 'admin.accent',
  roomDeviceCount: 'admin.deviceCount',
} as const;

function createId(prefix: string) {
  const randomId = globalThis.crypto?.randomUUID?.() ?? Math.random().toString(36).slice(2, 12);
  return `${prefix}-${randomId}`;
}

function createLocationId() {
  return createId('location').toLowerCase();
}

function createNestedId(prefix: string) {
  return createId(prefix).toUpperCase();
}

function parseInteger(value: string | undefined | null, fallback = 0) {
  const parsedValue = Number.parseInt(value ?? '', 10);
  return Number.isNaN(parsedValue) ? fallback : parsedValue;
}

function getMoreProperties(properties?: LocationElementPropertiesModel | null) {
  return properties?.moreProperties ?? {};
}

function getMetadataValue(properties: LocationElementPropertiesModel | null | undefined, key: string) {
  return getMoreProperties(properties)[key] ?? '';
}

function writeMetadata<T extends { properties?: LocationElementPropertiesModel | null }>(item: T, key: string, value: string): T {
  const moreProperties = { ...(item.properties?.moreProperties ?? {}) };

  if (!value) {
    delete moreProperties[key];
  } else {
    moreProperties[key] = value;
  }

  return {
    ...item,
    properties: {
      ...(item.properties ?? {}),
      moreProperties,
    },
  };
}

function normalizeBuildingType(value: string): BuildingType {
  return value === 'annex' ? 'annex' : 'residential';
}

function normalizeAccent(value: string): RoomRecord['accent'] {
  if (value === 'sky' || value === 'sand') {
    return value;
  }

  return 'sage';
}

function normalizeRoomCategory(value: string, roomKind: number): RoomCategory {
  if (value === 'living' || value === 'wet' || value === 'circulation' || value === 'technical' || value === 'exterior') {
    return value;
  }

  switch (roomKind) {
    case 1:
      return 'circulation';
    case 3:
    case 4:
      return 'wet';
    case 2:
    case 5:
    case 6:
    case 7:
    case 8:
      return 'living';
    case 9:
      return 'exterior';
    default:
      return 'technical';
  }
}

function inferLevelOrder(zone: LocationZoneModel) {
  return zone.rooms?.[0]?.floorLevel ?? 0;
}

function toRoomRecord(room: LocationRoomModel): RoomRecord {
  return {
    accent: normalizeAccent(getMetadataValue(room.properties, metadataKeys.roomAccent)),
    areaM2: Math.max(0, parseInteger(getMetadataValue(room.properties, metadataKeys.roomAreaM2), 0)),
    category: normalizeRoomCategory(getMetadataValue(room.properties, metadataKeys.roomCategory), room.roomKind),
    deviceCount: Math.max(0, parseInteger(getMetadataValue(room.properties, metadataKeys.roomDeviceCount), 0)),
    id: room.id,
    name: room.name,
  };
}

function toLevelRecord(zone: LocationZoneModel): LevelRecord {
  const order = parseInteger(getMetadataValue(zone.properties, metadataKeys.levelOrder), inferLevelOrder(zone));

  return {
    id: zone.id,
    name: zone.name,
    order,
    rooms: (zone.rooms ?? []).map(toRoomRecord),
  };
}

function toBuildingRecord(location: LocationModel): BuildingRecord {
  const levels = (location.zones ?? []).map(toLevelRecord);

  return {
    id: location.id,
    levels: sortLevels(levels),
    name: location.name,
    surfaceM2: Math.max(0, parseInteger(getMetadataValue(location.properties, metadataKeys.surfaceM2), 0)),
    type: normalizeBuildingType(getMetadataValue(location.properties, metadataKeys.buildingType)),
  };
}

function createLocationSkeleton(): LocationModel {
  return {
    hasAutomationsMesh: false,
    id: createLocationId(),
    kind: 0,
    measureAggregationRules: [],
    name: '',
    properties: {
      moreProperties: {},
    },
    zones: [],
  };
}

function createZoneSkeleton(order: number): LocationZoneModel {
  return writeMetadata(
    {
      id: createNestedId('ZONE'),
      measureAggregationRules: [],
      name: '',
      properties: {
        moreProperties: {},
      },
      rooms: [],
    },
    metadataKeys.levelOrder,
    String(order),
  );
}

function createRoomSkeleton(floorLevel: number): LocationRoomModel {
  return {
    floorLevel,
    groupMappingForServices: {},
    id: createNestedId('ROOM'),
    measureAggregationRules: [],
    name: '',
    properties: {
      moreProperties: {},
    },
    roomKind: roomKindByCategory.technical,
    roomMappingForServices: {},
    shape: [],
    walls: [],
  };
}

function applyBuildingDraft(location: LocationModel, draft: BuildingDraft) {
  let nextLocation: LocationModel = {
    ...location,
    name: draft.name.trim(),
    zones: location.zones ?? [],
  };

  nextLocation = writeMetadata(nextLocation, metadataKeys.buildingType, draft.type);
  nextLocation = writeMetadata(nextLocation, metadataKeys.surfaceM2, String(Math.max(1, parseInteger(draft.surfaceM2, 1))));
  return nextLocation;
}

function applyLevelDraft(zone: LocationZoneModel, draft: LevelDraft) {
  const nextOrder = parseInteger(draft.order, 0);
  let nextZone: LocationZoneModel = {
    ...zone,
    name: draft.name.trim(),
    rooms: (zone.rooms ?? []).map((room) => ({
      ...room,
      floorLevel: nextOrder,
    })),
  };

  nextZone = writeMetadata(nextZone, metadataKeys.levelOrder, String(nextOrder));
  return nextZone;
}

function applyRoomDraft(room: LocationRoomModel, draft: RoomDraft, floorLevel: number) {
  let nextRoom: LocationRoomModel = {
    ...room,
    floorLevel,
    name: draft.name.trim(),
    roomKind: roomKindByCategory[draft.category],
  };

  nextRoom = writeMetadata(nextRoom, metadataKeys.roomCategory, draft.category);
  nextRoom = writeMetadata(nextRoom, metadataKeys.roomAreaM2, String(Math.max(1, parseInteger(draft.areaM2, 1))));
  nextRoom = writeMetadata(nextRoom, metadataKeys.roomAccent, draft.accent);
  nextRoom = writeMetadata(nextRoom, metadataKeys.roomDeviceCount, String(Math.max(0, parseInteger(draft.deviceCount, 0))));
  return nextRoom;
}

function formatLevelOrder(order: number) {
  return order >= 0 ? `+${order}` : String(order);
}

function createBuildingDraft(building?: BuildingRecord): BuildingDraft {
  return {
    name: building?.name ?? '',
    type: building?.type ?? 'residential',
    surfaceM2: building ? String(building.surfaceM2) : '',
  };
}

function createLevelDraft(level?: LevelRecord): LevelDraft {
  return {
    name: level?.name ?? '',
    order: level ? String(level.order) : '0',
  };
}

function createRoomDraft(room?: RoomRecord): RoomDraft {
  return {
    name: room?.name ?? '',
    category: room?.category ?? 'living',
    areaM2: room ? String(room.areaM2) : '',
    deviceCount: room ? String(room.deviceCount) : '0',
    accent: room?.accent ?? 'sage',
  };
}

function sortLevels(levels: LevelRecord[]) {
  return [...levels].sort((left, right) => left.order - right.order);
}

export function MeshPlacesPage() {
  const { t } = useTranslation();
  const { errorMessage, isLoading, isRefreshing, isSaving, locations, refreshLocations, saveLocation } = useLocationsData();
  const buildings = locations.map(toBuildingRecord);
  const [selectedBuildingId, setSelectedBuildingId] = useState('');
  const [selectedLevelId, setSelectedLevelId] = useState('');
  const [activeDialog, setActiveDialog] = useState<ActiveDialog | null>(null);
  const [buildingDraft, setBuildingDraft] = useState<BuildingDraft>(createBuildingDraft());
  const [levelDraft, setLevelDraft] = useState<LevelDraft>(createLevelDraft());
  const [roomDraft, setRoomDraft] = useState<RoomDraft>(createRoomDraft());

  const selectedBuilding = buildings.find((building) => building.id === selectedBuildingId) ?? buildings[0] ?? null;
  const selectedLocation = locations.find((location) => location.id === selectedBuilding?.id) ?? locations[0] ?? null;
  const selectedLevel = selectedBuilding?.levels.find((level) => level.id === selectedLevelId) ?? selectedBuilding?.levels[0] ?? null;
  const selectedZone = selectedLocation?.zones?.find((zone) => zone.id === selectedLevel?.id) ?? selectedLocation?.zones?.[0] ?? null;

  useEffect(() => {
    if (!selectedBuilding) {
      if (selectedBuildingId) {
        setSelectedBuildingId('');
      }

      if (selectedLevelId) {
        setSelectedLevelId('');
      }

      return;
    }

    if (selectedBuilding.id !== selectedBuildingId) {
      setSelectedBuildingId(selectedBuilding.id);
      return;
    }

    if (!selectedLevel) {
      const fallbackLevelId = selectedBuilding.levels[0]?.id ?? '';
      if (fallbackLevelId !== selectedLevelId) {
        setSelectedLevelId(fallbackLevelId);
      }
      return;
    }

    if (selectedLevel.id !== selectedLevelId) {
      setSelectedLevelId(selectedLevel.id);
    }
  }, [selectedBuilding, selectedBuildingId, selectedLevel, selectedLevelId]);

  const totalLevels = buildings.reduce((sum, building) => sum + building.levels.length, 0);
  const totalRooms = buildings.reduce(
    (sum, building) => sum + building.levels.reduce((levelSum, level) => levelSum + level.rooms.length, 0),
    0,
  );
  const totalDevices = buildings.reduce(
    (sum, building) =>
      sum + building.levels.reduce((levelSum, level) => levelSum + level.rooms.reduce((roomSum, room) => roomSum + room.deviceCount, 0), 0),
    0,
  );
  const categoryCount = new Set(
    buildings.flatMap((building) => building.levels.flatMap((level) => level.rooms.map((room) => room.category))),
  ).size;

  function openBuildingCreateDialog() {
    setBuildingDraft(createBuildingDraft());
    setActiveDialog({ kind: 'building', mode: 'create' });
  }

  function openBuildingEditDialog(building: BuildingRecord) {
    setBuildingDraft(createBuildingDraft(building));
    setActiveDialog({ kind: 'building', mode: 'edit', buildingId: building.id });
  }

  function openLevelCreateDialog() {
    if (!selectedBuilding) {
      return;
    }

    setLevelDraft(createLevelDraft());
    setActiveDialog({ kind: 'level', mode: 'create', buildingId: selectedBuilding.id });
  }

  function openLevelEditDialog(level: LevelRecord) {
    if (!selectedBuilding) {
      return;
    }

    setLevelDraft(createLevelDraft(level));
    setActiveDialog({ kind: 'level', mode: 'edit', buildingId: selectedBuilding.id, levelId: level.id });
  }

  function openRoomCreateDialog() {
    if (!selectedBuilding || !selectedLevel) {
      return;
    }

    setRoomDraft(createRoomDraft());
    setActiveDialog({ kind: 'room', mode: 'create', buildingId: selectedBuilding.id, levelId: selectedLevel.id });
  }

  function openRoomEditDialog(room: RoomRecord) {
    if (!selectedBuilding || !selectedLevel) {
      return;
    }

    setRoomDraft(createRoomDraft(room));
    setActiveDialog({ kind: 'room', mode: 'edit', buildingId: selectedBuilding.id, levelId: selectedLevel.id, roomId: room.id });
  }

  function closeDialog() {
    setActiveDialog(null);
  }

  async function handleBuildingSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const existingLocation = activeDialog?.kind === 'building' && activeDialog.mode === 'edit'
      ? locations.find((location) => location.id === activeDialog.buildingId) ?? null
      : null;

    const savedLocation = await saveLocation(applyBuildingDraft(existingLocation ?? createLocationSkeleton(), buildingDraft));
    setSelectedBuildingId(savedLocation.id);
    setSelectedLevelId(savedLocation.zones?.[0]?.id ?? '');
    closeDialog();
  }

  async function handleLevelSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!activeDialog || activeDialog.kind !== 'level' || !selectedLocation) {
      return;
    }

    const nextZone = activeDialog.mode === 'edit'
      ? applyLevelDraft(selectedLocation.zones?.find((zone) => zone.id === activeDialog.levelId) ?? createZoneSkeleton(parseInteger(levelDraft.order, 0)), levelDraft)
      : applyLevelDraft(createZoneSkeleton(parseInteger(levelDraft.order, 0)), levelDraft);

    const savedLocation = await saveLocation({
      ...selectedLocation,
      zones:
        activeDialog.mode === 'edit'
          ? (selectedLocation.zones ?? []).map((zone) => (zone.id === activeDialog.levelId ? nextZone : zone))
          : [...(selectedLocation.zones ?? []), nextZone],
    });

    setSelectedBuildingId(savedLocation.id);
    setSelectedLevelId(nextZone.id);
    closeDialog();
  }

  async function handleRoomSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!activeDialog || activeDialog.kind !== 'room' || !selectedLocation || !selectedZone) {
      return;
    }

    const nextRoom = activeDialog.mode === 'edit'
      ? applyRoomDraft(selectedZone.rooms?.find((room) => room.id === activeDialog.roomId) ?? createRoomSkeleton(selectedLevel?.order ?? 0), roomDraft, selectedLevel?.order ?? 0)
      : applyRoomDraft(createRoomSkeleton(selectedLevel?.order ?? 0), roomDraft, selectedLevel?.order ?? 0);

    const savedLocation = await saveLocation({
      ...selectedLocation,
      zones: (selectedLocation.zones ?? []).map((zone) => {
        if (zone.id !== selectedZone.id) {
          return zone;
        }

        return {
          ...zone,
          rooms:
            activeDialog.mode === 'edit'
              ? (zone.rooms ?? []).map((room) => (room.id === activeDialog.roomId ? nextRoom : room))
              : [...(zone.rooms ?? []), nextRoom],
        };
      }),
    });

    setSelectedBuildingId(savedLocation.id);
    setSelectedLevelId(selectedZone.id);
    closeDialog();
  }

  const buildingDialogOpen = activeDialog?.kind === 'building';
  const levelDialogOpen = activeDialog?.kind === 'level';
  const roomDialogOpen = activeDialog?.kind === 'room';

  return (
    <div className="front-places-page">
      <section className="front-places-hero">
        <div className="front-places-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('placesPage.eyebrow')}</div>
            <h1 className="front-places-title">{t('placesPage.title')}</h1>
            <p className="front-places-copy">{t('placesPage.description')}</p>
          </div>
          <button
            className="front-button front-button-secondary front-button-large"
            disabled={isRefreshing || isSaving}
            onClick={() => {
              void refreshLocations();
            }}
            type="button"
          >
            {isRefreshing ? t('common.actions.refreshing') : t('common.actions.refresh')}
          </button>
        </div>
        {errorMessage ? <div className="front-observability-feedback front-observability-feedback-error">{errorMessage}</div> : null}
      </section>

      <section className="front-places-summary-grid" aria-label={t('placesPage.summary.ariaLabel')}>
        <article className="front-places-summary-card">
          <div className="front-console-stat-value">{buildings.length}</div>
          <div className="front-console-stat-label">{t('placesPage.summary.buildings')}</div>
        </article>
        <article className="front-places-summary-card">
          <div className="front-console-stat-value">{totalLevels}</div>
          <div className="front-console-stat-label">{t('placesPage.summary.levels')}</div>
        </article>
        <article className="front-places-summary-card">
          <div className="front-console-stat-value">{totalRooms}</div>
          <div className="front-console-stat-label">{t('placesPage.summary.rooms')}</div>
          <div className="front-console-stat-detail">{t('placesPage.summary.categories', { count: categoryCount })}</div>
        </article>
        <article className="front-places-summary-card">
          <div className="front-console-stat-value">{totalDevices}</div>
          <div className="front-console-stat-label">{t('placesPage.summary.devices')}</div>
        </article>
      </section>

      <section className="front-places-panel">
        <div className="front-places-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('placesPage.buildings.eyebrow')}</div>
            <h2 className="front-places-section-title">{t('placesPage.buildings.title')}</h2>
          </div>
          <button className="front-button front-button-secondary front-button-large" onClick={openBuildingCreateDialog} type="button">
            {t('placesPage.buildings.add')}
          </button>
        </div>

        <div className="front-places-table-wrap">
          <table className="front-places-table">
            <thead>
              <tr>
                <th>{t('placesPage.buildings.columns.name')}</th>
                <th>{t('placesPage.buildings.columns.type')}</th>
                <th>{t('placesPage.buildings.columns.surface')}</th>
                <th>{t('placesPage.buildings.columns.levels')}</th>
                <th>{t('placesPage.buildings.columns.rooms')}</th>
                <th aria-label={t('placesPage.buildings.columns.actions')} />
              </tr>
            </thead>
            <tbody>
              {isLoading && buildings.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    <div className="front-observability-feedback">{t('placesPage.feedback.loading')}</div>
                  </td>
                </tr>
              ) : null}
              {!isLoading && buildings.length === 0 ? (
                <tr>
                  <td colSpan={6}>
                    <div className="front-observability-feedback">{t('placesPage.buildings.empty')}</div>
                  </td>
                </tr>
              ) : null}
              {buildings.map((building, index) => {
                const roomCount = building.levels.reduce((sum, level) => sum + level.rooms.length, 0);
                const isSelected = building.id === selectedBuilding?.id;

                return (
                  <tr className={isSelected ? 'front-places-table-row-active' : undefined} key={building.id}>
                    <td>
                      <button
                        className="front-places-table-name"
                        onClick={() => {
                          setSelectedBuildingId(building.id);
                          setSelectedLevelId(building.levels[0]?.id ?? '');
                        }}
                        type="button"
                      >
                        <span className="front-places-table-index">{String(index + 1).padStart(2, '0')}</span>
                        <span>{building.name}</span>
                      </button>
                    </td>
                    <td>{t(`placesPage.buildings.types.${building.type}`)}</td>
                    <td>{building.surfaceM2} m2</td>
                    <td>{building.levels.length}</td>
                    <td>{roomCount}</td>
                    <td className="front-places-table-actions">
                      <button className="front-button front-button-secondary front-button-small" onClick={() => openBuildingEditDialog(building)} type="button">
                        {t('placesPage.actions.edit')}
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      <section className="front-places-panel">
        <div className="front-places-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('placesPage.levels.eyebrow')}</div>
            <h2 className="front-places-section-title">{t('placesPage.levels.title')}</h2>
            <p className="front-places-panel-copy">{t('placesPage.levels.description')}</p>
          </div>
          <div className="front-places-header-actions">
            <button
              className="front-button front-button-secondary front-button-large"
              disabled={!selectedBuilding}
              onClick={openLevelCreateDialog}
              type="button"
            >
              {t('placesPage.levels.addLevel')}
            </button>
            <button
              className="front-button front-button-primary front-button-large"
              disabled={!selectedLevel}
              onClick={openRoomCreateDialog}
              type="button"
            >
              {t('placesPage.levels.addRoom')}
            </button>
          </div>
        </div>

        <div className="front-places-workspace">
          <aside className="front-places-sidebar">
            {buildings.map((building) => (
              <div className="front-places-sidebar-group" key={building.id}>
                <div className="front-places-sidebar-title">{building.name}</div>
                <div className="front-places-sidebar-list">
                  {sortLevels(building.levels).map((level) => {
                    const isActive = building.id === selectedBuilding?.id && level.id === selectedLevel?.id;

                    return (
                      <div className={`front-places-level-item${isActive ? ' front-places-level-item-active' : ''}`} key={level.id}>
                        <button
                          className="front-places-level-trigger"
                          onClick={() => {
                            setSelectedBuildingId(building.id);
                            setSelectedLevelId(level.id);
                          }}
                          type="button"
                        >
                          <span>{level.order >= 0 ? `+${level.order}` : String(level.order)}</span>
                          <span>{level.name}</span>
                          <span>{level.rooms.length}</span>
                        </button>
                        {building.id === selectedBuilding?.id ? (
                          <button className="front-places-inline-edit" onClick={() => openLevelEditDialog(level)} type="button">
                            {t('placesPage.actions.edit')}
                          </button>
                        ) : null}
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </aside>

          <div className="front-places-stage">
            <div className="front-places-stage-header">
              <div className="front-places-stage-path">
                {selectedBuilding ? `${selectedBuilding.name} -> ${selectedLevel?.name ?? ''}` : t('placesPage.levels.noSelection')}
              </div>
              <div className="front-places-stage-count">
                {selectedLevel ? t('placesPage.levels.roomCount', { count: selectedLevel.rooms.length }) : '0'}
              </div>
            </div>

            <div className="front-places-room-canvas">
              {selectedLevel?.rooms.length ? (
                selectedLevel.rooms.map((room) => (
                  <button
                    className={`front-places-room-tile front-places-room-tile-${room.accent}`}
                    key={room.id}
                    onClick={() => openRoomEditDialog(room)}
                    type="button"
                  >
                    <span className="front-places-room-name">{room.name}</span>
                    <span className="front-places-room-meta">{room.areaM2} m2 · {room.deviceCount} app.</span>
                  </button>
                ))
              ) : (
                <div className="front-observability-feedback">{selectedBuilding ? t('placesPage.levels.emptyRooms') : t('placesPage.levels.noSelection')}</div>
              )}
              <div className="front-places-room-canvas-label">{t('placesPage.levels.canvasLabel')}</div>
            </div>

            <div className="front-places-room-list">
              {selectedLevel?.rooms.map((room, index) => (
                <article className="front-places-room-row" key={room.id}>
                  <div className="front-places-room-row-main">
                    <span className={`front-places-room-dot front-places-room-dot-${room.accent}`} />
                    <span className="front-places-room-row-index">{String(index + 1).padStart(2, '0')}</span>
                    <div>
                      <h3 className="front-places-room-row-title">{room.name}</h3>
                    </div>
                  </div>
                  <div className="front-places-room-row-metrics">
                    <span>{t(`placesPage.roomCategories.${room.category}`)}</span>
                    <span>{room.areaM2} m2</span>
                    <span>{t('placesPage.levels.devices', { count: room.deviceCount })}</span>
                    <button className="front-button front-button-secondary front-button-small" onClick={() => openRoomEditDialog(room)} type="button">
                      {t('placesPage.actions.edit')}
                    </button>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </div>
      </section>

      <Dialog
        closeLabel={t('placesPage.dialogs.close')}
        description={t(
          activeDialog?.kind === 'building' && activeDialog.mode === 'edit'
            ? 'placesPage.dialogs.building.editDescription'
            : 'placesPage.dialogs.building.createDescription',
        )}
        footer={
          <>
            <button className="front-button front-button-secondary front-button-large" onClick={closeDialog} type="button">
              {t('placesPage.dialogs.cancel')}
            </button>
            <button className="front-button front-button-primary front-button-large" disabled={isSaving} form="places-building-form" type="submit">
              {t(activeDialog?.kind === 'building' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.save' : 'placesPage.dialogs.create')}
            </button>
          </>
        }
        onOpenChange={(open) => {
          if (!open) {
            closeDialog();
          }
        }}
        open={buildingDialogOpen}
        size="md"
        title={t(activeDialog?.kind === 'building' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.building.editTitle' : 'placesPage.dialogs.building.createTitle')}
      >
        <form className="front-dialog-form" id="places-building-form" onSubmit={handleBuildingSubmit}>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.name')}</span>
            <input
              className="front-dialog-input"
              onChange={(event) => setBuildingDraft((current) => ({ ...current, name: event.target.value }))}
              required
              type="text"
              value={buildingDraft.name}
            />
          </label>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.type')}</span>
            <select
              className="front-dialog-input"
              onChange={(event) => setBuildingDraft((current) => ({ ...current, type: event.target.value as BuildingType }))}
              value={buildingDraft.type}
            >
              <option value="residential">{t('placesPage.buildings.types.residential')}</option>
              <option value="annex">{t('placesPage.buildings.types.annex')}</option>
            </select>
          </label>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.surface')}</span>
            <input
              className="front-dialog-input"
              min="1"
              onChange={(event) => setBuildingDraft((current) => ({ ...current, surfaceM2: event.target.value }))}
              required
              type="number"
              value={buildingDraft.surfaceM2}
            />
          </label>
        </form>
      </Dialog>

      <Dialog
        closeLabel={t('placesPage.dialogs.close')}
        description={t(activeDialog?.kind === 'level' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.level.editDescription' : 'placesPage.dialogs.level.createDescription')}
        footer={
          <>
            <button className="front-button front-button-secondary front-button-large" onClick={closeDialog} type="button">
              {t('placesPage.dialogs.cancel')}
            </button>
            <button className="front-button front-button-primary front-button-large" disabled={isSaving} form="places-level-form" type="submit">
              {t(activeDialog?.kind === 'level' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.save' : 'placesPage.dialogs.create')}
            </button>
          </>
        }
        onOpenChange={(open) => {
          if (!open) {
            closeDialog();
          }
        }}
        open={levelDialogOpen}
        size="sm"
        title={t(activeDialog?.kind === 'level' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.level.editTitle' : 'placesPage.dialogs.level.createTitle')}
      >
        <form className="front-dialog-form" id="places-level-form" onSubmit={handleLevelSubmit}>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.name')}</span>
            <input
              className="front-dialog-input"
              onChange={(event) => setLevelDraft((current) => ({ ...current, name: event.target.value }))}
              required
              type="text"
              value={levelDraft.name}
            />
          </label>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.order')}</span>
            <input
              className="front-dialog-input"
              onChange={(event) => setLevelDraft((current) => ({ ...current, order: event.target.value }))}
              required
              type="number"
              value={levelDraft.order}
            />
          </label>
        </form>
      </Dialog>

      <Dialog
        closeLabel={t('placesPage.dialogs.close')}
        description={t(activeDialog?.kind === 'room' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.room.editDescription' : 'placesPage.dialogs.room.createDescription')}
        footer={
          <>
            <button className="front-button front-button-secondary front-button-large" onClick={closeDialog} type="button">
              {t('placesPage.dialogs.cancel')}
            </button>
            <button className="front-button front-button-primary front-button-large" disabled={isSaving} form="places-room-form" type="submit">
              {t(activeDialog?.kind === 'room' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.save' : 'placesPage.dialogs.create')}
            </button>
          </>
        }
        onOpenChange={(open) => {
          if (!open) {
            closeDialog();
          }
        }}
        open={roomDialogOpen}
        size="md"
        title={t(activeDialog?.kind === 'room' && activeDialog.mode === 'edit' ? 'placesPage.dialogs.room.editTitle' : 'placesPage.dialogs.room.createTitle')}
      >
        <form className="front-dialog-form" id="places-room-form" onSubmit={handleRoomSubmit}>
          <label className="front-dialog-field">
            <span className="front-dialog-label">{t('placesPage.dialogs.fields.name')}</span>
            <input
              className="front-dialog-input"
              onChange={(event) => setRoomDraft((current) => ({ ...current, name: event.target.value }))}
              required
              type="text"
              value={roomDraft.name}
            />
          </label>
          <div className="front-dialog-grid">
            <label className="front-dialog-field">
              <span className="front-dialog-label">{t('placesPage.dialogs.fields.category')}</span>
              <select
                className="front-dialog-input"
                onChange={(event) => setRoomDraft((current) => ({ ...current, category: event.target.value as RoomCategory }))}
                value={roomDraft.category}
              >
                <option value="living">{t('placesPage.roomCategories.living')}</option>
                <option value="wet">{t('placesPage.roomCategories.wet')}</option>
                <option value="circulation">{t('placesPage.roomCategories.circulation')}</option>
                <option value="technical">{t('placesPage.roomCategories.technical')}</option>
                <option value="exterior">{t('placesPage.roomCategories.exterior')}</option>
              </select>
            </label>
            <label className="front-dialog-field">
              <span className="front-dialog-label">{t('placesPage.dialogs.fields.accent')}</span>
              <select
                className="front-dialog-input"
                onChange={(event) => setRoomDraft((current) => ({ ...current, accent: event.target.value as RoomRecord['accent'] }))}
                value={roomDraft.accent}
              >
                <option value="sage">{t('placesPage.dialogs.accents.sage')}</option>
                <option value="sky">{t('placesPage.dialogs.accents.sky')}</option>
                <option value="sand">{t('placesPage.dialogs.accents.sand')}</option>
              </select>
            </label>
            <label className="front-dialog-field">
              <span className="front-dialog-label">{t('placesPage.dialogs.fields.area')}</span>
              <input
                className="front-dialog-input"
                min="1"
                onChange={(event) => setRoomDraft((current) => ({ ...current, areaM2: event.target.value }))}
                required
                type="number"
                value={roomDraft.areaM2}
              />
            </label>
            <label className="front-dialog-field">
              <span className="front-dialog-label">{t('placesPage.dialogs.fields.devices')}</span>
              <input
                className="front-dialog-input"
                min="0"
                onChange={(event) => setRoomDraft((current) => ({ ...current, deviceCount: event.target.value }))}
                required
                type="number"
                value={roomDraft.deviceCount}
              />
            </label>
          </div>
        </form>
      </Dialog>
    </div>
  );
}