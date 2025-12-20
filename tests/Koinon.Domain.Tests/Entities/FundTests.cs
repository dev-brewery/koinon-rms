using Koinon.Domain.Entities;
using Xunit;

namespace Koinon.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Fund entity.
/// </summary>
public class FundTests
{
    [Fact]
    public void Fund_Creation_ShouldSetDefaultValues()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "General Fund"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, fund.Guid);
        Assert.True(fund.IsTaxDeductible);
        Assert.True(fund.IsActive);
        Assert.True(fund.IsPublic);
        Assert.Equal(0, fund.Order);
    }

    [Fact]
    public void Fund_Name_IsRequired()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Building Fund"
        };

        // Assert
        Assert.NotNull(fund.Name);
        Assert.Equal("Building Fund", fund.Name);
    }

    [Fact]
    public void Fund_PublicName_CanBeSet()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "internal_building_fund",
            PublicName = "Building Fund 2025"
        };

        // Assert
        Assert.Equal("Building Fund 2025", fund.PublicName);
    }

    [Fact]
    public void Fund_IdKey_IsComputed()
    {
        // Arrange
        var fund = new Fund
        {
            Name = "Test Fund",
            Id = 42
        };

        // Act
        var idKey = fund.IdKey;

        // Assert
        Assert.NotNull(idKey);
        Assert.NotEmpty(idKey);
        // IdKey should be URL-safe Base64
        Assert.DoesNotContain("+", idKey);
        Assert.DoesNotContain("/", idKey);
        Assert.DoesNotContain("=", idKey);
    }

    [Fact]
    public void Fund_AllOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Minimal Fund"
        };

        // Assert
        Assert.Null(fund.PublicName);
        Assert.Null(fund.Description);
        Assert.Null(fund.GlCode);
        Assert.Null(fund.StartDate);
        Assert.Null(fund.EndDate);
        Assert.Null(fund.ParentFundId);
        Assert.Null(fund.CampusId);
    }

    [Fact]
    public void Fund_CanSetAllProperties()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);

        // Act
        var fund = new Fund
        {
            Name = "Easter Campaign",
            PublicName = "Easter 2025",
            Description = "Special Easter offering for missions",
            GlCode = "4250",
            IsTaxDeductible = true,
            IsActive = true,
            IsPublic = true,
            StartDate = startDate,
            EndDate = endDate,
            Order = 5,
            ParentFundId = 10,
            CampusId = 2
        };

        // Assert
        Assert.Equal("Easter Campaign", fund.Name);
        Assert.Equal("Easter 2025", fund.PublicName);
        Assert.Equal("Special Easter offering for missions", fund.Description);
        Assert.Equal("4250", fund.GlCode);
        Assert.True(fund.IsTaxDeductible);
        Assert.True(fund.IsActive);
        Assert.True(fund.IsPublic);
        Assert.Equal(startDate, fund.StartDate);
        Assert.Equal(endDate, fund.EndDate);
        Assert.Equal(5, fund.Order);
        Assert.Equal(10, fund.ParentFundId);
        Assert.Equal(2, fund.CampusId);
    }

    [Fact]
    public void Fund_IsTaxDeductible_CanBeFalse()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Non-Deductible Fund",
            IsTaxDeductible = false
        };

        // Assert
        Assert.False(fund.IsTaxDeductible);
    }

    [Fact]
    public void Fund_IsActive_CanBeFalse()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Archived Fund",
            IsActive = false
        };

        // Assert
        Assert.False(fund.IsActive);
    }

    [Fact]
    public void Fund_IsPublic_CanBeFalse()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Internal Fund",
            IsPublic = false
        };

        // Assert
        Assert.False(fund.IsPublic);
    }

    [Fact]
    public void Fund_InheritsFromEntity()
    {
        // Arrange
        var fund = new Fund
        {
            Name = "Test Fund"
        };

        // Assert
        Assert.IsAssignableFrom<Entity>(fund);
        Assert.IsAssignableFrom<IEntity>(fund);
        Assert.IsAssignableFrom<IAuditable>(fund);
    }

    [Fact]
    public void Fund_AuditFields_CanBeSet()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var fund = new Fund
        {
            Name = "Test Fund",
            CreatedDateTime = now,
            CreatedByPersonAliasId = 1,
            ModifiedDateTime = now.AddMinutes(5),
            ModifiedByPersonAliasId = 2
        };

        // Assert
        Assert.Equal(now, fund.CreatedDateTime);
        Assert.Equal(1, fund.CreatedByPersonAliasId);
        Assert.Equal(now.AddMinutes(5), fund.ModifiedDateTime);
        Assert.Equal(2, fund.ModifiedByPersonAliasId);
    }

    [Fact]
    public void Fund_ParentFund_NavigationPropertyCanBeSet()
    {
        // Arrange
        var parentFund = new Fund
        {
            Name = "Main Missions"
        };

        var childFund = new Fund
        {
            Name = "Africa Missions",
            ParentFundId = 1
        };

        // Act
        childFund.ParentFund = parentFund;

        // Assert
        Assert.NotNull(childFund.ParentFund);
        Assert.Equal("Main Missions", childFund.ParentFund.Name);
    }

    [Fact]
    public void Fund_ChildFunds_CollectionIsInitialized()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Parent Fund"
        };

        // Assert
        Assert.NotNull(fund.ChildFunds);
        Assert.Empty(fund.ChildFunds);
    }

    [Fact]
    public void Fund_ChildFunds_CanBeAdded()
    {
        // Arrange
        var parentFund = new Fund
        {
            Name = "General Missions"
        };

        var childFund1 = new Fund
        {
            Name = "Africa Missions"
        };

        var childFund2 = new Fund
        {
            Name = "Asia Missions"
        };

        // Act
        parentFund.ChildFunds.Add(childFund1);
        parentFund.ChildFunds.Add(childFund2);

        // Assert
        Assert.Equal(2, parentFund.ChildFunds.Count);
        Assert.Contains(childFund1, parentFund.ChildFunds);
        Assert.Contains(childFund2, parentFund.ChildFunds);
    }

    [Fact]
    public void Fund_Campus_NavigationPropertyCanBeSet()
    {
        // Arrange
        var campus = new Campus
        {
            Name = "West Campus"
        };

        var fund = new Fund
        {
            Name = "West Campus Building Fund",
            CampusId = 1
        };

        // Act
        fund.Campus = campus;

        // Assert
        Assert.NotNull(fund.Campus);
        Assert.Equal("West Campus", fund.Campus.Name);
    }

    [Fact]
    public void Fund_CampaignDates_CanDefineTimeWindow()
    {
        // Arrange
        var startDate = new DateTime(2025, 3, 1);
        var endDate = new DateTime(2025, 4, 30);

        // Act
        var fund = new Fund
        {
            Name = "Spring Campaign",
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        Assert.NotNull(fund.StartDate);
        Assert.NotNull(fund.EndDate);
        Assert.Equal(startDate, fund.StartDate);
        Assert.Equal(endDate, fund.EndDate);
        Assert.True(fund.EndDate > fund.StartDate);
    }

    [Fact]
    public void Fund_GlCode_CanStoreAccountingCode()
    {
        // Arrange & Act
        var fund = new Fund
        {
            Name = "Building Fund",
            GlCode = "4100"
        };

        // Assert
        Assert.Equal("4100", fund.GlCode);
    }

    [Fact]
    public void Fund_Order_DeterminesSortPosition()
    {
        // Arrange & Act
        var fund1 = new Fund
        {
            Name = "First Fund",
            Order = 1
        };

        var fund2 = new Fund
        {
            Name = "Second Fund",
            Order = 2
        };

        var fund3 = new Fund
        {
            Name = "Third Fund",
            Order = 3
        };

        // Assert
        Assert.True(fund1.Order < fund2.Order);
        Assert.True(fund2.Order < fund3.Order);
    }
}
