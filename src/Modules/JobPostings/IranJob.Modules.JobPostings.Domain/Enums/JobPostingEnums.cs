namespace IranJob.Modules.JobPostings.Domain.Enums;

public enum JobPostingStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3
}

public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2,
    Contract = 3,
    Internship = 4,
    Temporary = 5
}

public enum WorkArrangement
{
    OnSite = 1,
    Hybrid = 2,
    Remote = 3
}

public enum SalaryPeriod
{
    Hourly = 1,
    Monthly = 2,
    Yearly = 3
}
