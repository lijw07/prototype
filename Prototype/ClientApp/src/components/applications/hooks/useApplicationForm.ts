// Custom hook for application form logic
// Handles form state, validation, and submission following SRP

import { useState, useCallback, useEffect } from 'react';
import type { Application } from '../../../types/api.types';

// Data source type enumeration
export const DataSourceTypeEnum: { [key: number]: string } = {
  // Database connections
  0: 'MicrosoftSqlServer',
  1: 'MySql', 
  2: 'PostgreSql',
  3: 'MongoDb',
  4: 'Redis',
  5: 'Oracle',
  6: 'MariaDb',
  7: 'Sqlite',
  8: 'Cassandra',
  9: 'ElasticSearch',
  
  // API connections
  10: 'RestApi',
  11: 'GraphQL',
  12: 'SoapApi', 
  13: 'ODataApi',
  14: 'WebSocket',
  
  // File-based connections
  15: 'CsvFile',
  16: 'JsonFile',
  17: 'XmlFile',
  18: 'ExcelFile',
  
  // Cloud connections
  19: 'AzureSqlDatabase',
  20: 'AmazonS3',
  21: 'AzureBlobStorage',
  22: 'GoogleCloudStorage'
};

// Helper function to get enum string name from numeric or string value
export const getDataSourceTypeName = (value: number | string): string => {
  if (typeof value === 'number') {
    return DataSourceTypeEnum[value] || 'Unknown';
  }
  return value || 'Unknown';
};

interface ApplicationFormData {
  applicationName: string;
  applicationDescription: string;
  applicationDataSourceType: number;
  connection: {
    host: string;
    port: string;
    databaseName: string;
    authenticationType: string;
    username?: string;
    password?: string;
    authenticationDatabase?: string;
    awsAccessKeyId?: string;
    awsSecretAccessKey?: string;
    awsRoleArn?: string;
    principal?: string;
    serviceName?: string;
    serviceRealm?: string;
    canonicalizeHostName?: boolean;
  };
}

interface FormValidationErrors {
  [key: string]: string | undefined;
}

interface FormState {
  data: ApplicationFormData;
  errors: FormValidationErrors;
  isValid: boolean;
  isSubmitting: boolean;
  submitSuccess: boolean;
  editingApp: Application | null;
}

const initialFormData: ApplicationFormData = {
  applicationName: '',
  applicationDescription: '',
  applicationDataSourceType: 0,
  connection: {
    host: '',
    port: '',
    databaseName: '',
    authenticationType: 'UserPassword',
    username: '',
    password: ''
  }
};

