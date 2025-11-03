import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

export enum LogLevel {
  Debug = 0,
  Info = 1,
  Warn = 2,
  Error = 3,
  None = 4
}

@Injectable({
  providedIn: 'root'
})
export class LoggerService {
  private logLevel: LogLevel = environment.production ? LogLevel.Warn : LogLevel.Debug;
  private enableConsole: boolean = true;

  constructor() {}

  /**
   * Set the minimum log level
   */
  setLogLevel(level: LogLevel): void {
    this.logLevel = level;
  }

  /**
   * Enable or disable console output
   */
  setConsoleEnabled(enabled: boolean): void {
    this.enableConsole = enabled;
  }

  /**
   * Log debug message (development only)
   */
  debug(message: string, ...optionalParams: any[]): void {
    this.log(LogLevel.Debug, message, optionalParams);
  }

  /**
   * Log info message
   */
  info(message: string, ...optionalParams: any[]): void {
    this.log(LogLevel.Info, message, optionalParams);
  }

  /**
   * Log warning message
   */
  warn(message: string, ...optionalParams: any[]): void {
    this.log(LogLevel.Warn, message, optionalParams);
  }

  /**
   * Log error message
   */
  error(message: string, error?: any, ...optionalParams: any[]): void {
    this.log(LogLevel.Error, message, error ? [error, ...optionalParams] : optionalParams);
  }

  /**
   * Internal log method
   */
  private log(level: LogLevel, message: string, params: any[]): void {
    if (level < this.logLevel || !this.enableConsole) {
      return;
    }

    const timestamp = new Date().toISOString();
    const logMessage = `[${timestamp}] ${this.getLevelName(level)}: ${message}`;

    switch (level) {
      case LogLevel.Debug:
        console.debug(logMessage, ...params);
        break;
      case LogLevel.Info:
        console.log(logMessage, ...params);
        break;
      case LogLevel.Warn:
        console.warn(logMessage, ...params);
        break;
      case LogLevel.Error:
        console.error(logMessage, ...params);
        break;
    }

    // In production, you could send logs to a remote service here
    if (environment.production && level >= LogLevel.Error) {
      this.sendToRemoteService(level, message, params);
    }
  }

  /**
   * Get human-readable log level name
   */
  private getLevelName(level: LogLevel): string {
    switch (level) {
      case LogLevel.Debug: return 'DEBUG';
      case LogLevel.Info: return 'INFO';
      case LogLevel.Warn: return 'WARN';
      case LogLevel.Error: return 'ERROR';
      default: return 'UNKNOWN';
    }
  }

  /**
   * Send logs to remote monitoring service (placeholder)
   * Implement this to send logs to your monitoring solution
   */
  private sendToRemoteService(level: LogLevel, message: string, params: any[]): void {
    // TODO: Implement remote logging service integration
    // Examples: Azure Application Insights, Sentry, LogRocket, etc.
    //
    // Example structure:
    // {
    //   timestamp: new Date().toISOString(),
    //   level: this.getLevelName(level),
    //   message: message,
    //   data: params,
    //   userAgent: navigator.userAgent,
    //   url: window.location.href
    // }
  }
}
