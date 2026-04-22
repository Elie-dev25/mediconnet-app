import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { firstValueFrom, of, throwError } from 'rxjs';
import { QuestionsPredefiniesService } from './questions-predefinies.service';
import { createHttpClientMock } from '../../test-helpers';

describe('QuestionsPredefiniesService', () => {
  let service: QuestionsPredefiniesService;
  let httpMock: ReturnType<typeof createHttpClientMock>;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new QuestionsPredefiniesService(httpMock.http);
    vi.spyOn(console, 'log').mockImplementation(() => undefined);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('getQuestionsParSpecialite returns [] for invalid id (0)', async () => {
    const result = await firstValueFrom(service.getQuestionsParSpecialite(0, 'premiere'));
    expect(result).toEqual([]);
    expect(httpMock.get).not.toHaveBeenCalled();
  });

  it('getQuestionsParSpecialite loads index then questions file (premiere)', async () => {
    const indexData = { specialites: [{ id: 5, key: 'cardio', nom: 'Cardiologie' }] };
    httpMock.get.mockReturnValueOnce(of(indexData));
    httpMock.get.mockReturnValueOnce(of({ questions: [{ id: 'q1', texte: 'T?', type: 'texte', obligatoire: true }] }));

    const result = await firstValueFrom(service.getQuestionsParSpecialite(5, 'premiere'));
    expect(result).toHaveLength(1);
    expect(httpMock.get.mock.calls[0][0]).toMatch(/index\.json/);
    expect(httpMock.get.mock.calls[1][0]).toMatch(/cardio\/firstconsult\.json/);
  });

  it('getQuestionsParSpecialite uses secondconsult.json for suivante', async () => {
    const indexData = { specialites: [{ id: 5, key: 'cardio', nom: 'Cardiologie' }] };
    httpMock.get.mockReturnValueOnce(of(indexData));
    httpMock.get.mockReturnValueOnce(of({ questions: [] }));

    await firstValueFrom(service.getQuestionsParSpecialite(5, 'suivante'));
    expect(httpMock.get.mock.calls[1][0]).toMatch(/cardio\/secondconsult\.json/);
  });

  it('getQuestionsParSpecialite returns [] if specialite not in index', async () => {
    httpMock.get.mockReturnValueOnce(of({ specialites: [] }));
    const result = await firstValueFrom(service.getQuestionsParSpecialite(99, 'premiere'));
    expect(result).toEqual([]);
  });

  it('getQuestionsParSpecialite returns [] on index load error', async () => {
    httpMock.get.mockReturnValueOnce(throwError(() => new Error('boom')));
    const result = await firstValueFrom(service.getQuestionsParSpecialite(5, 'premiere'));
    expect(result).toEqual([]);
  });

  it('getQuestionsByKey returns [] for empty key', async () => {
    const result = await firstValueFrom(service.getQuestionsByKey('', 'premiere'));
    expect(result).toEqual([]);
    expect(httpMock.get).not.toHaveBeenCalled();
  });

  it('getQuestionsByKey loads the questions file directly', async () => {
    httpMock.get.mockReturnValueOnce(of({ questions: [{ id: '1', texte: 'Q', type: 'texte', obligatoire: false }] }));
    const result = await firstValueFrom(service.getQuestionsByKey('neuro', 'premiere'));
    expect(result).toHaveLength(1);
  });

  it('getSpecialitesDisponibles returns the index (cached)', async () => {
    httpMock.get.mockReturnValue(of({ specialites: [{ id: 1, key: 'a', nom: 'A' }] }));
    const a = await firstValueFrom(service.getSpecialitesDisponibles());
    const b = await firstValueFrom(service.getSpecialitesDisponibles());
    expect(a).toEqual([{ id: 1, key: 'a', nom: 'A' }]);
    expect(b).toEqual(a);
    // shareReplay ensures single HTTP call
    expect(httpMock.get).toHaveBeenCalledTimes(1);
  });

  it('getToutesQuestionsSpecialite returns [] for id 0', async () => {
    const result = await firstValueFrom(service.getToutesQuestionsSpecialite(0));
    expect(result).toEqual([]);
  });

  it('getToutesQuestionsSpecialite returns [] when specialite not found', async () => {
    httpMock.get.mockReturnValueOnce(of({ specialites: [] }));
    const result = await firstValueFrom(service.getToutesQuestionsSpecialite(42));
    expect(result).toEqual([]);
  });

  it('getToutesQuestionsSpecialite concatenates premiere + suivante', async () => {
    httpMock.get.mockReturnValueOnce(of({ specialites: [{ id: 1, key: 'k', nom: 'N' }] }));
    httpMock.get.mockReturnValueOnce(of({ questions: [{ id: 'p', texte: 'P', type: 'texte', obligatoire: true }] }));
    httpMock.get.mockReturnValueOnce(of({ questions: [{ id: 's', texte: 'S', type: 'texte', obligatoire: false }] }));
    const result = await firstValueFrom(service.getToutesQuestionsSpecialite(1));
    expect(result).toHaveLength(2);
    expect(result.map(q => q.id)).toEqual(['p', 's']);
  });
});
