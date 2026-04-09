namespace CoffeeHRM.Models;

public enum GenderType
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum AttendanceStatus
{
    Pending = 1,
    Present = 2,
    Late = 3,
    EarlyLeave = 4,
    Overtime = 5,
    Absent = 6
}

public enum PayrollStatus
{
    Draft = 1,
    Generated = 2,
    Approved = 3,
    Paid = 4,
    Cancelled = 5
}

public enum PayrollDetailType
{
    Allowance = 1,
    Bonus = 2,
    Penalty = 3,
    Overtime = 4,
    Insurance = 5,
    Tax = 6
}

public enum ContractType
{
    FullTime = 1,
    PartTime = 2,
    Probation = 3,
    Intern = 4
}

public enum RecruitmentStatus
{
    Draft = 1,
    Open = 2,
    InProgress = 3,
    Closed = 4,
    Cancelled = 5
}

public enum CandidateStatus
{
    Applied = 1,
    Screening = 2,
    Interviewing = 3,
    Offered = 4,
    Hired = 5,
    Rejected = 6,
    Withdrawn = 7
}

public enum EmployeeTrainingStatus
{
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum LeaveRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum ShiftSwapStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}

public enum AttendanceAdjustmentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
