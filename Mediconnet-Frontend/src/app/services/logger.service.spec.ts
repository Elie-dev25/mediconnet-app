import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { LoggerService, LogLevel } from './logger.service';

describe('LoggerService', () => {
  let service: LoggerService;

  beforeEach(() => {
    service = new LoggerService();
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('debug logs when level is DEBUG', () => {
    service.setLogLevel(LogLevel.DEBUG);
    service.debug('hello', 1, 2);
    expect(console.log).toHaveBeenCalledWith('[DEBUG] hello', 1, 2);
  });

  it('debug does not log when level is INFO', () => {
    service.setLogLevel(LogLevel.INFO);
    service.debug('hello');
    expect(console.log).not.toHaveBeenCalled();
  });

  it('info logs when level <= INFO', () => {
    service.setLogLevel(LogLevel.INFO);
    service.info('hello');
    expect(console.log).toHaveBeenCalledWith('[INFO] hello');
  });

  it('info does not log when level is WARN', () => {
    service.setLogLevel(LogLevel.WARN);
    service.info('hello');
    expect(console.log).not.toHaveBeenCalled();
  });

  it('warn logs via console.warn', () => {
    service.setLogLevel(LogLevel.WARN);
    service.warn('hello');
    expect(console.warn).toHaveBeenCalledWith('[WARN] hello');
  });

  it('warn does not log when level is ERROR', () => {
    service.setLogLevel(LogLevel.ERROR);
    service.warn('hello');
    expect(console.warn).not.toHaveBeenCalled();
  });

  it('error logs via console.error', () => {
    service.setLogLevel(LogLevel.ERROR);
    service.error('oops', { a: 1 });
    expect(console.error).toHaveBeenCalledWith('[ERROR] oops', { a: 1 });
  });

  it('error does not log when level is NONE', () => {
    service.setLogLevel(LogLevel.NONE);
    service.error('oops');
    expect(console.error).not.toHaveBeenCalled();
  });

  it('setLogLevel changes level dynamically', () => {
    service.setLogLevel(LogLevel.NONE);
    service.debug('x');
    service.info('x');
    service.warn('x');
    service.error('x');
    expect(console.log).not.toHaveBeenCalled();
    expect(console.warn).not.toHaveBeenCalled();
    expect(console.error).not.toHaveBeenCalled();
  });
});
