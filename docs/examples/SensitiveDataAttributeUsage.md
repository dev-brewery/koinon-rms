# SensitiveDataAttribute Usage Examples

The `SensitiveDataAttribute` is used to mark properties containing sensitive data that should be masked in audit logs. This helps protect personally identifiable information (PII) and other sensitive data from exposure in logs.

## Basic Usage

### Full Masking (Default)

```csharp
using Koinon.Domain.Attributes;

public class Person
{
    // Entire value will be replaced with "***"
    [SensitiveData]
    public string? SocialSecurityNumber { get; set; }
}
```

### Partial Masking

Useful for credit cards, phone numbers, etc. where you want to show the last 4 digits:

```csharp
using Koinon.Domain.Attributes;
using Koinon.Domain.Enums;

public class PaymentMethod
{
    // Will show as "****5678"
    [SensitiveData(MaskType = SensitiveMaskType.Partial)]
    public string? CreditCardNumber { get; set; }
}
```

### Hash Masking

For sensitive data where you need to verify values match without revealing the actual value:

```csharp
using Koinon.Domain.Attributes;
using Koinon.Domain.Enums;

public class UserCredential
{
    // Will show as SHA256 hash
    [SensitiveData(MaskType = SensitiveMaskType.Hash)]
    public string? Password { get; set; }
}
```

### With Documentation

Add a reason to document why the data is sensitive:

```csharp
using Koinon.Domain.Attributes;
using Koinon.Domain.Enums;

public class Person
{
    [SensitiveData(MaskType = SensitiveMaskType.Full, Reason = "Protected health information")]
    public string? MedicalConditions { get; set; }

    [SensitiveData(MaskType = SensitiveMaskType.Partial, Reason = "PII - financial data")]
    public string? BankAccountNumber { get; set; }
}
```

## Real-World Example

```csharp
using Koinon.Domain.Attributes;
using Koinon.Domain.Entities;
using Koinon.Domain.Enums;

public class Person : Entity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // Full masking for SSN
    [SensitiveData(Reason = "PII - government ID")]
    public string? SocialSecurityNumber { get; set; }

    // Partial masking for phone (show last 4)
    [SensitiveData(MaskType = SensitiveMaskType.Partial, Reason = "PII - contact info")]
    public string? PhoneNumber { get; set; }

    // Hash for password verification
    [SensitiveData(MaskType = SensitiveMaskType.Hash)]
    public string? PasswordHash { get; set; }
}
```

## Mask Type Reference

| MaskType | Example Input | Example Output | Use Case |
|----------|--------------|----------------|----------|
| Full | "123-45-6789" | "***" | SSN, full credit card |
| Partial | "1234567890" | "****7890" | Phone, last 4 of card |
| Hash | "password123" | "ef92b7..." | Passwords, API keys |

## Implementation Notes

- The attribute itself only marks the property - actual masking logic should be implemented in your audit logging service
- Use reflection to detect the attribute at runtime
- Apply masking before writing to audit logs
- Never store masked values in the database - only mask during logging

## Example Audit Service

```csharp
public class AuditService
{
    private string MaskValue(PropertyInfo property, object? value)
    {
        var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
        if (sensitiveAttr == null || value == null)
            return value?.ToString() ?? "";

        var stringValue = value.ToString() ?? "";

        return sensitiveAttr.MaskType switch
        {
            SensitiveMaskType.Full => "***",
            SensitiveMaskType.Partial => stringValue.Length > 4 
                ? new string('*', stringValue.Length - 4) + stringValue[^4..]
                : "***",
            SensitiveMaskType.Hash => ComputeSHA256Hash(stringValue),
            _ => "***"
        };
    }

    private string ComputeSHA256Hash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16]; // First 16 chars
    }
}
```
