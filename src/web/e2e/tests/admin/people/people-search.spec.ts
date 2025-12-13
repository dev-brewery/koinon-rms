/**
 * E2E Tests: People Search and Filtering
 * Tests search functionality and filter operations on the people list
 *
 * ASSUMPTIONS (UI not yet fully implemented):
 * - People list has search input with debouncing (similar to groups/families)
 * - Search performs partial match on first name, last name, email
 * - Filters exist for gender and campus
 * - Empty state shows "No people found" message
 * - Search results update without full page reload
 *
 * NOTE: Update selectors when UI is implemented to use data-testid attributes
 */

import { test, expect } from '@playwright/test';
import { PeoplePage } from '../../../fixtures/page-objects/people.page';
import { LoginPage } from '../../../fixtures/page-objects/login.page';
import { testData } from '../../../fixtures/test-data';

test.describe('People Search', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should search by first name', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for "John"
    await peoplePage.searchPeople('John');

    // Should show John Smith
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Should not show people without "John" in name
    await peoplePage.expectPersonNotVisible(testData.people.barbaraJohnson.fullName);
  });

  test('should search by last name', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for "Smith"
    await peoplePage.searchPeople('Smith');

    // Should show all Smiths
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.janeSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.johnnySmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.jennySmith.fullName);

    // Should not show Johnsons
    await peoplePage.expectPersonNotVisible(testData.people.bobJohnson.fullName);
  });

  test('should search by partial name', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for "Bob" (partial of Bobby/Bob)
    await peoplePage.searchPeople('Bob');

    // Should show Bob Johnson
    await peoplePage.expectPersonVisible(testData.people.bobJohnson.fullName);
  });

  test('should search case-insensitively', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search with different casing
    await peoplePage.searchPeople('SMITH');

    // Should still find Smith family
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Clear and try lowercase
    await peoplePage.searchPeople('smith');
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
  });

  test('should show empty state when no results', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for non-existent person
    await peoplePage.searchPeople('XYZ123NotARealPerson');

    // Should show empty state
    await expect(page.getByText(/no people found/i)).toBeVisible();
  });

  test('should clear search results', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for specific person
    await peoplePage.searchPeople('John Smith');
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Clear search
    await peoplePage.searchInput.clear();
    await page.waitForLoadState('networkidle');

    // Should show all people again
    await peoplePage.expectPersonVisible(testData.people.bobJohnson.fullName);
  });

  test('should search by email', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search by email
    await peoplePage.searchPeople('john.smith@example.com');

    // Should find John Smith
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Should not show others
    await peoplePage.expectPersonNotVisible(testData.people.janeSmith.fullName);
  });

  test('should search by partial email', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search by email domain
    await peoplePage.searchPeople('example.com');

    // Should show all people with example.com email (those who have emails)
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.janeSmith.fullName);
  });

  test('should debounce search input', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Type quickly without waiting
    await peoplePage.searchInput.fill('J');
    await peoplePage.searchInput.fill('Jo');
    await peoplePage.searchInput.fill('Joh');
    await peoplePage.searchInput.fill('John');

    // Wait for debounce to complete
    await page.waitForLoadState('networkidle');

    // Should show results for "John"
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
  });

  test('should search with special characters', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search with @ symbol (from email)
    await peoplePage.searchPeople('@example');

    // Should find people with emails
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
  });

  test('should handle empty search gracefully', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search with empty string
    await peoplePage.searchPeople('');

    // Should show all people (no filter applied)
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.bobJohnson.fullName);
  });
});

