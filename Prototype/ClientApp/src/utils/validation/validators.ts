/**
 * Comprehensive validation utilities for form fields and data validation
 * These validators return error messages or null if valid
 * Uses shared constants to ensure frontend-backend consistency
 */

import { VALIDATION_CONSTANTS, VALIDATION_MESSAGES } from '../validationConstants';

export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

// Basic validation functions
export const validators = {
  // Required field validation
  required: (value: any, fieldName: string = 'Field'): string | null => {
    if (value === null || value === undefined || value === '') {
      return `${fieldName} is required`;
    }
    if (typeof value === 'string' && !value.trim()) {
      return `${fieldName} is required`;
    }
    return null;
  },

  // Email validation
  email: (value: string): string | null => {
    if (!value) return null; // Use required() separately if needed
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(value)) {
      return 'Please enter a valid email address';
    }
    return null;
  },

  // Password validation
  password: (value: string): string | null => {
    if (!value) return null;
    
    const { MIN_LENGTH, MAX_LENGTH, REQUIRE_UPPERCASE, REQUIRE_LOWERCASE, REQUIRE_DIGIT } = VALIDATION_CONSTANTS.PASSWORD;
    
    if (value.length < MIN_LENGTH) {
      return VALIDATION_MESSAGES.MIN_LENGTH('Password', MIN_LENGTH);
    }
    
    if (value.length > MAX_LENGTH) {
      return VALIDATION_MESSAGES.MAX_LENGTH('Password', MAX_LENGTH);
    }
    
    if (REQUIRE_LOWERCASE && !/(?=.*[a-z])/.test(value)) {
      return VALIDATION_MESSAGES.WEAK_PASSWORD;
    }
    
    if (REQUIRE_UPPERCASE && !/(?=.*[A-Z])/.test(value)) {
      return VALIDATION_MESSAGES.WEAK_PASSWORD;
    }
    
    if (REQUIRE_DIGIT && !/(?=.*\d)/.test(value)) {
      return VALIDATION_MESSAGES.WEAK_PASSWORD;
    }
    
    return null;
  },

  // Password confirmation
  confirmPassword: (password: string, confirmPassword: string): string | null => {
    if (!confirmPassword) return null;
    
    if (password !== confirmPassword) {
      return VALIDATION_MESSAGES.PASSWORD_MISMATCH;
    }
    return null;
  },

  // Phone number validation
  phoneNumber: (value: string): string | null => {
    if (!value) return null;
    
    const { MIN_DIGITS, MAX_DIGITS, MAX_LENGTH } = VALIDATION_CONSTANTS.PHONE;
    
    if (value.length > MAX_LENGTH) {
      return VALIDATION_MESSAGES.MAX_LENGTH('Phone number', MAX_LENGTH);
    }
    
    // Remove all non-digit characters
    const cleaned = value.replace(/\D/g, '');
    
    if (cleaned.length < MIN_DIGITS) {
      return `Phone number must be at least ${MIN_DIGITS} digits`;
    }
    
    if (cleaned.length > MAX_DIGITS) {
      return `Phone number must be no more than ${MAX_DIGITS} digits`;
    }
    
    return null;
  },

  // Username validation
  username: (value: string): string | null => {
    if (!value) return null;
    
    if (value.length < 3) {
      return 'Username must be at least 3 characters long';
    }
    
    if (value.length > 30) {
      return 'Username must be no more than 30 characters long';
    }
    
    if (!/^[a-zA-Z0-9_.-]+$/.test(value)) {
      return 'Username can only contain letters, numbers, dots, hyphens, and underscores';
    }
    
    return null;
  },

  // Minimum length validation
  minLength: (min: number) => (value: string): string | null => {
    if (!value) return null;
    
    if (value.length < min) {
      return `Must be at least ${min} characters long`;
    }
    return null;
  },

  // Maximum length validation
  maxLength: (max: number) => (value: string): string | null => {
    if (!value) return null;
    
    if (value.length > max) {
      return `Must be no more than ${max} characters long`;
    }
    return null;
  },

  // Numeric validation
  numeric: (value: string): string | null => {
    if (!value) return null;
    
    if (!/^\d+$/.test(value)) {
      return 'Must contain only numbers';
    }
    return null;
  },

  // Alphanumeric validation
  alphanumeric: (value: string): string | null => {
    if (!value) return null;
    
    if (!/^[a-zA-Z0-9]+$/.test(value)) {
      return 'Must contain only letters and numbers';
    }
    return null;
  },

  // URL validation
  url: (value: string): string | null => {
    if (!value) return null;
    
    try {
      new URL(value);
      return null;
    } catch {
      return 'Please enter a valid URL';
    }
  },

  // Date validation
  date: (value: string): string | null => {
    if (!value) return null;
    
    const date = new Date(value);
    if (isNaN(date.getTime())) {
      return 'Please enter a valid date';
    }
    return null;
  },

  // Future date validation
  futureDate: (value: string): string | null => {
    if (!value) return null;
    
    const date = new Date(value);
    const now = new Date();
    
    if (isNaN(date.getTime())) {
      return 'Please enter a valid date';
    }
    
    if (date <= now) {
      return 'Date must be in the future';
    }
    
    return null;
  },

  // Past date validation
  pastDate: (value: string): string | null => {
    if (!value) return null;
    
    const date = new Date(value);
    const now = new Date();
    
    if (isNaN(date.getTime())) {
      return 'Please enter a valid date';
    }
    
    if (date >= now) {
      return 'Date must be in the past';
    }
    
    return null;
  },

  // Custom pattern validation
  pattern: (regex: RegExp, message: string) => (value: string): string | null => {
    if (!value) return null;
    
    if (!regex.test(value)) {
      return message;
    }
    return null;
  },

  // Range validation for numbers
  range: (min: number, max: number) => (value: string | number): string | null => {
    if (!value) return null;
    
    const num = typeof value === 'string' ? parseFloat(value) : value;
    
    if (isNaN(num)) {
      return 'Must be a valid number';
    }
    
    if (num < min || num > max) {
      return `Must be between ${min} and ${max}`;
    }
    
    return null;
  },

  // File size validation (in bytes)
  fileSize: (maxSize: number) => (file: File): string | null => {
    if (!file) return null;
    
    if (file.size > maxSize) {
      const maxMB = (maxSize / (1024 * 1024)).toFixed(2);
      return `File size must be less than ${maxMB}MB`;
    }
    
    return null;
  },

  // File type validation
  fileType: (allowedTypes: string[]) => (file: File): string | null => {
    if (!file) return null;
    
    if (!allowedTypes.includes(file.type)) {
      return `File type must be one of: ${allowedTypes.join(', ')}`;
    }
    
    return null;
  }
};

