/**
 * NotesSection Component
 * Displays and manages interaction notes for a person
 */

import { useState } from 'react';
import { cn } from '@/lib/utils';
import { usePersonNotes, useCreateNote, useUpdateNote, useDeleteNote } from '@/hooks/usePeople';
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from '@/services/api/types';

// Hardcoded note type options (will be fetched from DefinedValues API in future)
const NOTE_TYPE_OPTIONS = [
  { label: 'General', value: 'general' },
  { label: 'Prayer Request', value: 'prayer-request' },
  { label: 'Pastoral Visit', value: 'pastoral-visit' },
  { label: 'Counseling', value: 'counseling' },
] as const;

type NoteTypeValue = (typeof NOTE_TYPE_OPTIONS)[number]['value'];

const NOTE_TYPE_COLORS: Record<NoteTypeValue, string> = {
  general: 'bg-gray-100 text-gray-700',
  'prayer-request': 'bg-purple-100 text-purple-700',
  'pastoral-visit': 'bg-blue-100 text-blue-700',
  counseling: 'bg-green-100 text-green-700',
};

function getNoteTypeColor(noteTypeName: string): string {
  const normalized = noteTypeName.toLowerCase().replace(/\s+/g, '-') as NoteTypeValue;
  return NOTE_TYPE_COLORS[normalized] ?? 'bg-gray-100 text-gray-700';
}

function formatNoteDate(dateTime: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(dateTime));
}

// ---------------------------------------------------------------------------
// Add Note Form
// ---------------------------------------------------------------------------

interface AddNoteFormProps {
  personIdKey: string;
  onCancel: () => void;
}

