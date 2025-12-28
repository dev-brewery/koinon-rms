namespace Koinon.Domain.Entities;

/// <summary>
/// This file contains intentional syntax errors to test parser error handling.
/// </summary>
public class SyntaxErrorEntity : Entity
{
    // Missing closing brace for property
    public string Name { get; set;

    // Missing semicolon
    public int Age { get; set }

    // Unclosed string literal
    public string Description = "This is unclosed

    // Mismatched braces
    public void DoSomething()
    {
        if (true)
        {
            Console.WriteLine("Test")
        }
    }}

    // Missing type
    public MissingType Value { get; set; }
