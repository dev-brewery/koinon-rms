# BatchDonationEntryService Implementation

## Task
Implement BatchDonationEntryService for manual contribution entry with batch reconciliation for issue #256.

## Files to Create

### 1. Request DTOs (src/Koinon.Application/DTOs/Requests/)

**CreateBatchRequest.cs**
```csharp
namespace Koinon.Application.DTOs.Requests;

public record CreateBatchRequest
{
    public required string Name { get; init; }
    public required DateTime BatchDate { get; init; }
    public decimal? ControlAmount { get; init; }
    public int? ControlItemCount { get; init; }
    public string? CampusIdKey { get; init; }
    public string? Note { get; init; }
}
```

**AddContributionRequest.cs**
```csharp
namespace Koinon.Application.DTOs.Requests;

public record AddContributionRequest
{
    public string? PersonIdKey { get; init; }
    public required DateTime TransactionDateTime { get; init; }
    public string? TransactionCode { get; init; }
    public required string TransactionTypeValueIdKey { get; init; }
    public required List<ContributionDetailRequest> Details { get; init; }
    public string? Summary { get; init; }
}
```

**ContributionDetailRequest.cs**
```csharp
namespace Koinon.Application.DTOs.Requests;

public record ContributionDetailRequest
{
    public required string FundIdKey { get; init; }
    public required decimal Amount { get; init; }
    public string? Summary { get; init; }
}
```

**UpdateContributionRequest.cs**
```csharp
namespace Koinon.Application.DTOs.Requests;

public record UpdateContributionRequest
{
    public string? PersonIdKey { get; init; }
    public required DateTime TransactionDateTime { get; init; }
    public string? TransactionCode { get; init; }
    public required string TransactionTypeValueIdKey { get; init; }
    public required List<ContributionDetailRequest> Details { get; init; }
    public string? Summary { get; init; }
}
```

### 2. Response DTOs (create directory src/Koinon.Application/DTOs/Giving/)

**ContributionBatchDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record ContributionBatchDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public required DateTime BatchDate { get; init; }
    public required string Status { get; init; }
    public decimal? ControlAmount { get; init; }
    public string? CampusIdKey { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedDateTime { get; init; }
    public DateTime? ModifiedDateTime { get; init; }
}
```

**ContributionDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record ContributionDto
{
    public required string IdKey { get; init; }
    public string? PersonIdKey { get; init; }
    public string? PersonName { get; init; }
    public string? BatchIdKey { get; init; }
    public required DateTime TransactionDateTime { get; init; }
    public string? TransactionCode { get; init; }
    public required string TransactionTypeValueIdKey { get; init; }
    public required string SourceTypeValueIdKey { get; init; }
    public string? Summary { get; init; }
    public string? CampusIdKey { get; init; }
    public required List<ContributionDetailDto> Details { get; init; }
    public required decimal TotalAmount { get; init; }
}
```

**ContributionDetailDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record ContributionDetailDto
{
    public required string IdKey { get; init; }
    public required string FundIdKey { get; init; }
    public required string FundName { get; init; }
    public required decimal Amount { get; init; }
    public string? Summary { get; init; }
}
```

**BatchSummaryDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record BatchSummaryDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public decimal? ControlAmount { get; init; }
    public required decimal ActualAmount { get; init; }
    public required int ContributionCount { get; init; }
    public required decimal Variance { get; init; }
    public required bool IsBalanced { get; init; }
}
```

**PersonLookupDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record PersonLookupDto
{
    public required string IdKey { get; init; }
    public required string FullName { get; init; }
    public string? Email { get; init; }
}
```

**FundDto.cs**
```csharp
namespace Koinon.Application.DTOs.Giving;

