# Koinon RMS Alpha Release Notes

**Version:** 0.1.0-alpha
**Release Date:** December 2025
**Status:** Alpha Testing

## Overview

Koinon RMS is a modern Church Management System built with .NET 8 and React, designed for performance-critical deployments including WiFi-connected check-in kiosks.

This alpha release focuses on the **Check-in Kiosk System**, providing a production-ready experience for child check-in workflows.

## What's Included in Alpha

### Core Check-in System

- **Family Lookup** - Search by phone number or family name
- **Member Selection** - Visual cards for selecting family members to check in
- **Schedule-based Check-in** - Check into active group schedules
- **Success Confirmation** - Clear feedback with check-in details
- **Offline Support** - Continue checking in when network is unavailable

### Performance Targets

| Operation | Target | Status |
|-----------|--------|--------|
| Online family search | <200ms | Validated |
| Offline family search | <50ms | Validated |
| Member selection | <100ms | Validated |
| Check-in confirmation | <200ms | Validated |
| Full check-in flow | <600ms | Validated |
| Dashboard load | <1000ms | Validated |

### Admin Console

- **Dashboard** - Overview of check-in activity and statistics
- **People Management** - View and manage person records
- **Family Management** - View and manage family units
- **Group Management** - View groups and memberships
- **Schedule Management** - Configure check-in schedules
- **Room Roster** - Real-time view of checked-in attendees by room

### User Experience

- Responsive design for tablets (iPad, Android tablets)
- Loading states for all async operations
- Empty states with helpful guidance
- Error recovery with retry capabilities
- Toast notifications for operation feedback

## What's NOT Included (Deferred)

### Deferred to Beta

- **Authorized Pickup Management** - Complex edge cases need more testing
- **Follow-up System Automation** - Retry mechanism needs stabilization
- **Advanced Attendance Reporting** - Not critical for MVP check-in workflow
- **Label Printing** - Hardware integration requires field testing
- **QR Code Check-in** - Alternative check-in method

### Deferred to v1.0

- **Financial Management** - Giving, pledges, statements
- **Multi-campus Features** - Campus-specific configurations
- **Background Check Integration** - Third-party verification
- **Communications System** - Email/SMS campaigns
- **Workflow Automation** - Custom workflow builder
- **Learning Management** - Course and training management

## Technical Stack

| Component | Technology |
|-----------|------------|
| Backend | .NET 8, ASP.NET Core Web API |
| Frontend | React 18, TypeScript, Vite |
| Database | PostgreSQL 16 |
| Cache | Redis |
| Container | Docker, Docker Compose |

## Browser Support

- Chrome 90+ (recommended for kiosks)
- Firefox 90+
- Safari 14+
- Edge 90+

## System Requirements

### Minimum (Development/Testing)
- 2 CPU cores
- 4 GB RAM
- 10 GB disk space
- Docker and Docker Compose

### Recommended (Production)
- 4+ CPU cores
- 8+ GB RAM
- 50+ GB SSD
- Linux host (Ubuntu 22.04 or similar)

## Known Limitations

See [ALPHA-KNOWN-ISSUES.md](./ALPHA-KNOWN-ISSUES.md) for current issues and workarounds.

## Getting Started

1. Review [ALPHA-DEPLOYMENT.md](./ALPHA-DEPLOYMENT.md) for setup instructions
2. Follow [ALPHA-TESTING-GUIDE.md](./ALPHA-TESTING-GUIDE.md) for testing procedures
3. Report issues via GitHub Issues

## Feedback

We welcome feedback from alpha testers! Please report:
- Bugs and issues via GitHub Issues
- Feature requests via GitHub Discussions
- Security concerns via private email

## Changelog

### 0.1.0-alpha (December 2025)

- Initial alpha release
- Check-in kiosk system with offline support
- Admin console with dashboard and management pages
- Performance validation meeting all MVP targets
- E2E test coverage for critical workflows
