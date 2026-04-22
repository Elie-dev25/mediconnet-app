import { Injectable } from '@angular/core';

declare global {
  interface Window { jsPDF: any; }
}

export interface ExportPdfOptions {
  filename?: string;
  title?: string;
  headers?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ExportPdfService {

  exportToPdf(data: any[], options: ExportPdfOptions = {}): void {
    const {
      filename = 'export.pdf',
      title = 'Export',
      headers = []
    } = options;

    if (!data || Array.from(data).length === 0) {
      console.warn('Aucune donnée à exporter');
      return;
    }

    // Vérifier si jsPDF est disponible
    if (typeof window.jsPDF === 'undefined') {
      console.error('jsPDF n\'est pas disponible. Veuillez inclure la bibliothèque jsPDF.');
      return;
    }

    // Créer le document PDF
    const doc = new window.jsPDF();
    
    // Ajouter le titre
    doc.setFontSize(16);
    doc.text(title, 14, 20);
    
    // Ajouter la date
    doc.setFontSize(10);
    doc.text(`Généré le: ${new Date().toLocaleDateString('fr-FR')}`, 14, 30);
    
    // Préparer les données
    const pdfHeaders = Array.from(headers).length > 0 ? headers : Object.keys(Array.from(data)[0]);
    const rows = Array.from(data).map((item: any) => 
      pdfHeaders.map((header: any) => String(item[header] ?? ''))
    );
    
    // Créer le tableau
    let yPosition = 40;
    const lineHeight = 10;
    const pageHeight = doc.internal.pageSize.height;
    const margin = 14;
    const tableWidth = doc.internal.pageSize.width - 2 * margin;
    
    // En-têtes du tableau
    doc.setFontSize(12);
    doc.setFont(undefined, 'bold');
    let xPosition = margin;
    
    pdfHeaders.forEach((header: any, index: any) => {
      const cellWidth = tableWidth / pdfHeaders.length;
      doc.text(header, xPosition, yPosition);
      xPosition += cellWidth;
    });
    
    yPosition += lineHeight;
    
    // Données du tableau
    doc.setFontSize(10);
    doc.setFont(undefined, 'normal');
    
    rows.forEach((row: any) => {
      // Vérifier si on a besoin d'une nouvelle page
      if (yPosition > pageHeight - margin) {
        doc.addPage();
        yPosition = margin;
      }
      
      xPosition = margin;
      row.forEach((cell: any) => {
        const cellWidth = tableWidth / pdfHeaders.length;
        // Tronquer le texte si trop long
        const truncatedText = cell.length > 15 ? cell.substring(0, 15) + '...' : cell;
        doc.text(truncatedText, xPosition, yPosition);
        xPosition += cellWidth;
      });
      
      yPosition += lineHeight;
    });
    
    // Sauvegarder le PDF
    doc.save(filename);
  }

  exportHistoriqueMouvements(data: any[]): void {
    this.exportToPdf(data, {
      filename: `historique-mouvements-${this.getDateString()}.pdf`,
      title: 'Historique des Mouvements',
      headers: ['ID', 'Date', 'Type', 'Quantité', 'Motif', 'Médicament', 'Utilisateur']
    });
  }

  exportHistoriqueCommandes(data: any[]): void {
    this.exportToPdf(data, {
      filename: `historique-commandes-${this.getDateString()}.pdf`,
      title: 'Historique des Commandes',
      headers: ['ID', 'Date', 'Statut', 'Fournisseur', 'Nb Médicaments', 'Utilisateur']
    });
  }

  exportHistoriqueDispensations(data: any[]): void {
    this.exportToPdf(data, {
      filename: `historique-dispensations-${this.getDateString()}.pdf`,
      title: 'Historique des Dispensations',
      headers: ['ID', 'Date', 'Ordonnance', 'Nb Médicaments', 'Patient', 'Utilisateur']
    });
  }

  exportHistoriqueConsolide(data: any[]): void {
    this.exportToPdf(data, {
      filename: `historique-consolide-${this.getDateString()}.pdf`,
      title: 'Historique Consolidé',
      headers: ['ID', 'Date', 'Type', 'Sous-type', 'Description', 'Statut', 'Utilisateur']
    });
  }

  private getDateString(): string {
    const now = new Date();
    return now.toISOString().split('T')[0].replaceAll('-', '');
  }
}
