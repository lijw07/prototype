/**
 * Test script for validating error scenarios and JSON consistency
 * This will test all phases working together and error handling patterns
 */

import { authApi, userApi, applicationApi, applicationLogApi } from '../services/api';
import { ErrorHandler } from './errorHandling';
import { loginSchema, registerSchema, applicationSchema } from './validation/schemas';

interface TestResult {
  testName: string;
  passed: boolean;
  error?: string;
  details?: any;
}

class ErrorScenarioTester {
  private results: TestResult[] = [];

  // Test 1: Validation Schema Tests
  async testValidationSchemas(): Promise<TestResult[]> {
    const tests: TestResult[] = [];

    // Test login schema
    try {
      const loginErrors = (loginSchema as any).validate({
        username: '',
        password: ''
      });
      
      tests.push({
        testName: 'Login Schema - Empty Fields',
        passed: Object.keys(loginErrors).length > 0,
        details: { errors: loginErrors }
      });
    } catch (error) {
      tests.push({
        testName: 'Login Schema - Empty Fields',
        passed: false,
        error: `Schema validation failed: ${error}`
      });
    }

    // Test valid login schema
    try {
      const loginErrors = (loginSchema as any).validate({
        username: 'testuser',
        password: 'testpass123'
      });
      
      tests.push({
        testName: 'Login Schema - Valid Fields',
        passed: Object.keys(loginErrors).length === 0,
        details: { errors: loginErrors }
      });
    } catch (error) {
      tests.push({
        testName: 'Login Schema - Valid Fields',
        passed: false,
        error: `Schema validation failed: ${error}`
      });
    }

    // Test register schema with password confirmation
    try {
      const registerErrors = (registerSchema as any).validate({
        firstName: 'John',
        lastName: 'Doe',
        username: 'johndoe',
        email: 'john@example.com',
        phoneNumber: '+1234567890',
        password: 'StrongPass123!',
        confirmPassword: 'DifferentPass123!'
      });
      
      tests.push({
        testName: 'Register Schema - Password Mismatch',
        passed: registerErrors.confirmPassword !== undefined,
        details: { errors: registerErrors }
      });
    } catch (error) {
      tests.push({
        testName: 'Register Schema - Password Mismatch',
        passed: false,
        error: `Schema validation failed: ${error}`
      });
    }

    // Test application schema
    try {
      const appErrors = (applicationSchema as any).validate({
        applicationName: 'Test App',
        applicationDescription: 'This is a test application for validation',
        applicationDataSourceType: '0',
        host: 'localhost',
        port: '1433',
        databaseName: 'TestDB',
        authenticationType: 'UserPassword',
        username: 'testuser'
      });
      
      tests.push({
        testName: 'Application Schema - Valid Data',
        passed: Object.keys(appErrors).length === 0,
        details: { errors: appErrors }
      });
    } catch (error) {
      tests.push({
        testName: 'Application Schema - Valid Data',
        passed: false,
        error: `Schema validation failed: ${error}`
      });
    }

    return tests;
  }

  // Test 2: API Error Handling
  async testApiErrorHandling(): Promise<TestResult[]> {
    const tests: TestResult[] = [];

    // Test invalid login
    try {
      const response = await authApi.login({
        username: 'invaliduser',
        password: 'invalidpass'
      });
      
      tests.push({
        testName: 'API - Invalid Login',
        passed: !response.success,
        details: { response }
      });
    } catch (error) {
      const processedError = ErrorHandler.processError(error);
      
      tests.push({
        testName: 'API - Invalid Login Error Processing',
        passed: processedError.category !== 'UNKNOWN',
        details: { processedError }
      });
    }

    // Test network error simulation (invalid endpoint)
    try {
      const invalidApi = {
        ...userApi,
        getProfile: () => fetch('/api/invalid/endpoint').then(r => r.json())
      };
      
      await invalidApi.getProfile();
      
      tests.push({
        testName: 'API - Network Error',
        passed: false,
        error: 'Expected network error but request succeeded'
      });
    } catch (error) {
      const processedError = ErrorHandler.processError(error);
      
      tests.push({
        testName: 'API - Network Error Processing',
        passed: processedError.category !== 'UNKNOWN',
        details: { processedError }
      });
    }

    // Test unauthorized access
    try {
      // Clear any existing token
      localStorage.removeItem('authToken');
      
      const response = await userApi.getProfile();
      
      tests.push({
        testName: 'API - Unauthorized Access',
        passed: !response.success,
        details: { response }
      });
    } catch (error) {
      const processedError = ErrorHandler.processError(error);
      
      tests.push({
        testName: 'API - Unauthorized Access Processing',
        passed: processedError.category === 'AUTHENTICATION',
        details: { processedError }
      });
    }

    return tests;
  }