export const useApplicationForm = () => {
  const [state, setState] = useState<FormState>({
    data: initialFormData,
    errors: {},
    isValid: false,
    isSubmitting: false,
    submitSuccess: false,
    editingApp: null
  });

  const [showPasswords, setShowPasswords] = useState({
    password: false,
    awsSecretAccessKey: false
  });

  // Validation rules
  const validateField = useCallback((fieldName: string, value: any): string | undefined => {
    switch (fieldName) {
      case 'applicationName':
        if (!value || value.trim().length === 0) {
          return 'Application name is required';
        }
        if (value.length < 3) {
          return 'Application name must be at least 3 characters';
        }
        if (value.length > 100) {
          return 'Application name must be less than 100 characters';
        }
        break;

      case 'applicationDescription':
        if (!value || value.trim().length === 0) {
          return 'Description is required';
        }
        if (value.length > 500) {
          return 'Description must be less than 500 characters';
        }
        break;

      case 'connection.host':
        if (!value || value.trim().length === 0) {
          return 'Host is required';
        }
        // Basic hostname/IP validation
        const hostRegex = /^[a-zA-Z0-9.-]+$/;
        if (!hostRegex.test(value)) {
          return 'Invalid host format';
        }
        break;

      case 'connection.port':
        if (!value) {
          return 'Port is required';
        }
        const port = parseInt(value);
        if (isNaN(port) || port < 1 || port > 65535) {
          return 'Port must be between 1 and 65535';
        }
        break;

      case 'connection.databaseName':
        if (!value || value.trim().length === 0) {
          return 'Database name is required';
        }
        break;

      case 'connection.username':
        if (state.data.connection.authenticationType === 'UserPassword' && (!value || value.trim().length === 0)) {
          return 'Username is required for user/password authentication';
        }
        break;

      case 'connection.password':
        if (state.data.connection.authenticationType === 'UserPassword' && (!value || value.trim().length === 0)) {
          return 'Password is required for user/password authentication';
        }
        break;

      default:
        return undefined;
    }
    return undefined;
  }, [state.data.connection.authenticationType]);

  // Validate entire form
  const validateForm = useCallback(() => {
    const errors: FormValidationErrors = {};
    
    // Validate basic fields
    errors.applicationName = validateField('applicationName', state.data.applicationName);
    errors.applicationDescription = validateField('applicationDescription', state.data.applicationDescription);
    errors['connection.host'] = validateField('connection.host', state.data.connection.host);
    errors['connection.port'] = validateField('connection.port', state.data.connection.port);
    errors['connection.databaseName'] = validateField('connection.databaseName', state.data.connection.databaseName);
    
    // Conditional validation based on authentication type
    if (state.data.connection.authenticationType === 'UserPassword') {
      errors['connection.username'] = validateField('connection.username', state.data.connection.username);
      errors['connection.password'] = validateField('connection.password', state.data.connection.password);
    }

    // Remove undefined errors
    Object.keys(errors).forEach(key => {
      if (errors[key] === undefined) {
        delete errors[key];
      }
    });

    const isValid = Object.keys(errors).length === 0;

    setState(prev => ({
      ...prev,
      errors,
      isValid
    }));

    return isValid;
  }, [state.data, validateField]);

  // Update form field
  const updateField = useCallback((fieldPath: string, value: any) => {
    setState(prev => {
      const newData = { ...prev.data };
      
      // Handle nested field paths like 'connection.host'
      if (fieldPath.includes('.')) {
        const [parent, child] = fieldPath.split('.');
        if (parent === 'connection') {
          newData.connection = {
            ...newData.connection,
            [child]: value
          };
        }
      } else {
        (newData as any)[fieldPath] = value;
      }

      return {
        ...prev,
        data: newData
      };
    });
  }, []);

  // Reset form to initial state
  const resetForm = useCallback(() => {
    setState(prev => ({
      ...prev,
      data: initialFormData,
      errors: {},
      isValid: false,
      isSubmitting: false,
      submitSuccess: false,
      editingApp: null
    }));
    setShowPasswords({
      password: false,
      awsSecretAccessKey: false
    });
  }, []);

  // Load application data for editing
  const loadApplication = useCallback((application: Application) => {
    const formData: ApplicationFormData = {
      applicationName: application.applicationName,
      applicationDescription: application.applicationDescription,
      applicationDataSourceType: typeof application.applicationDataSourceType === 'number' 
        ? application.applicationDataSourceType 
        : 0, // Default to first option if string
      connection: {
        host: application.connection.host,
        port: application.connection.port,
        databaseName: application.connection.databaseName,
        authenticationType: application.connection.authenticationType,
        username: application.connection.username || '',
        password: '', // Don't pre-fill password for security
        authenticationDatabase: application.connection.authenticationDatabase || '',
        awsAccessKeyId: application.connection.awsAccessKeyId || '',
        awsSecretAccessKey: '', // Don't pre-fill secret for security
        awsRoleArn: application.connection.awsRoleArn || '',
        principal: application.connection.principal || '',
        serviceName: application.connection.serviceName || '',
        serviceRealm: application.connection.serviceRealm || '',
        canonicalizeHostName: application.connection.canonicalizeHostName || false
      }
    };

    setState(prev => ({
      ...prev,
      data: formData,
      editingApp: application,
      errors: {},
      isValid: false,
      submitSuccess: false
    }));
  }, []);

  // Set form submission state
  const setSubmitting = useCallback((isSubmitting: boolean) => {
    setState(prev => ({ ...prev, isSubmitting }));
  }, []);

  // Set submission success state
  const setSubmitSuccess = useCallback((success: boolean) => {
    setState(prev => ({ ...prev, submitSuccess: success }));
  }, []);

  // Toggle password visibility
  const togglePasswordVisibility = useCallback((field: keyof typeof showPasswords) => {
    setShowPasswords(prev => ({ ...prev, [field]: !prev[field] }));
  }, []);

  // Get authentication type options based on data source type
  const getAuthenticationOptions = useCallback(() => {
    const dataSourceType = state.data.applicationDataSourceType;
    
    // Different auth types for different data sources
    switch (dataSourceType) {
      case 3: // MongoDB
        return [
          { value: 'UserPassword', label: 'Username/Password' },
          { value: 'NoAuth', label: 'No Authentication' }
        ];
      case 4: // Redis
        return [
          { value: 'UserPassword', label: 'Username/Password' },
          { value: 'NoAuth', label: 'No Authentication' }
        ];
      case 19: // Azure SQL Database
        return [
          { value: 'UserPassword', label: 'Username/Password' },
          { value: 'AzureAdPassword', label: 'Azure AD Password' },
          { value: 'AzureAdIntegrated', label: 'Azure AD Integrated' }
        ];
      default:
        return [
          { value: 'UserPassword', label: 'Username/Password' },
          { value: 'Integrated', label: 'Integrated Security' },
          { value: 'NoAuth', label: 'No Authentication' }
        ];
    }
  }, [state.data.applicationDataSourceType]);

  // Auto-validate when data changes
  useEffect(() => {
    if (state.data.applicationName || state.data.applicationDescription || state.data.connection.host) {
      validateForm();
    }
  }, [state.data, validateForm]);

  return {
    // State
    formData: state.data,
    errors: state.errors,
    isValid: state.isValid,
    isSubmitting: state.isSubmitting,
    submitSuccess: state.submitSuccess,
    editingApp: state.editingApp,
    showPasswords,

    // Actions
    updateField,
    resetForm,
    loadApplication,
    validateForm,
    setSubmitting,
    setSubmitSuccess,
    togglePasswordVisibility,
    getAuthenticationOptions,

    // Computed values
    isEditing: state.editingApp !== null,
    dataSourceTypeName: getDataSourceTypeName(state.data.applicationDataSourceType)
  };
};

export type { ApplicationFormData, FormValidationErrors, FormState };