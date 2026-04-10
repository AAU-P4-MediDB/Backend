namespace Backend.Models
{
    public static class ErrorCodes
    {
        // 0 — Success
        public const string Success = "0";

        // 1.YY — Application / system error
        public static class App
        {
            public const string CommunicationLost      = "1.01";
            public const string BackendError           = "1.02";
            public const string DatabaseError          = "1.03";
            public const string PatientAlreadyExists   = "1.04";
            public const string PatientNotFound        = "1.05";
            public const string JournalNotFound        = "1.06";
            public const string DatabaseTimeout        = "1.07";
            public const string DatabaseWriteFailure   = "1.08";
            public const string DatabaseReadFailure    = "1.09";
            public const string ServiceUnavailable     = "1.10";
            public const string InternalServerError    = "1.11";
            public const string DependentServiceFailed = "1.12";
            public const string FileUploadFailure      = "1.13";
            public const string FileTooLarge           = "1.14";
            public const string UnsupportedFileType    = "1.15";
        }

        // 2.YY — User / validation error
        public static class User
        {
            public const string InvalidCredentials       = "2.01";
            public const string AlreadyRegistered        = "2.02";
            public const string ClinicNotFound           = "2.03";
            public const string UserNotFound             = "2.04";
            public const string MissingRequiredField     = "2.05";
            public const string InvalidFieldFormat       = "2.06";
            public const string InvalidCpr               = "2.07";
            public const string InvalidEmailFormat       = "2.08";
            public const string PasswordRequirements     = "2.09";
            public const string InvalidDateOfBirth       = "2.10";
            public const string PatientAlreadyAssigned   = "2.11";
            public const string DoctorClinicMismatch     = "2.12";
        }

        // 3.YY — Connection / infrastructure error
        public static class Connection
        {
            public const string RequestTimeout       = "3.01";
            public const string NodeUnreachable      = "3.02";
            public const string SyncFailure          = "3.03";
            public const string NetworkError         = "3.04";
            public const string DnsResolutionFailure = "3.05";
        }

        // 4.YY — Security / authorisation error
        public static class Security
        {
            public const string Unauthorised          = "4.01";
            public const string Forbidden             = "4.02";
            public const string SessionExpired        = "4.03";
            public const string AccountLocked         = "4.04";
            public const string AccountSuspended      = "4.05";
            public const string TooManyFailedLogins   = "4.06";
            public const string RequiresAdmin         = "4.07";
            public const string CrossClinicViolation  = "4.08";
        }

        // 7.YY — Miscellaneous error
        public static class Misc
        {
            public const string UuidAlreadyExists    = "7.01";
            public const string GenericRegistration  = "7.02";
            public const string UnknownError         = "7.03";
            public const string NotImplemented       = "7.04";
            public const string InvalidUuidFormat    = "7.05";
        }
    }
}