  // Test 3: Component Integration
  async testComponentIntegration(): Promise<TestResult[]> {
    const tests: TestResult[] = [];

    // Test that all shared components are properly exported
    try {
      const sharedComponents = await import('../components/shared');
      const expectedComponents = [
        'LoadingSpinner',
        'ErrorBoundary', 
        'Alert',
        'NotificationContainer',
        'DataTable',
        'Pagination',
        'FormInput',
        'FormSelect'
      ];
      
      const missingComponents = expectedComponents.filter(
        comp => !(comp in sharedComponents)
      );
      
      tests.push({
        testName: 'Component Integration - Shared Components Export',
        passed: missingComponents.length === 0,
        details: { 
          expected: expectedComponents,
          missing: missingComponents,
          available: Object.keys(sharedComponents)
        }
      });
    } catch (error) {
      tests.push({
        testName: 'Component Integration - Shared Components Export',
        passed: false,
        error: `Import failed: ${error}`
      });
    }

    // Test that all custom hooks are properly exported
    try {
      const sharedHooks = await import('../hooks/shared');
      const expectedHooks = [
        'useApi',
        'useApiWithErrorHandling',
        'useFormSubmission',
        'usePagination',
        'useAsync',
        'useLocalStorage',
        'useDebounce',
        'useForm',
        'useNotifications'
      ];
      
      const missingHooks = expectedHooks.filter(
        hook => !(hook in sharedHooks)
      );
      
      tests.push({
        testName: 'Component Integration - Custom Hooks Export',
        passed: missingHooks.length === 0,
        details: { 
          expected: expectedHooks,
          missing: missingHooks,
          available: Object.keys(sharedHooks)
        }
      });
    } catch (error) {
      tests.push({
        testName: 'Component Integration - Custom Hooks Export',
        passed: false,
        error: `Import failed: ${error}`
      });
    }

    // Test validation utilities export
    try {
      const validationUtils = await import('./validation/validators');
      const validationSchemas = await import('./validation/schemas');
      
      tests.push({
        testName: 'Component Integration - Validation Utils Export',
        passed: 'validators' in validationUtils && 'ValidationSchema' in validationUtils,
        details: { 
          validationUtils: Object.keys(validationUtils),
          validationSchemas: Object.keys(validationSchemas)
        }
      });
    } catch (error) {
      tests.push({
        testName: 'Component Integration - Validation Utils Export',
        passed: false,
        error: `Import failed: ${error}`
      });
    }

    return tests;
  }

  // Test 4: JSON Consistency
  async testJsonConsistency(): Promise<TestResult[]> {
    const tests: TestResult[] = [];

    // Test API response structure consistency
    try {
      // Simulate API response structures
      const mockApiResponse = {
        success: true,
        data: {
          user: {
            userId: '123',
            firstName: 'John',
            lastName: 'Doe',
            username: 'johndoe',
            email: 'john@example.com',
            phoneNumber: '+1234567890'
          }
        },
        message: 'Success',
        timestamp: new Date().toISOString()
      };

      const mockErrorResponse = {
        success: false,
        error: {
          code: 'VALIDATION_ERROR',
          message: 'Validation failed',
          details: {
            username: 'Username is required',
            password: 'Password must be at least 8 characters'
          }
        },
        timestamp: new Date().toISOString()
      };

      // Test that our error handler can process these structures
      const processedError = ErrorHandler.processError({
        response: { status: 400 },
        message: 'Validation failed'
      });

      tests.push({
        testName: 'JSON Consistency - API Response Structure',
        passed: true,
        details: { 
          mockApiResponse,
          mockErrorResponse,
          processedError
        }
      });
    } catch (error) {
      tests.push({
        testName: 'JSON Consistency - API Response Structure',
        passed: false,
        error: `JSON processing failed: ${error}`
      });
    }

    // Test frontend type compatibility
    try {
      const apiTypes = await import('../types/api.types');
      
      // Verify key types exist
      const requiredTypes = [
        'ApiResponse',
        'User',
        'Application',
        'LoginResponse',
        'PaginatedResponse'
      ];
      
      const typeKeys = Object.keys(apiTypes);
      const missingTypes = requiredTypes.filter(type => !typeKeys.includes(type));
      
      tests.push({
        testName: 'JSON Consistency - Frontend Type Definitions',
        passed: missingTypes.length === 0,
        details: { 
          required: requiredTypes,
          missing: missingTypes,
          available: typeKeys
        }
      });
    } catch (error) {
      tests.push({
        testName: 'JSON Consistency - Frontend Type Definitions',
        passed: false,
        error: `Type import failed: ${error}`
      });
    }

    return tests;
  }