function AddNoteForm({ personIdKey, onCancel }: AddNoteFormProps) {
  const createMutation = useCreateNote(personIdKey);
  const [noteTypeValue, setNoteTypeValue] = useState<string>(NOTE_TYPE_OPTIONS[0].value);
  const [text, setText] = useState('');
  const [noteDateTime, setNoteDateTime] = useState('');
  const [isPrivate, setIsPrivate] = useState(false);
  const [isAlert, setIsAlert] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) return;

    const request: CreateNoteRequest = {
      noteTypeValueIdKey: noteTypeValue,
      text: text.trim(),
      isPrivate,
      isAlert,
      ...(noteDateTime ? { noteDateTime: new Date(noteDateTime).toISOString() } : {}),
    };

    await createMutation.mutateAsync(request);
    onCancel();
  };

  return (
    <form
      onSubmit={(e) => void handleSubmit(e)}
      className="bg-gray-50 rounded-lg border border-gray-200 p-4 space-y-4"
    >
      <h3 className="text-sm font-semibold text-gray-900">Add Note</h3>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Note Type</label>
        <select
          value={noteTypeValue}
          onChange={(e) => setNoteTypeValue(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-600"
        >
          {NOTE_TYPE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Note</label>
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="Enter note..."
          rows={4}
          required
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-600 resize-y"
        />
      </div>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">
          Note Date (optional)
        </label>
        <input
          type="datetime-local"
          value={noteDateTime}
          onChange={(e) => setNoteDateTime(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-600"
        />
      </div>

      <div className="flex items-center gap-6">
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isPrivate}
            onChange={(e) => setIsPrivate(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-600"
          />
          Private
        </label>
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isAlert}
            onChange={(e) => setIsAlert(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-600"
          />
          Alert
        </label>
      </div>

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={createMutation.isPending || !text.trim()}
          className="px-4 py-2 bg-primary-600 text-white rounded-lg text-sm hover:bg-primary-700 transition-colors disabled:opacity-50"
        >
          {createMutation.isPending ? 'Saving...' : 'Save Note'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={createMutation.isPending}
          className="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg text-sm hover:bg-gray-300 transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Edit Note Form (inline)
// ---------------------------------------------------------------------------

interface EditNoteFormProps {
  personIdKey: string;
  note: NoteDto;
  onCancel: () => void;
}

function EditNoteForm({ personIdKey, note, onCancel }: EditNoteFormProps) {
  const updateMutation = useUpdateNote(personIdKey);
  const [noteTypeValue, setNoteTypeValue] = useState(note.noteTypeValueIdKey);
  const [text, setText] = useState(note.text);
  const [isPrivate, setIsPrivate] = useState(note.isPrivate);
  const [isAlert, setIsAlert] = useState(note.isAlert);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) return;

    const request: UpdateNoteRequest = {
      noteTypeValueIdKey: noteTypeValue,
      text: text.trim(),
      isPrivate,
      isAlert,
    };

    await updateMutation.mutateAsync({ noteIdKey: note.idKey, request });
    onCancel();
  };

  return (
    <form
      onSubmit={(e) => void handleSubmit(e)}
      className="space-y-3 p-4 bg-gray-50 rounded-lg border border-gray-200"
    >
      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Note Type</label>
        <select
          value={noteTypeValue}
          onChange={(e) => setNoteTypeValue(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-600"
        >
          {NOTE_TYPE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      <div>
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          rows={4}
          required
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary-600 resize-y"
        />
      </div>

      <div className="flex items-center gap-6">
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isPrivate}
            onChange={(e) => setIsPrivate(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-600"
          />
          Private
        </label>
        <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
          <input
            type="checkbox"
            checked={isAlert}
            onChange={(e) => setIsAlert(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-600"
          />
          Alert
        </label>
      </div>

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={updateMutation.isPending || !text.trim()}
          className="px-3 py-1.5 bg-primary-600 text-white rounded-md text-sm hover:bg-primary-700 transition-colors disabled:opacity-50"
        >
          {updateMutation.isPending ? 'Saving...' : 'Save'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={updateMutation.isPending}
          className="px-3 py-1.5 bg-gray-200 text-gray-700 rounded-md text-sm hover:bg-gray-300 transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Single Note Row
// ---------------------------------------------------------------------------

interface NoteRowProps {
  personIdKey: string;
  note: NoteDto;
}

function NoteRow({ personIdKey, note }: NoteRowProps) {
  const deleteMutation = useDeleteNote(personIdKey);
  const [isEditing, setIsEditing] = useState(false);
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false);

  const handleDelete = async () => {
    await deleteMutation.mutateAsync(note.idKey);
    setIsConfirmingDelete(false);
  };

  return (
    <li
      className={cn(
        'pl-4 pr-4 py-4 rounded-lg border',
        note.isAlert
          ? 'border-l-4 border-l-amber-400 border-t-gray-200 border-r-gray-200 border-b-gray-200'
          : 'border-gray-200',
      )}
    >
      {isEditing ? (
        <EditNoteForm
          personIdKey={personIdKey}
          note={note}
          onCancel={() => setIsEditing(false)}
        />
      ) : (
        <div className="space-y-2">
          {/* Header row: type badge, flags, actions */}
          <div className="flex items-start justify-between gap-2">
            <div className="flex items-center gap-2 flex-wrap">
              <span
                className={cn(
                  'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium',
                  getNoteTypeColor(note.noteTypeName),
                )}
              >
                {note.noteTypeName}
              </span>
              {note.isAlert && (
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-700">
                  Alert
                </span>
              )}
              {note.isPrivate && (
                <span
                  className="inline-flex items-center gap-1 text-xs text-gray-500"
                  title="Private note"
                >
                  {/* Lock icon */}
                  <svg
                    className="w-3.5 h-3.5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                    />
                  </svg>
                  Private
                </span>
              )}
            </div>

            <div className="flex items-center gap-1 shrink-0">
              <button
                type="button"
                onClick={() => setIsEditing(true)}
                className="p-1 text-gray-400 hover:text-gray-700 rounded transition-colors"
                aria-label="Edit note"
              >
                <svg
                  className="w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"
                  />
                </svg>
              </button>
              <button
                type="button"
                onClick={() => setIsConfirmingDelete(true)}
                className="p-1 text-gray-400 hover:text-red-600 rounded transition-colors"
                aria-label="Delete note"
              >
                <svg
                  className="w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
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

          {/* Note text */}
          <p className="text-sm text-gray-800 whitespace-pre-wrap">{note.text}</p>

          {/* Footer: author + date */}
          <div className="flex items-center gap-2 text-xs text-gray-500">
            {note.authorPersonName && <span>{note.authorPersonName}</span>}
            {note.authorPersonName && <span aria-hidden="true">&middot;</span>}
            <span>{formatNoteDate(note.noteDateTime)}</span>
          </div>

          {/* Delete confirmation */}
          {isConfirmingDelete && (
            <div
              className="flex items-center gap-3 pt-2 border-t border-gray-100"
              aria-live="polite"
            >
              <p className="text-xs text-gray-700">Delete this note?</p>
              <button
                type="button"
                onClick={() => void handleDelete()}
                disabled={deleteMutation.isPending}
                className="px-3 py-1 bg-red-600 text-white rounded text-xs hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
              <button
                type="button"
                onClick={() => setIsConfirmingDelete(false)}
                disabled={deleteMutation.isPending}
                className="px-3 py-1 bg-gray-200 text-gray-700 rounded text-xs hover:bg-gray-300 transition-colors disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          )}
        </div>
      )}
    </li>
  );
}

// ---------------------------------------------------------------------------
// NotesSection (public export)
// ---------------------------------------------------------------------------

interface NotesSectionProps {
  personIdKey: string;
}

export function NotesSection({ personIdKey }: NotesSectionProps) {
  const { data, isLoading, error } = usePersonNotes(personIdKey);
  const [isAddingNote, setIsAddingNote] = useState(false);

  // Notes already come back newest-first from the API, but sort defensively
  const notes: NoteDto[] = data?.data
    ? [...data.data].sort(
        (a, b) => new Date(b.noteDateTime).getTime() - new Date(a.noteDateTime).getTime(),
      )
    : [];

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
        <div className="flex items-center justify-center py-8">
          <div className="w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
        <p className="text-red-600 text-sm">Failed to load notes</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">
          Notes
          {notes.length > 0 && (
            <span className="ml-2 text-sm font-normal text-gray-500">({notes.length})</span>
          )}
        </h2>
        {!isAddingNote && (
          <button
            type="button"
            onClick={() => setIsAddingNote(true)}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-primary-600 text-white rounded-lg text-sm hover:bg-primary-700 transition-colors"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 4v16m8-8H4"
              />
            </svg>
            Add Note
          </button>
        )}
      </div>

      {isAddingNote && (
        <div className="mb-4">
          <AddNoteForm personIdKey={personIdKey} onCancel={() => setIsAddingNote(false)} />
        </div>
      )}

      {notes.length === 0 && !isAddingNote ? (
        <div className="text-center py-8">
          <svg
            className="w-10 h-10 text-gray-300 mx-auto mb-3"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <p className="text-gray-500 text-sm">No notes yet</p>
        </div>
      ) : (
        <ul className="space-y-3">
          {notes.map((note) => (
            <NoteRow key={note.idKey} personIdKey={personIdKey} note={note} />
          ))}
        </ul>
      )}
    </div>
  );
}
