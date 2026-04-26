---
name: create-entity-logic-mongo
description: 'Create one MaNoir entity slice with entity model, pure business rules, logic partials, and Mongo operations. Use when adding a new aggregate or document family with XLogic, XMongoOperations, and a clean split between pure logic and persistence.'
argument-hint: 'Describe the entity name, whether it belongs in Contracts, which operations are needed, and what rules must stay pure and persistence-free.'
user-invocable: true
---

# Create Entity Logic Mongo

Use this skill when you need to create one new backend entity slice in the MaNoir Core style.

## Goal

Create a coherent slice with:

- the entity model;
- pure business rules separated from persistence;
- the main logic class split into partial files by concern;
- a Mongo operations helper dedicated to database access.

## Code Style Constraints

When this skill creates or suggests project code, explicitly avoid:

- top-level statements;
- minimal APIs;
- nullable reference types enabled by default;
- implicit usings enabled by default.

Prefer explicit classes, explicit `using` directives, and classic project configuration.

## Target Pattern

Follow the current Core-style backend split:

1. one root partial logic class such as `XLogic.cs` for constructor and shared dependencies;
2. focused partial files such as:
- `XLogic.Persistence.cs` for read/write orchestration;
- `XLogic.Crud.cs` for basic lifecycle operations;
- `XBusinessRules.cs` or equivalent for pure operations and normalization helpers;
3. one `XMongoOperations.cs` class for direct MongoDB collection access and query primitives.

## Pure Logic Rule

Keep pure logic truly persistence-free.

Pure methods should:

- normalize identifiers;
- validate in-memory state;
- apply business rule transformations;
- decide whether a state change is allowed.

Pure methods should not:

- call MongoDB;
- depend on ASP.NET;
- depend on HTTP concepts;
- hide persistence access behind a pseudo-pure helper.

## Persistence Rule

`XMongoOperations` should stay low-level and explicit.

It should:

- expose collection access and focused Mongo queries;
- validate only raw persistence prerequisites such as missing identifiers;
- avoid embedding orchestration or business policy.

If the shared Core package already publishes the Mongo helper infrastructure through NuGet, use that public helper instead of rebuilding connection plumbing locally.

### Mongo Helper Example

```csharp
using MaNoir.Core.DataAccess;
using MongoDB.Driver;

public sealed class DeviceMongoOperations
{
	private readonly MongoDbHelper _mongo;
	private readonly IMongoCollection<Device> _collection;

	public DeviceMongoOperations()
	{
		_mongo = new MongoDbHelper();
		_collection = _mongo.GetCollection<Device>();
	}

	public Task<Device> GetByIdAsync(string id, CancellationToken cancellationToken = default)
	{
		return _collection.Find(device => device.Id == id).FirstOrDefaultAsync(cancellationToken);
	}
}
```

Assume the helper resolves the MongoDB connection through environment variables provided by the platform bootstrap.

## Contracts Placement

Put the entity in `Contracts` only when it is a real public exchange model.

If the type is only useful inside this repo, keep it out of `Contracts`.

## Procedure

1. Decide whether the entity model is public or internal.
2. Create the entity model in the right project.
3. Create the root partial `XLogic.cs` with constructor and dependencies.
4. Create the pure rules file for normalization and state transitions.
5. Create the persistence partial for orchestrated read/write methods.
6. Create `XMongoOperations.cs` for raw Mongo access.
7. Keep naming, normalization, and rule patterns aligned with the current Core codebase.

## Minimum Checklist

Before considering the slice complete, verify:

- identifiers are normalized consistently;
- pure rules do not call persistence;
- Mongo operations do not own business decisions;
- logic methods orchestrate the sequence clearly;
- contracts were introduced only if cross-repo exposure is justified.

## Output Format

Return or create:

- the target files to add;
- what each file owns;
- which methods are pure;
- which methods touch Mongo;
- which model types belong in Contracts and which stay internal.

## Targeted Excerpts

Use targeted patterns like these in the answer instead of referencing files from this repository.

### Logic Root Example

```csharp
public sealed partial class DeviceLogic
{
	private readonly DeviceMongoOperations _mongoOperations;

	public DeviceLogic()
	{
		_mongoOperations = new DeviceMongoOperations();
	}
}
```

### Pure Rules Example

```csharp
public sealed partial class DeviceLogic
{
	internal static string NormalizeDeviceId(string deviceId)
	{
		if (string.IsNullOrWhiteSpace(deviceId))
			return null;

		return deviceId.Trim().ToLowerInvariant();
	}
}
```

### Persistence Orchestration Example

```csharp
public sealed partial class DeviceLogic
{
	public async Task<Device> GetByIdAsync(string deviceId, CancellationToken cancellationToken = default)
	{
		string normalizedDeviceId = NormalizeDeviceId(deviceId);
		if (normalizedDeviceId == null)
			return null;

		return await _mongoOperations.GetByIdAsync(normalizedDeviceId, cancellationToken);
	}
}
```
