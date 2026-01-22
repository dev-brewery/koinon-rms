"""
Helper functions for RAG-based pattern detection.

These functions provide heuristics to detect anti-patterns and architectural violations.
"""
import re


def has_business_logic(code: str) -> bool:
    """
    Heuristics to detect business logic in controllers.

    Controllers should ONLY handle HTTP concerns, not business rules.
    """
    # Multiple calculations
    if code.count('=') > 5 and ('*' in code or '/' in code or '+' in code):
        return True

    # Loops over collections (business logic)
    if 'foreach' in code or 'for (' in code:
        return True

    # Complex conditionals (business rules)
    if code.count('if') > 3:
        return True

    # String manipulation (business logic)
    if '.Split(' in code or '.Substring(' in code or '.Replace(' in code:
        return True

    return False


def is_n_plus_one_pattern(code: str) -> bool:
    """
    Check if code has foreach with DB query inside - classic N+1 problem.

    Example violation:
        foreach (var person in people) {
            var family = await _context.Families.FirstOrDefaultAsync(f => f.Id == person.FamilyId);
        }
    """
    lines = code.split('\n')
    in_loop = False

    for line in lines:
        if 'foreach' in line or 'for (' in line:
            in_loop = True

        if in_loop and ('await' in line or 'FirstOrDefault' in line or 'Where(' in line):
            return True

        # Reset on loop end
        if in_loop and '}' in line:
            in_loop = False

    return False


def extract_class_name(code: str) -> str:
    """
    Extract class name from C# code.

    Example:
        "public class PersonDto : IDto" -> "PersonDto"
    """
    match = re.search(r'class\s+(\w+)', code)
    return match.group(1) if match else None


def extract_api_call(code: str) -> str:
    """
    Extract the API call snippet from component code.

    Looks for fetch() or axios.* calls.
    """
    lines = code.split('\n')

    for line in lines:
        if 'fetch(' in line or 'axios.' in line:
            return line.strip()

    return "API call not found"
