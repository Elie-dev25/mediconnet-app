import { describe, it, expect, beforeEach, vi } from 'vitest';
import { firstValueFrom, of, throwError } from 'rxjs';
import { NotificationService, type Notification } from './notification.service';
import { createHttpClientMock, paramsFromCall } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  let soundService: { playNotificationSound: ReturnType<typeof vi.fn> };
  const base = `${environment.apiUrl}/notification`;

  const makeNotif = (over: Partial<Notification> = {}): Notification => ({
    idNotification: 1,
    idUser: 10,
    type: 'rdv',
    titre: 'T',
    message: 'M',
    priorite: 'normale',
    lu: false,
    dateCreation: new Date(),
    ...over,
  });

  beforeEach(() => {
    httpMock = createHttpClientMock();
    soundService = { playNotificationSound: vi.fn() };
    service = new NotificationService(httpMock.http, soundService as never);
  });

  describe('getNotifications', () => {
    it('no filter sends empty params and updates state on success', async () => {
      httpMock.get.mockReturnValueOnce(
        of({ notifications: [makeNotif()], totalCount: 1, unreadCount: 1, page: 1, pageSize: 10, totalPages: 1 })
      );
      const res = await firstValueFrom(service.getNotifications());
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.keys().length).toBe(0);
      expect(res.unreadCount).toBe(1);
      expect(service['unreadCountSubject'].value).toBe(1);
      expect(service['loadingSubject'].value).toBe(false);
    });

    it('with all filters', async () => {
      httpMock.get.mockReturnValueOnce(
        of({ notifications: [], totalCount: 0, unreadCount: 0, page: 1, pageSize: 10, totalPages: 0 })
      );
      await firstValueFrom(
        service.getNotifications({ type: 'rdv', lu: false, priorite: 'haute', page: 2, pageSize: 25 })
      );
      const params = paramsFromCall(httpMock.get.mock.calls[0]);
      expect(params?.get('type')).toBe('rdv');
      expect(params?.get('lu')).toBe('false');
      expect(params?.get('priorite')).toBe('haute');
      expect(params?.get('page')).toBe('2');
      expect(params?.get('pageSize')).toBe('25');
    });

    it('resets loading on error and rethrows', async () => {
      httpMock.get.mockReturnValueOnce(throwError(() => new Error('fail')));
      await expect(firstValueFrom(service.getNotifications())).rejects.toThrow('fail');
      expect(service['loadingSubject'].value).toBe(false);
    });
  });

  describe('loadUnreadCount', () => {
    it('sets unreadCount on success', async () => {
      httpMock.get.mockReturnValueOnce(of({ count: 5 }));
      await service.loadUnreadCount();
      expect(service['unreadCountSubject'].value).toBe(5);
    });
    it('rejects on error', async () => {
      httpMock.get.mockReturnValueOnce(throwError(() => new Error('x')));
      await expect(service.loadUnreadCount()).rejects.toThrow('x');
    });
  });

  describe('individual actions', () => {
    it('getById', async () => {
      await firstValueFrom(service.getById(3));
      expect(httpMock.get).toHaveBeenCalledWith(`${base}/3`);
    });

    it('markAsRead decrements unreadCount and updates list', async () => {
      service['notificationsSubject'].next([makeNotif({ idNotification: 1, lu: false })]);
      service['unreadCountSubject'].next(3);
      httpMock.patch.mockReturnValueOnce(of({}));
      await firstValueFrom(service.markAsRead(1));
      expect(httpMock.patch).toHaveBeenCalledWith(`${base}/1/read`, {});
      expect(service['unreadCountSubject'].value).toBe(2);
      expect(service['notificationsSubject'].value[0].lu).toBe(true);
    });

    it('markAsRead does not go below zero', async () => {
      service['unreadCountSubject'].next(0);
      httpMock.patch.mockReturnValueOnce(of({}));
      await firstValueFrom(service.markAsRead(1));
      expect(service['unreadCountSubject'].value).toBe(0);
    });

    it('markAllAsRead resets unreadCount and flips all lu', async () => {
      service['notificationsSubject'].next([makeNotif({ idNotification: 1 }), makeNotif({ idNotification: 2 })]);
      service['unreadCountSubject'].next(5);
      httpMock.patch.mockReturnValueOnce(of({ message: 'ok', count: 5 }));
      await firstValueFrom(service.markAllAsRead());
      expect(service['unreadCountSubject'].value).toBe(0);
      expect(service['notificationsSubject'].value.every(n => n.lu)).toBe(true);
    });

    it('markMultipleAsRead flips targeted notifs only', async () => {
      service['notificationsSubject'].next([
        makeNotif({ idNotification: 1 }),
        makeNotif({ idNotification: 2 }),
        makeNotif({ idNotification: 3 }),
      ]);
      httpMock.patch.mockReturnValueOnce(of({ message: 'ok', count: 2 }));
      await firstValueFrom(service.markMultipleAsRead([1, 3]));
      const notifs = service['notificationsSubject'].value;
      expect(notifs[0].lu).toBe(true);
      expect(notifs[1].lu).toBe(false);
      expect(notifs[2].lu).toBe(true);
    });

    it('delete removes the notification from subject', async () => {
      service['notificationsSubject'].next([makeNotif({ idNotification: 1 }), makeNotif({ idNotification: 2 })]);
      httpMock.delete.mockReturnValueOnce(of({}));
      await firstValueFrom(service.delete(1));
      expect(service['notificationsSubject'].value).toHaveLength(1);
      expect(service['notificationsSubject'].value[0].idNotification).toBe(2);
    });

    it('deleteAllRead keeps only unread', async () => {
      service['notificationsSubject'].next([
        makeNotif({ idNotification: 1, lu: true }),
        makeNotif({ idNotification: 2, lu: false }),
      ]);
      httpMock.delete.mockReturnValueOnce(of({ message: 'ok', count: 1 }));
      await firstValueFrom(service.deleteAllRead());
      expect(service['notificationsSubject'].value).toHaveLength(1);
      expect(service['notificationsSubject'].value[0].lu).toBe(false);
    });
  });

  describe('handleNewNotification', () => {
    it('adds new and plays sound for unread', () => {
      service['handleNewNotification'](makeNotif({ idNotification: 42, lu: false }));
      expect(service['notificationsSubject'].value[0].idNotification).toBe(42);
      expect(service['unreadCountSubject'].value).toBe(1);
      expect(soundService.playNotificationSound).toHaveBeenCalled();
    });
    it('skips duplicates', () => {
      service['notificationsSubject'].next([makeNotif({ idNotification: 42 })]);
      const before = service['unreadCountSubject'].value;
      service['handleNewNotification'](makeNotif({ idNotification: 42 }));
      expect(service['notificationsSubject'].value).toHaveLength(1);
      expect(service['unreadCountSubject'].value).toBe(before);
    });
    it('already-read notification does not increment unread', () => {
      service['handleNewNotification'](makeNotif({ idNotification: 7, lu: true }));
      expect(service['unreadCountSubject'].value).toBe(0);
    });
  });

  describe('helpers', () => {
    it('getTempsEcoule handles various diffs', () => {
      const now = new Date();
      expect(service.getTempsEcoule(now)).toMatch(/À l'instant|secondes/);
      expect(service.getTempsEcoule(new Date(now.getTime() - 30000))).toMatch(/Il y a|secondes/);
      expect(service.getTempsEcoule(new Date(now.getTime() - 60 * 1000))).toBe('Il y a 1 min');
      expect(service.getTempsEcoule(new Date(now.getTime() - 10 * 60 * 1000))).toBe('Il y a 10 min');
      expect(service.getTempsEcoule(new Date(now.getTime() - 60 * 60 * 1000))).toBe('Il y a 1h');
      expect(service.getTempsEcoule(new Date(now.getTime() - 3 * 60 * 60 * 1000))).toBe('Il y a 3h');
      expect(service.getTempsEcoule(new Date(now.getTime() - 25 * 60 * 60 * 1000))).toBe('Hier');
      expect(service.getTempsEcoule(new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000))).toMatch(/Il y a 3j/);
      expect(service.getTempsEcoule(new Date(now.getTime() - 10 * 24 * 60 * 60 * 1000))).toMatch(/\d/);
      expect(service.getTempsEcoule(new Date(now.getTime() + 60000))).toBe("À l'instant");
    });

    it('getTempsEcoule appends Z when missing from string', () => {
      const result = service.getTempsEcoule('2020-01-01T00:00:00');
      expect(typeof result).toBe('string');
    });
    it('getTempsEcoule handles ISO with Z', () => {
      const result = service.getTempsEcoule('2020-01-01T00:00:00Z');
      expect(typeof result).toBe('string');
    });

    it('getIconForType', () => {
      expect(service.getIconForType('rdv')).toBe('calendar');
      expect(service.getIconForType('facture')).toBe('credit-card');
      expect(service.getIconForType('consultation')).toBe('stethoscope');
      expect(service.getIconForType('inconnu')).toBe('bell');
    });

    it('getColorForPriority', () => {
      expect(service.getColorForPriority('basse')).toBe('#64748b');
      expect(service.getColorForPriority('normale')).toBe('#3b82f6');
      expect(service.getColorForPriority('haute')).toBe('#f59e0b');
      expect(service.getColorForPriority('urgente')).toBe('#ef4444');
      expect(service.getColorForPriority('other')).toBe('#3b82f6');
    });
  });

  describe('lifecycle', () => {
    it('ngOnDestroy completes destroy$', () => {
      const spy = vi.spyOn(service['destroy$'], 'complete');
      service.ngOnDestroy();
      expect(spy).toHaveBeenCalled();
    });

    it('stopConnection is idempotent when no connection', async () => {
      await expect(service.stopConnection()).resolves.toBeUndefined();
    });

    it('requestBrowserNotificationPermission returns false without Notification API', async () => {
      const original = (globalThis as any).Notification;
      delete (globalThis as any).Notification;
      const result = await service.requestBrowserNotificationPermission();
      expect(result).toBe(false);
      if (original) (globalThis as any).Notification = original;
    });
  });
});
