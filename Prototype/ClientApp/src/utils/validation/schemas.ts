/**
 * Common validation schemas for forms throughout the application
 * These schemas can be imported and used with the useForm hook
 */

import { ValidationSchema, validators } from './validators';

// User-related schemas
export interface LoginFormData {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterFormData {
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
}

export interface UserProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

export interface ChangePasswordFormData {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// Application-related schemas
export interface ApplicationFormData {
  applicationName: string;
  applicationDescription: string;
  applicationDataSourceType: string;
  host: string;
  port: string;
  databaseName: string;
  authenticationType: string;
  username?: string;
}

// Validation schemas
export const loginSchema = new ValidationSchema<LoginFormData>()
  .field('username')
    .required('Username is required')
    .minLength(3)
  .and()
  .field('password')
    .required('Password is required')
    .minLength(1);

export const registerSchema = new ValidationSchema<RegisterFormData>()
  .field('firstName')
    .required('First name is required')
    .minLength(2)
    .maxLength(50)
    .pattern(/^[a-zA-Z\s]+$/, 'First name can only contain letters and spaces')
  .and()
  .field('lastName')
    .required('Last name is required')
    .minLength(2)
    .maxLength(50)
    .pattern(/^[a-zA-Z\s]+$/, 'Last name can only contain letters and spaces')
  .and()
  .field('username')
    .required('Username is required')
    .custom(validators.username)
  .and()
  .field('email')
    .required('Email is required')
    .email()
  .and()
  .field('phoneNumber')
    .required('Phone number is required')
    .custom(validators.phoneNumber)
  .and()
  .field('password')
    .required('Password is required')
    .password()
  .and()
  .field('confirmPassword')
    .required('Please confirm your password')
    .custom((value: string, data?: RegisterFormData) => {
      if (!data) return null;
      return validators.confirmPassword(data.password, value);
    });

export const userProfileSchema = new ValidationSchema<UserProfileFormData>()
  .field('firstName')
    .required('First name is required')
    .minLength(2)
    .maxLength(50)
    .pattern(/^[a-zA-Z\s]+$/, 'First name can only contain letters and spaces')
  .and()
  .field('lastName')
    .required('Last name is required')
    .minLength(2)
    .maxLength(50)
    .pattern(/^[a-zA-Z\s]+$/, 'Last name can only contain letters and spaces')
  .and()
  .field('email')
    .required('Email is required')
    .email()
  .and()
  .field('phoneNumber')
    .required('Phone number is required')
    .custom(validators.phoneNumber);

export const changePasswordSchema = new ValidationSchema<ChangePasswordFormData>()
  .field('currentPassword')
    .required('Current password is required')
  .and()
  .field('newPassword')
    .required('New password is required')
    .password()
    .custom((value: string, data?: ChangePasswordFormData) => {
      if (!data) return null;
      if (value === data.currentPassword) {
        return 'New password must be different from current password';
      }
      return null;
    })
  .and()
  .field('confirmPassword')
    .required('Please confirm your new password')
    .custom((value: string, data?: ChangePasswordFormData) => {
      if (!data) return null;
      return validators.confirmPassword(data.newPassword, value);
    });

export const applicationSchema = new ValidationSchema<ApplicationFormData>()
  .field('applicationName')
    .required('Application name is required')
    .minLength(3)
    .maxLength(100)
    .pattern(/^[a-zA-Z0-9\s\-_]+$/, 'Application name can only contain letters, numbers, spaces, hyphens, and underscores')
  .and()
  .field('applicationDescription')
    .required('Application description is required')
    .minLength(10)
    .maxLength(500)
  .and()
  .field('applicationDataSourceType')
    .required('Data source type is required')
  .and()
  .field('host')
    .required('Host is required')
    .pattern(/^[a-zA-Z0-9\-\.]+$/, 'Host must be a valid hostname or IP address')
  .and()
  .field('port')
    .required('Port is required')
    .custom(validators.numeric)
    .custom(validators.range(1, 65535))
  .and()
  .field('databaseName')
    .required('Database name is required')
    .minLength(1)
    .maxLength(100)
    .pattern(/^[a-zA-Z0-9_\-]+$/, 'Database name can only contain letters, numbers, underscores, and hyphens')
  .and()
  .field('authenticationType')
    .required('Authentication type is required');

// Search and filter schemas
export interface SearchFormData {
  searchTerm: string;
  category?: string;
  dateFrom?: string;
  dateTo?: string;
}

export const searchSchema = new ValidationSchema<SearchFormData>()
  .field('searchTerm')
    .minLength(2)
    .maxLength(100)
  .and()
  .field('dateFrom')
    .custom((value) => value ? validators.date(value) : null)
  .and()
  .field('dateTo')
    .custom((value) => value ? validators.date(value) : null)
    .custom((value: string, data?: SearchFormData) => {
      if (!data || !value || !data.dateFrom) return null;
      const fromDate = new Date(data.dateFrom);
      const toDate = new Date(value);
      if (toDate < fromDate) {
        return 'End date must be after start date';
      }
      return null;
    });

// File upload schemas
export interface FileUploadFormData {
  file: File | null;
  description?: string;
}

export const fileUploadSchema = new ValidationSchema<FileUploadFormData>()
  .field('file')
    .required('Please select a file')
    .custom(validators.fileSize(10 * 1024 * 1024)) // 10MB max
    .custom(validators.fileType([
      'text/csv',
      'application/vnd.ms-excel',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    ]))
  .and()
  .field('description')
    .maxLength(200);

// Contact form schema
export interface ContactFormData {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export const contactSchema = new ValidationSchema<ContactFormData>()
  .field('name')
    .required('Name is required')
    .minLength(2)
    .maxLength(100)
  .and()
  .field('email')
    .required('Email is required')
    .email()
  .and()
  .field('subject')
    .required('Subject is required')
    .minLength(5)
    .maxLength(200)
  .and()
  .field('message')
    .required('Message is required')
    .minLength(10)
    .maxLength(1000);

// Settings form schema
export interface SettingsFormData {
  emailNotifications: boolean;
  pushNotifications: boolean;
  marketingEmails: boolean;
  theme: string;
  language: string;
  timezone: string;
}

export const settingsSchema = new ValidationSchema<SettingsFormData>()
  .field('theme')
    .required('Theme selection is required')
    .custom((value) => {
      const validThemes = ['light', 'dark', 'auto'];
      return validThemes.includes(value) ? null : 'Invalid theme selection';
    })
  .and()
  .field('language')
    .required('Language selection is required')
    .custom((value) => {
      const validLanguages = ['en', 'es', 'fr', 'de'];
      return validLanguages.includes(value) ? null : 'Invalid language selection';
    })
  .and()
  .field('timezone')
    .required('Timezone selection is required');

// Export all schemas for easy importing
export const validationSchemas = {
  login: loginSchema,
  register: registerSchema,
  userProfile: userProfileSchema,
  changePassword: changePasswordSchema,
  application: applicationSchema,
  search: searchSchema,
  fileUpload: fileUploadSchema,
  contact: contactSchema,
  settings: settingsSchema
};

// Helper function to get schema by name
export function getValidationSchema(schemaName: keyof typeof validationSchemas) {
  return validationSchemas[schemaName];
}