/**
 * Shared validation constants between frontend and backend
 * These should match the backend ValidationConstants.cs
 */

export const VALIDATION_CONSTANTS = {
  // Password validation
  PASSWORD: {
    MIN_LENGTH: 8,
    MAX_LENGTH: 128, // Standardized to match backend max
    REQUIRE_UPPERCASE: true,
    REQUIRE_LOWERCASE: true,
    REQUIRE_DIGIT: true,
    REQUIRE_SPECIAL_CHAR: false, // Frontend doesn't enforce special chars by default
  },

  // Username validation  
  USERNAME: {
    MIN_LENGTH: 3,
    MAX_LENGTH: 50, // Standardized to match backend ValidationConstants
    PATTERN: /^[a-zA-Z0-9._-]+$/, // Alphanumeric with dots, underscores, hyphens
  },

  // Email validation
  EMAIL: {
    MAX_LENGTH: 100,
    PATTERN: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/, // Match backend pattern
  },

  // Phone number validation
  PHONE: {
    MIN_DIGITS: 10,
    MAX_DIGITS: 15, // Standardized to be more permissive
    MAX_LENGTH: 20, // With formatting characters
  },

  // Application validation
  APPLICATION: {
    NAME: {
      MIN_LENGTH: 3,
      MAX_LENGTH: 100,
    },
    DESCRIPTION: {
      MIN_LENGTH: 10,
      MAX_LENGTH: 500,
    },
  },

  // General field validation
  FIELD: {
    FIRST_NAME: {
      MIN_LENGTH: 2,
      MAX_LENGTH: 50,
    },
    LAST_NAME: {
      MIN_LENGTH: 2,
      MAX_LENGTH: 50,
    },
  },
} as const;

// Error messages matching backend patterns
export const VALIDATION_MESSAGES = {
  REQUIRED: (fieldName: string) => `${fieldName} is required`,
  MIN_LENGTH: (fieldName: string, min: number) => `${fieldName} must be at least ${min} characters long`,
  MAX_LENGTH: (fieldName: string, max: number) => `${fieldName} must be no more than ${max} characters long`,
  INVALID_EMAIL: 'Please enter a valid email address',
  INVALID_PHONE: 'Please enter a valid phone number',
  PASSWORD_MISMATCH: 'Passwords do not match',
  INVALID_USERNAME: 'Username can only contain letters, numbers, dots, underscores, and hyphens',
  WEAK_PASSWORD: 'Password must contain at least one uppercase letter, one lowercase letter, and one digit',
} as const;