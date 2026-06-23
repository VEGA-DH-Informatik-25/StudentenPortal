# Frontend

The Angular application lives under `CampusConnect/frontend`.

## Stack

- Angular 21 with standalone components
- Angular Router with lazy route components
- Signals for local UI state
- Zoneless change detection
- Functional guards and interceptors
- SCSS component styles
- Custom German/English i18n under `src/app/core/i18n`
- Theme preferences under `src/app/core/services/theme.ts`

## Commands

Run commands from `CampusConnect/frontend`:

```powershell
npm install
npm test
npm run build
npm start
```

The development server runs at `http://localhost:4200` and proxies `/api` to `http://localhost:5135` through `proxy.conf.json`.

## UI Preferences

The navbar settings menu contains language and appearance controls:

- `campusconnect.language`: non-sensitive language preference, `de` or `en`
- `campusconnect.theme`: non-sensitive theme preference, `system`, `light`, or `dark`

Authentication tokens must stay in memory and must never be stored in `localStorage` or `sessionStorage`.

## Text And Styling Rules

Add both German and English values for every new user-facing translation key. Use the `TranslatePipe` in templates and `I18n` in TypeScript for translated strings, locale formatting, and localized API error messages.

Use global design tokens from `src/styles.scss` for visible colors so components work in light and dark mode. Avoid hard-coded light surfaces in feature components.
