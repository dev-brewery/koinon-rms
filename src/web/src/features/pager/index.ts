/**
 * Pager feature module
 * Exports all components and hooks for parent paging functionality
 */

export { PagerSearch } from './PagerSearch';
export { SendPageDialog } from './SendPageDialog';
export { PageHistory } from './PageHistory';

export { usePagerSearch, useSendPage, usePageHistory } from './hooks';

export {
  PagerMessageType,
  PagerMessageStatus,
  type PagerAssignment,
  type PagerMessage,
  type PageHistory as PageHistoryType,
  type SendPageRequest,
} from './api';
