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
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/progressHub`, {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.onreconnecting(() => {
      console.log('SignalR: Reconnecting...');
    });

    this.connection.onreconnected(() => {
      console.log('SignalR: Reconnected');
    });

    this.connection.onclose(() => {
      console.log('SignalR: Connection closed');
    });
  }

  async ensureConnection(): Promise<void> {
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
    await this.ensureConnection();
    console.log(`🔗 SignalR: Connection ensured, state: ${this.connection?.state}`);
    try {
      await this.connection!.invoke('JoinProgressGroup', jobId);
      console.log(`✅ SignalR: Successfully joined progress group for job ${jobId}`);
    } catch (error) {
      console.error('❌ SignalR: Failed to join progress group:', error);
      throw error;
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