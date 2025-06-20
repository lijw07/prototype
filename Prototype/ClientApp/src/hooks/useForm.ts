import { useState, useCallback } from 'react';

interface ValidationRule {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: RegExp;
  custom?: (value: any) => string | null;
}

interface FormField {
  value: any;
  error: string;
  touched: boolean;
}

interface FormState {
  [key: string]: FormField;
}

interface UseFormOptions<T> {
  initialValues: T;
  validationRules?: { [K in keyof T]?: ValidationRule };
  onSubmit?: (values: T) => void | Promise<void>;
}

export function useForm<T extends Record<string, any>>({
  initialValues,
  validationRules = {},
  onSubmit,
}: UseFormOptions<T>) {
  const [formState, setFormState] = useState<FormState>(() => {
    const initialState: FormState = {};
    Object.keys(initialValues).forEach((key) => {
      initialState[key] = {
        value: initialValues[key],
        error: '',
        touched: false,
      };
    });
    return initialState;
  });

  const [isSubmitting, setIsSubmitting] = useState(false);

  const validateField = useCallback(
    (name: string, value: any): string => {
      const rules = validationRules[name as keyof T];
      if (!rules) return '';

      if (rules.required && (!value || value.toString().trim() === '')) {
        return `${name} is required`;
      }

      if (rules.minLength && value && value.toString().length < rules.minLength) {
        return `${name} must be at least ${rules.minLength} characters`;
      }

      if (rules.maxLength && value && value.toString().length > rules.maxLength) {
        return `${name} must be no more than ${rules.maxLength} characters`;
      }

      if (rules.pattern && value && !rules.pattern.test(value.toString())) {
        return `${name} format is invalid`;
      }

      if (rules.custom) {
        const customError = rules.custom(value);
        if (customError) return customError;
      }

      return '';
    },
    [validationRules]
  );

  const setValue = useCallback(
    (name: string, value: any, shouldValidate = true) => {
      setFormState((prev) => ({
        ...prev,
        [name]: {
          ...prev[name],
          value,
          error: shouldValidate ? validateField(name, value) : prev[name].error,
          touched: true,
        },
      }));
    },
    [validateField]
  );

  const setError = useCallback((name: string, error: string) => {
    setFormState((prev) => ({
      ...prev,
      [name]: {
        ...prev[name],
        error,
      },
    }));
  }, []);

  const validateForm = useCallback(() => {
    let isValid = true;
    const newFormState = { ...formState };

    Object.keys(formState).forEach((name) => {
      const error = validateField(name, formState[name].value);
      newFormState[name] = {
        ...newFormState[name],
        error,
        touched: true,
      };
      if (error) isValid = false;
    });

    setFormState(newFormState);
    return isValid;
  }, [formState, validateField]);

  const handleSubmit = useCallback(
    async (e?: React.FormEvent) => {
      if (e) e.preventDefault();

      if (!onSubmit) return;

      const isValid = validateForm();
      if (!isValid) return;

      setIsSubmitting(true);
      try {
        const values = Object.keys(formState).reduce((acc, key) => {
          acc[key as keyof T] = formState[key].value;
          return acc;
        }, {} as T);

        await onSubmit(values);
      } catch (error) {
        console.error('Form submission error:', error);
      } finally {
        setIsSubmitting(false);
      }
    },
    [formState, onSubmit, validateForm]
  );

  const reset = useCallback(() => {
    const resetState: FormState = {};
    Object.keys(initialValues).forEach((key) => {
      resetState[key] = {
        value: initialValues[key],
        error: '',
        touched: false,
      };
    });
    setFormState(resetState);
  }, [initialValues]);

  const values = Object.keys(formState).reduce((acc, key) => {
    acc[key as keyof T] = formState[key].value;
    return acc;
  }, {} as T);

  const errors = Object.keys(formState).reduce((acc, key) => {
    acc[key] = formState[key].error;
    return acc;
  }, {} as Record<string, string>);

  const touched = Object.keys(formState).reduce((acc, key) => {
    acc[key] = formState[key].touched;
    return acc;
  }, {} as Record<string, boolean>);

  const isValid = Object.values(formState).every((field) => !field.error);
  const hasErrors = Object.values(formState).some((field) => field.error && field.touched);

  return {
    values,
    errors,
    touched,
    isValid,
    hasErrors,
    isSubmitting,
    setValue,
    setError,
    validateForm,
    handleSubmit,
    reset,
  };
}

// Common validation rules
export const validationRules = {
  required: { required: true },
  email: {
    required: true,
    pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
    custom: (value: string) => {
      if (value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
        return 'Please enter a valid email address';
      }
      return null;
    },
  },
  password: {
    required: true,
    minLength: 8,
    custom: (value: string) => {
      if (value && value.length >= 8) {
        const hasUpperCase = /[A-Z]/.test(value);
        const hasLowerCase = /[a-z]/.test(value);
        const hasNumbers = /\d/.test(value);
        const hasSpecial = /[@$!%*?&]/.test(value);

        if (!hasUpperCase || !hasLowerCase || !hasNumbers || !hasSpecial) {
          return 'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character';
        }
      }
      return null;
    },
  },
  username: {
    required: true,
    minLength: 3,
    maxLength: 50,
  },
  name: {
    required: true,
    minLength: 1,
    maxLength: 50,
  },
};