# API Contract Tests

Contract tests ensure that the backend API and frontend client maintain compatible interfaces. This prevents breaking changes from reaching production.

## What are Contract Tests?

Contract tests verify that:
1. The API provides what the frontend expects (provider tests)
2. The frontend uses the API correctly (consumer tests)

Unlike integration tests, contract tests don't require both systems running simultaneously.

## Using Pact

We use [Pact](https://docs.pact.io/) for contract testing:

```
Frontend (Consumer) → Generates Contract → Backend (Provider) validates
```

### Workflow

1. **Frontend generates contract**: When frontend tests run, they record API interactions as a "contract"
2. **Backend validates contract**: Backend tests replay the contract and verify responses match
3. **CI/CD enforces compatibility**: PRs fail if contracts are broken

## Frontend Consumer Tests

```typescript
// src/web/src/api/__tests__/person-contract.test.ts
import { PactV3 } from '@pact-foundation/pact';

const provider = new PactV3({
  consumer: 'koinon-web',
  provider: 'koinon-api',
});

describe('Person API Contract', () => {
  it('should get person by idKey', async () => {
    await provider
      .given('person XYZ123 exists')
      .uponReceiving('a request for person XYZ123')
      .withRequest({
        method: 'GET',
        path: '/api/v1/people/XYZ123',
      })
      .willRespondWith({
        status: 200,
        headers: { 'Content-Type': 'application/json' },
        body: {
          idKey: 'XYZ123',
          firstName: 'John',
          lastName: 'Doe',
          email: 'john@example.com',
        },
      });

    // Contract is now recorded
  });
});
```

## Backend Provider Tests

```csharp
// tests/Koinon.Api.ContractTests/PersonApiContractTests.cs
public class PersonApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void ValidatePersonContract()
    {
        var config = new PactVerifierConfig
        {
            ProviderVersion = "1.0.0",
            PublishVerificationResults = true
        };

        var verifier = new PactVerifier(config);

        verifier
            .ServiceProvider("koinon-api", _fixture.CreateClient())
            .HonoursPactWith("koinon-web")
            .PactUri("../../../pacts/koinon-web-koinon-api.json")
            .Verify();
    }
}
```

## Running Contract Tests

```bash
# Frontend: Generate contracts
cd src/web
npm test -- --testPathPattern=contract

# Backend: Validate contracts
dotnet test tests/Koinon.Api.ContractTests

# CI: Both run automatically in pull requests
```

## Benefits for Agent Development

1. **Type Safety**: Contracts ensure generated TypeScript types match API
2. **Early Detection**: Breaking changes caught before manual testing
3. **Documentation**: Contracts serve as living API documentation
4. **Parallel Development**: Teams can work on frontend/backend independently

## Integration with OpenAPI

Our setup combines Pact contracts with OpenAPI generation:

1. OpenAPI generates TypeScript types from API
2. Pact ensures runtime behavior matches types
3. Both validated in CI/CD

This catches both compile-time and runtime incompatibilities.

## Implementation Status

⚠️ **Pending API Implementation**

Contract tests require:
- [ ] WU-3.1.1: API project configuration
- [ ] WU-3.2.x: API controllers
- [ ] WU-4.1.1: Frontend API client

Once complete, implement:
1. Pact provider tests for each controller
2. Pact consumer tests in frontend
3. CI/CD pipeline integration
4. Contract publishing to Pact Broker (optional)

## CI/CD Integration

```yaml
# .github/workflows/contract-tests.yml
- name: Run consumer tests
  run: npm test -- --testPathPattern=contract

- name: Run provider tests
  run: dotnet test tests/Koinon.Api.ContractTests

- name: Check contract compatibility
  run: |
    if ! diff pacts/*.json previous-pacts/*.json; then
      echo "⚠️ Contract changed - review carefully"
    fi
```

## Best Practices

1. **Provider States**: Use `given()` to set up test data
2. **Minimal Contracts**: Only verify fields the consumer uses
3. **Version Contracts**: Track contract versions with API versions
4. **Don't Mock Everything**: Contract tests complement, don't replace integration tests

## Further Reading

- [Pact Documentation](https://docs.pact.io/)
- [Contract Testing Guide](https://martinfowler.com/articles/consumerDrivenContracts.html)
- [Pact.NET](https://github.com/pact-foundation/pact-net)
