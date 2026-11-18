namespace ISourceKO.Models.Enums;

public enum AcademicField
{
    General = 0,
    ComputerScience = 1,
    Engineering = 2,
    MedicineAndHealth = 3,
    NaturalSciences = 4,
    Mathematics = 5,
    SocialSciences = 6,
    BusinessAndEconomics = 7,
    ArtsAndHumanities = 8,
    Chemistry = 9,
    PhysicsAndAstronomy = 10,
    Psychology = 11,
    LawAndPoliticalScience = 12,
    AccountingAndFinance = 13,
    Education = 14,
    NursingAndAlliedHealth = 15,
    ArchitectureAndDesign = 16,
    CommunicationAndMedia = 17,
    HistoryAndPhilosophy = 18,
    LiteratureAndLanguages = 19,
    EnvironmentalScience = 20
}

public static class AcademicFieldExtensions
{
    public static readonly IReadOnlyList<AcademicField> AllSubjects =
        Enum.GetValues<AcademicField>()
            .Where(f => f != AcademicField.General)
            .OrderBy(f => f.GetDisplayName())
            .ToList();

    public static string GetDisplayName(this AcademicField field) => field switch
    {
        AcademicField.General => "General / Interdisciplinary",
        AcademicField.AccountingAndFinance => "Accounting & Finance",
        AcademicField.ArchitectureAndDesign => "Architecture & Design",
        AcademicField.ArtsAndHumanities => "Arts & Humanities",
        AcademicField.BusinessAndEconomics => "Business & Economics",
        AcademicField.Chemistry => "Chemistry",
        AcademicField.CommunicationAndMedia => "Communication & Media",
        AcademicField.ComputerScience => "Computer Science & IT",
        AcademicField.Education => "Education",
        AcademicField.Engineering => "Engineering",
        AcademicField.EnvironmentalScience => "Environmental Science",
        AcademicField.HistoryAndPhilosophy => "History & Philosophy",
        AcademicField.LawAndPoliticalScience => "Law & Political Science",
        AcademicField.LiteratureAndLanguages => "Literature & Languages",
        AcademicField.Mathematics => "Mathematics & Statistics",
        AcademicField.MedicineAndHealth => "Medicine & Health",
        AcademicField.NaturalSciences => "Biology & Natural Sciences",
        AcademicField.NursingAndAlliedHealth => "Nursing & Allied Health",
        AcademicField.PhysicsAndAstronomy => "Physics & Astronomy",
        AcademicField.Psychology => "Psychology",
        AcademicField.SocialSciences => "Social Sciences",
        _ => field.ToString()
    };
}
