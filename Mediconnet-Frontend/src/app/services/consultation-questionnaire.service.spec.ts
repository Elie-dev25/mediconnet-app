import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { ConsultationQuestionnaireService } from './consultation-questionnaire.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('ConsultationQuestionnaireService', () => {
  let service: ConsultationQuestionnaireService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/consultations`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new ConsultationQuestionnaireService(httpMock.http);
  });

  it('getQuestions GETs /{id}/questions', async () => {
    await firstValueFrom(service.getQuestions(42));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/42/questions`);
  });

  it('upsertReponses POSTs /{id}/reponses with request', async () => {
    const req = { reponses: [{ questionId: 1, valeurReponse: 'oui' }] };
    await firstValueFrom(service.upsertReponses(5, req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/5/reponses`, req);
  });

  it('addQuestionLibre POSTs /{id}/questions with text and type', async () => {
    const req = { texteQuestion: 'Symptômes ?', typeQuestion: 'TEXT' };
    await firstValueFrom(service.addQuestionLibre(9, req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/9/questions`, req);
  });

  it('saveReponsesAvecQuestions POSTs /{id}/questionnaire wrapping in { reponses }', async () => {
    const items = [{ texteQuestion: 'A', typeQuestion: 'TEXT', valeurReponse: 'B' }];
    await firstValueFrom(service.saveReponsesAvecQuestions(3, items));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/3/questionnaire`, { reponses: items });
  });
});
