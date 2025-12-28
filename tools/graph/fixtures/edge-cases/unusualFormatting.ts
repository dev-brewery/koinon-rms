/**
 * Edge case: TypeScript file with unusual but valid formatting.
 * Tests parser resilience to whitespace variations.
 */

// Excessive spacing
export    interface    SpacedInterface    {
    name    :    string    ;
    value    :    number    ;
}

// Minimal spacing
export type CompactType={name:string;value:number;}

// Single-line interface
export interface SingleLine { name: string; age: number; email: string; }

// Multi-line with unusual breaks
export type
    MultiLineType
    =
    {
        property1
            :
            string
            ;
        property2
            :
            number
            ;
    }
    ;

// Mixed tabs and spaces (usually a linting violation, but valid)
export interface	TabInterface	{
	name	:	string	;
		value	:	number	;
}

// Lots of blank lines


export interface BlankLinesInterface {


    name: string;


    value: number;


}


// Array of objects with weird formatting
export const weirdArray = [
    {a:1,b:2,c:3},
    {
        a
        :
        1
        ,
        b
        :
        2
    }
    ,
    {a:1,
     b:2,
     c:3}
];

// Nested braces with unusual indentation
export type NestedType = {
outer: {
inner: {
deep: {
value: string
}
}
}
};

// Comment variations
/*Block*/export type BlockComment={value:string};
//Line
export type LineComment = {value:string};
/** Doc */export type DocComment={value:string};

// Trailing commas everywhere
export interface TrailingCommas {
    name: string,
    age: number,
}

export type TrailingType = {
    value: string,
};
