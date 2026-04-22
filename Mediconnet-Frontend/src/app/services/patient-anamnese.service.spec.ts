import { describe, it, expect, beforeEach } from 'vitest';
import { firstValueFrom } from 'rxjs';
import { PatientAnamneseService } from './patient-anamnese.service';
import { createHttpClientMock } from '../../test-helpers';
import { environment } from '../../environments/environment';

describe('PatientAnamneseService', () => {
  let service: PatientAnamneseService;
  let httpMock: ReturnType<typeof createHttpClientMock>;
  const base = `${environment.apiUrl}/patient/anamnese`;

  beforeEach(() => {
    httpMock = createHttpClientMock();
    service = new PatientAnamneseService(httpMock.http);
  });

  it('getQuestionsByRdv GETs /questions-rdv/{id}', async () => {
    await firstValueFrom(service.getQuestionsByRdv(17));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/questions-rdv/17`);
  });

  it('getQuestions GETs /questions/{id}', async () => {
    await firstValueFrom(service.getQuestions(9));
    expect(httpMock.get).toHaveBeenCalledWith(`${base}/questions/9`);
  });

  it('saveReponses POSTs /reponses with payload', async () => {
    const req = { consultationId: 2, reponses: [{ questionId: 1, reponse: 'x' }] };
    await firstValueFrom(service.saveReponses(req));
    expect(httpMock.post).toHaveBeenCalledWith(`${base}/reponses`, req);
  });

  it('saveReponsesAvecQuestions POSTs consultations/{id}/questionnaire with { reponses }', async () => {
    const req = {
      consultationId: 11,
      reponses: [{ texteQuestion: 'Q', valeurReponse: 'A' }],
    };
    await firstValueFrom(service.saveReponsesAvecQuestions(req));
    expect(httpMock.post).toHaveBeenCalledWith(
      `${environment.apiUrl}/consultations/11/questionnaire`,
      { reponses: req.reponses }
    );
  });
});