// Enhanced validation schema builder with data-aware validators
export class ValidationSchema<T extends Record<string, any>> {
  private rules: Partial<Record<keyof T, Array<(value: any, data?: T) => string | null>>> = {};

  field(fieldName: keyof T): FieldValidator<T> {
    return new FieldValidator(this, fieldName);
  }

  validate(data: T): Partial<Record<keyof T, string>> {
    const errors: Partial<Record<keyof T, string>> = {};

    for (const [field, validators] of Object.entries(this.rules)) {
      const value = data[field as keyof T];
      
      for (const validator of validators as Array<(value: any, data?: T) => string | null>) {
        const error = validator(value, data);
        if (error) {
          errors[field as keyof T] = error;
          break; // Stop at first error
        }
      }
    }

    return errors;
  }

  validateField(fieldName: keyof T, value: any, data: T): string[] {
    const validators = this.rules[fieldName] || [];
    const errors: string[] = [];
    
    for (const validator of validators) {
      const error = validator(value, data);
      if (error) {
        errors.push(error);
      }
    }
    
    return errors;
  }

  addRule(field: keyof T, validator: (value: any, data?: T) => string | null): void {
    if (!this.rules[field]) {
      this.rules[field] = [];
    }
    this.rules[field]!.push(validator);
  }
}

class FieldValidator<T extends Record<string, any>> {
  constructor(
    private schema: ValidationSchema<T>,
    private fieldName: keyof T
  ) {}

  required(message?: string): FieldValidator<T> {
    this.schema.addRule(
      this.fieldName,
      (value, data?) => validators.required(value, message || String(this.fieldName))
    );
    return this;
  }

  email(): FieldValidator<T> {
    this.schema.addRule(this.fieldName, (value, data?) => validators.email(value));
    return this;
  }

  password(): FieldValidator<T> {
    this.schema.addRule(this.fieldName, (value, data?) => validators.password(value));
    return this;
  }

  minLength(min: number): FieldValidator<T> {
    this.schema.addRule(this.fieldName, (value, data?) => validators.minLength(min)(value));
    return this;
  }

  maxLength(max: number): FieldValidator<T> {
    this.schema.addRule(this.fieldName, (value, data?) => validators.maxLength(max)(value));
    return this;
  }

  pattern(regex: RegExp, message: string): FieldValidator<T> {
    this.schema.addRule(this.fieldName, (value, data?) => validators.pattern(regex, message)(value));
    return this;
  }

  custom(validator: (value: any, data?: T) => string | null): FieldValidator<T> {
    this.schema.addRule(this.fieldName, validator);
    return this;
  }

  // Return to schema for chaining other fields
  and(): ValidationSchema<T> {
    return this.schema;
  }
}