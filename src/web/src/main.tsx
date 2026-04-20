import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import App from './App.tsx';
import './index.css';
import { initErrorTracking } from './services/errorTracking';

// Initialize error tracking
initErrorTracking();

// Configure React Query
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000, // 30 seconds
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

const DevtoolsPortal =
  import.meta.env.DEV && !import.meta.env.VITE_E2E
    ? React.lazy(() =>
        import('@tanstack/react-query-devtools').then((m) => ({
          default: m.ReactQueryDevtools,
        })),
      )
    : null;

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
      {DevtoolsPortal ? (
        <React.Suspense fallback={null}>
          <DevtoolsPortal initialIsOpen={false} />
        </React.Suspense>
      ) : null}
    </QueryClientProvider>
  </React.StrictMode>
);