public record FundDto
{
    public required string IdKey { get; init; }
    public required string Name { get; init; }
    public string? PublicName { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsPublic { get; init; }
}
```

### 3. Interface (src/Koinon.Application/Interfaces/IBatchDonationEntryService.cs)

```csharp
using Koinon.Application.Common;
using Koinon.Application.DTOs.Giving;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Interfaces;

public interface IBatchDonationEntryService
{
    Task<Result<ContributionBatchDto>> CreateBatchAsync(CreateBatchRequest request, CancellationToken ct);
    Task<Result<ContributionBatchDto>> GetBatchAsync(string batchIdKey, CancellationToken ct);
    Task<Result<BatchSummaryDto>> GetBatchSummaryAsync(string batchIdKey, CancellationToken ct);
    Task<Result> OpenBatchAsync(string batchIdKey, CancellationToken ct);
    Task<Result> CloseBatchAsync(string batchIdKey, CancellationToken ct);
    Task<Result<ContributionDto>> AddContributionAsync(string batchIdKey, AddContributionRequest request, CancellationToken ct);
    Task<Result<ContributionDto>> UpdateContributionAsync(string contributionIdKey, UpdateContributionRequest request, CancellationToken ct);
    Task<Result> DeleteContributionAsync(string contributionIdKey, CancellationToken ct);
    Task<IReadOnlyList<PersonLookupDto>> SearchContributorsAsync(string searchTerm, CancellationToken ct);
    Task<IReadOnlyList<FundDto>> GetActiveFundsAsync(CancellationToken ct);
}
```

### 4. Service Implementation (src/Koinon.Application/Services/BatchDonationEntryService.cs)

Use primary constructor pattern with these dependencies:
- IApplicationDbContext context
- IMapper mapper
- IUserContext userContext
- ILogger<BatchDonationEntryService> logger

Business Rules:
1. **Batch Status**: Open → Closed → Posted (no reopen, no edits after Closed)
2. **CreateBatchAsync**: Creates batch in Open status
3. **OpenBatchAsync**: No-op if already Open, error if Closed/Posted
4. **CloseBatchAsync**: Can only close Open batches
5. **AddContributionAsync**: Only allowed for Open batches
6. **UpdateContributionAsync/DeleteContributionAsync**: Only allowed if parent batch is Open
7. **Anonymous**: PersonIdKey = null → set PersonAliasId = null in Contribution
8. **Identified**: PersonIdKey provided → look up primary PersonAlias.Id
9. **SourceTypeValueId**: Query DefinedValue where DefinedType.Name = "Transaction Source" AND Value = "Manual Entry"
10. **Audit**: Log all Create/Update/Delete to FinancialAuditLog

Implementation:
- Use IdKeyHelper.Decode(idKey) to get int ID
- Use entity.IdKey to get IdKey string
- Use Result<T>.Success() and Result<T>.Failure(Error)
- Use Error.NotFound("Entity", idKey), Error.Validation("message"), Error.Conflict("message")
- Include related entities with .Include() for navigation properties
- Calculate TotalAmount = ContributionDetails.Sum(d => d.Amount)
- For BatchSummaryDto: Variance = (ControlAmount ?? 0) - ActualAmount, IsBalanced = Variance == 0
- SearchContributorsAsync: EF.Functions.ILike for case-insensitive search on FirstName, LastName, Email
- GetActiveFundsAsync: Where IsActive = true, OrderBy Order then Name

### 5. Validators (src/Koinon.Application/Validators/Giving/)

Create directory and add:

**CreateBatchRequestValidator.cs**
```csharp
using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators.Giving;

public class CreateBatchRequestValidator : AbstractValidator<CreateBatchRequest>
{
    public CreateBatchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BatchDate).NotEmpty();
        RuleFor(x => x.ControlAmount).GreaterThan(0).When(x => x.ControlAmount.HasValue);
        RuleFor(x => x.ControlItemCount).GreaterThan(0).When(x => x.ControlItemCount.HasValue);
    }
}
```

**AddContributionRequestValidator.cs**
```csharp
using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators.Giving;

public class AddContributionRequestValidator : AbstractValidator<AddContributionRequest>
{
    public AddContributionRequestValidator()
    {
        RuleFor(x => x.TransactionDateTime).NotEmpty();
        RuleFor(x => x.TransactionTypeValueIdKey).NotEmpty();
        RuleFor(x => x.Details).NotEmpty().WithMessage("At least one contribution detail is required");
        RuleForEach(x => x.Details).SetValidator(new ContributionDetailRequestValidator());
    }
}

public class ContributionDetailRequestValidator : AbstractValidator<ContributionDetailRequest>
{
    public ContributionDetailRequestValidator()
    {
        RuleFor(x => x.FundIdKey).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
```

**UpdateContributionRequestValidator.cs** (same as Add)

### 6. AutoMapper Mappings

Add to existing MappingProfile.cs in src/Koinon.Application/:

```csharp
// Giving DTOs
CreateMap<ContributionBatch, ContributionBatchDto>()
    .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

CreateMap<Contribution, ContributionDto>()
    .ForMember(d => d.TotalAmount, opt => opt.MapFrom(s => s.ContributionDetails.Sum(cd => cd.Amount)))
    .ForMember(d => d.PersonName, opt => opt.MapFrom(s => s.PersonAlias != null ? s.PersonAlias.Person.FullName : null));

CreateMap<ContributionDetail, ContributionDetailDto>()
    .ForMember(d => d.FundName, opt => opt.MapFrom(s => s.Fund!.Name));

CreateMap<Fund, FundDto>();

CreateMap<Person, PersonLookupDto>();
```

### 7. Register Service

Add to DependencyInjection.cs in src/Koinon.Application/:

```csharp
services.AddScoped<IBatchDonationEntryService, BatchDonationEntryService>();
```

Add validators:
```csharp
services.AddScoped<IValidator<CreateBatchRequest>, CreateBatchRequestValidator>();
services.AddScoped<IValidator<AddContributionRequest>, AddContributionRequestValidator>();
services.AddScoped<IValidator<UpdateContributionRequest>, UpdateContributionRequestValidator>();
```

## Audit Logging Example

```csharp
private async Task LogAuditAsync(FinancialAuditAction action, string entityType, string entityIdKey, object? details = null)
{
    var auditLog = new FinancialAuditLog
    {
        PersonId = _userContext.PersonId,
        ActionType = action,
        EntityType = entityType,
        EntityIdKey = entityIdKey,
        IpAddress = _userContext.IpAddress,
        UserAgent = _userContext.UserAgent,
        Details = details != null ? JsonSerializer.Serialize(details) : null,
        Timestamp = DateTime.UtcNow
    };
    context.FinancialAuditLogs.Add(auditLog);
}
```

Call after each Create/Update/Delete operation before SaveChangesAsync().
