namespace Mediconnet_Backend.DTOs.Consultation;

public class QuestionDto
{
    public int Id { get; set; }
    public string TexteQuestion { get; set; } = string.Empty;
    public string TypeQuestion { get; set; } = "texte";
    public bool EstPredefinie { get; set; }
}

public class ConsultationQuestionDto
{
    public int QuestionId { get; set; }
    public int OrdreAffichage { get; set; }
    public string TexteQuestion { get; set; } = string.Empty;
    public string TypeQuestion { get; set; } = "texte";
    public bool EstPredefinie { get; set; }

    public string? ValeurReponse { get; set; }
    public string? RempliPar { get; set; }
    public DateTime? DateSaisie { get; set; }
}

public class UpsertReponsesRequest
{
    public List<UpsertReponseItem> Reponses { get; set; } = new();
}

public class UpsertReponseItem
{
    public int QuestionId { get; set; }
    public string? ValeurReponse { get; set; }
}

public class AddQuestionLibreRequest
{
    public string TexteQuestion { get; set; } = string.Empty;
    public string TypeQuestion { get; set; } = "texte";
}

public class SaveQuestionnaireRequest
{
    public List<SaveReponseAvecQuestionItem> Reponses { get; set; } = new();
}

public class SaveReponseAvecQuestionItem
{
    public string TexteQuestion { get; set; } = string.Empty;
    public string? TypeQuestion { get; set; }
    public string? ValeurReponse { get; set; }
    public int? QuestionIdDb { get; set; }
}
