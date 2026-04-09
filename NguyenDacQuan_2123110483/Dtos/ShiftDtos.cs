namespace CoffeeHRM.Dtos;

public sealed record ShiftResponseDto(
    int Id,
    string ShiftCode,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ShiftUpsertDto(
    string ShiftCode,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    bool IsActive);
