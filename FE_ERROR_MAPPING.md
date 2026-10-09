Frontend error mapping — how to handle API error payloads

Purpose
- Provide a small snippet the FE (React) can use to interpret backend error responses produced by the SaaS API. The backend returns JSON like:
// { statusCode, message, errorCode, requestId, path, method, timestamp }

JavaScript mapping function (copy into a shared utils file):

```js
export function mapApiErrorToUserMessage(errorPayload) {
  if (!errorPayload) return { title: 'Error', message: 'Unknown error' };

  const { errorCode, message, statusCode, requestId } = errorPayload;

  // Default friendly fallback
  let userMessage = message || 'An unexpected error occurred';
  let type = 'error';

  switch (errorCode) {
	case 'INVALID_MODEL':
	  userMessage = 'Validation failed: ' + message; // message contains combined field errors
	  type = 'validation';
	  break;
	case 'DUPLICATE_KEY':
	  userMessage = 'A record with the same unique value already exists.';
	  type = 'conflict';
	  break;
	case 'FK_VIOLATION':
	  userMessage = 'Related entity not found. Please check referenced records.';
	  type = 'conflict';
	  break;
	case 'NULL_VALUE':
	  userMessage = 'A required field is missing. Please check the form.';
	  type = 'validation';
	  break;
	case 'TABLE_MISSING':
	  userMessage = 'Server not ready. Database schema missing (contact support).';
	  type = 'fatal';
	  break;
	case 'DB_ERROR':
	  userMessage = 'A database error occurred. Try again or contact support.';
	  type = 'fatal';
	  break;
	case 'NOT_FOUND':
	  userMessage = 'The requested resource was not found.';
	  type = 'info';
	  break;
	case 'FORBIDDEN':
	  userMessage = 'You do not have permission to perform this action.';
	  type = 'forbidden';
	  break;
	default:
	  // Preserve backend message if present (useful for validation messages)
	  userMessage = message || userMessage;
  }

  // Always include request id for support
  const developerInfo = requestId ? ` RequestId: ${requestId}` : '';

  return { type, title: userMessage, message: userMessage + developerInfo, statusCode };
}
```

Usage example in React (pseudo):

```js
try {
  const resp = await api.post('/um-api/users', payload);
  // success
} catch (err) {
  const payload = err.response?.data || null;
  const friendly = mapApiErrorToUserMessage(payload);
  showToast(friendly.title);
  console.error('API error', payload);
}
```

Notes
- FE should display `requestId` to the user (or copy it into a bug report) so backend logs can be correlated.
- For validation errors, the backend currently combines messages into a single string in message. FE may parse that string to highlight fields if a structured format is later provided.
- Keep UX friendly: only show detailed messages in admin panels; for regular users show generic text and record requestId.

