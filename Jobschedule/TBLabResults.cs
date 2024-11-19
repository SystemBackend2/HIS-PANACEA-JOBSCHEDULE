using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jobschedule
{
    internal class TBLabResults
    {

        public decimal LabResultId { get; set; }
        public decimal? FinanceId { get; set; }
        public decimal? ExpenseId { get; set; }
        public decimal? OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? OrderTime { get; set; }
        public string LabOrderCode { get; set; }
        public decimal? TestId { get; set; }
        public string LabResultCode { get; set; }
        public decimal? PatientId { get; set; }
        public int? RunHn { get; set; }
        public int? YearHn { get; set; }
        public string Hn { get; set; }
        public decimal? ServiceId { get; set; }
        public decimal? ClinicId { get; set; }
        public decimal? AdmitId { get; set; }
        public decimal? RunAn { get; set; }
        public decimal? YearAn { get; set; }
        public string An { get; set; }
        public string SampleId { get; set; }
        public decimal? Age { get; set; }
        public string Gender { get; set; }
        public string DataType { get; set; }
        public string ResultValue { get; set; }
        public decimal? NumberValue { get; set; }
        public string Symbol { get; set; }
        public string TextValue { get; set; }
        public decimal? MinNumberRef { get; set; }
        public decimal? MaxNumberRef { get; set; }
        public decimal? LowCriticalRef { get; set; }
        public decimal? HighCriticalRef { get; set; }
        public string MinTextRef { get; set; }
        public string MaxTextRef { get; set; }
        public string Suggestion { get; set; }
        public string Remark { get; set; }
        public string Conceal { get; set; }
        public string Severity { get; set; }
        public string UserReported { get; set; }
        public DateTime? DateReported { get; set; }
        public string UserCreated { get; set; }
        public DateTime? DateCreated { get; set; }
        public string UserModified { get; set; }
        public DateTime? DateModified { get; set; }
        public string UserTested { get; set; }
        public DateTime? DateTested { get; set; }
        public string UserApproved { get; set; }
        public DateTime? DateApproved { get; set; }
        public string UserReceived { get; set; }
        public DateTime? DateReceived { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; }
        public string BagNumber { get; set; }
        public string Component { get; set; }
        public string Issueby { get; set; }
        public string Issuedate { get; set; }
        public string Preliminary { get; set; }
        public string UserAccepted { get; set; }
        public string DateAccepted { get; set; }

    }
}
