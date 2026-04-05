using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Terminar.Modules.Courses.Application.Queries.ExportCourses;
using Terminar.Modules.Registrations.Application.Queries.ExportCourseRoster;
using Terminar.Modules.Registrations.Application.Queries.GetCourseRoster;

namespace Terminar.Api.Services;

public interface ICsvExportService
{
    byte[] BuildCoursesOnlyCsv(
        List<ExportCourseRowDto> courses,
        Dictionary<Guid, (int Enrolled, int Waitlisted)> counts,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<ExportColumnDefinition> columnDefs);

    byte[] BuildWithParticipantsCsv(
        List<ExportCourseRowDto> courses,
        List<ExportParticipantRowDto> participants,
        IReadOnlyList<EnabledCustomFieldDto> customFields,
        Dictionary<Guid, (int Enrolled, int Waitlisted)> counts,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<ExportColumnDefinition> columnDefs);
}

public sealed class CsvExportService : ICsvExportService
{
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
    };

    public byte[] BuildCoursesOnlyCsv(
        List<ExportCourseRowDto> courses,
        Dictionary<Guid, (int Enrolled, int Waitlisted)> counts,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<ExportColumnDefinition> columnDefs)
    {
        var selectedSet = new HashSet<string>(selectedColumns);
        var headers = columnDefs
            .Where(c => selectedSet.Contains(c.Key) && c.Group == ExportColumnGroup.CourseInfo)
            .OrderBy(c => columnDefs.ToList().IndexOf(c))
            .ToList();

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, CsvConfig);

        // Write headers
        foreach (var col in headers)
            csv.WriteField(col.Label ?? col.Key);
        csv.NextRecord();

        // Write rows
        foreach (var course in courses)
        {
            counts.TryGetValue(course.CourseId, out var cnt);
            foreach (var col in headers)
            {
                csv.WriteField(GetCourseField(course, col.Key, cnt.Enrolled, cnt.Waitlisted));
            }
            csv.NextRecord();
        }

        writer.Flush();
        return ms.ToArray();
    }

    public byte[] BuildWithParticipantsCsv(
        List<ExportCourseRowDto> courses,
        List<ExportParticipantRowDto> participants,
        IReadOnlyList<EnabledCustomFieldDto> customFields,
        Dictionary<Guid, (int Enrolled, int Waitlisted)> counts,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<ExportColumnDefinition> columnDefs)
    {
        var selectedSet = new HashSet<string>(selectedColumns);
        var courseHeaders = columnDefs
            .Where(c => selectedSet.Contains(c.Key) && c.Group == ExportColumnGroup.CourseInfo)
            .ToList();
        var participantHeaders = columnDefs
            .Where(c => selectedSet.Contains(c.Key) && c.Group == ExportColumnGroup.ParticipantInfo)
            .ToList();
        var customFieldHeaders = columnDefs
            .Where(c => selectedSet.Contains(c.Key) && c.Group == ExportColumnGroup.CustomFields)
            .ToList();

        var allHeaders = courseHeaders.Concat(participantHeaders).Concat(customFieldHeaders).ToList();

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, CsvConfig);

        // Write headers
        foreach (var col in allHeaders)
            csv.WriteField(col.Label ?? col.Key);
        csv.NextRecord();

        var participantsByCourse = participants.GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var course in courses)
        {
            counts.TryGetValue(course.CourseId, out var cnt);
            var courseParticipants = participantsByCourse.GetValueOrDefault(course.CourseId) ?? [];

            if (courseParticipants.Count == 0)
            {
                // Emit one row with blank participant fields
                foreach (var col in courseHeaders)
                    csv.WriteField(GetCourseField(course, col.Key, cnt.Enrolled, cnt.Waitlisted));
                foreach (var _ in participantHeaders)
                    csv.WriteField(string.Empty);
                foreach (var _ in customFieldHeaders)
                    csv.WriteField(string.Empty);
                csv.NextRecord();
            }
            else
            {
                foreach (var p in courseParticipants)
                {
                    foreach (var col in courseHeaders)
                        csv.WriteField(GetCourseField(course, col.Key, cnt.Enrolled, cnt.Waitlisted));
                    foreach (var col in participantHeaders)
                        csv.WriteField(GetParticipantField(p, col.Key));
                    foreach (var col in customFieldHeaders)
                    {
                        // key is "cf_{fieldDefinitionId}"
                        var fieldId = Guid.TryParse(col.Key[3..], out var gid) ? gid : Guid.Empty;
                        p.CustomFieldValues.TryGetValue(fieldId, out var val);
                        csv.WriteField(val ?? string.Empty);
                    }
                    csv.NextRecord();
                }
            }
        }

        writer.Flush();
        return ms.ToArray();
    }

    private static string GetCourseField(ExportCourseRowDto course, string key, int enrolled, int waitlisted) =>
        key switch
        {
            "course_title" => course.Title,
            "course_status" => course.Status,
            "course_first_session_at" => course.FirstSessionAt?.ToString("yyyy-MM-dd") ?? string.Empty,
            "course_last_session_ends_at" => course.LastSessionEndsAt?.ToString("yyyy-MM-dd") ?? string.Empty,
            "course_location" => course.Location ?? string.Empty,
            "course_capacity" => course.Capacity.ToString(),
            "course_enrolled_count" => enrolled.ToString(),
            "course_waitlisted_count" => waitlisted.ToString(),
            "course_type" => course.CourseType,
            "course_registration_mode" => course.RegistrationMode,
            "course_description" => course.Description,
            _ => string.Empty
        };

    private static string GetParticipantField(ExportParticipantRowDto p, string key) =>
        key switch
        {
            "participant_name" => p.ParticipantName,
            "participant_email" => p.ParticipantEmail,
            "enrollment_status" => p.EnrollmentStatus,
            "enrollment_date" => p.EnrollmentDate.ToString("yyyy-MM-dd"),
            "excusal_count" => p.ExcusalCount?.ToString() ?? string.Empty,
            _ => string.Empty
        };
}
