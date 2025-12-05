# Koinon RMS Feature Specifications

This document defines the complete feature set for Koinon RMS, a comprehensive Church Management System.

---

## System Overview

Koinon RMS is designed to handle the full lifecycle of church ministry operations, from member management to event coordination, communications, and financial tracking.

---

## Core Domain Features

### 1. People & Relationships (CRM)

**Person Management**
- Individual demographic information (name, birthdate, gender, marital status, etc.)
- Photo management
- Contact information (email, phone, addresses)
- Family grouping and relationship tracking
- Person merge/duplicate detection
- Record status tracking (Active, Inactive, Pending)
- Connection status (Member, Attendee, Visitor, Prospect)
- Background checks and identity verification
- Personal device tracking
- Person search with multiple search key types

**Person Aliases**
- Multiple identity tracking (for person merges)
- Historical record preservation

**Contact Information**
- Multiple phone numbers per person (Mobile, Home, Work)
- Multiple addresses with location geocoding
- Email verification and bounce tracking

**Person Signals & Preferences**
- Custom signal types for tracking person status
- Individual preference management
- Personalized entity tracking

### 2. Groups & Community

**Group Management**
- Hierarchical group structures
- Group type system with customizable roles
- Group member tracking with roles
- Attendance tracking
- Group requirements and member requirements
- Group scheduling with exclusions
- Group location assignments
- Group demographics
- Historical tracking of groups and members

**Group Types**
- Families (special handling)
- Small groups
- Serving teams
- Security roles
- Check-in templates
- Custom group types

**Group Features**
- Member assignments and schedule templates
- Workflow triggers on group events
- Group syncing capabilities
- Peer networks
- Location-based scheduling configurations

### 3. Communications

**Communication System**
- Email communications
- SMS/text messaging
- Push notifications
- Communication templates
- System communications (automated)
- Communication attachments
- Recipient tracking and status
- Response tracking

**Communication Flows**
- Multi-step communication workflows
- Flow instances and tracking
- Conversion tracking
- Recipient journey management

**Notification System**
- In-app notifications
- Notification types and delivery
- Notification recipients

**Supporting Features**
- Email sections for templating
- SMS pipelines and actions
- System phone number management
- Snippet library with types

### 4. Events & Attendance

**Event Management**
- Event calendars
- Event items with recurrence
- Event occurrences
- Event audiences
- Calendar-to-content-channel integration
- Interactive experiences with scheduling

**Registration System**
- Registration templates with customizable forms
- Registration instances (specific event offerings)
- Registrant management
- Fee structures and discounts
- Placement management
- Session tracking
- Form builder with fields

**Attendance Tracking**
- Attendance occurrences
- Check-in sessions
- Attendance codes (for kiosk check-in)
- Attendance data analytics
- Check-in labels for printing

**Interactive Experiences**
- Experience definitions
- Actions and answers
- Occurrence scheduling
- Campus-based scheduling

### 5. Finance & Giving

**Contribution Management**
- Financial transactions
- Transaction details (split gifts)
- Scheduled/recurring transactions
- Payment detail tracking
- Saved payment methods (bank accounts, cards)
- Transaction images (check scanning)
- Refund processing
- Transaction alerts and alert types

**Financial Accounts**
- Hierarchical account structure
- Fund designation tracking

**Batching**
- Financial batch management
- Batch processing workflows

**Pledges**
- Pledge tracking
- Pledge fulfillment reporting

**Statements**
- Contribution statement templates
- Statement generation

**Benevolence**
- Benevolence requests
- Request documentation
- Result tracking
- Benevolence types
- Workflow integration

**Payment Processing**
- Gateway integration
- Scheduled transaction processing

### 6. Workflows & Automation

**Workflow Engine**
- Workflow types and categories
- Workflow instances
- Activity types and instances
- Action types and instances
- Workflow triggers (time, entity events)
- Workflow logs

**Form Builder**
- Dynamic form creation
- Form sections and attributes
- Action forms for user interaction
- Form builder templates

