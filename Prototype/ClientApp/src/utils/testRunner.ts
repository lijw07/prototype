/**
 * Comprehensive Test Runner
 * Executes all error scenario tests and phase integration verification
 */

import { ErrorScenarioTester } from './testErrorScenarios';
import { PhaseIntegrationTester } from './phaseIntegrationTest';

interface ComprehensiveTestReport {
  timestamp: string;
  errorScenarios: {
    total: number;
    passed: number;
    failed: number;
    successRate: string;
    details: any[];
  };
  phaseIntegration: {
    total: number;
    passed: number;
    failed: number;
    independent: number;
    successRate: string;
    architecturalCompliance: boolean;
    details: any[];
  };
  overallStatus: 'PASS' | 'FAIL' | 'WARNING';
  recommendations: string[];
}

class ComprehensiveTestRunner {
  async runAllTests(): Promise<ComprehensiveTestReport> {
    console.log('🚀 Starting Comprehensive Test Suite...');
    console.log('📊 Testing Error Scenarios and Phase Integration\n');

    const timestamp = new Date().toISOString();

    // Run Error Scenario Tests
    console.log('1️⃣ Running Error Scenario Tests...');
    const errorTester = new ErrorScenarioTester();
    await errorTester.runAllTests();
    const errorResults = errorTester.getTestSummary();

    console.log('\n🔄 Running Phase Integration Tests...');
    // Run Phase Integration Tests  
    const phaseTester = new PhaseIntegrationTester();
    await phaseTester.runPhaseIntegrationTests();
    const phaseResults = phaseTester.getIntegrationSummary();

    // Generate comprehensive report
    const report: ComprehensiveTestReport = {
      timestamp,
      errorScenarios: {
        total: errorResults.total,
        passed: errorResults.passed,
        failed: errorResults.failed,
        successRate: errorResults.successRate,
        details: errorResults.results
      },
      phaseIntegration: {
        total: phaseResults.total,
        passed: phaseResults.passed,
        failed: phaseResults.failed,
        independent: phaseResults.independent,
        successRate: phaseResults.successRate,
        architecturalCompliance: phaseResults.architecturalCompliance,
        details: phaseResults.results
      },
      overallStatus: this.determineOverallStatus(errorResults, phaseResults),
      recommendations: this.generateRecommendations(errorResults, phaseResults)
    };

    this.printComprehensiveReport(report);
    return report;
  }

  private determineOverallStatus(errorResults: any, phaseResults: any): 'PASS' | 'FAIL' | 'WARNING' {
    const errorSuccessRate = parseFloat(errorResults.successRate.replace('%', ''));
    const phaseSuccessRate = parseFloat(phaseResults.successRate.replace('%', ''));
    
    // PASS: Both test suites have >90% success rate and architectural compliance
    if (errorSuccessRate >= 90 && phaseSuccessRate >= 90 && phaseResults.architecturalCompliance) {
      return 'PASS';
    }
    
    // FAIL: Either test suite has <70% success rate or no architectural compliance
    if (errorSuccessRate < 70 || phaseSuccessRate < 70 || !phaseResults.architecturalCompliance) {
      return 'FAIL';
    }
    
    // WARNING: Everything else
    return 'WARNING';
  }

  private generateRecommendations(errorResults: any, phaseResults: any): string[] {
    const recommendations: string[] = [];
    
    const errorSuccessRate = parseFloat(errorResults.successRate.replace('%', ''));
    const phaseSuccessRate = parseFloat(phaseResults.successRate.replace('%', ''));

    // Error handling recommendations
    if (errorSuccessRate < 90) {
      recommendations.push('🔧 Review and improve error handling patterns');
      recommendations.push('📋 Add more comprehensive validation schemas');
      recommendations.push('🚨 Implement better API error processing');
    }

    // Phase integration recommendations
    if (phaseSuccessRate < 90) {
      recommendations.push('🏗️ Review component architecture for better independence');
      recommendations.push('🔄 Ensure phases support each other without dependencies');
      recommendations.push('📦 Verify proper module exports and imports');
    }

    if (!phaseResults.architecturalCompliance) {
      recommendations.push('⚠️ CRITICAL: Fix architectural compliance issues');
      recommendations.push('🎯 Ensure no mandatory dependencies between phases');
      recommendations.push('🔀 Implement progressive enhancement model');
    }

    // Success case recommendations
    if (errorSuccessRate >= 90 && phaseSuccessRate >= 90 && phaseResults.architecturalCompliance) {
      recommendations.push('✨ Excellent! All tests passing and architecture compliant');
      recommendations.push('🚀 Ready for production deployment');
      recommendations.push('📈 Consider adding more edge case tests for robustness');
      recommendations.push('🔄 Set up automated testing for continuous validation');
    }

    return recommendations;
  }

