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
    EnvironmentalScience = 20,
    // Expanded Disciplines for v2.0.2
    DataScienceAndAI = 21,
    Cybersecurity = 22,
    CivilAndEnvironmentalEngineering = 23,
    ElectricalAndElectronicsEngineering = 24,
    MechanicalAndAerospaceEngineering = 25,
    ChemicalAndMaterialsEngineering = 26,
    PublicHealthAndEpidemiology = 27,
    PharmacyAndPharmacology = 28,
    Biotechnology = 29,
    EconomicsAndEconometrics = 30,
    BusinessAdministrationAndMarketing = 31,
    SociologyAndAnthropology = 32,
    PoliticalScienceAndPublicAdmin = 33,
    FineArtsAndMusic = 34,
    PhilippineAndSoutheastAsianStudies = 35,
    PhilosophyAndEthics = 36,
    HospitalityAndTourism = 37
}

public static class AcademicFieldExtensions
{
    public static readonly IReadOnlyList<AcademicField> AllSubjects =
        Enum.GetValues<AcademicField>()
            .Where(f => f != AcademicField.General)
            .OrderBy(f => f.GetDisplayName())
            .ToList();

    public static string GetFacultyGroup(this AcademicField field) => field switch
    {
        AcademicField.ComputerScience or 
        AcademicField.DataScienceAndAI or 
        AcademicField.Cybersecurity or 
        AcademicField.Mathematics => "Computing, Data & Mathematics",

        AcademicField.Engineering or 
        AcademicField.CivilAndEnvironmentalEngineering or 
        AcademicField.ElectricalAndElectronicsEngineering or 
        AcademicField.MechanicalAndAerospaceEngineering or 
        AcademicField.ChemicalAndMaterialsEngineering or 
        AcademicField.PhysicsAndAstronomy or 
        AcademicField.Chemistry => "Engineering & Physical Sciences",

        AcademicField.MedicineAndHealth or 
        AcademicField.NursingAndAlliedHealth or 
        AcademicField.PublicHealthAndEpidemiology or 
        AcademicField.PharmacyAndPharmacology or 
        AcademicField.NaturalSciences or 
        AcademicField.Biotechnology or 
        AcademicField.EnvironmentalScience => "Health, Medical & Life Sciences",

        AcademicField.AccountingAndFinance or 
        AcademicField.BusinessAndEconomics or 
        AcademicField.EconomicsAndEconometrics or 
        AcademicField.BusinessAdministrationAndMarketing or 
        AcademicField.HospitalityAndTourism => "Business, Finance & Economics",

        AcademicField.Psychology or 
        AcademicField.SocialSciences or 
        AcademicField.SociologyAndAnthropology or 
        AcademicField.PoliticalScienceAndPublicAdmin or 
        AcademicField.LawAndPoliticalScience or 
        AcademicField.Education or 
        AcademicField.CommunicationAndMedia => "Social Sciences, Law & Education",

        AcademicField.ArtsAndHumanities or 
        AcademicField.ArchitectureAndDesign or 
        AcademicField.HistoryAndPhilosophy or 
        AcademicField.PhilosophyAndEthics or 
        AcademicField.LiteratureAndLanguages or 
        AcademicField.FineArtsAndMusic or 
        AcademicField.PhilippineAndSoutheastAsianStudies => "Humanities, Arts & Regional Studies",

        _ => "General & Interdisciplinary"
    };

    public static string GetDisplayName(this AcademicField field) => field switch
    {
        AcademicField.General => "General / Interdisciplinary",
        AcademicField.AccountingAndFinance => "Accounting & Finance",
        AcademicField.ArchitectureAndDesign => "Architecture & Design",
        AcademicField.ArtsAndHumanities => "Arts & Humanities",
        AcademicField.Biotechnology => "Biotechnology & Genetic Engineering",
        AcademicField.BusinessAdministrationAndMarketing => "Business Administration & Marketing",
        AcademicField.BusinessAndEconomics => "Business & Economics",
        AcademicField.ChemicalAndMaterialsEngineering => "Chemical & Materials Engineering",
        AcademicField.Chemistry => "Chemistry",
        AcademicField.CivilAndEnvironmentalEngineering => "Civil & Environmental Engineering",
        AcademicField.CommunicationAndMedia => "Communication & Media Studies",
        AcademicField.ComputerScience => "Computer Science & IT",
        AcademicField.Cybersecurity => "Cybersecurity & Network Systems",
        AcademicField.DataScienceAndAI => "Data Science & Artificial Intelligence",
        AcademicField.EconomicsAndEconometrics => "Economics & Econometrics",
        AcademicField.Education => "Education & Pedagogy",
        AcademicField.ElectricalAndElectronicsEngineering => "Electrical & Electronics Engineering",
        AcademicField.Engineering => "General Engineering",
        AcademicField.EnvironmentalScience => "Environmental Science & Sustainability",
        AcademicField.FineArtsAndMusic => "Fine Arts, Music & Performing Arts",
        AcademicField.HistoryAndPhilosophy => "History & Philosophy",
        AcademicField.HospitalityAndTourism => "Hospitality & Tourism Management",
        AcademicField.LawAndPoliticalScience => "Law & Legal Studies",
        AcademicField.LiteratureAndLanguages => "Literature & Languages",
        AcademicField.Mathematics => "Mathematics & Statistics",
        AcademicField.MechanicalAndAerospaceEngineering => "Mechanical & Aerospace Engineering",
        AcademicField.MedicineAndHealth => "Medicine & Clinical Specialties",
        AcademicField.NaturalSciences => "Biology & Life Sciences",
        AcademicField.NursingAndAlliedHealth => "Nursing & Allied Health",
        AcademicField.PharmacyAndPharmacology => "Pharmacy & Pharmacology",
        AcademicField.PhilippineAndSoutheastAsianStudies => "Philippine & Southeast Asian Studies",
        AcademicField.PhilosophyAndEthics => "Philosophy, Ethics & Theology",
        AcademicField.PhysicsAndAstronomy => "Physics & Astronomy",
        AcademicField.PoliticalScienceAndPublicAdmin => "Political Science & Public Admin",
        AcademicField.Psychology => "Psychology & Cognitive Science",
        AcademicField.PublicHealthAndEpidemiology => "Public Health & Epidemiology",
        AcademicField.SocialSciences => "Social Sciences (General)",
        AcademicField.SociologyAndAnthropology => "Sociology & Anthropology",
        _ => field.ToString()
    };
}
