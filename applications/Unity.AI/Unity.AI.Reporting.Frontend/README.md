# Unity AI Reporting - Frontend

Angular frontend providing an intuitive chat interface for natural language data queries with AI-powered SQL generation.

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
- **Build**: Angular CLI 20.0.3

## Quick Start

### Using Docker Compose (Recommended)

From the `applications` directory:

```bash
# Development
docker-compose -f docker-compose.dev.yml up

# Production
docker-compose up -d
```

### Local Development

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
- `api.service.ts` - Backend API communication
- `auth.service.ts` - JWT authentication and token management
- `toast.service.ts` - Toast notification management
- `logger.service.ts` - Centralized logging service

## Routes

- `/` - Root route (redirects based on auth/admin status)
- `/app` - Main chat interface
- `/admin` - Admin feedback dashboard (admin only)

## Environment Configuration

The application uses Angular environments:

- `environment.ts` - Development configuration
- `environment.prod.ts` - Production configuration

Configuration includes:
- API URL
- Production flag for logging

## Authentication

The frontend uses JWT tokens for authentication:
- Tokens are passed via URL parameter or stored in localStorage
- Token validation and refresh handled by `auth.service.ts`
- Admin status checked via JWT payload (`is_it_admin` claim)

## Logging

The application uses a custom `LoggerService` with proper log levels:
- `DEBUG` - Development only (stripped in production)
- `INFO` - General information
- `WARN` - Warning messages
- `ERROR` - Error messages with stack traces

Production builds automatically set log level to `WARN` or higher.

## Styling

- BC Government Design System (@bcgov/bc-sans font)
- Custom CSS with responsive design
- Accessible color schemes and typography
- BC Government brand colors

## Building for Production

```bash
ng build --configuration production
```

Output is generated in `dist/` directory, optimized for performance.
