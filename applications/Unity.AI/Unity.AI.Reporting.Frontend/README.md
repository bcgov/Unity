# Unity AI Reporting - Frontend

Angular frontend providing an intuitive chat interface for natural language data queries with AI-powered SQL generation.

## Deployment

**Note**: The frontend is deployed as part of a **combined container** with the backend. Flask serves both the Angular static files and API endpoints. See the [main README](../README.md) for deployment instructions.

## Features

- Real-time chat interface for data queries
- Interactive SQL query visualization  
- SQL explanation and editing
- Chat history management
- Admin panel for feedback review
- User feedback and bug reporting
- JWT authentication with role-based access
- Responsive design with BC Government styling

## Tech Stack

- **Framework**: Angular 20.0.0
- **UI**: BC Sans typography, custom CSS
- **HTTP**: Angular HttpClient with RxJS
- **Build**: Angular CLI 20.0.3, esbuild

## Local Development

For local development (separate from Docker):

1. Install dependencies:
```bash
npm install
```

2. Start development server:
```bash
npm start
```

Application runs on `http://localhost:4200`

## Available Scripts

- `npm start` - Start development server (port 4200)
- `npm run build` - Production build
- `npm run watch` - Build and watch for changes
- `npm test` - Run unit tests with Karma

## Key Components

### Main Application
- `app.ts` - Main chat interface component
- `root.component.ts` - Route guard and authentication check

### Features
- `sidebar/sidebar.ts` - Chat history and navigation
- `admin/admin.component.ts` - Admin feedback dashboard
- `sql-explanation/sql-explanation.ts` - SQL query display
- `sql-loader/sql-loader.ts` - Loading animation
- `toast/toast.component.ts` - Toast notifications
- `alert/alert.ts` - Confirmation dialogs

### Services
- `api.service.ts` - Backend API communication (calls `/api/*`)
- `auth.service.ts` - JWT authentication and token management
- `config.service.ts` - Runtime configuration (loads `/config.json`)
- `toast.service.ts` - Toast notification management
- `logger.service.ts` - Centralized logging service

## Routes

- `/` - Root route (redirects based on auth/admin status)
- `/app` - Main chat interface (protected)
- `/admin` - Admin feedback dashboard (admin only)
- `/access-denied` - Access denied page

## Configuration

### Build-Time Configuration

The application loads configuration from `/config.json` at startup:

```json
{
  "environment": "production",
  "version": "1.0.0",
  "revision": "abc1234",
  "buildDate": "2025-01-15T10:30:00Z"
}
```

- **apiUrl**: Defaults to `/api` if not specified (for combined container)
- **environment**: `production` or `development`
- **version**: Application version from build args
- **revision**: Git revision from build args
- **buildDate**: ISO timestamp of build

Configuration is generated at build time in the Dockerfile.

### How It Works in Combined Container

1. **Build**: Angular app compiled to static files, `config.json` generated with build info
2. **Deploy**: Static files served by Flask at `/app/frontend`
3. **Runtime**: Angular loads `config.json` at startup
4. **API Calls**: All requests to `/api/*` go to Flask backend (same origin)

## Authentication

JWT tokens for authentication:
- Tokens passed via URL parameter (`?token=...`) or stored in localStorage
- Token validation handled by `auth.service.ts`
- Admin status checked via JWT payload (`is_it_admin` claim)
- Protected routes use `auth.guard.ts`

## Logging

Custom `LoggerService` with log levels:
- `DEBUG` - Development only
- `INFO` - General information
- `WARN` - Warning messages  
- `ERROR` - Error messages with stack traces

Production builds set log level to `WARN` or higher.

## Styling

- BC Government Design System (@bcgov/bc-sans font)
- Custom CSS with responsive design
- Accessible color schemes and typography
- BC Government brand colors

## Building for Production

```bash
npm run build
```

Output in `dist/recap/browser/` directory, optimized with esbuild.

**Note**: Production builds are handled by the Dockerfile in the combined container setup.
