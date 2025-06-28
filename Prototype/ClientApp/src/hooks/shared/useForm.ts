import { useState, useCallback, useMemo } from 'react';

// Form field configuration
interface FieldConfig {
  required?: boolean;
  validate?: (value: any) => string | null;
  transform?: (value: any) => any;
}

// Form configuration
interface FormConfig<T> {
  initialValues: T;
  validationRules?: Partial<Record<keyof T, FieldConfig>>;
  onSubmit?: (values: T) => Promise<void> | void;
}

// Form state
interface FormState<T> {
  values: T;
  errors: Partial<Record<keyof T, string>>;
  touched: Partial<Record<keyof T, boolean>>;
  isSubmitting: boolean;
  isValid: boolean;
  isDirty: boolean;
}

// Form actions
interface FormActions<T> {
  setValue: (field: keyof T, value: any) => void;
  setValues: (values: Partial<T>) => void;
  setError: (field: keyof T, error: string) => void;
  setErrors: (errors: Partial<Record<keyof T, string>>) => void;
  setTouched: (field: keyof T, touched?: boolean) => void;
  handleChange: (field: keyof T) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => void;
  handleBlur: (field: keyof T) => (e: React.FocusEvent) => void;
  handleSubmit: (e?: React.FormEvent) => Promise<void>;
  resetForm: () => void;
  validateForm: () => boolean;
  validateField: (field: keyof T) => string | null;
  getFieldProps: (field: keyof T) => {
    value: any;
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => void;
    onBlur: (e: React.FocusEvent) => void;
    error?: string;
  };
}

/**
 * Custom hook for comprehensive form management
 * Handles validation, submission, field state, and common form operations
 */
export function useForm<T extends Record<string, any>>(
  config: FormConfig<T>
): FormState<T> & FormActions<T> {
  const { initialValues, validationRules = {}, onSubmit } = config;

  const [values, setValues] = useState<T>(initialValues);
  const [errors, setErrors] = useState<Partial<Record<keyof T, string>>>({});
  const [touched, setTouched] = useState<Partial<Record<keyof T, boolean>>>({});
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // Calculated state
  const isValid = useMemo(() => {
    return Object.keys(errors).length === 0;
  }, [errors]);

  const isDirty = useMemo(() => {
    return JSON.stringify(values) !== JSON.stringify(initialValues);
  }, [values, initialValues]);

  // Validate a single field
  const validateField = useCallback((field: keyof T): string | null => {
    const value = values[field];
    const rules = (validationRules as any)[field] as FieldConfig | undefined;

    if (!rules) return null;

    // Required validation
    if (rules.required && (!value || (typeof value === 'string' && !value.trim()))) {
      return `${String(field)} is required`;
    }

    // Custom validation
    if (rules.validate && value) {
      return rules.validate(value);
    }

    return null;
  }, [values, validationRules]);

  // Validate entire form
  const validateForm = useCallback((): boolean => {
    const newErrors: Partial<Record<keyof T, string>> = {};

    Object.keys(validationRules).forEach((field) => {
      const error = validateField(field as keyof T);
      if (error) {
        newErrors[field as keyof T] = error;
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [validateField, validationRules]);

  // Set single field value
  const setValue = useCallback((field: keyof T, value: any) => {
    const rules = (validationRules as any)[field] as FieldConfig | undefined;
    const transformedValue = rules?.transform ? rules.transform(value) : value;
    
    setValues(prev => ({
      ...prev,
      [field]: transformedValue
    }));

    // Clear error when field is modified
    if (errors[field]) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  }, [errors, validationRules]);

  // Set multiple field values
  const setFormValues = useCallback((newValues: Partial<T>) => {
    setValues(prev => ({ ...prev, ...newValues }));
  }, []);

  // Set single field error
  const setError = useCallback((field: keyof T, error: string) => {
    setErrors(prev => ({ ...prev, [field]: error }));
  }, []);

  // Set multiple field errors
  const setFormErrors = useCallback((newErrors: Partial<Record<keyof T, string>>) => {
    setErrors(prev => ({ ...prev, ...newErrors }));
  }, []);

  // Set field touched state
  const setFieldTouched = useCallback((field: keyof T, isTouched: boolean = true) => {
    setTouched(prev => ({ ...prev, [field]: isTouched }));
  }, []);

  // Handle input change
  const handleChange = useCallback((field: keyof T) => {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
      const value = e.target.type === 'checkbox' 
        ? (e.target as HTMLInputElement).checked 
        : e.target.value;
      setValue(field, value);
    };
  }, [setValue]);

  // Handle input blur
  const handleBlur = useCallback((field: keyof T) => {
    return (e: React.FocusEvent) => {
      setFieldTouched(field, true);
      
      // Validate field on blur
      const error = validateField(field);
      if (error) {
        setError(field, error);
      }
    };
  }, [setFieldTouched, validateField, setError]);

  // Handle form submission
  const handleSubmit = useCallback(async (e?: React.FormEvent) => {
    if (e) {
      e.preventDefault();
    }

    // Mark all fields as touched
    const allTouched = Object.keys(values).reduce((acc, key) => {
      acc[key as keyof T] = true;
      return acc;
    }, {} as Partial<Record<keyof T, boolean>>);
    setTouched(allTouched);

    // Validate form
    const isFormValid = validateForm();
    
    if (!isFormValid || !onSubmit) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit(values);
    } catch (error) {
      console.error('Form submission error:', error);
    } finally {
      setIsSubmitting(false);
    }
  }, [values, validateForm, onSubmit]);

  // Reset form to initial state
  const resetForm = useCallback(() => {
    setValues(initialValues);
    setErrors({});
    setTouched({});
    setIsSubmitting(false);
  }, [initialValues]);

  // Get field props for easy binding
  const getFieldProps = useCallback((field: keyof T) => {
    return {
      value: values[field] || '',
      onChange: handleChange(field),
      onBlur: handleBlur(field),
      error: touched[field] ? errors[field] : undefined
    };
  }, [values, handleChange, handleBlur, touched, errors]);

  return {
    // State
    values,
    errors,
    touched,
    isSubmitting,
    isValid,
    isDirty,

    // Actions
    setValue,
    setValues: setFormValues,
    setError,
    setErrors: setFormErrors,
    setTouched: setFieldTouched,
    handleChange,
    handleBlur,
    handleSubmit,
    resetForm,
    validateForm,
    validateField,
    getFieldProps
  };
}