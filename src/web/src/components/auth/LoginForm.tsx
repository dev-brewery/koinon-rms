/**
 * Login Form Component
 * Handles user authentication with email/password
 */

import { useState, FormEvent } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { ApiClientError } from '../../services/api';

export function LoginForm() {
  const { login } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await login({ username, password });
    } catch (err) {
      // 1. Check for ApiClientError first (most specific)
      if (err instanceof ApiClientError) {
        if (err.statusCode === 401) {
          setError('Invalid username or password');
        } else if (err.statusCode === 408) {
          setError('Request timed out. Please try again.');
        } else {
          setError('Something went wrong. Please try again.');
        }
      }
      // 2. Check for network errors (TypeError from fetch)
      else if (err instanceof TypeError) {
        setError('Unable to connect to server. Please try again.');
      }
      // 3. Other Error instances
      else if (err instanceof Error) {
        setError('Something went wrong. Please try again.');
      }
      // 4. Unknown error types (fallback)
      else {
        setError('Something went wrong. Please try again.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-sm mx-auto">
      <div>
        <label
          htmlFor="username"
          className="block text-sm font-medium text-gray-700 mb-1"
        >
          Username
        </label>
        <input
          id="username"
          type="text"
          value={username}
          onChange={e => setUsername(e.target.value)}
          required
          autoComplete="username"
          className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      <div>
        <label
          htmlFor="password"
          className="block text-sm font-medium text-gray-700 mb-1"
        >
          Password
        </label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
          autoComplete="current-password"
          className="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      {error && (
        <div
          className="text-red-600 text-sm p-3 bg-red-50 rounded-md border border-red-200"
          role="alert"
        >
          {error}
        </div>
      )}

      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-blue-600 text-white py-2 px-4 rounded-md font-medium hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      >
        {isLoading ? 'Signing in...' : 'Sign In'}
      </button>
    </form>
  );
}
