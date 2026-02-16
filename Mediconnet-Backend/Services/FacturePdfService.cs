using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Mediconnet_Backend.Core.Entities;

namespace Mediconnet_Backend.Services;

public interface IFacturePdfService
{
    byte[] GenererFacturePdf(Facture facture);
}

public class FacturePdfService : IFacturePdfService
{
    public FacturePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenererFacturePdf(Facture facture)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, facture));
                page.Content().Element(c => ComposeContent(c, facture));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Facture facture)
    {
        container.Column(column =>
        {
            column.Spacing(10);

            // En-tête avec logo et infos hôpital
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("MEDICONNECT").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Centre Hospitalier").FontSize(12);
                    col.Item().Text("Yaoundé, Cameroun").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Tel: +237 6XX XXX XXX").FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("FACTURE ASSURANCE").Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"N° {facture.NumeroFacture}").FontSize(12);
                    col.Item().Text($"Date: {facture.DateFacture:dd/MM/yyyy}").FontSize(10);
                    col.Item().Text($"Statut: {GetStatutLabel(facture.Statut)}").FontSize(10)
                        .FontColor(GetStatutColor(facture.Statut));
                });
            });

            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Informations patient et assurance
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
                {
                    col.Item().Text("PATIENT").Bold().FontSize(11);
                    var patientNom = facture.Patient?.Utilisateur?.Nom ?? "N/A";
                    var patientPrenom = facture.Patient?.Utilisateur?.Prenom ?? "";
                    col.Item().Text($"Nom: {patientNom} {patientPrenom}");
                    col.Item().Text($"ID Patient: {facture.IdPatient}");
                    col.Item().Text($"N° Carte Assurance: {facture.Patient?.NumeroCarteAssurance ?? "N/A"}");
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
                {
                    col.Item().Text("ASSURANCE").Bold().FontSize(11);
                    col.Item().Text($"Nom: {facture.Assurance?.Nom ?? "N/A"}");
                    col.Item().Text($"Type: {facture.Assurance?.TypeAssurance ?? "N/A"}");
                    col.Item().Text($"Email: {facture.Assurance?.EmailFacturation ?? "N/A"}");
                });
            });
        });
    }

    private void ComposeContent(IContainer container, Facture facture)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Informations de la prestation
            column.Item().Text($"Type de prestation: {facture.TypeFacture?.ToUpper() ?? "N/A"}").Bold();

            // Tableau des lignes de facture
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4); // Description
                    columns.RelativeColumn(1); // Quantité
                    columns.RelativeColumn(2); // Prix unitaire
                    columns.RelativeColumn(2); // Total
                });

                // En-tête du tableau
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                        .Text("Description").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter()
                        .Text("Qté").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("Prix Unit.").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight()
                        .Text("Total").FontColor(Colors.White).Bold();
                });

                // Lignes de facture
                if (facture.Lignes != null && facture.Lignes.Any())
                {
                    foreach (var ligne in facture.Lignes)
                    {
                        var bgColor = facture.Lignes.ToList().IndexOf(ligne) % 2 == 0 
                            ? Colors.White 
                            : Colors.Grey.Lighten4;

                        table.Cell().Background(bgColor).Padding(5)
                            .Text(ligne.Description ?? "Article");
                        table.Cell().Background(bgColor).Padding(5).AlignCenter()
                            .Text(ligne.Quantite.ToString());
                        table.Cell().Background(bgColor).Padding(5).AlignRight()
                            .Text($"{ligne.PrixUnitaire:N0} FCFA");
                        table.Cell().Background(bgColor).Padding(5).AlignRight()
                            .Text($"{ligne.MontantTotal:N0} FCFA");
                    }
                }
                else
                {
                    table.Cell().ColumnSpan(4).Padding(10).AlignCenter()
                        .Text("Aucune ligne de facture").Italic();
                }
            });

            // Totaux
            column.Item().AlignRight().Width(250).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                var montantAssurance = facture.MontantAssurance ?? 0;
                var montantPatient = facture.MontantPatient ?? (facture.MontantTotal - montantAssurance);
                var tauxCouverture = facture.TauxCouverture ?? 0;

                table.Cell().Padding(5).Text("Sous-total:").Bold();
                table.Cell().Padding(5).Element(c => c.AlignRight().Text($"{facture.MontantTotal:N0} FCFA"));

                table.Cell().Padding(5).Text($"Couverture assurance ({tauxCouverture:N0}%):").Bold();
                table.Cell().Padding(5).Element(c => c.AlignRight().Text($"{montantAssurance:N0} FCFA").FontColor(Colors.Green.Darken2));

                table.Cell().Padding(5).Text("Part patient:").Bold();
                table.Cell().Padding(5).Element(c => c.AlignRight().Text($"{montantPatient:N0} FCFA"));

                table.Cell().Background(Colors.Blue.Darken2).Padding(8)
                    .Text("MONTANT DÛ PAR L'ASSURANCE:").Bold().FontColor(Colors.White);
                table.Cell().Background(Colors.Blue.Darken2).Padding(8)
                    .Element(c => c.AlignRight().Text($"{montantAssurance:N0} FCFA").Bold().FontColor(Colors.White));
            });

            // Notes
            if (!string.IsNullOrEmpty(facture.Notes))
            {
                column.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
                {
                    col.Item().Text("Notes:").Bold();
                    col.Item().Text(facture.Notes);
                });
            }

            // Informations de paiement
            column.Item().PaddingTop(20).Background(Colors.Grey.Lighten3).Padding(15).Column(col =>
            {
                col.Item().Text("INFORMATIONS DE PAIEMENT").Bold().FontSize(11);
                col.Item().PaddingTop(5).Text("Merci de procéder au règlement dans les 30 jours suivant la réception de cette facture.");
                col.Item().Text("Référence à mentionner: " + facture.NumeroFacture).Bold();
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("MediConnect - Système de Gestion Hospitalière").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
            column.Item().Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private string GetStatutLabel(string? statut) => statut?.ToLower() switch
    {
        "en_attente" => "En attente",
        "envoyee_assurance" => "Envoyée à l'assurance",
        "payee" => "Payée",
        "partiellement_payee" => "Partiellement payée",
        "annulee" => "Annulée",
        "rejetee" => "Rejetée",
        _ => statut ?? "Inconnu"
    };

    private string GetStatutColor(string? statut) => statut?.ToLower() switch
    {
        "en_attente" => Colors.Orange.Darken2,
        "envoyee_assurance" => Colors.Blue.Darken2,
        "payee" => Colors.Green.Darken2,
        "partiellement_payee" => Colors.Yellow.Darken3,
        "annulee" => Colors.Grey.Darken2,
        "rejetee" => Colors.Red.Darken2,
        _ => Colors.Grey.Darken1
    };
}
