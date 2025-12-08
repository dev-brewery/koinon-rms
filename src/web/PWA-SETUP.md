# PWA Setup Documentation

The Koinon Check-in Kiosk is configured as a Progressive Web App (PWA) with offline capabilities.

## Features Implemented

### 1. Service Worker Registration
- Auto-update service worker that checks for new versions every hour
- Automatic cache management via Workbox
- Graceful degradation if service worker fails to register

### 2. Offline Capabilities
- **Static Assets**: JavaScript, CSS, fonts cached with CacheFirst strategy (30-day expiration)
- **Images**: Cached with CacheFirst strategy (30-day expiration)
- **API Calls**: Cached with NetworkFirst strategy, 5-second timeout, 5-minute expiration
- **Check-in Configuration**: Special cache for `/api/v1/checkin/configuration` endpoint (1-hour expiration)

### 3. PWA Manifest
- **Name**: Koinon Check-in
- **Short Name**: Check-in
- **Theme Color**: #1e3a8a (primary-900)
- **Background Color**: #ffffff
- **Display**: Standalone (full-screen kiosk mode)
- **Start URL**: /checkin
- **Icons**: 192x192 and 512x512 PNG icons

### 4. PWA Components

#### OfflineIndicator
- Shows banner when device goes offline
- Shows "Back online" confirmation when connection restored
- Located at the top of the screen
- Accessible with ARIA live region

#### PWAUpdatePrompt
- Displays when a new version is available
- Allows user to update immediately or dismiss
- Auto-dismisses after 5 seconds if not interacted with
- Positioned at bottom-right on desktop, bottom-center on mobile

#### InstallPrompt
- Appears when app can be installed to home screen
- Triggers on `beforeinstallprompt` event
- Allows user to install or dismiss
- Shows at top of screen with prominent gradient styling

## Files Created/Modified

### Configuration
- `/home/mbrewer/projects/koinon-rms/src/web/vite.config.ts` - Added VitePWA plugin
- `/home/mbrewer/projects/koinon-rms/src/web/src/vite-env.d.ts` - Added PWA type references
- `/home/mbrewer/projects/koinon-rms/src/web/package.json` - Added vite-plugin-pwa and workbox-window dependencies

### Assets
- `/home/mbrewer/projects/koinon-rms/src/web/public/icon.svg` - Source SVG icon
- `/home/mbrewer/projects/koinon-rms/src/web/public/icons/icon-192x192.png` - Placeholder icon (1x1 - needs replacement)
- `/home/mbrewer/projects/koinon-rms/src/web/public/icons/icon-512x512.png` - Placeholder icon (1x1 - needs replacement)

### Components
- `/home/mbrewer/projects/koinon-rms/src/web/src/components/pwa/PWAUpdatePrompt.tsx`
- `/home/mbrewer/projects/koinon-rms/src/web/src/components/pwa/OfflineIndicator.tsx`
- `/home/mbrewer/projects/koinon-rms/src/web/src/components/pwa/InstallPrompt.tsx`
- `/home/mbrewer/projects/koinon-rms/src/web/src/components/pwa/index.ts`

### Integration Points
- `/home/mbrewer/projects/koinon-rms/src/web/src/App.tsx` - Service worker registration and PWA component integration
- `/home/mbrewer/projects/koinon-rms/src/web/src/pages/CheckinPage.tsx` - OfflineIndicator integration

## Production Deployment Checklist

### Before deploying to production:

1. **Replace Placeholder Icons**
   The current PNG icons are 1x1 placeholders. Convert the SVG to proper sizes:
   ```bash
   # Option 1: Using ImageMagick
   convert public/icon.svg -resize 192x192 public/icons/icon-192x192.png
   convert public/icon.svg -resize 512x512 public/icons/icon-512x512.png

   # Option 2: Using Inkscape
   inkscape public/icon.svg -w 192 -h 192 -o public/icons/icon-192x192.png
   inkscape public/icon.svg -w 512 -h 512 -o public/icons/icon-512x512.png

   # Option 3: Online converter (export at proper sizes)
   ```

2. **Test Offline Functionality**
   - Open Chrome DevTools → Application → Service Workers
   - Check "Offline" checkbox
   - Verify app loads and shows offline indicator
   - Verify cached data is accessible

3. **Test Install Prompt**
   - Visit site on mobile device or desktop Chrome
   - Verify "Install" prompt appears
   - Test installation to home screen
   - Verify standalone mode works correctly

4. **Test Update Prompt**
   - Deploy new version
   - Wait for service worker to detect update
   - Verify update prompt appears
   - Test update flow

5. **Verify Cache Configuration**
   - Check service worker is caching correct assets
   - Verify API responses are cached appropriately
   - Test cache expiration behavior

## Cache Strategy Details

### CacheFirst (Static Assets)
- **Used for**: JavaScript, CSS, fonts, images
- **Behavior**: Serves from cache if available, fetches from network if not
- **Best for**: Immutable assets with content hashing
- **Expiration**: 30 days, max 100 entries

### NetworkFirst (API)
- **Used for**: API calls, check-in configuration
- **Behavior**: Tries network first (5s timeout), falls back to cache
- **Best for**: Dynamic data that should be fresh but needs offline fallback
- **Expiration**: 5 minutes (general API), 1 hour (config)

## Browser Support

- Chrome/Edge: Full support (recommended for kiosks)
- Firefox: Full support
- Safari: Partial support (no install prompt)
- Mobile browsers: Full support on Android, partial on iOS

## Troubleshooting

### Service worker not updating
1. Clear application cache in DevTools
2. Unregister service worker
3. Hard reload (Ctrl+Shift+R)

### Icons not showing
1. Verify PNG files exist at correct paths
2. Check browser console for 404 errors
3. Ensure icons are proper size and format

### Offline mode not working
1. Check service worker is registered
2. Verify cache strategies in DevTools → Application → Cache Storage
3. Check network requests in DevTools → Network tab

## Performance Considerations

- Service worker adds ~28KB to initial bundle (workbox-window)
- Cached assets reduce subsequent load times significantly
- Offline mode enables <50ms response times for cached data
- Consider adding more granular cache strategies for specific endpoints as needed

## Security Notes

- Service workers only work over HTTPS (or localhost for development)
- Cache is origin-bound (separate for each domain)
- Service worker has full access to site resources
- Update checks happen automatically every hour