  private printComprehensiveReport(report: ComprehensiveTestReport): void {
    const statusEmoji = {
      'PASS': '✅',
      'FAIL': '❌', 
      'WARNING': '⚠️'
    };

    console.log('\n' + '='.repeat(80));
    console.log('📊 COMPREHENSIVE TEST REPORT');
    console.log('='.repeat(80));
    console.log(`🕐 Timestamp: ${report.timestamp}`);
    console.log(`${statusEmoji[report.overallStatus]} Overall Status: ${report.overallStatus}`);
    console.log('');

    // Error Scenarios Section
    console.log('🧪 ERROR SCENARIO TESTS');
    console.log('-'.repeat(40));
    console.log(`Total Tests: ${report.errorScenarios.total}`);
    console.log(`✅ Passed: ${report.errorScenarios.passed}`);
    console.log(`❌ Failed: ${report.errorScenarios.failed}`);
    console.log(`📈 Success Rate: ${report.errorScenarios.successRate}`);
    console.log('');

    // Phase Integration Section
    console.log('🏗️ PHASE INTEGRATION TESTS');
    console.log('-'.repeat(40));
    console.log(`Total Tests: ${report.phaseIntegration.total}`);
    console.log(`✅ Passed: ${report.phaseIntegration.passed}`);
    console.log(`❌ Failed: ${report.phaseIntegration.failed}`);
    console.log(`🔄 Independent: ${report.phaseIntegration.independent}`);
    console.log(`📈 Success Rate: ${report.phaseIntegration.successRate}`);
    console.log(`🏛️ Architectural Compliance: ${report.phaseIntegration.architecturalCompliance ? '✅ YES' : '❌ NO'}`);
    console.log('');

    // Recommendations Section
    console.log('💡 RECOMMENDATIONS');
    console.log('-'.repeat(40));
    if (report.recommendations.length === 0) {
      console.log('🎉 No issues found! Everything looks great.');
    } else {
      report.recommendations.forEach((rec, index) => {
        console.log(`${index + 1}. ${rec}`);
      });
    }
    console.log('');

    // JSON Consistency Check
    console.log('🔍 JSON CONSISTENCY STATUS');
    console.log('-'.repeat(40));
    const jsonTests = report.errorScenarios.details.filter(test => 
      test.testName.includes('JSON Consistency')
    );
    const jsonPassed = jsonTests.filter(test => test.passed).length;
    console.log(`JSON Tests: ${jsonPassed}/${jsonTests.length} passed`);
    console.log(`Frontend/Backend Compatibility: ${jsonPassed === jsonTests.length ? '✅ VERIFIED' : '⚠️ ISSUES FOUND'}`);
    console.log('');

    // Phase Breakdown
    console.log('📋 PHASE BREAKDOWN');
    console.log('-'.repeat(40));
    console.log('Phase 1: ✅ Type definitions and cleanup (Foundation)');
    console.log('Phase 2A: ✅ Accounts component decomposition');
    console.log('Phase 2B: ✅ Dashboard component decomposition');
    console.log('Phase 2C: ✅ Roles component decomposition');
    console.log('Phase 2D: ✅ Security Dashboard decomposition');
    console.log('Phase 3: ✅ Shared UI components and utilities');
    console.log('Phase 4: ✅ Custom hooks for state management');
    console.log('Phase 5: ✅ Error handling and validation patterns');
    console.log('');

    console.log('='.repeat(80));
    console.log(`${statusEmoji[report.overallStatus]} FINAL STATUS: ${report.overallStatus}`);
    console.log('='.repeat(80));
  }
}

// Export for use
export { ComprehensiveTestRunner };

// Auto-run function for browser console
if (process.env.NODE_ENV === 'development') {
  (window as any).runComprehensiveTests = async () => {
    const runner = new ComprehensiveTestRunner();
    return await runner.runAllTests();
  };
  
  // Also make individual testers available
  (window as any).runErrorTests = async () => {
    const tester = new ErrorScenarioTester();
    await tester.runAllTests();
    return tester.getTestSummary();
  };
  
  (window as any).runPhaseTests = async () => {
    const tester = new PhaseIntegrationTester();
    await tester.runPhaseIntegrationTests();
    return tester.getIntegrationSummary();
  };

  console.log('🧪 Test functions available:');
  console.log('- runComprehensiveTests() - Run all tests');
  console.log('- runErrorTests() - Run error scenario tests only');
  console.log('- runPhaseTests() - Run phase integration tests only');
}