test.describe('People Filters', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test.skip('should filter by gender', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Filter by Male
    await peoplePage.filterByGenderValue('Male');

    // Should show male people
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.bobJohnson.fullName);

    // Should not show female people
    await peoplePage.expectPersonNotVisible(testData.people.janeSmith.fullName);
  });

  test.skip('should filter by Female gender', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Filter by Female
    await peoplePage.filterByGenderValue('Female');

    // Should show female people
    await peoplePage.expectPersonVisible(testData.people.janeSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.barbaraJohnson.fullName);

    // Should not show male people
    await peoplePage.expectPersonNotVisible(testData.people.johnSmith.fullName);
  });

  test.skip('should filter by campus', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Filter by specific campus
    await peoplePage.filterByCampusValue('Main Campus');

    // Results should only show people from Main Campus
    // Exact expectations depend on test data setup
  });

  test.skip('should combine search and filter', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for "Smith" AND filter by Male
    await peoplePage.searchPeople('Smith');
    await peoplePage.filterByGenderValue('Male');

    // Should show John Smith (male Smith)
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Should not show Jane Smith (female Smith)
    await peoplePage.expectPersonNotVisible(testData.people.janeSmith.fullName);

    // Should not show Bob Johnson (male but not Smith)
    await peoplePage.expectPersonNotVisible(testData.people.bobJohnson.fullName);
  });

  test.skip('should clear gender filter', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Apply gender filter
    await peoplePage.filterByGenderValue('Male');
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Clear filter (select "All" or empty option)
    await peoplePage.filterByGenderValue('All');

    // Should show all genders again
    await peoplePage.expectPersonVisible(testData.people.janeSmith.fullName);
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
  });

  test.skip('should show filter result count', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Filter by gender
    await peoplePage.filterByGenderValue('Male');

    // Should show count of results (e.g., "Showing 4 people")
    await expect(page.getByText(/showing \d+ people/i)).toBeVisible();
  });
});

test.describe('People List Pagination', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test.skip('should paginate results', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Set page size to small number (e.g., 2)
    await peoplePage.pageSizeSelect.selectOption({ label: '25' });

    // If there are more than 25 people, should see next button
    // Click next page
    await peoplePage.nextPageButton.click();

    // Should show different results
    await expect(page).toHaveURL(/page=2/);
  });

  test.skip('should show pagination controls when needed', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // With default page size, may or may not have pagination
    // If there are many people, should see controls
    const hasNextButton = await peoplePage.nextPageButton.isVisible();

    if (hasNextButton) {
      await expect(peoplePage.previousPageButton).toBeDisabled();
    }
  });

  test.skip('should change page size', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Change page size
    await peoplePage.pageSizeSelect.selectOption({ label: '50' });

    // Should load more results per page
    await page.waitForLoadState('networkidle');

    // URL should reflect page size
    await expect(page).toHaveURL(/pageSize=50/);
  });

  test.skip('should disable previous button on first page', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // On first page, previous should be disabled
    await expect(peoplePage.previousPageButton).toBeDisabled();
  });

  test.skip('should navigate back from page 2', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Go to page 2
    await peoplePage.nextPageButton.click();
    await expect(page).toHaveURL(/page=2/);

    // Go back to page 1
    await peoplePage.previousPageButton.click();
    await expect(page).toHaveURL(/page=1/);
  });
});

test.describe('People List - Search Edge Cases', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.expectLoggedIn();
  });

  test('should handle very long search queries', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Very long search string
    const longSearch = 'A'.repeat(200);
    await peoplePage.searchPeople(longSearch);

    // Should handle gracefully (no results)
    await expect(page.getByText(/no people found/i)).toBeVisible();
  });

  test('should handle search with numbers', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search with numbers
    await peoplePage.searchPeople('123');

    // Should handle gracefully
    await page.waitForLoadState('networkidle');
  });

  test('should preserve search across navigation', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search for someone
    await peoplePage.searchPeople('Smith');
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);

    // Click on a person
    await peoplePage.clickPerson(testData.people.johnSmith.fullName);

    // Go back
    await page.goBack();

    // Search should be preserved
    const searchValue = await peoplePage.searchInput.inputValue();
    expect(searchValue).toBe('Smith');
  });

  test('should trim whitespace from search', async ({ page }) => {
    const peoplePage = new PeoplePage(page);

    await peoplePage.gotoList();

    // Search with leading/trailing spaces
    await peoplePage.searchPeople('  Smith  ');

    // Should still find results
    await peoplePage.expectPersonVisible(testData.people.johnSmith.fullName);
  });
});
