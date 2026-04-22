/**
 * NotesSection Component
 * Displays and manages notes/interaction log for a person.
 */

import { useState } from 'react';
import { usePersonNotes, useCreatePersonNote, useUpdatePersonNote, useDeletePersonNote } from '@/hooks/usePeople';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { ConfirmDialog } from '@/components/ui/ConfirmDialog';
import { useToast } from '@/contexts/ToastContext';
import type { PersonNoteDto, CreatePersonNoteRequest, UpdatePersonNoteRequest } from '@/services/api/types';

// ============================================================================
// Constants
// ============================================================================

const NOTE_TYPES = ['General', 'Prayer Request', 'Pastoral Visit', 'Counseling'] as const;
type NoteType = typeof NOTE_TYPES[number];

const NOTE_TYPE_BADGE_STYLES: Record<NoteType, string> = {
  General: 'bg-gray-100 text-gray-700',
  'Prayer Request': 'bg-purple-100 text-purple-800',
  'Pastoral Visit': 'bg-blue-100 text-blue-800',
  Counseling: 'bg-orange-100 text-orange-800',
};

// ============================================================================
// Helpers
// ============================================================================

function formatDate(isoString: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(isoString));
}

function toDateInputValue(isoString: string): string {
  // Convert ISO datetime to YYYY-MM-DD for <input type="date">
  return isoString.split('T')[0];
}

function toIsoDateTime(dateString: string): string {
  // Convert YYYY-MM-DD from date input to ISO datetime
  return `${dateString}T00:00:00.000Z`;
}

// ============================================================================
// Note Type Badge
// ============================================================================

interface NoteTypeBadgeProps {
  noteType: string | null;
}

function NoteTypeBadge({ noteType }: NoteTypeBadgeProps) {
  if (!noteType) return null;

  const styles = NOTE_TYPE_BADGE_STYLES[noteType as NoteType] ?? 'bg-gray-100 text-gray-700';

  return (
    <span className={`inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full ${styles}`}>
      {noteType}
    </span>
  );
}

// ============================================================================
// Note Form
// ============================================================================

interface NoteFormValues {
  text: string;
  noteDate: string;
  noteType: string;
  isPrivate: boolean;
  isAlert: boolean;
}

interface NoteFormProps {
  initial?: NoteFormValues;
  isSubmitting: boolean;
  onSubmit: (values: NoteFormValues) => void;
  onCancel: () => void;
  submitLabel: string;
}

function NoteForm({ initial, isSubmitting, onSubmit, onCancel, submitLabel }: NoteFormProps) {
  const today = new Date().toISOString().split('T')[0];

  const [text, setText] = useState(initial?.text ?? '');
  const [noteDate, setNoteDate] = useState(initial?.noteDate ?? today);
  const [noteType, setNoteType] = useState(initial?.noteType ?? '');
  const [isPrivate, setIsPrivate] = useState(initial?.isPrivate ?? false);
  const [isAlert, setIsAlert] = useState(initial?.isAlert ?? false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit({ text, noteDate, noteType, isPrivate, isAlert });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 bg-gray-50 rounded-lg p-4 border border-gray-200">
      <div>
        <label htmlFor="note-text" className="block text-sm font-medium text-gray-700 mb-1">
          Note <span className="text-red-500">*</span>
        </label>
        <textarea
          id="note-text"
          value={text}
          onChange={(e) => setText(e.target.value)}
          rows={4}
          required
          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 resize-y"
          placeholder="Enter note text..."
        />
      </div>

      <div className="flex flex-wrap gap-4">
        <div className="flex-1 min-w-40">
          <label htmlFor="note-date" className="block text-sm font-medium text-gray-700 mb-1">
            Date <span className="text-red-500">*</span>
          </label>
          <input
            id="note-date"
            type="date"
            value={noteDate}
            onChange={(e) => setNoteDate(e.target.value)}
            required
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          />
        </div>

        <div className="flex-1 min-w-40">
          <label htmlFor="note-type" className="block text-sm font-medium text-gray-700 mb-1">
            Type
          </label>
          <select
            id="note-type"
            value={noteType}
            onChange={(e) => setNoteType(e.target.value)}
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
          >
            <option value="">Select type...</option>
            {NOTE_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="flex flex-wrap gap-6">
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isPrivate}
            onChange={(e) => setIsPrivate(e.target.checked)}
            className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          Private
        </label>
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isAlert}
            onChange={(e) => setIsAlert(e.target.checked)}
            className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
          />
          Alert
        </label>
      </div>

      <div className="flex justify-end gap-2 pt-1">
        <button
          type="button"
          onClick={onCancel}
          disabled={isSubmitting}
          className="px-4 py-2 text-sm font-medium text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isSubmitting || !text.trim()}
          className="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 disabled:opacity-50 transition-colors"
        >
          {isSubmitting ? 'Saving...' : submitLabel}
        </button>
      </div>
    </form>
  );
}

