/** The part of a failed Axios request that is read here. */
interface ApiErrorLike {
  response?: {
    status?: number;
    data?: {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };
  };
}

/** The HTTP status of a failed request, or undefined when there was no answer (for example the server is down). */
export function getErrorStatus(error: unknown): number | undefined {
  return (error as ApiErrorLike | undefined)?.response?.status;
}

/**
 * Reads the user-facing message from a failed request. The API answers errors as "problem details"
 * (a "detail" text, or an "errors" map for invalid input); anything else falls back to a generic text.
 */
export function getApiErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  const response = (error as ApiErrorLike | undefined)?.response;
  const data = response?.data;

  if (data?.detail) return String(data.detail);
  if (data?.errors && typeof data.errors === 'object') {
    const first = Object.values(data.errors).flat()[0];
    if (first) return String(first);
  }
  if (data?.title) return String(data.title);
  if (response?.status === 401) return 'Your session has expired. Please sign in again.';
  if (response?.status === 403) return 'You do not have permission to do this.';
  if (!response) return 'Could not reach the server. Please check your connection.';
  return fallback;
}
