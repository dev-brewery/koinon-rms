namespace   Koinon.Domain.Entities  ;

/// <summary>
/// Entity with unusual but valid C# formatting.
/// Tests parser resilience to whitespace variations.
/// </summary>
public    class    UnusualFormattingEntity    :    Entity
{
    // Excessive spacing
    public    required    string    Name    {    get    ;    set    ;    }

    // Tabs and spaces mixed
	public	string?	Description	{	get;	set;	}

    // Multiple blank lines above and below


    public int Value { get; set; }



    // Single-line property with spaces
    public string? SingleLine { get; set; }

    // Property with newlines in weird places
    public string?
        MultiLine
        {
            get;
            set;
        }

    // Minimal spacing
    public int Compact{get;set;}

    // Comment styles
    /* Block comment */public string? BlockComment{get;set;}
    //Line comment
    public string? LineComment{get;set;}

    /// XML doc with     extra     spacing
    public    string?    XmlDoc    {    get    ;    set    ;    }

    // Property with region
    #region Properties
    public string? RegionProperty { get; set; }
    #endregion

    // Nested braces with unusual indentation
public void Method()
{
if(true)
{
var x=1;
}
}
}
