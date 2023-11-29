namespace Pos_System.API.Enums
{
    public enum NotificationType
    {
        SalaryCycleUpdateSuccess = 0,
        SalaryCycleUpdateFail = 1,
        SalaryCyclePaid = 2,
        SalaryCycleStarted = 3,

        ProjectCreate = 10,
        ProjectUpdate = 11,
        ProjectEnded = 12,

        ProjectPMChange = 13,

        VoucherReedemSuccess = 20,
        VoucherRedeemFailed = 21,

        MemberSendPointFailed = 30,
        MemberSendPointSuccess = 31,

        // Project Request
        ProjectMemberRequestAccepted = 40,
        ProjectMemberRequestRejected = 41,

        ProjectMemberRequestPending = 42,

        // Project Report
        ProjectReportAccepted = 50,
        ProjectReportRejected = 51,

        ProjectReportPending = 53,

        Others = 999999,
        Test = 10000
    }
}