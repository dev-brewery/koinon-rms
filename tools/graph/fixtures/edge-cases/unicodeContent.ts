/**
 * Edge case: TypeScript file with Unicode content.
 * Tests handling of international characters, emojis, and special symbols.
 * 日本語、中文、العربية、Ελληνικά
 */

/**
 * Interface with Unicode property names (valid in ES6+)
 */
export interface UnicodeInterface {
  /** Name with emoji 🎉 */
  name: string;

  /** Description with accents: café, naïve, façade */
  description?: string;

  /** Text in various languages */
  日本語?: string;
  中文?: string;
  العربية?: string;

  /** Math symbols: ∑∫∂√∞ */
  mathSymbols?: string;

  /** Currency symbols: $€£¥₹ */
  amount?: number;
}

/**
 * Type with Unicode in comments and strings
 */
export type UnicodeType = {
  // French: àéèêëïôù
  french: string;

  // German: ÄäÖöÜüß
  german: string;

  // Spanish: ñÑáéíóúü¿¡
  spanish: string;

  // Emoji: 😀😃😄😁🎉🎊✨🌟⭐
  emoji: string;
};

/**
 * Function with Unicode string literals
 */
export function processUnicode(text: string): string {
  const greetings = {
    english: "Hello",
    japanese: "こんにちは",
    chinese: "你好",
    arabic: "مرحبا",
    greek: "Γειά σου",
    russian: "Здравствуйте",
  };

  return `${greetings.english} - ${text}`;
}

/**
 * Constant with Unicode values
 */
export const UNICODE_CONSTANTS = {
  COPYRIGHT: "©",
  REGISTERED: "®",
  TRADEMARK: "™",
  ARROWS: "←↑→↓↔↕",
  CHECKMARK: "✓✔",
  BALLOT: "☐☑☒",
  STARS: "★☆⭐",
  HEARTS: "♥♡💗",
  MUSIC: "♩♪♫♬🎵🎶",
} as const;
