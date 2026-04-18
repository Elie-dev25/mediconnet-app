import { Injectable } from '@angular/core';
import { ConsultationDetailDto } from './consultation-complete.service';

export interface PrintableConsultation {
  // Informations établissement
  etablissement: {
    nom: string;
    adresse: string;
    telephone: string;
    email: string;
  };
  // Informations patient
  patient: {
    nom: string;
    prenom: string;
    dateNaissance?: string;
    age?: number;
    sexe?: string;
    numeroDossier?: string;
    telephone?: string;
    adresse?: string;
  };
  // Informations médecin
  medecin: {
    nom: string;
    prenom?: string;
    specialite?: string;
    service?: string;
  };
  // Consultation
  consultation: ConsultationDetailDto;
}

@Injectable({
  providedIn: 'root'
})
export class PrintService {
  
  private readonly etablissement = {
    nom: 'MédiConnect',
    adresse: 'Centre Hospitalier Universitaire',
    telephone: '+237 6XX XXX XXX',
    email: 'contact@mediconnect.cm'
  };

  /**
   * Génère et imprime une fiche patient professionnelle
   */
  printConsultation(data: PrintableConsultation): void {
    const printContent = this.generateConsultationPrintContent(data);
    this.openPrintWindow(printContent);
  }

  /**
   * Génère et télécharge un PDF de la fiche patient
   */
  downloadConsultationPDF(data: PrintableConsultation): void {
    const printContent = this.generateConsultationPrintContent(data);
    this.downloadAsPDF(printContent, `fiche-patient-${data.patient.numeroDossier || 'consultation'}.pdf`);
  }

  /**
   * Génère le contenu HTML pour l'impression de consultation
   */
  private generateConsultationPrintContent(data: PrintableConsultation): string {
    const { patient, medecin, consultation } = data;
    const etablissement = data.etablissement || this.etablissement;
    
    return `
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Fiche Patient - ${patient.prenom} ${patient.nom}</title>
  <style>
    ${this.getPrintStyles()}
  </style>
</head>
<body>
  <div class="print-document">
    ${this.generateHeader(etablissement, patient, medecin, consultation)}
    ${this.generateBody(consultation)}
    ${this.generateFooter()}
  </div>
</body>
</html>
    `;
  }

  /**
   * Génère l'en-tête du document
   */
  private generateHeader(
    etablissement: PrintableConsultation['etablissement'],
    patient: PrintableConsultation['patient'],
    medecin: PrintableConsultation['medecin'],
    consultation: ConsultationDetailDto
  ): string {
    const dateConsultation = consultation.dateConsultation 
      ? new Date(consultation.dateConsultation).toLocaleDateString('fr-FR', {
          weekday: 'long',
          day: 'numeric',
          month: 'long',
          year: 'numeric'
        })
      : 'Non renseignée';

    const heureConsultation = consultation.dateConsultation
      ? new Date(consultation.dateConsultation).toLocaleTimeString('fr-FR', {
          hour: '2-digit',
          minute: '2-digit'
        })
      : '';

    return `
    <header class="document-header">
      <div class="header-top">
        <div class="logo-section">
          <div class="logo">
            <svg viewBox="0 0 40 40" width="50" height="50">
              <circle cx="20" cy="20" r="18" fill="#2563eb" stroke="#1d4ed8" stroke-width="2"/>
              <path d="M20 8 L20 32 M8 20 L32 20" stroke="white" stroke-width="4" stroke-linecap="round"/>
            </svg>
          </div>
          <div class="etablissement-info">
            <h1>${etablissement.nom}</h1>
            <p>${etablissement.adresse}</p>
            <p>Tél: ${etablissement.telephone} | Email: ${etablissement.email}</p>
          </div>
        </div>
        <div class="document-title">
          <h2>FICHE DE CONSULTATION</h2>
          <p class="document-date">Date: ${dateConsultation}${heureConsultation ? ' à ' + heureConsultation : ''}</p>
        </div>
      </div>
      
      <div class="header-info-grid">
        <div class="info-box patient-box">
          <h3>👤 PATIENT</h3>
          <div class="info-content">
            <p><strong>Nom:</strong> ${patient.nom}</p>
            <p><strong>Prénom:</strong> ${patient.prenom}</p>
            ${patient.age ? `<p><strong>Âge:</strong> ${patient.age} ans</p>` : ''}
            ${patient.sexe ? `<p><strong>Sexe:</strong> ${patient.sexe}</p>` : ''}
            ${patient.numeroDossier ? `<p><strong>N° Dossier:</strong> ${patient.numeroDossier}</p>` : ''}
          </div>
        </div>
        
        <div class="info-box medecin-box">
          <h3>🩺 MÉDECIN</h3>
          <div class="info-content">
            <p><strong>Dr.</strong> ${medecin.prenom || ''} ${medecin.nom}</p>
            ${medecin.specialite ? `<p><strong>Spécialité:</strong> ${medecin.specialite}</p>` : ''}
            ${medecin.service ? `<p><strong>Service:</strong> ${medecin.service}</p>` : ''}
          </div>
        </div>
      </div>
    </header>
    `;
  }