// ============================================================================
// Note Item
// ============================================================================

interface NoteItemProps {
  note: PersonNoteDto;
  onEdit: (note: PersonNoteDto) => void;
  onDelete: (note: PersonNoteDto) => void;
}

function NoteItem({ note, onEdit, onDelete }: NoteItemProps) {
  return (
    <div className="py-4 border-b border-gray-100 last:border-0">
      <div className="flex items-start justify-between gap-2">
        <div className="flex flex-wrap items-center gap-2 text-sm text-gray-500">
          <span className="font-medium text-gray-900">{formatDate(note.noteDateTime)}</span>
          {note.noteTypeName && <NoteTypeBadge noteType={note.noteTypeName} />}
          {note.isAlert && (
            <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-red-100 text-red-700">
              Alert
            </span>
          )}
          {note.isPrivate && (
            <span className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-yellow-100 text-yellow-700">
              Private
            </span>
          )}
        </div>

        <div className="flex items-center gap-1 shrink-0">
          <button
            type="button"
            onClick={() => onEdit(note)}
            className="p-1 text-gray-400 hover:text-primary-600 rounded transition-colors"
            aria-label="Edit note"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
              />
            </svg>
          </button>
          <button
            type="button"
            onClick={() => onDelete(note)}
            className="p-1 text-gray-400 hover:text-red-600 rounded transition-colors"
            aria-label="Delete note"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
              />
            </svg>
          </button>
        </div>
      </div>

      <p className="mt-2 text-sm text-gray-800 whitespace-pre-wrap">{note.text}</p>

      {note.authorPersonName && (
        <p className="mt-1 text-xs text-gray-500">Added by {note.authorPersonName}</p>
      )}
    </div>
  );
}

// ============================================================================
// Loading Skeleton
// ============================================================================

function NotesSkeleton() {
  return (
    <div className="space-y-4" role="status" aria-label="Loading notes">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="py-4 border-b border-gray-100 last:border-0 space-y-2">
          <div className="flex gap-3">
            <Skeleton variant="text" height={16} width={120} />
            <Skeleton variant="text" height={16} width={80} />
          </div>
          <Skeleton variant="text" height={14} width="90%" />
          <Skeleton variant="text" height={14} width="70%" />
          <Skeleton variant="text" height={12} width={100} />
        </div>
      ))}
    </div>
  );
}

// ============================================================================
// Main Component
// ============================================================================

type FormMode = 'add' | 'edit';

interface ActiveForm {
  mode: FormMode;
  note?: PersonNoteDto;
}

interface NotesSectionProps {
  personIdKey: string;
}

