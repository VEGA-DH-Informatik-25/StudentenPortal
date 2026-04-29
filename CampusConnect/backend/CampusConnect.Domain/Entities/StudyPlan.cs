namespace CampusConnect.Domain.Entities;

public sealed record StudyPlan(
    string StudyProgram,
    string SourceUrl,
    DateTime RetrievedAt,
    IReadOnlyList<StudyPlanModule> Modules);

public sealed record StudyPlanModule(
    string Code,
    string Name,
    int? StudyYear,
    int Ects,
    bool IsRequired,
    IReadOnlyList<StudyPlanExam> Exams);

public sealed record StudyPlanExam(string Name, string Scope, bool? IsGraded);