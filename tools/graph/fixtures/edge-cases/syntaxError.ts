/**
 * Edge case: TypeScript file with syntax errors.
 * Tests parser error handling and recovery.
 */

export interface ValidInterface {
  name: string;
  age: number;

// SYNTAX ERROR: Missing closing brace above

export type BrokenType = {
  value: string
  // SYNTAX ERROR: Missing semicolon and closing brace

export const malformedObject = {
  key: "value"
  // SYNTAX ERROR: Missing comma
  another: "value"
  // SYNTAX ERROR: Missing closing brace

// SYNTAX ERROR: Unclosed string
export const unclosedString = "This string never ends

// SYNTAX ERROR: Invalid generic syntax
export type BadGeneric<T extends = string;

// SYNTAX ERROR: Mismatched brackets
export function badFunction(): void {
  const arr = [1, 2, 3;
  return;
}}

// SYNTAX ERROR: Invalid type annotation
export const invalidType: : string = "test";
