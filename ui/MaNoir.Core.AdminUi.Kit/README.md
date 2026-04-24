# MaNoir.Core.AdminUi.Kit

React/Radix component library and visual foundations for MaNoir back-office applications.

## Scope

The package only contains:

- design tokens;
- reusable UI primitives;
- generic back-office technical patterns.

The package does not contain:

- business pages;
- business workflows;
- components explicitly tied to Platform, CommunicationHub, or a domain.

## Commands

- `npm run build`
- `npm run storybook`
- `npm run build-storybook`
- `npm run typecheck`

## Initial Surface

- `Button`
- `Card`
- `Field`
- `TextField`

## Usage

```tsx
import '@manoir-app/core-admin-ui-kit/styles.css';
import { Button, Card, Field, TextField } from '@manoir-app/core-admin-ui-kit';
```