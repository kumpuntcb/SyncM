using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncEmpMEA.ModelEmp
{
    public class Meta
    {
        public string Copyright { get; set; }
        public List<string> Authors { get; set; }
    }

    public class AssistData
    {
        public int? SyncEmpId { get; set; }
        public int? AssistId { get; set; }
        public string AssistName { get; set; }
        public string AssistNameEng { get; set; }
        public string AssistShortName { get; set; }
        public string AssistShortNameEng { get; set; }
        public string CLevel { get; set; }
        public string CostCenter { get; set; }
        public int? DepId { get; set; }
        public string DepName { get; set; }
        public string DepNameEng { get; set; }
        public string DepShortName { get; set; }
        public string DepShortNameEng { get; set; }
        public int? DivId { get; set; }
        public string DivName { get; set; }
        public string DivNameEng { get; set; }
        public string DivShortName { get; set; }
        public string DivShortNameEng { get; set; }
        public string Email { get; set; }
        public string EmpId { get; set; }
        public string EmpPicture { get; set; }
        public string FirstName { get; set; }
        public string FirstNameEng { get; set; }
        public bool IsCommander { get; set; }
        public bool IsContractEmployee { get; set; }
        public bool IsNormalPeriod { get; set; }
        public int? JobId { get; set; }
        public string JobName { get; set; }
        public string JobNameEng { get; set; }
        public string JobShortName { get; set; }
        public string JobShortNameEng { get; set; }
        public string LastName { get; set; }
        public string LastNameEng { get; set; }
        public string McJobName { get; set; }
        public string OrgDisplayName { get; set; }
        public int? OrgId { get; set; }
        public string OrgLabel { get; set; }
        public string OrgLevelId { get; set; }
        public string OrgName { get; set; }
        public string OrgNameEng { get; set; }
        public string OrgShortName { get; set; }
        public string OrgShortNameEng { get; set; }
        public int? PartId { get; set; }
        public string PartName { get; set; }
        public string PartNameEng { get; set; }
        public string PartShortName { get; set; }
        public string PartShortNameEng { get; set; }
        public int? PathId { get; set; }
        public string PathName { get; set; }
        public string PathNameEng { get; set; }
        public string PathShortName { get; set; }
        public string PathShortNameEng { get; set; }
        public int? PosId { get; set; }
        public string PosName { get; set; }
        public string PosNameEng { get; set; }
        public string PosShortName { get; set; }
        public string PosShortNameEng { get; set; }
        public string Prefix { get; set; }
        public string PrefixEng { get; set; }
        public int? SecId { get; set; }
        public string SecName { get; set; }
        public string SecNameEng { get; set; }
        public string SecShortName { get; set; }
        public string SecShortNameEng { get; set; }
        public string Tel { get; set; }
        public string TelExtension { get; set; }
        public string TelInternalPrefix { get; set; }
        public string TelInternalSuffix { get; set; }
        public string TelOfficial { get; set; }
        public Guid Uuid { get; set; }
    }

    public class Root
    {
        public Meta Meta { get; set; }
        public List<AssistData> Data { get; set; }
    }

}
