/**
 * Structured Logger Service
 *
 * Provides JSON-formatted logs compatible with Azure Log Analytics.
 * Features:
 * - Structured JSON output in production
 * - Pretty-printed output in development
 * - Log level filtering via LOG_LEVEL env var
 * - Correlation ID propagation
 * - Request duration tracking
 * - child() method for request-scoped loggers
 */

export type LogLevel = "debug" | "info" | "warn" | "error";

export interface LogContext {
  correlationId?: string;
  method?: string;
  path?: string;
  status?: number;
  durationMs?: number;
  userId?: string;
  [key: string]: unknown;
}

interface LogEntry {
  timestamp: string;
  level: LogLevel;
  message: string;
  [key: string]: unknown;
}

const LOG_LEVELS: Record<LogLevel, number> = {
  debug: 0,
  info: 1,
  warn: 2,
  error: 3,
};

function getMinLevel(): LogLevel {
  const envLevel = (process.env.LOG_LEVEL || "info").toLowerCase();
  if (envLevel in LOG_LEVELS) {
    return envLevel as LogLevel;
  }
  return "info";
}

function shouldLog(level: LogLevel): boolean {
  return LOG_LEVELS[level] >= LOG_LEVELS[getMinLevel()];
}

function isStructuredMode(): boolean {
  const env = process.env.STRUCTURED_LOGGING;
  if (env === undefined) {
    // Default: structured in production, pretty in development
    return process.env.BLACK_GATE_ENV === "prod";
  }
  return env.toLowerCase() === "true";
}

function formatPretty(entry: LogEntry): string {
  const { timestamp, level, message, ...context } = entry;
  const levelIcon = {
    debug: "🐛",
    info: "📋",
    warn: "⚠️",
    error: "❌",
  }[level];

  const time = new Date(timestamp).toLocaleTimeString();
  const contextStr =
    Object.keys(context).length > 0
      ? ` ${JSON.stringify(context)}`
      : "";

  return `${levelIcon} [${time}] ${message}${contextStr}`;
}

function writeLog(level: LogLevel, message: string, context: LogContext): void {
  if (!shouldLog(level)) {
    return;
  }

  const entry: LogEntry = {
    timestamp: new Date().toISOString(),
    level,
    message,
    ...context,
  };

  const output = isStructuredMode()
    ? JSON.stringify(entry)
    : formatPretty(entry);

  switch (level) {
    case "error":
      console.error(output);
      break;
    case "warn":
      console.warn(output);
      break;
    default:
      console.log(output);
  }
}

export interface Logger {
  debug(message: string, context?: LogContext): void;
  info(message: string, context?: LogContext): void;
  warn(message: string, context?: LogContext): void;
  error(message: string, context?: LogContext): void;
  child(defaultContext: LogContext): Logger;
}

function createLogger(defaultContext: LogContext = {}): Logger {
  return {
    debug(message: string, context?: LogContext): void {
      writeLog("debug", message, { ...defaultContext, ...context });
    },
    info(message: string, context?: LogContext): void {
      writeLog("info", message, { ...defaultContext, ...context });
    },
    warn(message: string, context?: LogContext): void {
      writeLog("warn", message, { ...defaultContext, ...context });
    },
    error(message: string, context?: LogContext): void {
      writeLog("error", message, { ...defaultContext, ...context });
    },
    child(childContext: LogContext): Logger {
      return createLogger({ ...defaultContext, ...childContext });
    },
  };
}

/** Root logger instance */
export const logger = createLogger();