### 7. Connections & Follow-up

**Connection System**
- Connection types
- Connection opportunities
- Connection requests
- Request activities
- Status tracking with automation
- Connector assignments
- Campus and group configurations
- Workflow integration

### 8. Content Management (CMS)

**Content Features**
- Content channels
- Content items
- Content versioning
- Media library
- File management

### 9. Security & Authentication

**Authentication**
- User logins
- Multiple authentication methods
- Remote authentication sessions
- Two-factor authentication
- Single sign-on (SSO) capabilities
- Person tokens for temporary access

**Authorization**
- Role-based security
- Entity-level permissions
- Auth claims and scopes
- OAuth2/OpenID Connect clients
- Security auditing

### 10. Reporting & Analytics

**Interaction Tracking**
- Interaction channels
- Interaction components
- Interaction sessions
- Device type tracking
- Session location tracking
- Entity-specific interactions

**Data Analysis**
- Entity metadata
- Entity search indexing
- Data views and filters
- Report generation

### 11. Prayer Requests

**Prayer Management**
- Prayer requests
- Prayer categories
- Request status tracking
- Auto-expiration
- Public/private requests

### 12. Learning Management (LMS)

**Course Management**
- Courses and programs
- Lessons and activities
- Enrollment tracking
- Progress tracking
- Completion certificates

### 13. System Administration

**Core System**
- Campus management with schedules and topics
- Locations with geocoding
- Schedules and calendar management
- Categories for organization
- Tags and tagged items
- Notes with attachments and types
- Documents and document types
- Entity types and sets
- Following system (people follow entities)

**Custom Data**
- Attributes (custom fields)
- Attribute values
- Attribute qualifiers
- Field types
- Attribute matrices (repeating data)
- Entity metadata

**File Storage**
- Binary file management
- File types
- Asset storage providers (local, cloud)
- File versioning

**Background Jobs**
- Service jobs
- Job scheduling
- Job history and logging
- Service logs

**System Tools**
- Exception logging
- Audit trail
- History tracking
- Entity intents
- Reminders with types
- Automation events and triggers

**Multi-tenancy**
- Entity-campus filtering
- Campus-specific configurations

**Data Quality**
- NCOA (address verification)
- Duplicate detection
- Data validation

**Notifications & Alerts**
- Notification messages and types
- Note watches
- Following suggestions
- Event notifications and subscriptions

**Signature Documents**
- Digital signature collection
- Document templates
- Signature tracking

**Web Farm**
- Distributed deployment support
- Node coordination

**AI Integration**
- AI-powered features
- AI service integration

**Assessments**
- Assessment types
- Assessment responses
- Scoring and results

**Badges**
- Achievement badges
- Badge earning tracking

---

## Technical Architecture Features

### API Layer
- RESTful API with OpenAPI documentation
- JWT authentication with refresh tokens
- Rate limiting and throttling
- Webhook support
- Bulk operations
- Filtering, sorting, pagination

### Data Layer
- PostgreSQL database
- Entity Framework Core
- Repository pattern
- Unit of work
- Database migrations
- Full-text search
- Geospatial queries

### Caching
- Redis distributed cache
- Cache invalidation strategies
- Session state management

### Offline Capabilities
- Progressive Web App (PWA)
- Service worker for offline functionality
- Local storage sync
- Conflict resolution

### Performance
- Response caching
- Query optimization
- Lazy loading
- Eager loading strategies
- Database indexing
- Connection pooling

### Integration
- REST API for third-party integration
- Webhook notifications
- Import/export capabilities
- OAuth2 for external apps

### Deployment
- Docker containerization
- Kubernetes orchestration
- Environment-based configuration
- Health checks
- Logging and monitoring
- Automated backups

---

## MVP Phase 1: Check-in System

The initial release focuses on a production-ready check-in system with:

- Family lookup by phone or name
- Person selection
- Check-in to scheduled events
- Label printing
- Attendance recording
- Offline capability
- Admin console for check-in management

Target: <200ms check-in time, <50ms offline