export function NotesSection({ personIdKey }: NotesSectionProps) {
  const toast = useToast();
  const [activeForm, setActiveForm] = useState<ActiveForm | null>(null);
  const [noteToDelete, setNoteToDelete] = useState<PersonNoteDto | null>(null);

  const { data: notes, isLoading, isError } = usePersonNotes(personIdKey);
  const createMutation = useCreatePersonNote();
  const updateMutation = useUpdatePersonNote();
  const deleteMutation = useDeletePersonNote();

  const sortedNotes = notes
    ? [...notes].sort((a, b) => new Date(b.noteDateTime).getTime() - new Date(a.noteDateTime).getTime())
    : [];

  const handleAddClick = () => {
    setActiveForm({ mode: 'add' });
  };

  const handleEditClick = (note: PersonNoteDto) => {
    setActiveForm({ mode: 'edit', note });
  };

  const handleDeleteClick = (note: PersonNoteDto) => {
    setNoteToDelete(note);
  };

  const handleCancel = () => {
    setActiveForm(null);
  };

  const handleAddSubmit = async (values: NoteFormValues) => {
    const request: CreatePersonNoteRequest = {
      text: values.text,
      noteDate: toIsoDateTime(values.noteDate),
      noteTypeDefinedValueIdKey: null,
      isPrivate: values.isPrivate,
      isAlert: values.isAlert,
    };

    try {
      await createMutation.mutateAsync({ personIdKey, request });
      toast.success('Note added', 'The note has been saved successfully.');
      setActiveForm(null);
    } catch {
      toast.error('Failed to add note', 'Please try again.');
    }
  };

  const handleEditSubmit = async (values: NoteFormValues) => {
    if (!activeForm?.note) return;

    const request: UpdatePersonNoteRequest = {
      text: values.text,
      noteDate: toIsoDateTime(values.noteDate),
      noteTypeDefinedValueIdKey: null,
      isPrivate: values.isPrivate,
      isAlert: values.isAlert,
    };

    try {
      await updateMutation.mutateAsync({
        personIdKey,
        noteIdKey: activeForm.note.idKey,
        request,
      });
      toast.success('Note updated', 'The note has been updated successfully.');
      setActiveForm(null);
    } catch {
      toast.error('Failed to update note', 'Please try again.');
    }
  };

  const handleDeleteConfirm = async () => {
    if (!noteToDelete) return;

    try {
      await deleteMutation.mutateAsync({ personIdKey, noteIdKey: noteToDelete.idKey });
      toast.success('Note deleted', 'The note has been removed.');
      setNoteToDelete(null);
    } catch {
      toast.error('Failed to delete note', 'Please try again.');
    }
  };

  const isSubmitting =
    createMutation.isPending || updateMutation.isPending;

  const getEditInitialValues = (note: PersonNoteDto): NoteFormValues => ({
    text: note.text,
    noteDate: toDateInputValue(note.noteDateTime),
    noteType: note.noteTypeName ?? '',
    isPrivate: note.isPrivate,
    isAlert: note.isAlert,
  });

  return (
    <section className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Notes</h2>
        {activeForm === null && (
          <button
            type="button"
            onClick={handleAddClick}
            className="px-3 py-1.5 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors"
          >
            Add Note
          </button>
        )}
      </div>

      {/* Add form */}
      {activeForm?.mode === 'add' && (
        <div className="mb-6">
          <h3 className="text-md font-semibold text-gray-900 mb-3">Add Note</h3>
          <NoteForm
            isSubmitting={isSubmitting}
            onSubmit={handleAddSubmit}
            onCancel={handleCancel}
            submitLabel="Add Note"
          />
        </div>
      )}

      {/* Loading */}
      {isLoading && <NotesSkeleton />}

      {/* Error */}
      {isError && (
        <EmptyState
          title="Failed to load notes"
          description="There was a problem loading notes for this person. Please try again."
        />
      )}

      {/* Notes list */}
      {!isLoading && !isError && notes !== undefined && (
        <>
          {sortedNotes.length === 0 && activeForm?.mode !== 'add' && (
            <EmptyState
              title="No notes yet"
              description="Add a note to start keeping track of interactions with this person."
            />
          )}

          {sortedNotes.length > 0 && (
            <div>
              {sortedNotes.map((note) => (
                <div key={note.idKey}>
                  {activeForm?.mode === 'edit' && activeForm.note?.idKey === note.idKey ? (
                    <div className="py-4 border-b border-gray-100 last:border-0">
                      <NoteForm
                        initial={getEditInitialValues(note)}
                        isSubmitting={isSubmitting}
                        onSubmit={handleEditSubmit}
                        onCancel={handleCancel}
                        submitLabel="Save Changes"
                      />
                    </div>
                  ) : (
                    <NoteItem
                      note={note}
                      onEdit={handleEditClick}
                      onDelete={handleDeleteClick}
                    />
                  )}
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {/* Delete confirmation dialog */}
      <ConfirmDialog
        isOpen={noteToDelete !== null}
        onClose={() => setNoteToDelete(null)}
        onConfirm={handleDeleteConfirm}
        title="Delete Note"
        description="Are you sure you want to delete this note? This action cannot be undone."
        confirmLabel="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </section>
  );
}
