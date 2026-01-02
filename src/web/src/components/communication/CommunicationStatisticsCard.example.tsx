/**
 * Example usage of CommunicationStatisticsCard
 * This file demonstrates how to use the component in a page
 */

import { CommunicationStatisticsCard } from './CommunicationStatisticsCard';

export function CommunicationStatisticsExample() {
  return (
    <div className="max-w-md">
      <CommunicationStatisticsCard
        recipientCount={100}
        deliveredCount={85}
        failedCount={5}
        openedCount={60}
      />
    </div>
  );
}

/**
 * Example with all messages delivered
 */
export function AllDeliveredExample() {
  return (
    <div className="max-w-md">
      <CommunicationStatisticsCard
        recipientCount={50}
        deliveredCount={50}
        failedCount={0}
        openedCount={30}
      />
    </div>
  );
}

/**
 * Example with pending messages
 */
export function PendingMessagesExample() {
  return (
    <div className="max-w-md">
      <CommunicationStatisticsCard
        recipientCount={100}
        deliveredCount={60}
        failedCount={3}
        openedCount={40}
      />
    </div>
  );
}

/**
 * Example with some failures
 */
export function WithFailuresExample() {
  return (
    <div className="max-w-md">
      <CommunicationStatisticsCard
        recipientCount={100}
        deliveredCount={75}
        failedCount={25}
        openedCount={50}
      />
    </div>
  );
}
