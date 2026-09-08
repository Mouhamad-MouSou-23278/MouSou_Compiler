import * as signalR from '@microsoft/signalr';
import { ExecutionLog } from '../types';

class SignalRManager {
  private connection: signalR.HubConnection | null = null;
  private logListeners: Array<(log: ExecutionLog) => void> = [];

  public async connect(): Promise<signalR.HubConnection> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return this.connection;
    }

    const token = localStorage.getItem('access_token') || '';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/execution', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('LogReceived', (log: { executionId: string; stream: 'stdout' | 'stderr' | 'system'; text: string; timestamp: string }) => {
      const executionLog: ExecutionLog = {
        id: Math.random().toString(36).substring(7),
        stream: log.stream,
        text: log.text,
        timestamp: log.timestamp || new Date().toISOString(),
      };
      this.logListeners.forEach((listener) => listener(executionLog));
    });

    try {
      await this.connection.start();
      console.log('SignalR connected to /hubs/execution');
    } catch (err) {
      console.warn('SignalR connection failed (will retry on run):', err);
    }

    return this.connection;
  }

  public onLog(listener: (log: ExecutionLog) => void) {
    this.logListeners.push(listener);
    return () => {
      this.logListeners = this.logListeners.filter((l) => l !== listener);
    };
  }

  public async joinExecution(executionId: string) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinExecution', executionId);
    }
  }

  public async leaveExecution(executionId: string) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveExecution', executionId);
    }
  }

  public async sendInput(executionId: string, input: string) {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendInput', executionId, input);
    }
  }
}

export const signalRManager = new SignalRManager();
