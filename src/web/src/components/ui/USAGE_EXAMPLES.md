# UI Component Usage Examples

## useMutationWithToast Hook

The `useMutationWithToast` hook wraps TanStack Query's `useMutation` to automatically show toast notifications on success or error.

### Basic Usage

```typescript
import { useMutationWithToast } from '@/hooks/useMutationWithToast';
import * as peopleApi from '@/services/api/people';

function CreatePersonForm() {
  const createPerson = useMutationWithToast({
    mutationFn: peopleApi.createPerson,
    successMessage: 'Person created successfully',
    errorMessage: 'Failed to create person',
  });

  const handleSubmit = (data) => {
    createPerson.mutate(data);
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* form fields */}
    </form>
  );
}
```

### Dynamic Messages

```typescript
const updatePerson = useMutationWithToast({
  mutationFn: ({ idKey, data }) => peopleApi.updatePerson(idKey, data),
  successMessage: (person) => `Updated ${person.firstName} ${person.lastName}`,
  errorMessage: (error) => error.message || 'Failed to update person',
});
```

### With Custom Callbacks

```typescript
const deletePerson = useMutationWithToast({
  mutationFn: peopleApi.deletePerson,
  successMessage: 'Person deleted successfully',
  errorMessage: 'Failed to delete person',
  onSuccess: (data) => {
    // Custom logic after success
    queryClient.invalidateQueries({ queryKey: ['people'] });
  },
  onError: (error) => {
    // Custom error handling
    console.error('Delete failed:', error);
  },
});
```

## ConfirmDialog Component

The `ConfirmDialog` component displays a modal confirmation dialog for destructive or important actions.

### Danger Variant (Delete Operations)

```typescript
import { useState } from 'react';
import { ConfirmDialog } from '@/components/ui';

function DeletePersonButton({ personId }: { personId: string }) {
  const [showConfirm, setShowConfirm] = useState(false);
  const deletePerson = useMutationWithToast({
    mutationFn: peopleApi.deletePerson,
    successMessage: 'Person deleted successfully',
  });

  return (
    <>
      <button onClick={() => setShowConfirm(true)}>Delete</button>

      <ConfirmDialog
        isOpen={showConfirm}
        onClose={() => setShowConfirm(false)}
        onConfirm={() => {
          deletePerson.mutate(personId, {
            onSuccess: () => setShowConfirm(false),
          });
        }}
        title="Delete Person"
        description="Are you sure you want to delete this person? This action cannot be undone."
        confirmLabel="Delete"
        cancelLabel="Cancel"
        variant="danger"
        isLoading={deletePerson.isPending}
      />
    </>
  );
}
```

### Warning Variant (Important Actions)

```typescript
<ConfirmDialog
  isOpen={isOpen}
  onClose={onClose}
  onConfirm={handleArchive}
  title="Archive Group"
  description="This will archive the group and hide it from active lists. You can restore it later."
  confirmLabel="Archive"
  variant="warning"
  isLoading={isProcessing}
/>
```

### Info Variant (Confirmations)

```typescript
<ConfirmDialog
  isOpen={isOpen}
  onClose={onClose}
  onConfirm={handleSendEmail}
  title="Send Email"
  description="This will send an email to all 250 group members. Continue?"
  confirmLabel="Send"
  variant="info"
/>
```

### Features

- **Escape Key**: Closes dialog (when not loading)
- **Backdrop Click**: Closes dialog (when not loading)
- **Focus Trap**: Auto-focuses cancel button on open
- **Loading State**: Disables buttons and shows spinner
- **Accessibility**: Proper ARIA attributes, keyboard navigation
- **Body Scroll Lock**: Prevents background scrolling when open
