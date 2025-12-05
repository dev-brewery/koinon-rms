/**
 * Base HTTP client for API communication
 * Handles authentication, error handling, and request/response transformation
 */

export class ApiException extends Error {
  constructor(
    public status: number,
    public statusText: string,
    public response: string,
    public headers: Record<string, string>,
    public result?: any
  ) {
    super(`HTTP ${status}: ${statusText}`);
    this.name = 'ApiException';
  }
}

export interface ApiClientConfig {
  baseUrl: string;
  getAccessToken?: () => Promise<string | null>;
  onUnauthorized?: () => void;
}

/**
 * Base API client with authentication and error handling
 */
export class ApiClient {
  constructor(private config: ApiClientConfig) {}

  /**
   * Perform an authenticated fetch request
   */
  async fetch(url: string, init?: RequestInit): Promise<Response> {
    const headers = new Headers(init?.headers);

    // Add authentication token if available
    const token = await this.config.getAccessToken?.();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    // Always use JSON
    if (!headers.has('Content-Type') && init?.body) {
      headers.set('Content-Type', 'application/json');
    }

    const response = await fetch(`${this.config.baseUrl}${url}`, {
      ...init,
      headers,
    });

    // Handle 401 Unauthorized
    if (response.status === 401) {
      this.config.onUnauthorized?.();
      throw new ApiException(
        401,
        'Unauthorized',
        await response.text(),
        this.getHeaders(response),
        null
      );
    }

    // Handle other errors
    if (!response.ok) {
      const text = await response.text();
      throw new ApiException(
        response.status,
        response.statusText,
        text,
        this.getHeaders(response),
        null
      );
    }

    return response;
  }

  private getHeaders(response: Response): Record<string, string> {
    const headers: Record<string, string> = {};
    response.headers.forEach((value, key) => {
      headers[key] = value;
    });
    return headers;
  }
}

/**
 * Get the API base URL from environment variables
 */
export function getApiBaseUrl(): string {
  return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
}

/**
 * Default API client instance
 * This will be configured with auth context in the app
 */
let defaultClient: ApiClient | null = null;

export function configureApiClient(config: ApiClientConfig) {
  defaultClient = new ApiClient(config);
}

export function getApiClient(): ApiClient {
  if (!defaultClient) {
    // Fallback for cases where client isn't configured yet
    defaultClient = new ApiClient({
      baseUrl: getApiBaseUrl(),
    });
  }
  return defaultClient;
}
