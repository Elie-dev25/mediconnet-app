import { Injectable } from '@angular/core';

declare var Blob: any;
declare var URL: any;
declare var document: any;

export interface ExportCsvOptions {
  filename?: string;
  separator?: string;
  headers?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ExportCsvService {

  exportToCsv(data: any[], options: ExportCsvOptions = {}): void {
    const {
      filename = 'export.csv',
      separator = ',',
      headers = []
    } = options;

    if (!data || data.length === 0) {
      console.warn('Aucune donnée à exporter');
      return;
    }

    // Use provided headers or extract keys from first object
    const csvHeaders = headers.length > 0 ? headers : Object.keys(data[0]);

    // Build CSV content
    const csvContent = [
      csvHeaders.join(separator),
      ...data.map(row => 
        csvHeaders.map(header => {
          const value = row[header] ?? '';
          // Escape quotes and wrap in quotes if contains separator or quotes
          const stringValue = String(value);
          if (stringValue.includes(separator) || stringValue.includes('"') || stringValue.includes('\n')) {
            return `"${stringValue.replaceAll('"', '""')}"`;
          }
          return stringValue;
        }).join(separator)
      )
    ].join('\n');

    // Create blob and download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', filename);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } else {
      console.error('Votre navigateur ne supporte pas le téléchargement de fichiers');
    }
  }

  exportHistoriqueMouvements(data: any[]): void {
    this.exportToCsv(data, {
      filename: `historique-mouvements-${this.getDateString()}.csv`,
      headers: ['id', 'dateMouvement', 'typeMouvement', 'quantite', 'motif', 'medicamentNom', 'utilisateurNom']
    });
  }

  exportHistoriqueCommandes(data: any[]): void {
    this.exportToCsv(data, {
      filename: `historique-commandes-${this.getDateString()}.csv`,
      headers: ['id', 'dateCommande', 'statut', 'fournisseurNom', 'medicamentsCount', 'utilisateurNom']
    });
  }

  exportHistoriqueDispensations(data: any[]): void {
    this.exportToCsv(data, {
      filename: `historique-dispensations-${this.getDateString()}.csv`,
      headers: ['id', 'dateDispensation', 'ordonnanceId', 'medicamentsCount', 'patientNom', 'utilisateurNom']
    });
  }

  exportHistoriqueConsolide(data: any[]): void {
    this.exportToCsv(data, {
      filename: `historique-consolide-${this.getDateString()}.csv`,
      headers: ['id', 'date', 'type', 'sousType', 'description', 'statut', 'utilisateurNom', 'details']
    });
  }

  private getDateString(): string {
    const now = new Date();
    return now.toISOString().split('T')[0].replaceAll('-', '');
  }
}
