# Unity AI Frontend

Angular frontend application for the Unity AI Reporting system. Provides an intuitive chat interface for natural language data queries.

## Features

- Modern Angular 20 application
- Real-time chat interface for data queries
- SQL query visualization and explanation
- Responsive design with BC Government styling
- JWT authentication integration
- Service-worker ready for offline capabilities

## Tech Stack

- **Framework**: Angular 20.0.0
- **UI Components**: BC Sans typography, custom styling
- **HTTP Client**: Angular HttpClient with RxJS
- **Build Tools**: Angular CLI 20.0.3
- **Testing**: Jasmine, Karma

## Development Setup

### Local Development

1. Navigate to frontend directory:
```bash
cd src/unity-ai.ReportingAI.Frontend
```

2. Install dependencies:
```bash
npm install
```

3. Start development server:
```bash
npm start
```

The application will be available at http://localhost:4200

### Docker Development

From the application root directory:
```bash
docker-compose -f docker-compose.dev.yml up frontend
```

## Available Scripts

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm run watch` - Build and watch for changes
- `npm test` - Run unit tests
- `npm run ng` - Run Angular CLI commands

## Key Components

- `app.component.ts` - Main application component
- `sidebar.component.ts` - Navigation sidebar
- `sql-loader.component.ts` - SQL query loading interface
- `sql-explanation.component.ts` - SQL query explanation display
- `alert.component.ts` - Alert notifications
- `services/api.service.ts` - Backend API communication
- `services/auth.service.ts` - Authentication management

## Environment Configuration

The frontend uses Angular environments for configuration. JWT secrets and API endpoints are configured at build time.

## Styling

The application uses:
- BC Government design system (@bcgov/bc-sans)
- Custom CSS for component styling
- Responsive design principles
- Accessible color schemes and typography

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Testing

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.