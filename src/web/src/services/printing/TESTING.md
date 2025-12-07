# PrintBridge Client Testing Guide

## Unit Tests

Run the test suite for PrintBridgeClient:

```bash
cd src/web
npm test -- src/services/printing/PrintBridgeClient.test.ts
```

### Test Coverage

The test suite covers:

- **Constructor & Configuration**
  - Default configuration
  - Custom configuration

- **Singleton Pattern**
  - Instance reuse
  - Instance reset

- **Health Check & Availability**
  - Successful health check
  - Failed health check
  - Availability caching

- **Printer Discovery**
  - List available printers
  - Handle unavailable PrintBridge

- **Printing**
  - Successful print request
  - Validation: empty ZPL content
  - Validation: invalid copies count
  - Unavailable PrintBridge error
  - Default printer selection

- **Test Printing**
  - Send test label to printer

- **Error Handling**
  - PrintBridgeError creation
  - Cache invalidation on network errors

- **Timeout Handling**
  - Request timeout behavior

## Integration Testing

### Manual Testing with PrintBridge

1. **Start PrintBridge**
   ```bash
   # Windows PowerShell
   cd tools/print-bridge/Koinon.PrintBridge/bin/Debug/net8.0-windows
   .\Koinon.PrintBridge.exe
   ```

2. **Start the React Frontend**
   ```bash
   cd src/web
   npm run dev
   # Opens http://localhost:5173
   ```

3. **Test Health Check**
   ```bash
   curl http://127.0.0.1:9632/api/v1/print/health
   ```

4. **Test Printer Discovery**
   ```bash
   curl http://127.0.0.1:9632/api/v1/print/printers
   ```

5. **Test Print Request**
   ```bash
   curl -X POST http://127.0.0.1:9632/api/v1/print/print \
     -H "Content-Type: application/json" \
     -d '{
       "printerName": "Zebra ZD420",
       "zplContent": "^XA^FO50,50^A0N,50,50^FDTest Label^FS^XZ",
       "copies": 1
     }'
   ```

6. **Test from React Console**
   ```javascript
   // In browser console
   import { getPrintBridgeClient } from '@/services/printing';

   const client = getPrintBridgeClient();

   // Check availability
   await client.isAvailable();  // true or false

   // Get printers
   const printers = await client.getPrinters();
   console.log(printers);

   // Print label
   await client.print({
     zplContent: '^XA^FDHello World^FS^XZ',
     printerName: 'Zebra ZD420'
   });
   ```

## E2E Testing Scenarios

### Scenario 1: Successful Check-In and Printing

**Steps:**
1. User searches for family in check-in UI
2. Selects opportunity and checks in
3. System generates labels via LabelGenerationService
4. Frontend fetches labels via getLabels()
5. Frontend detects PrintBridge availability via isAvailable()
6. Frontend sends each label to print() method
7. Verify labels print successfully

**Verification:**
- Labels physically print
- No errors in browser console
- No errors in PrintBridge output

### Scenario 2: PrintBridge Unavailable

**Steps:**
1. Kill PrintBridge application
2. User attempts check-in
3. System calls isAvailable()
4. Frontend gets false response
5. UI shows "Printing unavailable" message

**Verification:**
- Error message appears within 5 seconds
- User can proceed without printing
- Option to email labels instead

### Scenario 3: Printer Not Found

**Steps:**
1. Start PrintBridge without printer
2. User checks in and attempts print
3. System sends ZPL to non-existent printer
4. PrintBridge returns error

**Verification:**
- Error message: "Printer not found"
- Application handles gracefully
- User can skip printing and continue

### Scenario 4: Network Timeout

**Steps:**
1. Simulate slow network (DevTools throttling)
2. Set timeout to 1 second
3. Attempt print request
4. Request takes > 1 second

**Verification:**
- Request times out
- Error code: "TIMEOUT"
- UI shows timeout message
- Retry option available