  /**
   * Génère le corps du document avec toutes les sections
   */
  private generateBody(consultation: ConsultationDetailDto): string {
    let sections = '';
    let sectionNumber = 1;

    // 1. Anamnèse
    if (consultation.motif || consultation.anamnese) {
      sections += this.generateSection(sectionNumber++, 'ANAMNÈSE', `
        ${consultation.motif ? `<div class="field"><span class="field-label">Motif de consultation:</span><p>${consultation.motif}</p></div>` : ''}
        ${consultation.anamnese ? `<div class="field"><span class="field-label">Histoire de la maladie:</span><p>${consultation.anamnese}</p></div>` : ''}
        ${this.generateQuestionnaire(consultation.questionnaire)}
      `);
    }

    // 2. Examen clinique
    const examenClinique = consultation.examenClinique;
    const parametresVitaux = examenClinique?.parametresVitaux || consultation.parametresVitaux;
    const hasExamenClinique = consultation.notesCliniques || examenClinique || parametresVitaux;
    if (hasExamenClinique) {
      sections += this.generateSection(sectionNumber++, 'EXAMEN CLINIQUE', `
        ${this.generateParametresVitaux(consultation)}
        ${this.generateExamenClinique(examenClinique)}
        ${consultation.notesCliniques ? `<div class="field"><span class="field-label">Notes cliniques:</span><p>${consultation.notesCliniques}</p></div>` : ''}
      `);
    }

    // 3. Examen gynécologique (si applicable)
    if (consultation.examenGynecologique && this.hasExamenGynecoData(consultation.examenGynecologique)) {
      sections += this.generateSection(sectionNumber++, 'EXAMEN GYNÉCOLOGIQUE', 
        this.generateExamenGynecologique(consultation.examenGynecologique)
      );
    }

    // 3bis. Examen chirurgical (si applicable)
    if (consultation.examenChirurgical && this.hasExamenChirurgicalData(consultation.examenChirurgical)) {
      sections += this.generateSection(sectionNumber++, 'EXAMEN CHIRURGICAL',
        this.generateExamenChirurgical(consultation.examenChirurgical)
      );
    }

    // 4. Diagnostic
    if (consultation.diagnostic || consultation.conclusion) {
      sections += this.generateSection(sectionNumber++, 'DIAGNOSTIC', `
        ${consultation.diagnostic ? `<div class="field diagnostic-field"><span class="field-label">Diagnostic principal:</span><p class="diagnostic-text">${consultation.diagnostic}</p></div>` : ''}
      `);
    }

    // 5. Traitement
    const hasTraitement = consultation.ordonnance?.medicaments?.length || 
                          consultation.planTraitement?.ordonnance?.medicaments?.length ||
                          consultation.examensPrescrits?.length ||
                          consultation.planTraitement?.examensPrescrits?.length;
    if (hasTraitement) {
      sections += this.generateSection(sectionNumber++, 'TRAITEMENT', `
        ${this.generateOrdonnance(consultation)}
        ${this.generateExamensPrescrits(consultation)}
        ${this.generatePlanTraitement(consultation.planTraitement)}
      `);
    }

    // 6. Recommandations / Orientation
    const hasRecommandations = consultation.recommandations || 
                               consultation.planTraitement?.orientationSpecialiste ||
                               consultation.planTraitement?.motifOrientation;
    if (hasRecommandations) {
      sections += this.generateSection(sectionNumber++, 'RECOMMANDATIONS / ORIENTATION', `
        ${consultation.recommandations ? `<div class="field"><span class="field-label">Recommandations:</span><p>${consultation.recommandations}</p></div>` : ''}
        ${consultation.planTraitement?.orientationSpecialiste ? `<div class="field"><span class="field-label">Orientation spécialiste:</span><p>${consultation.planTraitement.orientationSpecialiste}</p></div>` : ''}
        ${consultation.planTraitement?.motifOrientation ? `<div class="field"><span class="field-label">Motif d'orientation:</span><p>${consultation.planTraitement.motifOrientation}</p></div>` : ''}
      `);
    }

    // 7. Suivi
    const conclusionDetaillee = consultation.conclusionDetaillee;
    const hasSuivi = conclusionDetaillee?.typeSuivi || 
                     conclusionDetaillee?.dateSuiviPrevue || 
                     conclusionDetaillee?.notesSuivi ||
                     consultation.rdvSuivi;
    if (hasSuivi) {
      sections += this.generateSection(sectionNumber++, 'SUIVI', `
        ${conclusionDetaillee?.typeSuivi ? `<div class="field"><span class="field-label">Type de suivi:</span><p>${conclusionDetaillee.typeSuivi}</p></div>` : ''}
        ${conclusionDetaillee?.dateSuiviPrevue ? `<div class="field"><span class="field-label">Date de suivi prévue:</span><p>${new Date(conclusionDetaillee.dateSuiviPrevue).toLocaleDateString('fr-FR')}</p></div>` : ''}
        ${conclusionDetaillee?.notesSuivi ? `<div class="field"><span class="field-label">Notes de suivi:</span><p>${conclusionDetaillee.notesSuivi}</p></div>` : ''}
        ${this.generateRdvSuivi(consultation.rdvSuivi)}
      `);
    }

    // 8. Conclusion
    if (consultation.conclusion || conclusionDetaillee?.resumeConsultation) {
      sections += this.generateSection(sectionNumber++, 'CONCLUSION', `
        ${consultation.conclusion ? `<div class="field"><span class="field-label">Conclusion:</span><p>${consultation.conclusion}</p></div>` : ''}
        ${conclusionDetaillee?.resumeConsultation ? `<div class="field"><span class="field-label">Résumé:</span><p>${conclusionDetaillee.resumeConsultation}</p></div>` : ''}
        ${conclusionDetaillee?.consignesPatient ? `<div class="field"><span class="field-label">Consignes au patient:</span><p>${conclusionDetaillee.consignesPatient}</p></div>` : ''}
      `);
    }

    return `<main class="document-body">${sections}</main>`;
  }

  /**
   * Génère une section numérotée
   */
  private generateSection(number: number, title: string, content: string): string {
    return `
    <section class="print-section">
      <h3 class="section-title">
        <span class="section-number">${number}</span>
        ${title}
      </h3>
      <div class="section-content">
        ${content}
      </div>
    </section>
    `;
  }

  /**
   * Génère le questionnaire médical
   */
  private generateQuestionnaire(questionnaire?: Array<{question: string; reponse: string}>): string {
    if (!questionnaire || questionnaire.length === 0) return '';
    
    const items = questionnaire.map(q => `
      <div class="questionnaire-item">
        <span class="question">❓ ${q.question}</span>
        <span class="answer">→ ${q.reponse || 'Non répondu'}</span>
      </div>
    `).join('');

    return `
    <div class="field questionnaire-field">
      <span class="field-label">Questionnaire médical:</span>
      <div class="questionnaire-list">${items}</div>
    </div>
    `;
  }

  /**
   * Génère les paramètres vitaux
   */
  private generateParametresVitaux(consultation: ConsultationDetailDto): string {
    const params = consultation.examenClinique?.parametresVitaux || consultation.parametresVitaux;
    if (!params) return '';

    const items: string[] = [];
    if (params.poids) items.push(`<div class="vital-item"><span class="vital-label">Poids</span><span class="vital-value">${params.poids} kg</span></div>`);
    if (params.taille) items.push(`<div class="vital-item"><span class="vital-label">Taille</span><span class="vital-value">${params.taille} cm</span></div>`);
    if (params.temperature) items.push(`<div class="vital-item"><span class="vital-label">Température</span><span class="vital-value">${params.temperature} °C</span></div>`);
    if (params.tensionArterielle) items.push(`<div class="vital-item"><span class="vital-label">Tension</span><span class="vital-value">${params.tensionArterielle}</span></div>`);
    if (params.frequenceCardiaque) items.push(`<div class="vital-item"><span class="vital-label">FC</span><span class="vital-value">${params.frequenceCardiaque} bpm</span></div>`);
    if (params.frequenceRespiratoire) items.push(`<div class="vital-item"><span class="vital-label">FR</span><span class="vital-value">${params.frequenceRespiratoire} rpm</span></div>`);
    if (params.saturationOxygene) items.push(`<div class="vital-item"><span class="vital-label">SpO₂</span><span class="vital-value">${params.saturationOxygene} %</span></div>`);
    if (params.glycemie) items.push(`<div class="vital-item"><span class="vital-label">Glycémie</span><span class="vital-value">${params.glycemie} g/L</span></div>`);

    if (items.length === 0) return '';

    return `
    <div class="field vitals-field">
      <span class="field-label">Paramètres vitaux:</span>
      <div class="vitals-grid">${items.join('')}</div>
    </div>
    `;
  }

  /**
   * Génère l'examen clinique
   */
  private generateExamenClinique(examen?: any): string {
    if (!examen) return '';

    const items: string[] = [];
    if (examen.inspection) items.push(`<div class="exam-item"><span class="exam-label">Inspection:</span><p>${examen.inspection}</p></div>`);
    if (examen.palpation) items.push(`<div class="exam-item"><span class="exam-label">Palpation:</span><p>${examen.palpation}</p></div>`);
    if (examen.auscultation) items.push(`<div class="exam-item"><span class="exam-label">Auscultation:</span><p>${examen.auscultation}</p></div>`);
    if (examen.percussion) items.push(`<div class="exam-item"><span class="exam-label">Percussion:</span><p>${examen.percussion}</p></div>`);
    if (examen.autresObservations) items.push(`<div class="exam-item"><span class="exam-label">Autres observations:</span><p>${examen.autresObservations}</p></div>`);

    if (items.length === 0) return '';

    return `<div class="exam-observations">${items.join('')}</div>`;
  }

  /**
   * Vérifie si l'examen gynécologique a des données
   */
  private hasExamenGynecoData(examen: any): boolean {
    return !!(examen.inspectionExterne || examen.examenSpeculum || examen.toucherVaginal || examen.autresObservations);
  }

  /**
   * Génère l'examen gynécologique
   */
  private generateExamenGynecologique(examen: any): string {
    const items: string[] = [];
    if (examen.inspectionExterne) items.push(`<div class="exam-item"><span class="exam-label">Inspection externe:</span><p>${examen.inspectionExterne}</p></div>`);
    if (examen.examenSpeculum) items.push(`<div class="exam-item"><span class="exam-label">Examen au spéculum:</span><p>${examen.examenSpeculum}</p></div>`);
    if (examen.toucherVaginal) items.push(`<div class="exam-item"><span class="exam-label">Toucher vaginal:</span><p>${examen.toucherVaginal}</p></div>`);
    if (examen.autresObservations) items.push(`<div class="exam-item"><span class="exam-label">Autres observations:</span><p>${examen.autresObservations}</p></div>`);
    return items.join('');
  }

  /**
   * Vérifie si l'examen chirurgical a des données
   */
  private hasExamenChirurgicalData(examen: any): boolean {
    return !!(examen.zoneExaminee || examen.inspectionLocale || examen.palpationLocale || 
              examen.signesInflammatoires || examen.conclusionChirurgicale || examen.decision);
  }

  /**
   * Génère l'examen chirurgical
   */
  private generateExamenChirurgical(examen: any): string {
    const items: string[] = [];
    if (examen.zoneExaminee) items.push(`<div class="exam-item"><span class="exam-label">Zone examinée:</span><p>${examen.zoneExaminee}</p></div>`);
    if (examen.inspectionLocale) items.push(`<div class="exam-item"><span class="exam-label">Inspection locale:</span><p>${examen.inspectionLocale}</p></div>`);
    if (examen.palpationLocale) items.push(`<div class="exam-item"><span class="exam-label">Palpation locale:</span><p>${examen.palpationLocale}</p></div>`);
    if (examen.signesInflammatoires) items.push(`<div class="exam-item"><span class="exam-label">Signes inflammatoires:</span><p>${examen.signesInflammatoires}</p></div>`);
    if (examen.conclusionChirurgicale) items.push(`<div class="exam-item"><span class="exam-label">Conclusion chirurgicale:</span><p>${examen.conclusionChirurgicale}</p></div>`);
    if (examen.decision) {
      const decisionLabels: {[key: string]: string} = {
        'surveillance': 'Surveillance',
        'traitement_medical': 'Traitement médical',
        'indication_operatoire': 'Indication opératoire'
      };
      items.push(`<div class="exam-item decision-item"><span class="exam-label">Décision:</span><p class="decision-badge decision-${examen.decision}">${decisionLabels[examen.decision] || examen.decision}</p></div>`);
    }
    return items.join('');
  }

  /**
   * Génère l'ordonnance médicaments
   */
  private generateOrdonnance(consultation: ConsultationDetailDto): string {
    const ordonnance = consultation.ordonnance || consultation.planTraitement?.ordonnance;
    if (!ordonnance?.medicaments?.length) return '';

    const rows = ordonnance.medicaments.map((med, index) => `
      <tr>
        <td>${index + 1}</td>
        <td><strong>${med.nomMedicament}</strong></td>
        <td>${med.dosage || '-'}</td>
        <td>${med.frequence || '-'}</td>
        <td>${med.duree || '-'}</td>
        <td>${med.quantite || '-'}</td>
        <td>${med.instructions || '-'}</td>
      </tr>
    `).join('');

    return `
    <div class="field ordonnance-field">
      <span class="field-label">💊 Ordonnance - Médicaments:</span>
      <table class="medications-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Médicament</th>
            <th>Dosage</th>
            <th>Fréquence</th>
            <th>Durée</th>
            <th>Qté</th>
            <th>Instructions</th>
          </tr>
        </thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
    `;
  }

  /**
   * Génère les examens prescrits
   */
  private generateExamensPrescrits(consultation: ConsultationDetailDto): string {
    const examens = consultation.examensPrescrits || 
                    consultation.planTraitement?.examensPrescrits?.map(e => ({
                      nomExamen: e.nomExamen,
                      instructions: e.notes
                    }));
    
    if (!examens?.length) return '';

    const items = examens.map(exam => `
      <div class="examen-prescrit-item">
        <span class="examen-name">🔬 ${exam.nomExamen}</span>
        ${exam.instructions ? `<span class="examen-instructions">${exam.instructions}</span>` : ''}
      </div>
    `).join('');

    return `
    <div class="field examens-field">
      <span class="field-label">Examens prescrits:</span>
      <div class="examens-list">${items}</div>
    </div>
    `;
  }

  /**
   * Génère le plan de traitement
   */
  private generatePlanTraitement(plan?: any): string {
    if (!plan) return '';

    const items: string[] = [];
    if (plan.explicationDiagnostic) items.push(`<div class="plan-item"><span class="plan-label">Explication du diagnostic:</span><p>${plan.explicationDiagnostic}</p></div>`);
    if (plan.optionsTraitement) items.push(`<div class="plan-item"><span class="plan-label">Options de traitement:</span><p>${plan.optionsTraitement}</p></div>`);

    if (items.length === 0) return '';

    return `<div class="plan-details">${items.join('')}</div>`;
  }

  /**
   * Génère le RDV de suivi
   */
  private generateRdvSuivi(rdv?: any): string {
    if (!rdv) return '';

    const dateRdv = rdv.dateHeure ? new Date(rdv.dateHeure).toLocaleDateString('fr-FR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }) : 'Non définie';

    return `
    <div class="rdv-suivi-box">
      <span class="rdv-title">📅 Rendez-vous de suivi planifié</span>
      <div class="rdv-details">
        <p><strong>Date:</strong> ${dateRdv}</p>
        ${rdv.medecinNom ? `<p><strong>Médecin:</strong> ${rdv.medecinNom}</p>` : ''}
        ${rdv.serviceNom ? `<p><strong>Service:</strong> ${rdv.serviceNom}</p>` : ''}
        ${rdv.motif ? `<p><strong>Motif:</strong> ${rdv.motif}</p>` : ''}
      </div>
    </div>
    `;
  }

  /**
   * Génère le pied de page
   */
  private generateFooter(): string {
    const now = new Date();
    const dateGeneration = now.toLocaleDateString('fr-FR', {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    return `
    <footer class="document-footer">
      <div class="signature-section">
        <div class="signature-box">
          <p>Signature du médecin</p>
          <div class="signature-line"></div>
        </div>
        <div class="stamp-box">
          <p>Cachet de l'établissement</p>
          <div class="stamp-area"></div>
        </div>
      </div>
      <div class="footer-info">
        <p>Document généré le ${dateGeneration}</p>
        <p class="confidential">⚠️ Document confidentiel - Usage médical uniquement</p>
      </div>
    </footer>
    `;
  }

  /**
   * Styles CSS pour l'impression
   */
  private getPrintStyles(): string {
    return `
    * {
      margin: 0;
      padding: 0;
      box-sizing: border-box;
    }

    @page {
      size: A4;
      margin: 15mm;
    }

    body {
      font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
      font-size: 11pt;
      line-height: 1.4;
      color: #1a1a1a;
      background: white;
    }

    .print-document {
      max-width: 210mm;
      margin: 0 auto;
      padding: 0;
    }

    /* Header */
    .document-header {
      border-bottom: 3px solid #2563eb;
      padding-bottom: 15px;
      margin-bottom: 20px;
    }

    .header-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 15px;
    }

    .logo-section {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .logo svg {
      width: 50px;
      height: 50px;
    }

    .etablissement-info h1 {
      font-size: 20pt;
      color: #2563eb;
      margin-bottom: 2px;
    }

    .etablissement-info p {
      font-size: 9pt;
      color: #666;
    }

    .document-title {
      text-align: right;
    }

    .document-title h2 {
      font-size: 14pt;
      color: #1e40af;
      border: 2px solid #2563eb;
      padding: 8px 15px;
      display: inline-block;
      margin-bottom: 5px;
    }

    .document-date {
      font-size: 10pt;
      color: #666;
    }

    .header-info-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 15px;
    }

    .info-box {
      border: 1px solid #e5e7eb;
      border-radius: 6px;
      padding: 10px;
      background: #f9fafb;
    }

    .info-box h3 {
      font-size: 10pt;
      color: #374151;
      border-bottom: 1px solid #e5e7eb;
      padding-bottom: 5px;
      margin-bottom: 8px;
    }

    .info-box p {
      font-size: 10pt;
      margin: 3px 0;
    }

    /* Body */
    .document-body {
      min-height: 500px;
    }

    .print-section {
      margin-bottom: 15px;
      page-break-inside: avoid;
    }

    .section-title {
      display: flex;
      align-items: center;
      gap: 10px;
      font-size: 12pt;
      color: #1e40af;
      background: linear-gradient(90deg, #eff6ff 0%, transparent 100%);
      padding: 8px 12px;
      border-left: 4px solid #2563eb;
      margin-bottom: 10px;
    }

    .section-number {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 22px;
      height: 22px;
      background: #2563eb;
      color: white;
      border-radius: 50%;
      font-size: 10pt;
      font-weight: bold;
    }

    .section-content {
      padding: 0 10px;
    }

    .field {
      margin-bottom: 10px;
    }

    .field-label {
      display: block;
      font-weight: 600;
      color: #374151;
      font-size: 10pt;
      margin-bottom: 3px;
    }

    .field p {
      color: #1f2937;
      padding-left: 10px;
      border-left: 2px solid #e5e7eb;
    }

    /* Vitals */
    .vitals-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 8px;
      margin-top: 5px;
    }

    .vital-item {
      background: #f0f9ff;
      border: 1px solid #bae6fd;
      border-radius: 4px;
      padding: 6px 8px;
      text-align: center;
    }

    .vital-label {
      display: block;
      font-size: 8pt;
      color: #0369a1;
    }

    .vital-value {
      display: block;
      font-size: 11pt;
      font-weight: bold;
      color: #0c4a6e;
    }

    /* Exam observations */
    .exam-observations {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
    }

    .exam-item {
      background: #fafafa;
      padding: 8px;
      border-radius: 4px;
    }

    .exam-label {
      font-weight: 600;
      color: #4b5563;
      font-size: 9pt;
    }

    .exam-item p {
      margin-top: 3px;
      font-size: 10pt;
    }

    /* Questionnaire */
    .questionnaire-list {
      margin-top: 5px;
    }

    .questionnaire-item {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
      padding: 5px 0;
      border-bottom: 1px dashed #e5e7eb;
    }

    .question {
      font-size: 9pt;
      color: #6b7280;
    }

    .answer {
      font-size: 10pt;
      color: #1f2937;
    }

    /* Diagnostic */
    .diagnostic-text {
      font-size: 12pt;
      font-weight: 500;
      color: #1e40af;
      background: #eff6ff;
      padding: 10px;
      border-radius: 4px;
      border-left: 4px solid #2563eb;
    }

    /* Medications table */
    .medications-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 8px;
      font-size: 9pt;
    }

    .medications-table th,
    .medications-table td {
      border: 1px solid #d1d5db;
      padding: 6px 8px;
      text-align: left;
    }

    .medications-table th {
      background: #f3f4f6;
      font-weight: 600;
      color: #374151;
    }

    .medications-table tr:nth-child(even) {
      background: #f9fafb;
    }

    /* Examens prescrits */
    .examens-list {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
      margin-top: 5px;
    }

    .examen-prescrit-item {
      background: #fef3c7;
      border: 1px solid #fcd34d;
      border-radius: 4px;
      padding: 8px;
    }

    .examen-name {
      display: block;
      font-weight: 600;
      color: #92400e;
      font-size: 10pt;
    }

    .examen-instructions {
      display: block;
      font-size: 9pt;
      color: #78350f;
      margin-top: 3px;
    }

    /* Decision badges */
    .decision-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 4px;
      font-weight: 600;
      font-size: 10pt;
    }