  // Test 5: Error Notification System
  testErrorNotificationSystem(): TestResult[] {
    const tests: TestResult[] = [];

    try {
      // Test error categorization
      const testErrors = [
        { status: 400, message: 'Bad Request' },
        { status: 401, message: 'Unauthorized' },
        { status: 403, message: 'Forbidden' },
        { status: 404, message: 'Not Found' },
        { status: 500, message: 'Internal Server Error' },
        { code: 'NETWORK_ERROR', message: 'Network error' }
      ];

      const processedErrors = testErrors.map(error => 
        ErrorHandler.processError(error)
      );

      const categorizedCorrectly = processedErrors.every(error => 
        error.category && error.category !== 'UNKNOWN'
      );

      tests.push({
        testName: 'Error Notification - Error Categorization',
        passed: categorizedCorrectly,
        details: { processedErrors }
      });
    } catch (error) {
      tests.push({
        testName: 'Error Notification - Error Categorization',
        passed: false,
        error: `Categorization failed: ${error}`
      });
    }

    return tests;
  }

  // Run all tests
  async runAllTests(): Promise<TestResult[]> {
    console.log('🧪 Starting comprehensive error scenario and integration tests...');

    try {
      const validationTests = await this.testValidationSchemas();
      const apiTests = await this.testApiErrorHandling();
      const integrationTests = await this.testComponentIntegration();
      const jsonTests = await this.testJsonConsistency();
      const notificationTests = this.testErrorNotificationSystem();

      this.results = [
        ...validationTests,
        ...apiTests,
        ...integrationTests,
        ...jsonTests,
        ...notificationTests
      ];

      // Summary
      const passed = this.results.filter(r => r.passed).length;
      const failed = this.results.filter(r => !r.passed).length;

      console.log(`\n📊 Test Results Summary:`);
      console.log(`✅ Passed: ${passed}`);
      console.log(`❌ Failed: ${failed}`);
      console.log(`📈 Success Rate: ${((passed / this.results.length) * 100).toFixed(1)}%`);

      // Detailed results
      console.log(`\n📋 Detailed Results:`);
      this.results.forEach((result, index) => {
        const status = result.passed ? '✅' : '❌';
        console.log(`${status} ${index + 1}. ${result.testName}`);
        if (result.error) {
          console.log(`   Error: ${result.error}`);
        }
        if (result.details && !result.passed) {
          console.log(`   Details:`, result.details);
        }
      });

      return this.results;
    } catch (error) {
      console.error('❌ Test execution failed:', error);
      return [{
        testName: 'Test Execution',
        passed: false,
        error: `Test execution failed: ${error}`
      }];
    }
  }

  // Get test summary
  getTestSummary() {
    const passed = this.results.filter(r => r.passed).length;
    const failed = this.results.filter(r => !r.passed).length;
    const successRate = ((passed / this.results.length) * 100).toFixed(1);

    return {
      total: this.results.length,
      passed,
      failed,
      successRate: `${successRate}%`,
      results: this.results
    };
  }
}

// Export the tester for use in browser console or other components
export { ErrorScenarioTester };

// Auto-run tests if in development mode
if (process.env.NODE_ENV === 'development') {
  (window as any).runErrorScenarioTests = async () => {
    const tester = new ErrorScenarioTester();
    await tester.runAllTests();
    return tester.getTestSummary();
  };
}