### Scenario 5: Large Batch Printing

**Steps:**
1. Check in 10 people
2. System generates 20 labels (2 per person)
3. Frontend sends all labels to print
4. Measure total time

**Verification:**
- All labels print successfully
- Total time < 10 seconds (best effort)
- No dropped print jobs

## Performance Testing

### Health Check Cache

Test that health check is cached:

```javascript
const client = getPrintBridgeClient();

console.time('First isAvailable');
await client.isAvailable();
console.timeEnd('First isAvailable');  // ~10ms

console.time('Cached isAvailable');
await client.isAvailable();
console.timeEnd('Cached isAvailable');  // <1ms (from cache)

// Wait 6 seconds and cache expires
await new Promise(r => setTimeout(r, 6000));

console.time('Fresh isAvailable');
await client.isAvailable();
console.timeEnd('Fresh isAvailable');  // ~10ms (new request)
```

### Print Request Performance

Test individual print request timing:

```javascript
const client = getPrintBridgeClient();

const zpl = '^XA^FO50,50^A0N,50,50^FDTest^FS^XZ';

console.time('Print Request');
const result = await client.print({
  zplContent: zpl,
  printerName: 'Zebra ZD420'
});
console.timeEnd('Print Request');  // Should be <100ms
```

## Debugging

### Enable Debug Logging in Browser

```javascript
// Monkey-patch client methods to log
const client = getPrintBridgeClient();
const originalPrint = client.print.bind(client);

client.print = async (request) => {
  console.log('Print request:', request);
  try {
    const result = await originalPrint(request);
    console.log('Print result:', result);
    return result;
  } catch (error) {
    console.error('Print error:', error);
    throw error;
  }
};
```

### Enable Debug Logging in PrintBridge

Start PrintBridge in Development mode:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project tools/print-bridge/Koinon.PrintBridge/Koinon.PrintBridge.csproj
```

Look for debug output like:
```
[dbg] Discovered printer: Zebra ZD420 (ZPL)
[inf] Successfully sent ZPL to printer Zebra ZD420
```

### Common Issues

#### "PrintBridge is not available"

**Causes:**
- PrintBridge not running
- Port 9632 in use
- Firewall blocking localhost:9632

**Solution:**
```bash
# Check if PrintBridge is running
Get-Process Koinon.PrintBridge

# Check port availability
netstat -ano | findstr 9632

# Start PrintBridge in debug mode
cd tools/print-bridge/Koinon.PrintBridge
dotnet run
```

#### "Printer not found"

**Causes:**
- Printer name is incorrect
- Printer driver not installed
- Printer offline

**Solution:**
```bash
# List available printers
curl http://127.0.0.1:9632/api/v1/print/printers | jq

# Verify in Windows
Get-Printer
```

#### Print request hangs

**Causes:**
- Printer offline
- Printer driver unresponsive
- Network connectivity issue

**Solution:**
- Check printer status in Windows Settings
- Restart printer
- Restart Print Spooler: `Restart-Service spooler`
- Check PrintBridge logs

## Test Checklist

Before release, verify:

- [ ] Unit tests pass: `npm test -- PrintBridgeClient.test.ts`
- [ ] TypeScript compiles without errors
- [ ] Health check returns correct status
- [ ] Printer discovery lists all printers
- [ ] Successful print request completes
- [ ] Error handling works for all error codes
- [ ] Cache invalidation works correctly
- [ ] Timeout behavior correct (5 second default)
- [ ] Offline scenario handled gracefully
- [ ] Network error recovery works
- [ ] Browser console has no errors
- [ ] PrintBridge console has no errors

## Related Documentation

- `README.md` - Overview and usage examples
- `types.ts` - TypeScript type definitions
- `PrintBridgeClient.ts` - Implementation details
- `/tools/print-bridge/README.md` - PrintBridge application docs
- `/tools/print-bridge/INTEGRATION.md` - Integration guide