    .decision-surveillance {
      background: #dcfce7;
      color: #166534;
    }

    .decision-traitement_medical {
      background: #dbeafe;
      color: #1e40af;
    }

    .decision-indication_operatoire {
      background: #fee2e2;
      color: #991b1b;
    }

    /* RDV Suivi */
    .rdv-suivi-box {
      background: #ecfdf5;
      border: 1px solid #6ee7b7;
      border-radius: 6px;
      padding: 10px;
      margin-top: 10px;
    }

    .rdv-title {
      font-weight: 600;
      color: #065f46;
      font-size: 11pt;
    }

    .rdv-details {
      margin-top: 5px;
    }

    .rdv-details p {
      font-size: 10pt;
      margin: 2px 0;
    }

    /* Footer */
    .document-footer {
      margin-top: 30px;
      padding-top: 15px;
      border-top: 2px solid #e5e7eb;
    }

    .signature-section {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 40px;
      margin-bottom: 20px;
    }

    .signature-box, .stamp-box {
      text-align: center;
    }

    .signature-box p, .stamp-box p {
      font-size: 9pt;
      color: #6b7280;
      margin-bottom: 5px;
    }

    .signature-line {
      border-bottom: 1px solid #374151;
      height: 40px;
    }

    .stamp-area {
      width: 80px;
      height: 80px;
      border: 2px dashed #d1d5db;
      border-radius: 50%;
      margin: 0 auto;
    }

