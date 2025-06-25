import * as signalR from '@microsoft/signalr';

export interface ProgressUpdate {
  jobId: string;
  progressPercentage: number;
  status: string;
  currentOperation?: string;
  processedRecords: number;
  totalRecords: number;
  currentFileName?: string;
  processedFiles: number;
  totalFiles: number;
  timestamp: string;
  errors?: string[];
}

export interface JobStart {
  jobId: string;
  jobType: string;
  totalFiles: number;
  estimatedTotalRecords: number;
  startTime: string;
}

export interface JobComplete {
  jobId: string;
  success: boolean;
  message: string;
  data?: any;
  completedAt: string;
  totalDuration: string;
}

export interface JobError {
  jobId: string;
  error: string;
  timestamp: string;
}

class ProgressService {
  private connection: signalR.HubConnection | null = null;
  private connectionPromise: Promise<void> | null = null;

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection() {
    const baseUrl = process.env.REACT_APP_API_URL || 'http://localhost:8080';
    const token = localStorage.getItem('authToken');
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/progressHub`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => token || '',
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting(() => {
      console.log('SignalR: Reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR: Reconnected');
    });

    this.connection.onclose((error) => {
      console.log('SignalR: Connection closed', error);
      this.connectionPromise = null;
    });
  }

  generateJobId(): string {
    return `job_${Date.now()}_${Math.random().toString(36).substring(2, 15)}`;
  }

  async refreshConnection(): Promise<void> {
    console.log('SignalR: Refreshing connection...');
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.connectionPromise = null;
    }
    this.initializeConnection();
    await this.ensureConnection();
  }

  async ensureConnection(): Promise<void> {
    const currentToken = localStorage.getItem('authToken');
    
    // If there's no token, we can't connect to an authorized hub
    if (!currentToken) {
      throw new Error('No authentication token available for SignalR connection');
    }
    
    // Reinitialize connection if it doesn't exist or if we need to refresh the token
    if (!this.connection) {
      this.initializeConnection();
    }

    if (this.connection!.state === signalR.HubConnectionState.Disconnected) {
      if (!this.connectionPromise) {
        this.connectionPromise = this.connection!.start()
          .then(() => {
            console.log('SignalR: Connected successfully');
            this.connectionPromise = null;
          })
          .catch((error) => {
            console.error('SignalR: Connection failed:', error);
            this.connectionPromise = null;
            throw error;
          });
      }
      await this.connectionPromise;
    }
  }

  async joinProgressGroup(jobId: string): Promise<void> {
    console.log(`🔗 SignalR: Attempting to join progress group for job ${jobId}`);
    try {
      await this.ensureConnection();
      console.log(`🔗 SignalR: Connection ensured, state: ${this.connection?.state}`);
      
      await this.connection!.invoke('JoinProgressGroup', jobId);
      console.log(`✅ SignalR: Successfully joined progress group for job ${jobId}`);
    } catch (error) {
      console.error('❌ SignalR: Failed to join progress group:', error);
      // If it's an authentication error, try refreshing the connection once
      if (error instanceof Error && error.message.includes('Unauthorized')) {
        console.log('🔄 SignalR: Attempting to refresh connection due to auth error');
        try {
          await this.refreshConnection();
          await this.connection!.invoke('JoinProgressGroup', jobId);
          console.log(`✅ SignalR: Successfully joined progress group after refresh for job ${jobId}`);
        } catch (refreshError) {
          console.error('❌ SignalR: Failed to join progress group even after refresh:', refreshError);
          throw refreshError;
        }
      } else {
        throw error;
      }
    }
  }

  async leaveProgressGroup(jobId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveProgressGroup', jobId);
        console.log(`SignalR: Left progress group for job ${jobId}`);
      } catch (error) {
        console.error('SignalR: Failed to leave progress group:', error);
      }
    }
  }

  onJobStarted(callback: (jobStart: JobStart) => void): void {
    this.connection?.on('JobStarted', callback);
  }

  onProgressUpdate(callback: (progress: ProgressUpdate) => void): void {
    this.connection?.on('ProgressUpdate', callback);
  }

  onJobCompleted(callback: (result: JobComplete) => void): void {
    this.connection?.on('JobCompleted', callback);
  }

  onJobError(callback: (error: JobError) => void): void {
    this.connection?.on('JobError', callback);
  }

  offJobStarted(callback: (jobStart: JobStart) => void): void {
    this.connection?.off('JobStarted', callback);
  }

  offProgressUpdate(callback: (progress: ProgressUpdate) => void): void {
    this.connection?.off('ProgressUpdate', callback);
  }

  offJobCompleted(callback: (result: JobComplete) => void): void {
    this.connection?.off('JobCompleted', callback);
  }

  offJobError(callback: (error: JobError) => void): void {
    this.connection?.off('JobError', callback);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.connectionPromise = null;
    }
  }

}

export const progressService = new ProgressService();