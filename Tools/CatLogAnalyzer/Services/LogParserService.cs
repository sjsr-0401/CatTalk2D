using System.IO;
using System.Text.Json;
using CatLogAnalyzer.Models;

namespace CatLogAnalyzer.Services;

/// <summary>
/// JSON 로그 파일 파싱 서비스
/// </summary>
public class LogParserService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// 폴더 내 모든 JSON 로그 파일 목록 가져오기
    /// </summary>
    public List<FileInfo> GetLogFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return new List<FileInfo>();

        return Directory.GetFiles(folderPath, "*.json")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();
    }

    /// <summary>
    /// JSON 파일 파싱
    /// </summary>
    public LogSession? ParseLogFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            var session = JsonSerializer.Deserialize<LogSession>(json, _jsonOptions);

            if (session != null)
            {
                // 파일 이름에서 추가 정보 추출 가능
                session.SessionId = string.IsNullOrEmpty(session.SessionId)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : session.SessionId;
            }

            return session;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON 파싱 오류: {ex.Message}");
            return null;
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"파일 읽기 오류: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 여러 파일 파싱 (비동기)
    /// </summary>
    public async Task<List<LogSession>> ParseMultipleFilesAsync(IEnumerable<string> filePaths)
    {
        var results = new List<LogSession>();

        foreach (var path in filePaths)
        {
            var session = await Task.Run(() => ParseLogFile(path));
            if (session != null)
            {
                results.Add(session);
            }
        }

        return results;
    }
}