    .footer-info {
      text-align: center;
      font-size: 8pt;
      color: #9ca3af;
    }

    .confidential {
      color: #dc2626;
      font-weight: 500;
      margin-top: 5px;
    }

    /* Print specific */
    @media print {
      body {
        print-color-adjust: exact;
        -webkit-print-color-adjust: exact;
      }

      .print-section {
        page-break-inside: avoid;
      }

      .document-footer {
        page-break-inside: avoid;
      }
    }
    `;
  }

  /**
   * Ouvre une fenêtre d'impression
   */
  private openPrintWindow(content: string): void {
    const printWindow = window.open('', '_blank', 'width=900,height=700');
    if (printWindow) {
      printWindow.document.open();
      printWindow.document.write(content);
      printWindow.document.close();
      
      // Utiliser setTimeout pour garantir le rendu avant impression
      setTimeout(() => {
        printWindow.focus();
        printWindow.print();
      }, 500);
    } else {
      console.error('Impossible d\'ouvrir la fenêtre d\'impression. Vérifiez les bloqueurs de popups.');
      alert('Impossible d\'ouvrir la fenêtre d\'impression. Veuillez désactiver le bloqueur de popups.');
    }
  }

  /**
   * Télécharge le contenu en PDF (utilise l'impression du navigateur)
   */
  private downloadAsPDF(content: string, filename: string): void {
    const printWindow = window.open('', '_blank', 'width=900,height=700');
    if (printWindow) {
      printWindow.document.open();
      printWindow.document.write(content);
      printWindow.document.close();
      
      setTimeout(() => {
        printWindow.focus();
        printWindow.print();
      }, 500);
    } else {
      console.error('Impossible d\'ouvrir la fenêtre d\'impression.');
      alert('Impossible d\'ouvrir la fenêtre d\'impression. Veuillez désactiver le bloqueur de popups.');
    }
  }
}
