# Koinon RMS Web Application

React TypeScript frontend for Koinon RMS, built with Vite.

## Tech Stack

- **React 18** - UI framework
- **TypeScript** - Type safety (strict mode)
- **Vite** - Build tool and dev server
- **TailwindCSS** - Utility-first styling
- **TanStack Query** - Server state management
- **React Router** - Client-side routing

## Development

```bash
# Install dependencies
npm install

# Start dev server (http://localhost:5173)
npm run dev

# Type checking
npm run typecheck

# Linting
npm run lint

# Production build
npm run build

# Preview production build
npm run preview
```

## Environment Variables

Create a `.env` file based on `.env.example`:

```env
VITE_API_BASE_URL=http://localhost:5000
```

## Project Structure

```
src/
├── api/              # API client and service modules
├── components/       # Reusable React components
├── contexts/         # React contexts (auth, etc.)
├── hooks/            # Custom React hooks
├── lib/              # Utility functions
├── routes/           # Route components
├── App.tsx           # Root component
├── main.tsx          # Application entry point
└── index.css         # Global styles and Tailwind
```

## API Client

The API client in `src/api/client.ts` handles:
- Bearer token authentication
- Automatic token refresh
- Error handling
- Request/response transformation

## Coding Standards

- Functional components only (no class components)
- Strict TypeScript (no `any` types)
- TanStack Query for all server state
- Custom hooks for shared logic
- Minimum touch target size: 48px (use `touch-target` utility class)

## Performance Targets

- Touch response: <10ms
- API requests: <200ms
- Initial load: <1s
- Code splitting for routes
